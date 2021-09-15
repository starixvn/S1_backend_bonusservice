using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonusService.Service.Library;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.BLL
{
    public class RebateBLL
    {
        private readonly RebateService _rebateService;
        private readonly VipRebateService _vipRebateService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly CustInfoService _custInfoService;
        private readonly CacheHelper _cacheHelper;
        
        public RebateBLL(IServiceProvider provider)
        {
            _rebateService = provider.GetRequiredService<RebateService>();
            _vipRebateService = provider.GetRequiredService<VipRebateService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            return _vipRebateService.GetVipRebateTask();
        }
        public int SetVipRebateTaskDone(int siteId)
        {
            return _vipRebateService.SetVipRebateTaskDone(siteId);
        }

        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int siteId, int custId, int is14)
        {
            return _vipRebateService.GetVipRebateTpBetRecord(siteId, custId, is14);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int pageSize, int pageNumber)
        {
            return _vipRebateService.GetVipRebateCustList(siteId, pageSize, pageNumber);
        }
        public int PayoutVipRebate(int siteId, int custId, string userName, VipRebateInfo vipRebateInfo, IEnumerable<VipRebateTpBetRecord> vipRebateTpBetRecords, IEnumerable<RebatePayoutLimit> rebatePayoutLimits)
        {
            var result = _vipRebateService.PayoutVipRebate(siteId, custId, userName, vipRebateInfo, vipRebateTpBetRecords, out decimal payAmount, out Dictionary<int, decimal> rebateLogList, out Dictionary<int, decimal> rebateCLogList, rebatePayoutLimits);

            if (result > 0)
            {
                var promotionType = 6;
                var custInfo = _custInfoService.GetCustInfo(custId);
                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = string.Format($"{custInfo.UserName} {_selLangBll.GetResourceValue(promotionType == 6 ? "lbl_RebateMsg" : "lbl_CashbackMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(title);

                if (promotionType == 6)
                {
                    foreach (var rebateLog in rebateCLogList)
                    {
                        var dAmount = rebateLog.Value.ToString("#,##0.##");
                        if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dAmount = $"{rebateLog.Value / 1000:#,##0.##}";
                        }

                        var rebateTpBetRecord = vipRebateTpBetRecords.FirstOrDefault(x => x.CategoryId == rebateLog.Key);

                        msg.AppendLine($"{GetTpCategoryList()[rebateLog.Key]} ({rebateTpBetRecord.Percentage}): {dAmount}");
                    }
                }

                _messageService.SendMessage(custInfo.SiteId, "Bonus Service", title, msg.ToString(), custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }

            return result;
        }
        public IEnumerable<RebatePayoutLimit> GetRebatePayoutLimit()
        {
            return _vipRebateService.GetRebatePayoutLimit();
        }

        public Dictionary<int, string> GetTpCategoryList()
        {
            string key = $"TpCategory";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)),
                i => _rebateService.GetTpCategoryList().ToDictionary(x => x.Id, x => x.Name));
        }

        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            string key = $"GetBonusCashBackGamesInfo_{CashbackId}_{SiteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _rebateService.GetBonusCashBackGamesInfo(CashbackId, SiteId));
        }
        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            return _rebateService.GetTpBetRecordInfo(StartDate, EndDate, SiteId, GameType);
        }

        public int InsBonusCashBack(KeyValuePair<int, BonusCashBackInfoList> keyValuePair)
        {
            return _rebateService.InsBonusCashBack(keyValuePair.Value);
        }
    }
}
