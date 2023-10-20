using BonusService.Service.BLL;
using BonusService.Service.Extensions;

using Newtonsoft.Json;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class PromotionAutoExtendWorker : BackgroundService
    {
        private readonly ILogger<PromotionAutoExtendWorker> _logger;
        private readonly PromotionAutoExtendBLL _promotionAutoExtendBll;
        private Timer _timerPromotionAutoExtend;
        private readonly string _specificDate = null;
        private readonly Random _random;

        public PromotionAutoExtendWorker(ILogger<PromotionAutoExtendWorker> logger, PromotionAutoExtendBLL promotionAutoExtendBll)
        {
            _logger = logger;
            _promotionAutoExtendBll = promotionAutoExtendBll;
            _random = new Random();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //this.SpecificDate = "2021-06-04 03:00:00";
            _timerPromotionAutoExtend = new Timer(PromotionAutoExtendTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private void PromotionAutoExtendTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var success = 0;
            var fail = 0;
            try
            {
                _logger.LogInformation("Promotion Auto Extend Task Start");
                var data = _promotionAutoExtendBll.GetAutoExtendPromotionExpiredList(_specificDate, _random.Next(950, 1000));
                _logger.LogInformation($"GetTransferBonusExpired Data Count：{data.Count()}");
                if (data.Any())
                {
                    foreach (var item in data)
                    {
                        try
                        {
                            var result = _promotionAutoExtendBll.SetPromotionExtend(item);
                            var status = result > 0 ? "Success" : "Fail";
                            _logger.LogInformation($"Promotion Auto Extend Status = {status}, Value = {JsonConvert.SerializeObject(item)}");

                            if (result > 0)
                            {
                                success++;
                            }
                            else
                            {
                                fail++;
                            }
                        }
                        catch (Exception ex)
                        {
                            fail++;
                            _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
                        }
                    }
                }

                _logger.LogInformation($"Promotion Auto Extend Task finish in {DateTime.UtcNow.AddHours(8)}, Total = {data.Count()}, Success = {success}, Fail = {fail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                _timerPromotionAutoExtend.Change(TimeSpan.FromHours(1), TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerPromotionAutoExtend?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}