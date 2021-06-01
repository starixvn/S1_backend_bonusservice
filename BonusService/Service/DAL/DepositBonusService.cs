using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
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
    public class DepositBonusService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;

        public DepositBonusService(IConfiguration configuration, Common common, WalletService walletService, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.common = common;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonus>("Service_DepositBonusQueue_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetClaimPromotion(int custId, string userName, int siteId, string bonusCode, int currencyId, int? deviceType, out decimal bonusAmount,  ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            promotion = conn.QueryFirstOrDefault<Promotion>("Service_PromotionV3_Get", new { CustId = custId, SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId, DeviceType = deviceType }, transaction: trans, commandType: CommandType.StoredProcedure);
            if (promotion == null)
            {
                return 0;
            }

            int result = 0;
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode, DeviceType = deviceType });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Service_PromotionCanJoinV6_Get", p, commandType: CommandType.StoredProcedure);

            var jResult = p.Get<int>("Result");
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            var amount = p.Get<decimal?>("Amount") ?? 0;
            if (canJoin == 1)
            {
                decimal dBonus = 0;
                if (promotion.PromotionType == PromotionType.ReDeposit ||
                    (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit))
                {
                    amount = promotion.BonusType == BonusType.Amount ? promotion.RequiredAmount : lockedAmount;

                    if (amount <= 0)
                    {
                        return 0;
                    }
                }

                var dLockedAmount = amount;
                decimal dRollOver = 0;
                var sBonusInfo = "";

                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                        {
                            if (amount < promotion.RequiredAmount)
                            {
                                return -4;
                            }

                            if (promotion.BonusType == BonusType.Amount)
                            {
                                if (promotion.RequiredAmount == 0)
                                {
                                    throw new Exception("Required Amount can't be 0");
                                }

                                dLockedAmount = promotion.RequiredAmount;
                                dBonus = promotion.Bonus;
                            }
                            else
                            {
                                dBonus = Math.Ceiling(promotion.Bonus * amount / 100);
                                if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                                {
                                    dBonus = promotion.MaxBonus;
                                    dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                                }
                            }

                            dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                            sBonusInfo = JsonConvert.SerializeObject(promotion,
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                            bonusAmount = dBonus;
                            lockedAmount = dLockedAmount;
                            rollOver = dRollOver;
                            expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                            break;
                        }

                    default:
                        return 0;
                }

                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                        {
                            p = new DynamicParameters(new
                            {
                                PromotionType = (int)promotion.PromotionType,
                                BonusCode = bonusCode,
                                CustId = custId,
                                UserName = userName,
                                CurrencyId = currencyId,
                                SiteId = siteId,
                                BonusInfo = sBonusInfo,
                                LockedAmount = dLockedAmount,
                                BonusAmount = dBonus,
                                promotion.ExpiryDays,
                                Memo = (string)null,
                                Prefix = $"B{common.GetEnv()}"
                            });
                            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                            result = conn.Execute("Service_BonusInfo_Ins", p, trans, commandType: CommandType.StoredProcedure);
                            var transId = p.Get<string>("TransId");

                            if (result > 0 && dLockedAmount > 0)
                            {
                                var type = 5 /*Bonus*/;
                                result = walletService.SetBalance(custId, siteId, userName, dLockedAmount * -1, type,
                                    $"Claim Promotion BonusCode: {bonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
                                    out _, out _, conn, trans);

                                if (result > 0)
                                {
                                    withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, dLockedAmount * -1,
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

                                result = RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, custId,
                                    siteId, userName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                                    $"Claim Promotion BonusCode: {bonusCode}", transId, conn, trans)
                                    ? 1
                                    : 0;
                            }
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (result > 0)
                {
                    result = conn.Execute("Service_PromotionBeenUsedV2_Set",
                        new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, trans,
                        commandType: CommandType.StoredProcedure);
                }

                if (result > 0)
                {
                    trans.Commit();
                }

                return result;
            }

            if (jResult == 3)
            {
                return -5;
            }

            return 0;

        }
        public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, PromoCategoryId = promoCategoryId, TpId = tpId, CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type, Memo = Memo, RefTransId = RefTransId });
            return conn.Execute("Common_Bonus_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure) > 0;
        }
        public void CheckDepositBonusExpired()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("Service_DepositBonusRemoveExpired_Set", commandType: CommandType.StoredProcedure);
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_DepositBonusStatus_Set", new { TransId = transId, Status = status }, commandType: CommandType.StoredProcedure);
        }
    }
}
