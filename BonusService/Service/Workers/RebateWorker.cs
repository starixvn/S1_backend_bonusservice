using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Net;

namespace BonusService
{
    public class RebateWorker : BackgroundService
    {
        private readonly ILogger<RebateWorker> _logger;
        private readonly RebateBLL rebateBLL;
        private readonly Common tools;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private readonly SelLangBLL selLangBLL;
        private Timer timer;

        public RebateWorker(ILogger<RebateWorker> logger, IHostEnvironment environment, RebateBLL rebateBLL, Common tools, Log2File log2File, SelLangBLL selLangBLL)
        {
            _logger = logger;
            this.rebateBLL = rebateBLL;
            this.tools = tools;
            this.log2File = log2File;
            this.environment = environment;
            this.selLangBLL = selLangBLL;
            
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
          //  Console.WriteLine(selLangBLL.GetResourceValue("lbl_HelloWorld", "en"));
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            bool isError = false;

            _logger.LogCritical($"Worker running at: {environment.EnvironmentName}");
            try
            {
                var rebateTaskList = rebateBLL.GetRebateTask();
                foreach (var rebateTask in (from p in rebateTaskList
                                            select new { p.BonusCode, p.SiteId, p.PromotionType, p.PeriodType }).Distinct())
                {
                    log2File.Info(JsonConvert.SerializeObject(rebateTask), "rebateTask");
                    int pageSize = 1000, pageNumber = 1;
                    int listCnt = 0;
                    do
                    {
                        var rebateInfoList = rebateBLL.GetRebateInfo(rebateTask.BonusCode, rebateTask.SiteId, pageSize, pageNumber);
                        foreach(var rebateInfo in rebateInfoList)
                        {
                            var rebateTpBetRecordList = rebateBLL.GetRebateTpBetRecord(rebateTask.BonusCode, rebateTask.SiteId, rebateInfo.CustId, rebateInfo.CurrencyId, 0).ToList();//.ToDictionary<string, RebateTpBetRecord>(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                            if (rebateTask.PeriodType == 3)
                            {
                                var dd = rebateTpBetRecordList.ToDictionary(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                                foreach (var rebateTpBetRecord14 in rebateBLL.GetRebateTpBetRecord(rebateTask.BonusCode, rebateTask.SiteId, rebateInfo.CustId, rebateInfo.CurrencyId, 1))
                                {
                                    if (dd.ContainsKey($"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"))
                                    {
                                        dd[$"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"].ValidBet += rebateTpBetRecord14.ValidBet;
                                        dd[$"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"].WinlostAmount += rebateTpBetRecord14.WinlostAmount;
                                    }
                                    else
                                    {
                                        rebateTpBetRecordList.Add(rebateTpBetRecord14);
                                    }
                                }
                            }

                            if (rebateTpBetRecordList.Count() > 0)
                            {
                                log2File.Info(JsonConvert.SerializeObject(rebateTpBetRecordList), "rebateTpBetRecordList");
                                //RebateTpBetRecord
                                rebateBLL.PayoutRebate(rebateTask.SiteId, rebateInfo.CustId, rebateTpBetRecordList.FirstOrDefault()?.UserName, rebateTask.BonusCode, rebateInfo, rebateTpBetRecordList, rebateTaskList);
                            }
                        }

                        listCnt = rebateInfoList.Count();
                        pageNumber++;
                    }
                    while (listCnt == pageSize);

                    rebateBLL.SetRebateTaskDone(rebateTask.BonusCode, rebateTask.SiteId);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex.ToString());
                log2File.Error(ex.ToString(), "Rebate");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);
                if (environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        void Cashback(long StartDay, long EndDay, BonusCashBack bonusCashBack)
        {
            var bonusCashBackInfoList = new Dictionary<int, BonusCashBackInfoList>();

            var BonusCashBackGamesInfo = rebateBLL.GetBonusCashBackGamesInfo(bonusCashBack.Id, bonusCashBack.SiteId);
            foreach (var bonusCashBackGames in BonusCashBackGamesInfo)
            {
                if (bonusCashBackGames.Status == 1)
                {
                    var TpBetRecordInfo = rebateBLL.GetTpBetRecordInfo(StartDay, EndDay, bonusCashBack.SiteId, bonusCashBackGames.GameId);

                    foreach (var TpBetRecord in TpBetRecordInfo)
                    {
                        if (TpBetRecord.WinlostAmountCount < 0)
                        {
                            decimal Percentage = bonusCashBackGames.Percentage ?? bonusCashBack.DefaultPercentage;
                            decimal Calculation = tools.Floor(Math.Abs(TpBetRecord.WinlostAmountCount) * Percentage / 100m, 2);
                            if (!bonusCashBackInfoList.ContainsKey(TpBetRecord.CustId))
                            {
                                var bonusCashBackInfoLists = new BonusCashBackInfoList();
                                bonusCashBackInfoLists.CurrencyId = TpBetRecord.CurrencyId;
                                bonusCashBackInfoLists.CustId = TpBetRecord.CustId;
                                bonusCashBackInfoLists.SiteId = bonusCashBack.SiteId;
                                bonusCashBackInfoLists.TpUserName = TpBetRecord.TpUserName;
                                bonusCashBackInfoLists.UserName = TpBetRecord.UserName;
                                bonusCashBackInfoLists.WithdrawCondition = bonusCashBack.WithdrawCondition;
                                bonusCashBackInfoLists.WinlostAmountCountCalculationTotal = Calculation;
                                bonusCashBackInfoLists.CalculationDay = StartDay;
                                bonusCashBackInfoList.Add(TpBetRecord.CustId, bonusCashBackInfoLists);

                                bonusCashBackInfoLists.BonusCashBackInfoListDetails = new List<BonusCashBackInfoListDetail>();
                                bonusCashBackInfoLists.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = bonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                            }
                            else
                            {
                                var bonusCashBackInfoLists = bonusCashBackInfoList[TpBetRecord.CustId];
                                bonusCashBackInfoLists.WinlostAmountCountCalculationTotal += Calculation;
                                bonusCashBackInfoLists.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = bonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                            }
                        }
                    }
                }
            }

            if (bonusCashBackInfoList != null)
            {
                foreach (var list in bonusCashBackInfoList)
                {
                    if (rebateBLL.InsBonusCashBack(list) > 0)
                    {
                        continue;
                    }
                }
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
