using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace BonusService.Service.DAL
{
    public class BonusAPIService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly DepositBonusService depositBonusService;
        private readonly string _connectionString;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly ILogger<BonusAPIService> _logger;

        public BonusAPIService(IServiceProvider serviceProvider, ILogger<BonusAPIService> logger)
        {
            configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            common = serviceProvider.GetRequiredService<Common>();
            walletService = serviceProvider.GetRequiredService<WalletService>();
            withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            depositBonusService = serviceProvider.GetRequiredService<DepositBonusService>();
            this._logger = logger;


        }

        public int SetAdjustmentBonus(DepositBonus depositBonus, string Operator, string memo, out string Adjustment_transId, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion, out int errorCode, out string errorMessage)
        {
            Adjustment_transId = null;
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;
            errorCode = 9000;
            errorMessage = "";
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var parameter = new DynamicParameters(new
            {
                depositBonus.CustId,
                depositBonus.CurrencyId,
                depositBonus.SiteId,
                depositBonus.BonusCode,
                depositBonus.DeviceType
            });

            promotion = _reportDbClient.QueryFirst<Promotion>("Member_PromotionV3_Get", parameter);

            if (promotion == null)
            {
                errorCode = 201;
                errorMessage = $"BonusCode:{depositBonus.BonusCode} not found";
                _logger.LogError($"BonusCode:{depositBonus.BonusCode} not found  parameter:{ Newtonsoft.Json.JsonConvert.SerializeObject(parameter)}");
                return 0;
            }else if(promotion.PromotionType != PromotionType.RetentionBonus)
            {
                errorCode = 202;
                errorMessage = $"BonusCode:{depositBonus.BonusCode} is not Retention Bonus";
                _logger.LogError($"BonusCode:{depositBonus.BonusCode} is not Retention Bonus  parameter:{ Newtonsoft.Json.JsonConvert.SerializeObject(parameter)}");
                return 0;
            }



            var dLockedAmount = 0;
            IEnumerable<Ref> reference = new List<Ref>();

            decimal dBonus = promotion.Bonus;
            decimal dRollOver = dBonus * promotion.RollOver;
            var sBonusInfo = JsonConvert.SerializeObject(promotion,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            bonusAmount = dBonus;
            lockedAmount = 0;
            rollOver = dRollOver;
            expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();


            var result = 0;

            var p = new DynamicParameters(new
            {
                PromotionType = (int)promotion.PromotionType,
                BonusCode = depositBonus.BonusCode,
                CustId = depositBonus.CustId,
                UserName = depositBonus.UserName,
                CurrencyId = depositBonus.CurrencyId,
                SiteId = depositBonus.SiteId,
                BonusInfo = sBonusInfo,
                LockedAmount = dLockedAmount,
                BonusAmount = dBonus,
                promotion.ExpiryDays,
                Memo = (string)null,
                Prefix = $"B{common.GetEnv()}",
                RefList = reference.Any() ? JsonConvert.SerializeObject(reference.FirstOrDefault()) : null
            });
            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Member_BonusInfo_InsV2", p, conn, trans);
            var transId = p.Get<string>("TransId");

            if (result > 0 && dLockedAmount > 0)
            {
                var type = 5 /*Bonus*/;
                result = walletService.SetBalance(depositBonus.CustId, depositBonus.SiteId, depositBonus.UserName, dLockedAmount * -1, type,
                    $"Claim Retention Bonus BonusCode: {depositBonus.BonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
                    out _, out _, conn, trans);

                if (result > 0)
                {
                    withdrawalLimitService.SetWithdrawalLimit(depositBonus.CustId, depositBonus.UserName, depositBonus.SiteId, dLockedAmount * -1,
                        null, transId, out _, conn, trans);
                }
                else
                {
                    return -2;
                }
            }

            if (result > 0)
            {
                var m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out var bonusId);

                result = depositBonusService.RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, depositBonus.CustId,
                    depositBonus.SiteId, depositBonus.UserName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                    $"Payment Claim Retention BonusCode: {depositBonus.BonusCode} ", transId, conn, trans)
                    ? 1
                    : 0;
            }

            if (result > 0)
            {
                p = new DynamicParameters(new
                {
                    SiteId = depositBonus.SiteId,
                    BonusCode = depositBonus.BonusCode,
                    CurrencyId = depositBonus.CurrencyId
                });

                result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set", p, conn, trans);
            }
            if (result > 0)
            { 
                result = PaymentAdjustment(depositBonus, bonusAmount, sBonusInfo, Operator, memo, conn, trans, out Adjustment_transId);
            }
            if (result > 0)
            {
                trans.Commit();
                errorCode = 0;
                errorMessage = "";
            }

            return result;

        }
        
        public int PaymentAdjustment(DepositBonus depositBonus, decimal bonusAmount, string sBonusInfo, string Operator, string memo, MySqlConnection conn, MySqlTransaction trans, out string transId)
        {

            var Adjustment_p = new DynamicParameters(
                new {
                    AdjustType = 5,//Retention Bonus
                    CustId = depositBonus.CustId,
                    UserName = depositBonus.UserName,
                    CurrencyId = depositBonus.CurrencyId,
                    SiteId = depositBonus.SiteId,
                    AdjustIdInfo = sBonusInfo,
                    Amount = bonusAmount,
                    Prefix = $"A{common.GetEnv()}",
                    Memo = memo
                }); 
            Adjustment_p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = conn.Execute("Admin_Adjustment_Ins", Adjustment_p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = Adjustment_p.Get<string>("TransId");
            if (result > 0)
            {
                var forStatus = new List<int>();
                forStatus.Add((int)AdjustStatus.Pending);
                if (SetAdjustmentInfo(transId, depositBonus.SiteId, $"Payment Claim Retention BonusCode: {depositBonus.BonusCode}  BonusAmount:{bonusAmount}", Operator, forStatus, AdjustStatus.Approved, conn, trans) > 0)
                {
                    transId = Adjustment_p.Get<string>("TransId");
                }
                else
                {
                    result = 0;
                }
            }

            return result;
        }

        public int SetAdjustmentInfo(string transId, int siteId, string comment, string Operator, IEnumerable<int> forStatus, AdjustStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            int adjustId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out adjustId);

            var result = 0;
            try
            {
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(
                    new { 
                        AdjustId = adjustId, 
                        SiteId = siteId, 
                        RefId = (string)null, 
                        Comment = comment, 
                        ForStatus = sForStatus, 
                        Status = (int)status, 
                        ThirdId = (string)null 
                    });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                result = conn.Execute("Adjustment_Set", p, trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    SetAdjustmentLog(adjustId, siteId, comment, (int)status, Operator);
                    if (result > 0)
                    {
                        result = p.Get<int?>("CustId") ?? result;
                    }
                }
            }
            catch (Exception ex)
            {
                SetAdjustmentLog(adjustId, siteId, ex.Message, (int)AdjustStatus.Error, Operator);
            }

            return result;
        }
        private int SetAdjustmentLog(int adjustId, int siteId, string comment, int status, string Operator)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("AdjustmentLog_Ins", new { AdjustId = adjustId, SiteId = siteId, Comment = comment, Action = status, Operator = Operator }, commandType: CommandType.StoredProcedure);
        }
    }
}
