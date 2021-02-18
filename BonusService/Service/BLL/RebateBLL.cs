using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BonusService.Service.BLL
{
    public class RebateBLL
    {
        private readonly RebateService rebateService;
        private readonly VipRebateService vipRebateService;
        private readonly IMemoryCache cache;
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;
        private readonly CustInfoService custInfoService;
        public RebateBLL(IMemoryCache cache, VipRebateService vipRebateService, RebateService rebateService, SelLangBLL selLangBLL, CustInfoService custInfoService, MessageService messageService)
        {
            this.rebateService = rebateService;
            this.vipRebateService = vipRebateService;
            this.cache = cache;
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;
            this.custInfoService = custInfoService;
        }
        public IEnumerable<RebateTask> GetRebateTask()
        {
            return rebateService.GetRebateTask();
        }
        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            return vipRebateService.GetVipRebateTask();
        }
        public int SetRebateTaskDone(string bonusCode, int siteId)
        {
            return rebateService.SetRebateTaskDone(bonusCode, siteId);
        }
        public IEnumerable<RebateInfo> GetRebateInfo(string bonusCode, int siteId, int pageSize, int pageNumber)
        {
            return rebateService.GetRebateInfo(bonusCode, siteId, pageSize, pageNumber);
        }
        public IEnumerable<RebateTpBetRecord> GetRebateTpBetRecord(string bonusCode, int siteId, int custId, int currencyId, int is14)
        {
            return rebateService.GetRebateTpBetRecord(bonusCode, siteId, custId, currencyId, is14);
        }
        public int PayoutRebate(int siteId, int custId, string userName, string bonusCode, RebateInfo rebateInfo, IEnumerable<RebateTpBetRecord> rebateTpBetRecords, IEnumerable<RebateTask> rebateTasks)
        {
            var result = rebateService.PayoutRebate(siteId, custId, userName, bonusCode, rebateInfo, rebateTpBetRecords, rebateTasks, out decimal payAmount, out Dictionary<int, decimal> rebateLogList);

            if (result > 0)
            {
                var promotionType = rebateTasks.Where(x => x.BonusCode == bonusCode).FirstOrDefault()?.PromotionType ?? 6;
                var custInfo = custInfoService.GetCustInfo(custId);
                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = string.Format($"{custInfo.UserName} {selLangBLL.GetResourceValue(promotionType == 6 ? "lbl_RebateMsg" : "lbl_CashbackMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(title);

                if (promotionType == 6)
                {
                    foreach (var rebateLog in rebateLogList)
                    {
                        var dAmount = rebateLog.Value.ToString("#,##0.##");
                        if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dAmount = $"{rebateLog.Value / 1000:#,##0.##}";
                        }

                        msg.AppendLine($"{GetTpGameList()[rebateLog.Key]}: {dAmount}");
                    }
                }

                messageService.SendMessage(custInfo.SiteId, "Bonus Service", title, msg.ToString(), custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }

            return result;
        }
        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int siteId, int custId, int is14)
        {
            return vipRebateService.GetVipRebateTpBetRecord(siteId, custId, is14);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int pageSize, int pageNumber)
        {
            return vipRebateService.GetVipRebateCustList(siteId, pageSize, pageNumber);
        }
        public int PayoutVipRebate(int siteId, int custId, string userName, VipRebateInfo vipRebateInfo, IEnumerable<VipRebateTpBetRecord> vipRebateTpBetRecords, IEnumerable<RebatePayoutLimit> rebatePayoutLimits)
        {
            var result = vipRebateService.PayoutVipRebate(siteId, custId, userName, vipRebateInfo, vipRebateTpBetRecords, out decimal payAmount, out Dictionary<int, decimal> rebateLogList, out Dictionary<int, decimal> rebateCLogList, rebatePayoutLimits);

            if (result > 0)
            {
                var promotionType = 6;
                var custInfo = custInfoService.GetCustInfo(custId);
                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = string.Format($"{custInfo.UserName} {selLangBLL.GetResourceValue(promotionType == 6 ? "lbl_RebateMsg" : "lbl_CashbackMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(title);

                if (promotionType == 6)
                {
                    foreach (var rebateLog in rebateCLogList)
                    {
                        var dAmount = rebateLog.Value.ToString("#,##0.##");
                        if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dAmount = $"{rebateLog.Value / 1000:#,##0.##}";
                        }

                        msg.AppendLine($"{GetTpCategoryList()[rebateLog.Key]}: {dAmount}");
                    }
                }

                messageService.SendMessage(custInfo.SiteId, "Bonus Service", title, msg.ToString(), custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }

            return result;
        }
        public IEnumerable<RebatePayoutLimit> GetRebatePayoutLimit()
        {
            return vipRebateService.GetRebatePayoutLimit();
        }
        public Dictionary<int, string> GetTpGameList()
        {
            string key = $"TpGameList";
            var tpGameList = cache.Get<Dictionary<int, string>>(key);
            if (tpGameList != null)
            {
                return tpGameList;
            }

            tpGameList = rebateService.GetTpGameList().ToDictionary(x => x.Type, x => x.TypeName);
            if (tpGameList != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, tpGameList, cacheEntryOptions);
            }

            return tpGameList;
        }
        public Dictionary<int, string> GetTpCategoryList()
        {
            string key = $"TpCategory";
            var tpGameList = cache.Get<Dictionary<int, string>>(key);
            if (tpGameList != null)
            {
                return tpGameList;
            }

            tpGameList = rebateService.GetTpCategoryList().ToDictionary(x => x.Id, x => x.Name);
            if (tpGameList != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, tpGameList, cacheEntryOptions);
            }

            return tpGameList;
        }
        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            string key = $"GetBonusCashBackInfo";

            var GetBonusCashBackInfo = cache.Get<IEnumerable<BonusCashBack>>(key);
            if (GetBonusCashBackInfo == null)
            {
                GetBonusCashBackInfo = rebateService.GetBonusCashBackInfo();

                if (GetBonusCashBackInfo != null)
                {
                    cache.Set(key, GetBonusCashBackInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return GetBonusCashBackInfo;
        }
        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            string key = $"GetBonusCashBackGamesInfo_{CashbackId}_{SiteId}";

            var GetBonusCashBackGamesInfo = cache.Get<IEnumerable<BonusCashBackGames>>(key);
            if (GetBonusCashBackGamesInfo == null)
            {
                GetBonusCashBackGamesInfo = rebateService.GetBonusCashBackGamesInfo(CashbackId, SiteId);

                if (GetBonusCashBackGamesInfo != null)
                {
                    cache.Set(key, GetBonusCashBackGamesInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return GetBonusCashBackGamesInfo;
        }
        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            return rebateService.GetTpBetRecordInfo(StartDate, EndDate, SiteId, GameType);
        }

        public int InsBonusCashBack(KeyValuePair<int, BonusCashBackInfoList> keyValuePair)
        {
            return rebateService.InsBonusCashBack(keyValuePair.Value);
        }
    }
}
