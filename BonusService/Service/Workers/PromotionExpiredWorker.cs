using BonusService.Service.BLL;
using BonusService.Service.Extensions;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class PromotionExpiredWorker : BackgroundService
    {
        private readonly ILogger<PromotionExpiredWorker> _logger;
        private readonly ExpiredBonusBLL _expiredBonusBll;
        private Timer _timerExpired;
        private readonly Random _random;

        public PromotionExpiredWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PromotionExpiredWorker>>();
            _expiredBonusBll = provider.GetRequiredService<ExpiredBonusBLL>();
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerExpired = new Timer(DoPromotionExpiredTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }    

        private void DoPromotionExpiredTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Expired Promotion Task Async Start");
                var checkTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var promotionList = _expiredBonusBll.GetPromotionExpired(_random.Next(50, 100)); // 避免 ORM 或 db execution plan cache
                foreach (var promotion in promotionList)
                {
                    try
                    {
                        // 檢查過期改狀態
                        if (checkTime > promotion.PeriodTo || checkTime > promotion.DisplayTo)
                        {
                            _expiredBonusBll.SetPromotionStatus(promotion.SiteId, promotion.BonusCode);
                            _logger.LogInformation($"SiteId={promotion.SiteId} | {promotion.BonusCode} is close");
                        }
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
                _timerExpired?.Change(TimeSpan.FromMinutes(30), TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerExpired?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
