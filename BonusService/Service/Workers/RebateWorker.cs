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

namespace BonusService
{
    public class RebateWorker : BackgroundService
    {
        private readonly ILogger<RebateWorker> _logger;
        private readonly RebateBLL rebateBLL;
        private readonly Common tools;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private Timer timer;

        public RebateWorker(ILogger<RebateWorker> logger, IHostEnvironment environment, RebateBLL rebateBLL, Common tools, Log2File log2File)
        {
            _logger = logger;
            this.rebateBLL = rebateBLL;
            this.tools = tools;
            this.log2File = log2File;
            this.environment = environment;
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
                var rebateTaskList = rebateBLL.GetRebateTask();
                foreach (var rebateTask in (from p in rebateTaskList
                                            select new { p.BonusCode, p.SiteId }).Distinct())
                {
                    _logger.LogCritical($"Worker running at: {JsonConvert.SerializeObject(rebateTask)}");

                    int pageSize = 1000, pageNumber = 1;
                    int listCnt = 0;
                    do
                    {

                        var rebateInfoList = rebateBLL.GetRebateInfo(rebateTask.BonusCode, rebateTask.SiteId, pageSize, pageNumber);



                        listCnt = rebateInfoList.Count();
                        pageNumber++;
                    }
                    while (listCnt == pageSize);
                    //DateTime utcDate = DateTime.Now.ToUniversalTime();
                    //var settleTime1 = utcDate.Add(new TimeSpan(0, Convert.ToInt32(60 * rebateTask.TimeZone), 0));
                    //DateTimeOffset calTime = new DateTimeOffset(settleTime1.Year, settleTime1.Month, settleTime1.Day, rebateTask.PayHour, rebateTask.PayMinute, 0, new TimeSpan(0, Convert.ToInt32(60 * rebateTask.TimeZone), 0));
                    ////DateTimeOffset calTime = new DateTimeOffset(2020, 02, 11, 16, 0, 0, new TimeSpan(0, Convert.ToInt32(60 * cashBack.TimeZone), 0));

                    //if (calTime.LocalDateTime.ToString("yyyyMMddHHmm") == DateTime.Now.ToString("yyyyMMddHHmm") || true)
                    //{
                    //    if (rebateTask.Period == 1)
                    //    {
                    //        DateTime dateTime = calTime.AddDays(-1).DateTime;
                    //        var StartTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(60 * rebateTask.TimeZone), 0));
                    //        long StartDay = StartTime.ToUnixTimeSeconds();
                    //        long EndDay = StartTime.AddDays(1).ToUnixTimeSeconds() - 1;

                    //        Cashback(StartDay, EndDay, rebateTask);
                    //    }
                    //    else if (rebateTask.Period == 2 && (int)calTime.LocalDateTime.DayOfWeek == rebateTask.DayOfWeek)
                    //    {
                    //        for (var stepDay = 7; stepDay > 0; stepDay--)
                    //        {
                    //            DateTime dateTime = calTime.AddDays(-1 * stepDay).DateTime;
                    //            var StartTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(60 * rebateTask.TimeZone), 0));
                    //            long StartDay = StartTime.ToUnixTimeSeconds();
                    //            long EndDay = StartTime.AddDays(1).ToUnixTimeSeconds() - 1;

                    //            Cashback(StartDay, EndDay, rebateTask);
                    //        }
                    //    }
                    //}
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
