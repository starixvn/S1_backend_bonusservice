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
        private readonly Dictionary<int, string> _tpCategoryList;
        private IWebHostEnvironment _webHostEnvironment;

        public RebateBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RebateBLL>>();
            _rebateService = provider.GetRequiredService<RebateService>();
            _vipRebateService = provider.GetRequiredService<VipRebateService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _tpCategoryList = _rebateService.GetTpCategoryList().ToDictionary(x => x.Id, x => x.Name);
            _webHostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
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
                if (task.LastCurrencySettledTime.Date == task.CurrencyNowTime.Date)
                {
                    _logger.LogWarning($"task.LastCurrencySettledTime Invaild. task value={JsonConvert.SerializeObject(task)}");
                    continue;
                }


                doTaskList.Add(task);
            }

            return doTaskList;
        }

        public int SetVipRebateTaskDone(int siteId, int currencyId)
        {
            return _vipRebateService.SetVipRebateTaskDone(siteId, currencyId);
        }

        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int custId, DateTime specificDate, string timeZone)
        {
            return _vipRebateService.GetVipRebateTpBetRecord(custId, specificDate, timeZone);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int currencyId, int periodType, DateTime specificDate, string currencyTimeZone)
        {
            return _vipRebateService.GetVipRebateCustList(siteId, currencyId, periodType, specificDate, currencyTimeZone);
        }
        public int PayoutVipRebate(int siteId, VipRebateInfo vipRebateInfo, IEnumerable<VipRebateTpBetRecord> vipRebateTpBetRecordList, IEnumerable<RebatePayoutLimit> rebatePayoutLimits, long RelId)
        {
            //原始資料包含相同GameType不同GameId的資料，先用GameId排除不計算Rebate的遊戲
            var excludeGameList = GetExcludeGameList(siteId, vipRebateInfo.CurrencyId);
            _logger.LogInformation($"excludeGameList:{JsonConvert.SerializeObject(excludeGameList)}");
            foreach (var vipRebateTpBetRecord in vipRebateTpBetRecordList)
            {
                _logger.LogInformation($"vipRebateTpBetRecord:{JsonConvert.SerializeObject(vipRebateTpBetRecord)}");
                if (excludeGameList.TryGetValue(vipRebateTpBetRecord.GameType, out var gameTypeExcludeGameList) &&
                    gameTypeExcludeGameList.ContainsKey(vipRebateTpBetRecord.GameID))
                {
                    vipRebateTpBetRecord.ValidTurnover = 0;
                    vipRebateTpBetRecord.RedeemAmount = 0;
                    _logger.LogInformation($"exclude Rebate Game:{JsonConvert.SerializeObject(vipRebateTpBetRecord)}");
                }
                else
                    vipRebateTpBetRecord.ValidTurnover = vipRebateTpBetRecord.ValidBet;
            }

            //下個流程寫Log會錯誤，重新用GameType匯總資料
            var gameTypeRebateList = vipRebateTpBetRecordList
                    .GroupBy(x => x.GameType)
                    .Select(gameTypeList =>
                        {
                            var first = gameTypeList.First();
                            return new VipRebateTpBetRecord
                            {
                                SettledDate = first.SettledDate,
                                CategoryId = first.CategoryId,
                                GameType = gameTypeList.Key,
                                Percentage = first.Percentage,
                                RedeemAmount = gameTypeList.Sum(i => i.RedeemAmount),
                                ValidBet = gameTypeList.Sum(i => i.ValidBet),
                                ValidTurnover = gameTypeList.Sum(i => i.ValidTurnover),
                            };
                        }).ToList();
            _logger.LogInformation($"gameTypeRebateList:{JsonConvert.SerializeObject(gameTypeRebateList)}");

            var result = _vipRebateService.PayoutVipRebate(siteId, vipRebateInfo, gameTypeRebateList, rebatePayoutLimits, out var payAmount, out var rebateCLogList, RelId);

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

                    msg.AppendLine($"{_tpCategoryList[rebateLog.Key]} : {dAmount}");
                }

                _messageService.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, msg.ToString()));
            }

            return result;
        }
        public IEnumerable<RebatePayoutLimit> GetRebatePayoutLimit()
        {
            return _vipRebateService.GetRebatePayoutLimit();
        }

        public Dictionary<int, Dictionary<string, string>> GetExcludeGameList(int siteId, int currencyId)
        {
            var key = $"Rebate:ExcludeGameList:{siteId}_{currencyId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)),
                i =>
                    {
                        var result = new Dictionary<int, Dictionary<string, string>>();
                        //用GameType分類建立GameId檢查字典
                        var gameTypeGroups = _rebateService.GetExcludeGameList(siteId, currencyId).GroupBy(i => i.GameType);
                        foreach (var gameTypeList in gameTypeGroups)
                        {
                            var gameTypeResult = new Dictionary<string, string>();
                            foreach (var game in gameTypeList)
                            {
                                //先加入GameId到字典
                                if (!string.IsNullOrWhiteSpace(game.GameId))
                                    gameTypeResult.Add(game.GameId, game.GameId);

                                //再用AppId加入字典，避免第三方注單提供的GameId與我們Table設定的GameId不同
                                if (!string.IsNullOrWhiteSpace(game.AppId))
                                    gameTypeResult.TryAdd(game.AppId, game.AppId);

                                //再用SettleGameId加入字典，避免第三方注單提供的GameId與Settle的GameId不同
                                if (!string.IsNullOrWhiteSpace(game.SettleGameId))
                                    gameTypeResult.TryAdd(game.SettleGameId, game.SettleGameId);
                            }
                            result.Add(gameTypeList.Key, gameTypeResult);
                        }
                        return result;
                    }
                );
        }
    }
}
