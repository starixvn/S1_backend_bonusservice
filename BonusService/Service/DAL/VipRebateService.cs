using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace BonusService.Service.DAL
{
    public class VipRebateService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly Common common;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        public VipRebateService(IConfiguration configuration,
                             WalletService walletService,
            Common common,
            WithdrawalLimitService withdrawalLimitService,
            IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            this.common = common;
        }
        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int periodType, int custId, DateTime specificDate, string timeZone)
        {
            var p = new
            {
                _PeriodType = periodType,
                _CustId = custId,
                _SpecificDate = specificDate,
                _CurrencyTimeZone = timeZone
            };

            return _reportDbClient.Query<VipRebateTpBetRecord>("Service_VipRebateTpBetRecord_Get_V5", new DynamicParameters(p));
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int currencyId, int periodType, DateTime specificDate, string timeZone, int pageNumber, int pageSize)
        {
            var p = new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _PeriodType = periodType,
                _SpecificDate = specificDate,
                _CurrencyTimeZone = timeZone,
                _PageNumber = pageNumber,
                _PageSize = pageSize
            };
            return _reportDbClient.Query<VipRebateInfo>("Service_VipRebateCustList_Get_V4", new DynamicParameters(p));
        }
        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            return _reportDbClient.Query<RebateTask>("Service_VipRebateTask_Get_V2");
        }
        public int PayoutVipRebate(int siteId, VipRebateInfo rebateInfo, IEnumerable<VipRebateTpBetRecord> rebateTpBetRecords, out decimal payAmount, out Dictionary<int, decimal> rebateLogList, out Dictionary<int, decimal> rebateCLogList, IEnumerable<RebatePayoutLimit> rebatePayoutLimits)
        {
            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            payAmount = 0m;
            var promotionType = PromotionType.Rebate;
            rebateLogList = new Dictionary<int, decimal>();
            rebateCLogList = new Dictionary<int, decimal>();

            var tc = DateTime.Now.Ticks;

            foreach (var rr in rebateTpBetRecords)
            {
                var sum = rr.RedeemAmount;
                if (sum != 0)
                {
                    //var rebateRecord = GetRebateRecord(rebateInfo.CustId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, (int)promotionType, conn, trans);
                    //if (rebateRecord == null)
                    //{
                    SetRebateRecord(rebateInfo.CustId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, rr.ValidBet, rr.Percentage, sum, (int)promotionType, tc, conn, trans);
                    var amount = sum;
                    amount = common.Floor(amount, 2);

                    if (rebateLogList.ContainsKey(rr.GameType))
                    {
                        rebateLogList[rr.GameType] += amount;
                    }
                    else
                    {
                        rebateLogList.Add(rr.GameType, amount);
                    }

                    if (rebateCLogList.ContainsKey(rr.CategoryId))
                    {
                        rebateCLogList[rr.CategoryId] += amount;
                    }
                    else
                    {
                        rebateCLogList.Add(rr.CategoryId, amount);
                    }
                    //}
                }
            }
            var newRebateCLogList = new Dictionary<int, decimal>(rebateCLogList);
            foreach (var rebateKey in newRebateCLogList.Keys)
            {
                var redeemAmount = rebateCLogList[rebateKey];
                var rebatePayoutLimit = rebatePayoutLimits.FirstOrDefault(x => x.CurrencyId == rebateInfo.CurrencyId && x.SiteId == siteId && x.CategoryId == rebateKey);
                if (rebatePayoutLimit != null)
                {
                    if (rebateCLogList[rebateKey] < rebatePayoutLimit.MinPayout)
                        redeemAmount = 0;

                    if (rebatePayoutLimit.MaxPayout != 0 && rebatePayoutLimit.MaxPayout < rebateCLogList[rebateKey])
                        redeemAmount = rebatePayoutLimit.MaxPayout;
                }

                //不論有沒有到達可發放值，只要有該類型則紀錄
                conn.Execute("Common_RebateCLog_Set", new
                {
                    CustId = rebateInfo.CustId,
                    CurrencyId = rebateInfo.CurrencyId,
                    CategoryId = rebateKey,
                    RebateType = RebateType.VipRebate,
                    UserName = rebateInfo.UserName,
                    SiteId = siteId,
                    CalculatedAmount = rebateCLogList[rebateKey],
                    RedeemAmount = redeemAmount,
                    MinPayout = rebatePayoutLimit.MinPayout,
                    MaxPayout = rebatePayoutLimit.MaxPayout,
                    CanReward = redeemAmount > 0,
                    Memo = "",
                    RelId = tc
                }, transaction: trans, commandType: CommandType.StoredProcedure);

                payAmount += redeemAmount;
            }

            if (payAmount > 0)
            {
                result = walletService.SetBalance(rebateInfo.CustId, siteId, rebateInfo.UserName, payAmount, 17 - (int)promotionType, $"Rebate: {payAmount:#,##0.##}", "", out _, out _, trans);

                if (result > 0)
                {
                    conn.Execute("Common_RebateMLog_Ins", new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = payAmount, Memo = "", BonusCode = "VipRebate", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        foreach (var rebateLog in rebateLogList)
                        {
                            conn.Execute("Common_RebateLog_Ins", new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, GameType = rebateLog.Key, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = rebateLog.Value, Memo = "", BonusCode = "VipRebate", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);
                        }
                    }
                }
            }
            trans.Commit();
            return result;
        }
        public RebateRecord GetRebateRecord(int CustId, int SettledDate, int GameType, int PromotionType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Query<RebateRecord>("Service_RebateRecord_Get", new { CustId, SettledDate, GameType, BonusCode = "VipRebate", PromotionType }, transaction: trans, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }
        public int SetRebateRecord(int CustId, int SettledDate, int GameType, decimal ValidBet, decimal Percentage, decimal Sum, int PromotionType, long RelId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_RebateRecord_Set_V3", new { CustId, SettledDate, GameType, ValidBet, Percentage, Sum, PromotionType, BonusCode = "VipRebate", RelId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<RebatePayoutLimit> GetRebatePayoutLimit()
        {
            return _reportDbClient.Query<RebatePayoutLimit>("Service_RebatePayoutLimit_Get");
        }
        public int SetVipRebateTaskDone(int siteId, int currencyId)
        {
            return _mainDbClient.Execute("Service_VipRebateTaskDone_Set_V2", new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId }));
        }
    }
}
