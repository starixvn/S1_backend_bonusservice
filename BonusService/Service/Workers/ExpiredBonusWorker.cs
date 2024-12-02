using BonusService.Service.BLL;
using BonusService.Service.Extensions;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class ExpiredBonusWorker : BackgroundService
    {
        private readonly ILogger<ExpiredBonusWorker> _logger;
        private readonly ExpiredBonusBLL _expiredBonusBll;
        private Timer _timerExpiredBonus;
        private readonly Random _random;

        public ExpiredBonusWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ExpiredBonusWorker>>();
            _expiredBonusBll = provider.GetRequiredService<ExpiredBonusBLL>();
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerExpiredBonus = new Timer(DoExpiredBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoExpiredBonusTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Expired Bonus Task Async Start");
                var data = _expiredBonusBll.GetBonusExpired(_random.Next(950, 1000)); // 避免 ORM 或 db execution plan cache
                _logger.LogInformation($"GetBonusExpired Data Count：{data.Count()}");
                foreach (var item in data)
                {
                    try
                    {
                        _expiredBonusBll.RefundExpiredBonus(item,
                            $"CancelBonus: BonusId={item.BonusId}, Remaining Bonus={item.Bonus:#,##0.##}, Refund Amount={item.Refund:#,##0.##}");
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
                _timerExpiredBonus?.Change(TimeSpan.FromMinutes(2), TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerExpiredBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
