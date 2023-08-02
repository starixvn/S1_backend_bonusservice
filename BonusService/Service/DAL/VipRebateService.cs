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
        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int custId, DateTime specificDate, string timeZone)
        {
            var p = new
            {
                _CustId = custId,
                _SpecificDate = specificDate,
                _CurrencyTimeZone = timeZone
            };
            return _reportDbClient.Query<VipRebateTpBetRecord>("Service_VipRebateTpBetRecord_Get_V6", new DynamicParameters(p), 300);
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
            return _reportDbClient.Query<VipRebateInfo>("Service_VipRebateCustList_Get_V5", new DynamicParameters(p), 300);
        }
        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            return _reportDbClient.Query<RebateTask>("Service_VipRebateTask_Get_V2");
        }

        public int PayoutVipRebate(int siteId, VipRebateInfo rebateInfo, IEnumerable<VipRebateTpBetRecord> rebateTpBetRecordList, IEnumerable<RebatePayoutLimit> rebatePayoutLimits, out decimal payAmount, out Dictionary<int, decimal> rebateResultList, long RelId)
        {
            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            payAmount = 0m; // 發出的總金額
            var promotionType = PromotionType.Rebate;
            var rebateGametypeLogList = new Dictionary<int, decimal>();   // 使用GameType分類加總金額
            var rebateCategoryLogList = new Dictionary<int, decimal>();   // 使用Category分類加總金額
            rebateResultList = new Dictionary<int, decimal>();            // 檢查上下限額後最後實際發出的金額

            foreach (var rebateTpBetRecord in rebateTpBetRecordList)
            {
                //寫入原始計算資料到RebateRecord
                SetRebateRecord(
                    rebateInfo.CustId,
                    int.Parse(rebateTpBetRecord.SettledDate.ToString("yyyyMMdd")),
                    rebateTpBetRecord.GameType,
                    rebateTpBetRecord.ValidBet,
                    rebateTpBetRecord.Percentage,
                    rebateTpBetRecord.RedeemAmount,
                    rebateTpBetRecord.ValidTurnover,
                    (int)promotionType, RelId, trans);

                //四捨五入
                rebateTpBetRecord.RedeemAmount = _common.Floor(rebateTpBetRecord.RedeemAmount, 2);

                //匯總Gametype金額
                if (rebateGametypeLogList.ContainsKey(rebateTpBetRecord.GameType))
                {
                    rebateGametypeLogList[rebateTpBetRecord.GameType] += rebateTpBetRecord.RedeemAmount;
                }
                else
                {
                    rebateGametypeLogList.Add(rebateTpBetRecord.GameType, rebateTpBetRecord.RedeemAmount);
                }

                //匯總Category金額
                if (rebateCategoryLogList.ContainsKey(rebateTpBetRecord.CategoryId))
                {
                    rebateCategoryLogList[rebateTpBetRecord.CategoryId] += rebateTpBetRecord.RedeemAmount;
                }
                else
                {
                    rebateCategoryLogList.Add(rebateTpBetRecord.CategoryId, rebateTpBetRecord.RedeemAmount);
                }
            }

            foreach (var rebateCategory in rebateCategoryLogList)
            {
                //檢查上下限額
                var redeemAmount = rebateCategory.Value;
                var rebatePayoutLimit = rebatePayoutLimits.FirstOrDefault(x => x.CurrencyId == rebateInfo.CurrencyId && x.SiteId == siteId && x.CategoryId == rebateCategory.Key);
                if (rebatePayoutLimit != null)
                {
                    if (redeemAmount < rebatePayoutLimit.MinPayout)
                    {
                        redeemAmount = 0;
                    }

                    if (rebatePayoutLimit.MaxPayout != 0 && rebatePayoutLimit.MaxPayout < redeemAmount)
                    {
                        redeemAmount = rebatePayoutLimit.MaxPayout;
                    }

                }

                _mainDbClient.Execute("Common_RebateCLog_Set", trans, new
                {
                    rebateInfo.CustId,
                    rebateInfo.CurrencyId,
                    CategoryId = rebateCategory.Key,
                    RebateType = RebateType.VipRebate,
                    rebateInfo.UserName,
                    SiteId = siteId,
                    CalculatedAmount = rebateCategory.Value,
                    RedeemAmount = redeemAmount,
                    MinPayout = rebatePayoutLimit?.MinPayout ?? 0m,
                    MaxPayout = rebatePayoutLimit?.MaxPayout ?? 0m,
                    Memo = "",
                    RelId = RelId
                });

                if (redeemAmount > 0)
                {
                    rebateResultList.Add(rebateCategory.Key, redeemAmount);
                }

                payAmount += redeemAmount;
            }

            if (payAmount > 0)
            {
                result = _walletService.SetBalance(rebateInfo.CustId, siteId, rebateInfo.UserName, payAmount, 17 - (int)promotionType, $"Rebate: {payAmount:#,##0.##}", "", out _, out _, trans);

                if (result > 0)
                {
                    //寫入發放總金額紀錄
                    _mainDbClient.Execute("Common_RebateMLog_Ins", trans, new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = payAmount, Memo = "", BonusCode = "VipRebate", RelId = RelId });

                    if (result > 0)
                    {
                        //寫入Rebate GameType Log紀錄
                        foreach (var rebateLog in rebateGametypeLogList)
                        {
                            _mainDbClient.Execute("Common_RebateLog_Ins", trans, new { CustId = rebateInfo.CustId, CurrencyId = rebateInfo.CurrencyId, GameType = rebateLog.Key, RebateType = RebateType.VipRebate, UserName = rebateInfo.UserName, SiteId = siteId, RedeemAmount = rebateLog.Value, Memo = "", BonusCode = "VipRebate", RelId = RelId });
                        }
                    }
                }
            }
            _mainDbClient.CommitAndDisposeConnection(conn, trans);
            return result;
        }

        private int SetRebateRecord(int CustId, int SettledDate, int GameType, decimal ValidBet, decimal Percentage, decimal Sum, decimal ValidTurnover, int PromotionType, long RelId, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_RebateRecord_Set_V4", trans, new { CustId, SettledDate, GameType, ValidBet, ValidTurnover, Percentage, Sum, PromotionType, BonusCode = "VipRebate", RelId });
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
