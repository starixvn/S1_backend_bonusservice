using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class VipRebateService
    {
        private readonly WalletService _walletService;
        private readonly Common _common;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        public VipRebateService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _walletService = provider.GetRequiredService<WalletService>();
            _common = provider.GetRequiredService<Common>();
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

            return _reportDbClient.Query<VipRebateTpBetRecord>("Service_VipRebateTpBetRecord_Get_V5", new DynamicParameters(p),300);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int currencyId, int periodType, DateTime specificDate, string timeZone)
        {
            var p = new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _PeriodType = periodType,
                _SpecificDate = specificDate,
                _CurrencyTimeZone = timeZone
            };
            return _reportDbClient.Query<VipRebateInfo>("Service_VipRebateCustList_Get_V5", new DynamicParameters(p),300);
        }
        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            return _reportDbClient.Query<RebateTask>("Service_VipRebateTask_Get_V2");
        }
        public int PayoutVipRebate(int siteId, VipRebateInfo rebateInfo, IEnumerable<VipRebateTpBetRecord> rebateTpBetRecords, IEnumerable<RebatePayoutLimit> rebatePayoutLimits, out decimal payAmount, out Dictionary<int, decimal> rewardRebateCLogList)
        {
            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            payAmount = 0m;
            var promotionType = PromotionType.Rebate;
            var rebateLogList = new Dictionary<int, decimal>();
            var rebateCLogList = new Dictionary<int, decimal>();
            rewardRebateCLogList = new Dictionary<int, decimal>();

            var tc = DateTime.Now.Ticks;

            foreach (var rr in rebateTpBetRecords)
            {
                //if (rr.RedeemAmount != 0)
                //{
                //var rebateRecord = GetRebateRecord(rebateInfo.CustId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, (int)promotionType, conn, trans);
                //if (rebateRecord == null)
                //{
                SetRebateRecord(rebateInfo.CustId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, rr.ValidBet, rr.Percentage, rr.RedeemAmount, (int)promotionType, tc, trans);
                rr.RedeemAmount = _common.Floor(rr.RedeemAmount, 2);

                if (rebateLogList.ContainsKey(rr.GameType))
                    rebateLogList[rr.GameType] += rr.RedeemAmount;
                else
                    rebateLogList.Add(rr.GameType, rr.RedeemAmount);

                if (rebateCLogList.ContainsKey(rr.CategoryId))
                    rebateCLogList[rr.CategoryId] += rr.RedeemAmount;
                else
                    rebateCLogList.Add(rr.CategoryId, rr.RedeemAmount);
                //}
                //}
            }
            // var newRebateCLogList = new Dictionary<int, decimal>(rebateCLogList);
            foreach (var rebateKey in rebateCLogList.Keys)
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

                _mainDbClient.Execute("Common_RebateCLog_Set", trans ,new
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
                    Memo = "",
                    RelId = tc
                });

                if (redeemAmount > 0)
                    rewardRebateCLogList.Add(rebateKey, redeemAmount);

                payAmount += redeemAmount;
            }

            if (payAmount > 0)
            {
                result = _walletService.SetBalance(rebateInfo.CustId, siteId, rebateInfo.UserName, payAmount, 17 - (int)promotionType, $"Rebate: {payAmount:#,##0.##}", "", out _, out _, trans);

                if (result > 0)
                {
                    _mainDbClient.Execute("Common_RebateMLog_Ins", trans , new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = payAmount, Memo = "", BonusCode = "VipRebate", RelId = tc });

                    if (result > 0)
                    {
                        foreach (var rebateLog in rebateLogList)
                        {
                            _mainDbClient.Execute("Common_RebateLog_Ins", trans ,new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, GameType = rebateLog.Key, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = rebateLog.Value, Memo = "", BonusCode = "VipRebate", RelId = tc });
                        }
                    }
                }
            }
            _mainDbClient.CommitAndDisposeConnection(conn, trans);
            return result;
        }
        
        // public RebateRecord GetRebateRecord(int CustId, int SettledDate, int GameType, int PromotionType, MySqlConnection conn, MySqlTransaction trans)
        // {
        //     return conn.Query<RebateRecord>("Service_RebateRecord_Get", new { CustId, SettledDate, GameType, BonusCode = "VipRebate", PromotionType }, transaction: trans, commandType: CommandType.StoredProcedure).FirstOrDefault();
        // }

        private int SetRebateRecord(int CustId, int SettledDate, int GameType, decimal ValidBet, decimal Percentage, decimal Sum, int PromotionType, long RelId, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_RebateRecord_Set_V3", trans , new { CustId, SettledDate, GameType, ValidBet, Percentage, Sum, PromotionType, BonusCode = "VipRebate", RelId });
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
