using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class DepositBonusService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _connectionString;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;

        public DepositBonusService(IServiceProvider serviceProvider)
        {
            configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            common = serviceProvider.GetRequiredService<Common>();
            walletService = serviceProvider.GetRequiredService<WalletService>();
            withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonus>("Service_DepositBonusQueue_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonusCustList>("Service_DepositBonusQueueCustList_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId ,bool useMainDb = false)
        {
            var ConnectionString = configuration.GetConnectionString("ConnectionStringReport");
            if(useMainDb)
                ConnectionString = configuration.GetConnectionString("ConnectionString");//避免時間差造成資料不同步
            using MySqlConnection conn = new MySqlConnection(ConnectionString);

            return conn.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get",new { CustId  = custId }, commandType: CommandType.StoredProcedure);
        }
        public int SetClaimPromotion(DepositBonus depositBonus, out decimal bonusAmount,  ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

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
            promotion = GetMemberPromotion(parameter);
            if (promotion == null)
            {
                return 0;
            }

            var p = new DynamicParameters(new { depositBonus.CustId, depositBonus.CurrencyId, depositBonus.SiteId, depositBonus.BonusCode, depositBonus.DeviceType });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_PromotionCanJoin_Get_V7", p, conn, trans);

            var jResult = p.Get<int>("Result");
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            var amount = p.Get<decimal?>("Amount") ?? 0;
            var reference = new List<Ref>
            {
                new Ref
                {
                    DepositId = depositBonus.TransId,
                    Amount = depositBonus.Amount
                }
            };

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

                var result = 0;
                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                        {
                            p = new DynamicParameters(new
                            {
                                PromotionType = (int)promotion.PromotionType,
                                depositBonus.BonusCode,
                                depositBonus.CustId,
                                depositBonus.UserName,
                                depositBonus.CurrencyId,
                                depositBonus.SiteId,
                                BonusInfo = sBonusInfo,
                                LockedAmount = dLockedAmount,
                                BonusAmount = dBonus,
                                promotion.ExpiryDays,
                                Memo = (string)null,
                                Prefix = $"B{common.GetEnv()}",
                                RefList = reference.Any() ? JsonConvert.SerializeObject(reference) : null
                            });
                            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                            result = _mainDbClient.Execute("Member_BonusInfo_InsV2", p, conn, trans);
                            var transId = p.Get<string>("TransId");

                            if (result > 0 && dLockedAmount > 0)
                            {
                                var type = 5 /*Bonus*/;
                                result = walletService.SetBalance(depositBonus.CustId, depositBonus.SiteId, depositBonus.UserName, dLockedAmount * -1, type,
                                    $"Claim Promotion BonusCode: {depositBonus.BonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
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

                                result = RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, depositBonus.CustId,
                                    depositBonus.SiteId, depositBonus.UserName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                                    $"Claim Promotion BonusCode: {depositBonus.BonusCode}", transId, conn, trans)
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
                    result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set",
                        new DynamicParameters(new {depositBonus.SiteId, depositBonus.BonusCode, depositBonus.CurrencyId}), conn,
                        trans);
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

        public Promotion GetMemberPromotion(DynamicParameters parameters)
        {
            return _reportDbClient.QueryFirst<Promotion>("Member_PromotionV3_Get", parameters);
        }

        public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, PromoCategoryId = promoCategoryId, TpId = tpId, CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type,
                Memo,
                RefTransId });
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
