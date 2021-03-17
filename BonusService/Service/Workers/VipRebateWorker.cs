using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Hosting;
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
        private Timer timer;

        public VipRebateWorker(IHostEnvironment environment, RebateBLL rebateBLL, Common tools, Log2File log2File, SelLangBLL selLangBLL)
        {
            this.rebateBLL = rebateBLL;
            this.tools = tools;
            this.log2File = log2File;
            this.environment = environment;
            this.selLangBLL = selLangBLL;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            bool isError = false;

            try
            {
                var rebateTaskList = rebateBLL.GetVipRebateTask();
                foreach (var rebateTask in rebateTaskList)
                {
                    log2File.Info(JsonConvert.SerializeObject(rebateTask), "VipRebateTask");
                    int pageSize = 1000, pageNumber = 1;
                    int listCnt = 0;
                    var rebatePayoutLimit = rebateBLL.GetRebatePayoutLimit();

                    do
                    {
                        var rebateInfoList = rebateBLL.GetVipRebateCustList(rebateTask.SiteId, pageSize, pageNumber);
                        int ncount = 0;
                        foreach (var rebateInfo in rebateInfoList)
                        {
                            var rebateTpBetRecordList = rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, 0).ToList();//.ToDictionary<string, RebateTpBetRecord>(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                            if (rebateTask.PeriodType == 3)
                            {
                                var dd = rebateTpBetRecordList.ToDictionary(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                                foreach (var rebateTpBetRecord14 in rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, 1))
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
                                log2File.Info(JsonConvert.SerializeObject(rebateTpBetRecordList), "rebateTpBetRecordList");
                                rebateBLL.PayoutVipRebate(rebateTask.SiteId, rebateInfo.CustId, rebateInfo.UserName, rebateInfo, rebateTpBetRecordList, rebatePayoutLimit);
                            }
                        }

                        listCnt = rebateInfoList.Count();
                        pageNumber++;
                    }
                    while (listCnt == pageSize);

                    rebateBLL.SetVipRebateTaskDone(rebateTask.SiteId);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                log2File.Error(ex.ToString(), "VipRebate");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(60);
                if (environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

               // if (isError)
              //  {
              //      dueTimeSpan = TimeSpan.FromSeconds(5);
              //  }

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
