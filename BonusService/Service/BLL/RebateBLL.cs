using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace BonusService.Service.BLL
{
    public class RebateBLL
    {
        private readonly RebateService rebateService;
        private readonly IMemoryCache cache;

        public RebateBLL(IMemoryCache cache, RebateService rebateService)
        {
            this.rebateService = rebateService;
            this.cache = cache;
        }

        public IEnumerable<RebateTask> GetRebateTask()
        {
            return rebateService.GetRebateTask();
        }
        public IEnumerable<RebateInfo> GetRebateInfo(string bonusCode, int siteId, int pageSize, int pageNumber)
        {
            return rebateService.GetRebateInfo(bonusCode, siteId, pageSize, pageNumber);
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
