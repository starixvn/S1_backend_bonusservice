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
        private readonly IMemoryCache cache;
        private readonly SelLangBLL selLangBLL;
        private readonly Dictionary<int, bool> isConvert = new Dictionary<int, bool>()
            {
                {51,false },{15,false }
            };

        public RebateBLL(IMemoryCache cache, RebateService rebateService, SelLangBLL selLangBLL)
        {
            this.rebateService = rebateService;
            this.cache = cache;
            this.selLangBLL = selLangBLL;
        }

        public IEnumerable<RebateTask> GetRebateTask()
        {
            return rebateService.GetRebateTask();
        }
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
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
                var custInfo = rebateService.GetCustInfo(custId);
                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##} ('000)";
                }

                var title = string.Format($"{custInfo.UserName} {selLangBLL.GetResourceValue(promotionType == 6 ? "lbl_RebateMsg" : "lbl_CashbackMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(title);
                foreach(var rebateLog in rebateLogList)
                {
                    var dAmount = rebateLog.Value.ToString("#,##0.##");
                    if (IsConvertRequired(custInfo.CurrencyId))
                    {
                        dAmount = $"{rebateLog.Value / 1000:#,##0.##} ('000)";
                    }

                    msg.AppendLine($"{GetTpGameList()[rebateLog.Key]}: {dAmount}");
                }

                rebateService.SendMessage(custInfo.SiteId, "Bonus Service", title, msg.ToString(), custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }

            return result;
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
