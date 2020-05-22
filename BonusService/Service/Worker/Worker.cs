using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using System;

namespace BonusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly BonusCashBackBLL bonusCashBackBLL;

        public Worker(ILogger<Worker> logger, BonusCashBackBLL bonusCashBackBLL)
        {
            _logger = logger;
            this.bonusCashBackBLL = bonusCashBackBLL;
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
                                        var TpBetRecordInfo = bonusCashBackBLL.GetTpBetRecordInfo(StartDay, EndDay, cashBack.SiteId, BonusCashBackGames.GameId);

                                        foreach (var TpBetRecord in TpBetRecordInfo)
                                        {
                                            if (TpBetRecord.WinlostAmountCount < 0)
                                            {
                                                ////decimal Percentage = (BonusCashBackGames.Percentage == null) ? CashBackdata.DefaultPercentage : BonusCashBackGames.Percentage;
                                                ////long value = (long)Math.Pow(10.0, 2);
                                                ////decimal Calculation = Convert.ToDecimal(Math.Floor(TpBetRecord.WinlostAmountCount * Percentage / 100 * (decimal)value) / (decimal)value) * -1;
                                                //////Console.WriteLine(
                                                //////    $"{TpBetRecord.CustId}  " +
                                                //////    $"{TpBetRecord.UserName}  " +
                                                //////    $"{TpBetRecord.StakeCount}  " +
                                                //////    $"{TpBetRecord.WinlostAmountCount}  " +
                                                //////    $"{BonusCashBackGames.Percentage}  " +
                                                //////    $"{Calculation} "
                                                //////);

                                                ////var get = bonusCashBackInfoLists.FirstOrDefault(x => x.CustId == TpBetRecord.CustId);
                                                ////if (get != null)
                                                ////{
                                                ////    get.WinlostAmountCountCalculationTotal += Calculation;
                                                ////    get.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                                                ////}
                                                ////else
                                                ////{
                                                ////    var gameInfo = new List<BonusCashBackInfoListDetail>();
                                                ////    gameInfo.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation, Percentage = Percentage });
                                                ////    bonusCashBackInfoLists.Add(new BonusCashBackInfoList()
                                                ////    {
                                                ////        CustId = TpBetRecord.CustId,
                                                ////        UserName = TpBetRecord.UserName,
                                                ////        CurrencyId = TpBetRecord.CurrencyId,
                                                ////        SiteId = CashBackdata.SiteId,
                                                ////        TpUserName = TpBetRecord.TpUserName,
                                                ////        WinlostAmountCountCalculationTotal = Calculation,
                                                ////        BonusCashBackInfoListDetails = gameInfo
                                                ////    });
                                                ////}
                                            }
                                        }
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
        }
    }
}
