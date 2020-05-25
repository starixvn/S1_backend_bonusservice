using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using System;
using BonusService.Service.Library;
using System.Collections.Generic;
using BonusService.Service.Model;

namespace BonusService
{
    public class BonusCashBackWorker : BackgroundService
    {
        private readonly ILogger<BonusCashBackWorker> _logger;
        private readonly BonusCashBackBLL bonusCashBackBLL;
        private readonly Common tools;

        public BonusCashBackWorker(ILogger<BonusCashBackWorker> logger, BonusCashBackBLL bonusCashBackBLL, Common tools)
        {
            _logger = logger;
            this.bonusCashBackBLL = bonusCashBackBLL;
            this.tools = tools;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var cashBack in bonusCashBackBLL.GetBonusCashBackInfo())
                {
                    DateTime utcDate = DateTime.Now.ToUniversalTime();
                    var settleTime1 = utcDate.Add(new TimeSpan(0, Convert.ToInt32(60 * cashBack.TimeZone), 0));
                    DateTimeOffset calTime = new DateTimeOffset(settleTime1.Year, settleTime1.Month, settleTime1.Day, cashBack.PayHour, cashBack.PayMinute, 0, new TimeSpan(0, Convert.ToInt32(60 * cashBack.TimeZone), 0));
                    _logger.LogCritical("Worker running at: {time}", calTime);

                    if (calTime.LocalDateTime.ToString("yyyyMMddHHmm") == DateTime.Now.ToString("yyyyMMddHHmm"))
                    {
                        if (cashBack.Period == 1)
                        {
                            DateTime dateTime = calTime.AddDays(-1).DateTime;
                            var StartTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(60 * cashBack.TimeZone), 0));
                            long StartDay = StartTime.ToUnixTimeSeconds();
                            long EndDay = StartTime.AddDays(1).ToUnixTimeSeconds() - 1;

                            var BonusCashBackGamesInfo = bonusCashBackBLL.GetBonusCashBackGamesInfo(cashBack.Id, cashBack.SiteId);
                            foreach (var BonusCashBackGames in BonusCashBackGamesInfo)
                            {
                                if (BonusCashBackGames.Status == 1)
                                {
                                    Cashback(StartDay, EndDay, cashBack, BonusCashBackGames);
                                }
                            }
                        }
                        else if (cashBack.Period == 2 && (int)calTime.LocalDateTime.DayOfWeek == cashBack.DayOfWeek)
                        {
                            var BonusCashBackGamesInfo = bonusCashBackBLL.GetBonusCashBackGamesInfo(cashBack.Id, cashBack.SiteId);
                            foreach (var BonusCashBackGames in BonusCashBackGamesInfo)
                            {
                                if (BonusCashBackGames.Status == 1)
                                {
                                    for (var stepDay = 7; stepDay > 0; stepDay--)
                                    {
                                        DateTime dateTime = calTime.AddDays(-1 * 7).DateTime;
                                        var StartTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(60 * cashBack.TimeZone), 0));
                                        long StartDay = StartTime.ToUnixTimeSeconds();
                                        long EndDay = StartTime.AddDays(1).ToUnixTimeSeconds() - 1;

                                        Cashback(StartDay, EndDay, cashBack, BonusCashBackGames);
                                    }
                                }
                            }

                            //var BonusCashBackMinMaxInfo = GetBonusCashBackInfo.GetBonusCashBackMinMaxInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);

                            //foreach (var BonusCashBackMinMax in BonusCashBackMinMaxInfo)
                            //{
                            //    Console.WriteLine("--MinMax--");
                            //    Console.WriteLine(BonusCashBackMinMax.SiteId);
                            //    Console.WriteLine(BonusCashBackMinMax.CurrencyId);
                            //    Console.WriteLine(BonusCashBackMinMax.MinPay);
                            //    Console.WriteLine(BonusCashBackMinMax.MaxPay);
                            //    Console.WriteLine(BonusCashBackMinMax.MinBet);
                            //    Console.WriteLine(BonusCashBackMinMax.MaxBet);
                            //    Console.WriteLine("--MinMax--");
                            //}
                        }
                    }
                }

                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }

            void Cashback(long StartDay, long EndDay, BonusCashBack bonusCashBack, BonusCashBackGames bonusCashBackGames)
            {
                var TpBetRecordInfo = bonusCashBackBLL.GetTpBetRecordInfo(StartDay, EndDay, bonusCashBack.SiteId, bonusCashBackGames.GameId);
                var bonusCashBackInfoList = new Dictionary<int, BonusCashBackInfoList>();
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
                            bonusCashBackInfoLists.SiteId = TpBetRecord.SiteId;
                            bonusCashBackInfoLists.TpUserName = TpBetRecord.TpUserName;
                            bonusCashBackInfoLists.UserName = TpBetRecord.UserName;
                            bonusCashBackInfoLists.WinlostAmountCountCalculationTotal = Calculation;
                            bonusCashBackInfoList.Add(TpBetRecord.CustId, bonusCashBackInfoLists);

                            bonusCashBackInfoLists.BonusCashBackInfoListDetails = new List<BonusCashBackInfoListDetail>();
                            bonusCashBackInfoLists.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = bonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                        }
                        else
                        {
                            var bonusCashBackInfoLists = bonusCashBackInfoList[TpBetRecord.CustId];
                            bonusCashBackInfoLists.WinlostAmountCountCalculationTotal = Calculation;
                            bonusCashBackInfoLists.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = bonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                        }
                    }
                }

                if (bonusCashBackInfoList != null)
                {
                    foreach (var list in bonusCashBackInfoList)
                    {
                        bonusCashBackBLL.InsBonusCashBack(list);
                    }
                }
            }
        }
    }
}
