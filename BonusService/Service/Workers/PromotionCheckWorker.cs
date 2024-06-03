using BonusService.Service.DAL;
using BonusService.Service.Extensions;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class PromotionCheckWorker : BackgroundService
    {
        private readonly ILogger<PromotionCheckWorker> _logger;
        private readonly PromotionCheckService _promotionCheckService;
        private Timer _timer;
        private readonly Random _random;

        public PromotionCheckWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PromotionCheckWorker>>();
            _promotionCheckService = provider.GetRequiredService<PromotionCheckService>();
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoPromotionCheckTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }    

        private void DoPromotionCheckTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Expired Promotion Task Async Start");
                var checkTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var checkList = _promotionCheckService.GetPromotionCheckList(_random.Next(500, 600)); // 避免 ORM 或 db execution plan cache
                foreach (var promotion in checkList)
                {
                    try
                    {
                        // 檢查1：沒有AutoExtend的檢查是否過期，過期關閉
                        if (!promotion.AutoExtend &&
                            (checkTime > promotion.PeriodTo || checkTime > promotion.DisplayTo))
                        {
                            _promotionCheckService.SetPromotionStatus(promotion.SiteId, promotion.BonusCode);
                            _logger.LogInformation($"SiteId={promotion.SiteId} | {promotion.BonusCode} is close");
                        }

                        //　檢查2：有效結束日期跟顯示結束日期不一致的，更新為有效結束日期
                        //if (promotion.PeriodTo != promotion.DisplayTo)
                        //{
                        //    _promotionCheckService.SetPromotionEndTime(promotion.SiteId, promotion.BonusCode, promotion.PeriodTo);
                        //    _logger.LogInformation($"SiteId={promotion.SiteId} | {promotion.BonusCode} DisplayTo Update to {promotion.PeriodTo}");
                        //}
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                _timer?.Change(TimeSpan.FromMinutes(30), TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
