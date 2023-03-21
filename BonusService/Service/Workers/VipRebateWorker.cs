using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using System.Diagnostics;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class VipRebateWorker : BackgroundService
    {
        private readonly RebateBLL _rebateBll;
        private readonly ILogger<VipRebateWorker> _logger;
        private Timer timer;
        private int execCount = 0;

        public VipRebateWorker(IServiceProvider provider)
        {
            _rebateBll = provider.GetRequiredService<RebateBLL>();
            _logger = provider.GetRequiredService<ILogger<VipRebateWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            Interlocked.Increment(ref execCount);

            if (execCount == 1)
            {
                bool isError = false;
                try
                {
                    _logger.LogInformation("VipRebate Wroker：Verify Task Start.");

                    var doTaskList = _rebateBll.VerifyRebateTask();

                    _logger.LogInformation($"VipRebate Wroker：Verify Task End. Count = {doTaskList.Count()}");

                    foreach (var rebateTask in doTaskList)
                    {
                        _logger.LogInformation($"VipRebate Wroker：Do Task Start. SiteId:{rebateTask.SiteId} CurrencyId:{rebateTask.CurrencyId} SpecificDate:{rebateTask.SpecificDate}. ");

                        var sw = Stopwatch.StartNew();
                        var rebatePayoutLimit = _rebateBll.GetRebatePayoutLimit();

                        var rebateInfoList = _rebateBll.GetVipRebateCustList(rebateTask.SiteId, rebateTask.CurrencyId, (int)rebateTask.PeriodType, rebateTask.SpecificDate, rebateTask.TimeZone);
                        foreach (var rebateInfo in rebateInfoList)
                        {
                            var rebateTpBetRecordList = _rebateBll.GetVipRebateTpBetRecord(rebateInfo.CustId, rebateTask.SpecificDate, rebateTask.TimeZone);

                            if (rebateTpBetRecordList != null && rebateTpBetRecordList.Any())
                            {
                                _rebateBll.PayoutVipRebate(rebateTask.SiteId, rebateInfo, rebateTpBetRecordList, rebatePayoutLimit);
                            }
                        }
                        
                        _rebateBll.SetVipRebateTaskDone(rebateTask.SiteId, rebateTask.CurrencyId);

                        _logger.LogInformation($"VipRebate Wroker：Do Task End. SiteId:{rebateTask.SiteId} CurrencyId:{rebateTask.CurrencyId} SpecificDate:{rebateTask.SpecificDate}. \n"
                        + $"Finish Time (m:s.ms): {Math.Round(sw.Elapsed.TotalMinutes)}:{Math.Round(sw.Elapsed.TotalMinutes)}.{Math.Round(sw.Elapsed.TotalMilliseconds)}.");
                    }
                }
                catch (Exception ex)
                {
                    isError = true;
                    _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                }
                finally
                {
                    var dueTimeSpan = isError ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(30);

                    _logger.LogInformation($"VipRebate Task End. isError = {isError}.\n" +
                      $"Next Task Will Start At {DateTime.Now.Add(dueTimeSpan).AddHours(8).ToString("yyyy-MM-ddTHH:mm:ss")}");

                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
            Interlocked.Decrement(ref execCount);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
