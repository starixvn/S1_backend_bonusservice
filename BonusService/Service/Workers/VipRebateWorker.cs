using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class VipRebateWorker : BackgroundService
    {
        private readonly RebateBLL rebateBLL;
        private readonly Common tools;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<VipRebateWorker> _logger;
        private Timer timer;

        public VipRebateWorker(IHostEnvironment environment, RebateBLL rebateBLL, Common tools, Log2File log2File, SelLangBLL selLangBLL, ILogger<VipRebateWorker> logger)
        {
            this.rebateBLL = rebateBLL;
            this.tools = tools;
            this.log2File = log2File;
            this.environment = environment;
            this.selLangBLL = selLangBLL;
            this._logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            bool isError = false;
            int startTick = 0;
            try
            {
                var rebateTaskList = rebateBLL.GetVipRebateTask();
                foreach (var rebateTask in rebateTaskList)
                {
                    startTick = System.Environment.TickCount;
                    log2File.Info(JsonConvert.SerializeObject(rebateTask), "VipRebateTask");
                    _logger.LogInformation($"VipRebate SiteId:{rebateTask.SiteId} start ");
                    int pageSize = 1000, pageNumber = 1;
                    int listCnt = 0;
                    var rebatePayoutLimit = rebateBLL.GetRebatePayoutLimit();

                  
                    var rebateInfoList = rebateBLL.GetVipRebateCustList(rebateTask.SiteId, pageSize, pageNumber, rebateTask.SpecificDate);
                    log2File.Info($"SiteId:{rebateTask.SiteId} CustCount:{rebateInfoList.Count()} list {JsonConvert.SerializeObject(rebateInfoList)}", "rebateInfoList");
                    foreach (var rebateInfo in rebateInfoList)
                    {
                            var rebateTpBetRecordList = rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, rebateTask.SpecificDate, 0).ToList();//.ToDictionary<string, RebateTpBetRecord>(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                        if (rebateTask.PeriodType == 3)
                        {
                            var dd = rebateTpBetRecordList.ToDictionary(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                            foreach (var rebateTpBetRecord14 in rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, rebateTask.SpecificDate, 1))
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

                        if (rebateTpBetRecordList.Count > 0)
                        {
                            log2File.Info($"CustId:{rebateInfo.CustId} TpBetRecordList:{JsonConvert.SerializeObject(rebateTpBetRecordList)}", "rebateTpBetRecordList");
                            rebateBLL.PayoutVipRebate(rebateTask.SiteId, rebateInfo.CustId, rebateInfo.UserName, rebateInfo, rebateTpBetRecordList, rebatePayoutLimit);
                        }
                    }

                    rebateBLL.SetVipRebateTaskDone(rebateTask.SiteId);
                    int spendTime = System.Environment.TickCount - startTick;
                        
                    log2File.Info($"SiteId:{rebateTask.SiteId} SpecificDate:{rebateTask.SpecificDate} finish time: {spendTime / 60000}:{(spendTime % 60000) / 1000}.{(spendTime % 60000) % 1000}" , "VipRebateTask");
                    _logger.LogInformation($"SiteId:{rebateTask.SiteId} SpecificDate:{rebateTask.SpecificDate} finish time: {spendTime / 60000}:{(spendTime % 60000) / 1000}.{(spendTime % 60000) % 1000}");
                }
            }
            catch (Exception ex)
            {
                isError = true;
                log2File.Error(ex.ToString(), "VipRebate");
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(60);
                //  if (environment.IsDevelopment())
                //  {
                //      dueTimeSpan = TimeSpan.FromMinutes(1);
                //  }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
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
