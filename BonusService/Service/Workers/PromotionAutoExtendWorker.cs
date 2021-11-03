using BonusService.Service.BLL;
using BonusService.Service.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Extensions;

namespace BonusService.Service.Workers
{
    public class PromotionAutoExtendWorker : BackgroundService
    {
        private readonly ILogger<PromotionAutoExtendWorker> logger;
        private readonly PromotionAutoExtendBLL promotionAutoExtendBLL;
        private Timer timerPromotionAutoExtend;
        private DateTime ExecutTime;
        private string SpecificDate = null;

        public PromotionAutoExtendWorker(ILogger<PromotionAutoExtendWorker> logger, PromotionAutoExtendBLL promotionAutoExtendBLL)
        {
            this.logger = logger;
            this.promotionAutoExtendBLL = promotionAutoExtendBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //this.SpecificDate = "2021-06-04 03:00:00";
            timerPromotionAutoExtend = new Timer(PromotionAutoExtendTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void PromotionAutoExtendTaskAsync(object state)
        {
            int nCount = 0;
            try
            {
                if (GetUTC8Time().Date != this.ExecutTime.Date)
                {
                    nCount = 0;
                    var data = promotionAutoExtendBLL.GetAutoExtendPromotionExpiredList(this.SpecificDate);
                    nCount = data.Count();
                    if (data.Any())
                    {
                        List<Task> allTasks = new List<Task>();
                        foreach (var item in data)
                        {
                            //promotionAutoExtendBLL.SetPromotionExtend(item);
                            allTasks.Add(Task.Run(() =>
                            {
                                try
                                {
                                    promotionAutoExtendBLL.SetPromotionExtend(item);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
                                }
                            }));

                            if (allTasks.Count >= 4)
                            {
                                await Task.WhenAll(allTasks);
                                allTasks.Clear();
                            }
                        }
                        

                    }
                   
                    this.ExecutTime = GetUTC8Time();
                    Console.WriteLine($"Promotion Auto Extend finish in {this.ExecutTime} nCount = {nCount}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {

                timerPromotionAutoExtend.Change(TimeSpan.FromHours(1), TimeSpan.Zero);
                //timerPromotionAutoExtend.Change(TimeSpan.FromMinutes(5), TimeSpan.Zero);
            }

        }
        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerPromotionAutoExtend?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}