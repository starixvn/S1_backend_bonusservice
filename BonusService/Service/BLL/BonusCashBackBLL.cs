using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace BonusService.Service.BLL
{
    public class BonusCashBackBLL
    {
        private readonly BonusCashBackService bonusCashBackService;
        private readonly IMemoryCache cache;

        public BonusCashBackBLL(IMemoryCache cache, BonusCashBackService bonusCashBackService)
        {
            this.bonusCashBackService = bonusCashBackService;
            this.cache = cache;
        }

        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            string key = $"GetBonusCashBackInfo";

            var GetBonusCashBackInfo = cache.Get<IEnumerable<BonusCashBack>>(key);
            if (GetBonusCashBackInfo == null)
            {
                GetBonusCashBackInfo = bonusCashBackService.GetBonusCashBackInfo();

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
                GetBonusCashBackGamesInfo = bonusCashBackService.GetBonusCashBackGamesInfo(CashbackId, SiteId);

                if (GetBonusCashBackGamesInfo != null)
                {
                    cache.Set(key, GetBonusCashBackGamesInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return GetBonusCashBackGamesInfo;
        }
        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            return bonusCashBackService.GetTpBetRecordInfo(StartDate, EndDate, SiteId, GameType);
        }

        public int InsBonusCashBack(KeyValuePair<int, BonusCashBackInfoList> keyValuePair)
        {
            return bonusCashBackService.InsBonusCashBack(keyValuePair.Value);
        }
    }
}
