using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace BonusService.Service.DAL
{
    public class BonusAPIService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly DepositBonusService _depositBonusService;
        private readonly string _connectionString;
        private readonly Common _common;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly ILogger<BonusAPIService> _logger;
        private readonly BonusWalletService _bonusWalletService;
        private readonly SelLangBLL _selLangBll;
        private readonly CustInfoService _custInfoService;

        public BonusAPIService(IServiceProvider serviceProvider, ILogger<BonusAPIService> logger)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = _configuration.GetConnectionString("ConnectionString");
            _common = serviceProvider.GetRequiredService<Common>();
            _walletService = serviceProvider.GetRequiredService<WalletService>();
            _withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _depositBonusService = serviceProvider.GetRequiredService<DepositBonusService>();
            _bonusWalletService = serviceProvider.GetRequiredService<BonusWalletService>();
            _selLangBll = serviceProvider.GetRequiredService<SelLangBLL>();
            _custInfoService = serviceProvider.GetRequiredService<CustInfoService>();
            this._logger = logger;


        }        
        public int PaymentAdjustment(DepositBonus depositBonus, decimal bonusAmount, string sBonusInfo, string Operator, string memo, MySqlConnection conn, MySqlTransaction trans, out string transId)
        {

            var Adjustment_p = new DynamicParameters(
                new {
                    AdjustType = 5,//Retention Bonus
                    depositBonus.CustId,
                    depositBonus.UserName,
                    depositBonus.CurrencyId,
                    depositBonus.SiteId,
                    AdjustIdInfo = sBonusInfo,
                    Amount = bonusAmount,
                    Prefix = $"A{_common.GetEnv()}",
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
            var adjustId = 0;
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
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("AdjustmentLog_Ins", new { AdjustId = adjustId, SiteId = siteId, Comment = comment, Action = status,
                Operator }, commandType: CommandType.StoredProcedure);
        }
        public VerificationRewardData GetVerificationReward(int siteId, int currencyId)
        {
            var p = new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId });
            return _reportDbClient.QueryFirst<VerificationRewardData>("Service_VerificationReward_Get", p);
        }
        
        public VerificationRewardLimit GetVerificationRewardLimit(int siteId, int currencyId, OTPTypeEnum type)
        {
            var p = new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId , _Type = type});
            return _reportDbClient.QueryFirst<VerificationRewardLimit>("Service_VerificationRewardLimit_Get", p);
        }
        
        public CustVerificationRewardData GetCustVerificationReward(int custId,bool usedMainDb = false)
        {
            var p = new DynamicParameters(new { _CustId = custId });
            if(usedMainDb)
                return _mainDbClient.QueryFirst<CustVerificationRewardData>("Service_CustVerificationReward_Get_V2", p);
            else
                return _reportDbClient.QueryFirst<CustVerificationRewardData>("Service_CustVerificationReward_Get_V2", p);

        }
        public int SetCustVerifiedReward(int custId, int siteId, int currencyId, int verifyItem, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { _CustId = custId, _SiteId = siteId, _CurrencyId = currencyId, _VerificationItem = verifyItem });
            return _mainDbClient.Execute("Service_CustVerificationReward_Set", p, conn, trans);
        }

        public ApiResult PayoutVerificationReward(int custId, int siteId, int currencyId, string userName, OTPVerificationType OTPVerificationtype, decimal reward)
        {
            var apiResult = new ApiResult();
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var Type = 20;
                var transId = "";
                var result = 0;
                var comment = "";
                if (OTPVerificationtype == OTPVerificationType.Email)
                {
                    comment = $"email verification reward: {reward:#,##0.##}";
                }
                else if (OTPVerificationtype == OTPVerificationType.SMS)
                {
                    comment = $"phone verification reward: {reward:#,##0.##}";
                }
                else
                {
                    comment = $"verification reward: {reward:#,##0.##}";
                }


                result = _walletService.SetBalance(custId, siteId, userName, reward, Type,
                    comment, transId, out _, out _, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetBalance fail";
                    return apiResult;
                }
                result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, reward, null, transId, out _, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetWithdrawalLimit fail";
                    return apiResult;
                }

                result = SetCustVerifiedReward(custId, siteId, currencyId, (int)OTPVerificationtype, conn, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetCustVerifiedReward fail";
                    return apiResult;
                }

                trans.Commit();

                if(result>0)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = comment;
                }
                else
                {
                    apiResult.Message = "UnKnow Fail";
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 999;
                apiResult.RawMessage = ex.ToString();
            }
            return apiResult;
        }
    }
}
