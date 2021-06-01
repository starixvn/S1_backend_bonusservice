using BonusService.Service.BLL;
using BonusService.Service.Library;
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
    public class ExpiredBonusWorker : BackgroundService
    {
        private readonly ILogger<ExpiredBonusWorker> logger;
        private readonly ExpiredBonusBLL expiredBonusBLL;
        private readonly Log2File log2File;
        private Timer timerExpiredBonus;
        //private DateTime NextChickTime;
        private int LastCheckHour;

        public ExpiredBonusWorker(ILogger<ExpiredBonusWorker> logger, ExpiredBonusBLL expiredBonusBLL, Log2File log2File)
        {
            this.logger = logger;
            this.expiredBonusBLL = expiredBonusBLL;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerExpiredBonus = new Timer(DoExpiredBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoExpiredBonusTaskAsync(object state)
        {
            int LimitNum = 1000;
            int nCount = 0;
            try
            {
                logger.LogInformation($"DoExpiredBonus now time{GetUTC8Time()}");
                if (GetUTC8Time().Hour != LastCheckHour)
                {
                    LastCheckHour = GetUTC8Time().Hour;
                    logger.LogInformation($"DoExpiredBonus in check LastCheckHour = {LastCheckHour}");
                    //if (GetUTC8Time().Hour < 12) // Check twice a day at 12:00 and 24:00
                    //{
                    //    DateTime.TryParse($"{DateTime.Now:yyyy/MM/dd 12:00:00}", out NextChickTime);
                    //}
                    //else
                    //{
                    //    DateTime.TryParse($"{DateTime.Now.AddDays(1):yyyy/MM/dd 00:00:00}", out NextChickTime);
                    //}

                    List<Task> allTasks = new List<Task>();
                    do
                    {
                        nCount = 0;
                        var data = expiredBonusBLL.GetBonusExpired(LimitNum);
                        foreach (var item in data)
                        {
                            allTasks.Add(Task.Run(() =>
                            {
                                try
                                {
                                    expiredBonusBLL.RefundExpiredBonus(item);
                                }
                                catch (Exception ex1)
                                {
                                    log2File.Error(ex1.ToString(), "ExpiredBonus");
                                }
                            }));

                            if (allTasks.Count >= 4)
                            {
                                await Task.WhenAll(allTasks);
                                allTasks.Clear();
                            }
                        }

                        if (allTasks.Count > 0)
                        {
                            await Task.WhenAll(allTasks);
                            allTasks.Clear();
                        }

                        nCount = data.Count();
                    } while (nCount >= LimitNum);

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), "ExpiredBonus");
            }
            finally
            {
                //timerExpiredBonus.Change(TimeSpan.FromHours(1), TimeSpan.Zero);
                timerExpiredBonus.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }

        }
        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerExpiredBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
