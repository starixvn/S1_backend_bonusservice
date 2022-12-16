using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;

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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var nCount = 0;
            try
            {
                if (GetUTC8Time().Date != ExecutTime.Date)
                {
                    nCount = 0;
                    var data = promotionAutoExtendBLL.GetAutoExtendPromotionExpiredList(SpecificDate);
                    nCount = data.Count();
                    if (data.Any())
                    {
                        var allTasks = new List<Task>();
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
                   
                    ExecutTime = GetUTC8Time();
                    Console.WriteLine($"Promotion Auto Extend finish in {ExecutTime} nCount = {nCount}");
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