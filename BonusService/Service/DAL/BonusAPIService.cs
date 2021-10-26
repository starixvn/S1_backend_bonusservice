using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public BonusAPIService(IServiceProvider serviceProvider)
        {
            configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            common = serviceProvider.GetRequiredService<Common>();
            walletService = serviceProvider.GetRequiredService<WalletService>();
            withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            depositBonusService = serviceProvider.GetRequiredService<DepositBonusService>();
            
        }

        public int SetAdjustmentBonus(DepositBonus depositBonus, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

       
            promotion = _reportDbClient.QueryFirst<Promotion>("Member_PromotionV3_Get", new DynamicParameters(new
            {
                depositBonus.CustId,
                depositBonus.CurrencyId,
                depositBonus.SiteId,
                depositBonus.BonusCode,
                depositBonus.DeviceType
            }));
            if (promotion == null|| promotion.PromotionType != PromotionType.RetentionBonus)
            {
                return 0;
            }


            decimal dBonus = 0;
            var dLockedAmount = 0;
            decimal dRollOver = 0;
            var sBonusInfo = "";
            IEnumerable<Ref> reference = new List<Ref>();

            dBonus = promotion.Bonus;
            dRollOver = dBonus * promotion.RollOver;
            sBonusInfo = JsonConvert.SerializeObject(promotion,
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
                    $"Claim Promotion BonusCode: {depositBonus.BonusCode}", transId, conn, trans)
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
                trans.Commit();
            }

            return result;

        }
    }
}
