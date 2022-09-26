using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using BonusService.Service.Library;
using Newtonsoft.Json;

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
        private readonly ILogger<RebateBLL> _logger;

        public RebateBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RebateBLL>>();
            _rebateService = provider.GetRequiredService<RebateService>();
            _vipRebateService = provider.GetRequiredService<VipRebateService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<RebateTask> VerifyRebateTask()
        {
            //檢查參數
            //0.檢查參數是否正確
            //1.有設定指定日期，且日期大於2020/01/01，屬於重跑流程(目前因為系統也只有設定daily，故只有daily有再用)
            //2.檢查期間型別需檢查項目是否符合

            var rebateTaskList = _vipRebateService.GetVipRebateTask();

            var doTaskList = new List<RebateTask>();

            foreach (var task in rebateTaskList)
            {
                //Step1：有設定指定日期，且日期大於2020/01/01，屬於重跑流程
                if (task.SpecificDate > new DateTime(2020, 01, 01))
                {
                    _logger.LogInformation($"This Task Has SpecificDate To Redo Task in {task.SpecificDate.ToString()}.");
                    doTaskList.Add(task);
                    continue;
                }

                //Step2：檢查期間型別需檢查項目是否符合
                switch (task.PeriodType)
                {
                    case 1:  //1: Daily
                        break;
                    case 2:  //2:Weekly
                        var weekDay = (int)task.CurrencyNowTime.DayOfWeek; //Week 1234560 = Monday To Sunday
                        if (weekDay == 0) weekDay = 7; //7=Sunday

                        //幣別時區日期 必須為 指定星期，排程才可執行
                        if (weekDay != task.PeriodDay)
                        {
                            _logger.LogWarning($"task.PeriodDay is Invaild. task value={JsonConvert.SerializeObject(task)}.");
                            continue;
                        }
                        break;
                    case 3:  //3: Monthly
                        //目前暫無使用
                        //var currencyNowDay = task.CurrencyNowTime.Day;
                        //var lastDay = DateTime.DaysInMonth(task.CurrencyNowTime.Year, task.CurrencyNowTime.Month);
                        //if (currencyNowDay != task.PeriodDay)
                        //{
                        //    _logger.LogWarning($"task.PeriodDay is Invaild. task value={JsonConvert.SerializeObject(task)}.");
                        //}
                        //if ((lastDay == currencyNowDay) && (currencyNowDay < task.PeriodDay))
                        //{
                        //    _logger.LogWarning($"task.PeriodDay is Invaild. task value={JsonConvert.SerializeObject(task)}.");
                        //}
                        break;
                    default:
                        _logger.LogError($"task.PeriodType is Invaild. task value={JsonConvert.SerializeObject(task)}.");
                        continue;
                }

                //Step3：檢查小時是否符合
                //幣別時區小時 <= 指定小時，不執行排程
                if (task.CurrencyNowTime.Hour <= task.RedeemHours)
                {
                    _logger.LogWarning($"task.RedeemsHours Invaild. task value={JsonConvert.SerializeObject(task)}");
                    continue;
                }

                //Step4：檢查最近一次執行時間是否符合
                //LastCurrencySettledTime 是 Null，代表第一次執行排程
                //LastCurrencySettledTime.Date = CurrencyNowTime.Date，上次執行時間日期等於幣別時區時間日期，不可再執行(原邏輯)
                if (task.LastCurrencySettledTime !=  null)
                {
                    if (task.LastCurrencySettledTime.Date == task.CurrencyNowTime.Date)
                    {
                        _logger.LogWarning($"task.LastCurrencySettledTime Invaild. task value={JsonConvert.SerializeObject(task)}");
                        continue;
                    }
                }

                doTaskList.Add(task);
            }

            return doTaskList;
        }

        public int SetVipRebateTaskDone(int siteId, int currencyId)
        {
            return _vipRebateService.SetVipRebateTaskDone(siteId, currencyId);
        }

        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int periodType, int custId, DateTime specificDate, string timeZone)
        {
            return _vipRebateService.GetVipRebateTpBetRecord(periodType, custId, specificDate, timeZone);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int currencyId, int periodType, DateTime specificDate, string currencyTimeZone,int pageNumber,int pageSize)
        {
            return _vipRebateService.GetVipRebateCustList(siteId, currencyId, periodType, specificDate, currencyTimeZone,pageNumber,pageSize);
        }
        public int PayoutVipRebate(int siteId, VipRebateInfo vipRebateInfo, IEnumerable<VipRebateTpBetRecord> vipRebateTpBetRecords, IEnumerable<RebatePayoutLimit> rebatePayoutLimits)
        {

            var result = _vipRebateService.PayoutVipRebate(siteId, vipRebateInfo, vipRebateTpBetRecords, out decimal payAmount, out var rebateLogList, out var rebateCLogList, rebatePayoutLimits);

            if (result > 0)
            {
                var custInfo = _custInfoService.GetCustInfo(vipRebateInfo.CustId);
                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = string.Format($"{custInfo.UserName} {_selLangBll.GetResourceValue("lbl_RebateMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(title);

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
            var key = $"TpCategory";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)),
                i => _rebateService.GetTpCategoryList().ToDictionary(x => x.Id, x => x.Name));
        }

        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            var key = $"GetBonusCashBackGamesInfo_{CashbackId}_{SiteId}";
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
