using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                        int pageSize = 1000, pageNumber = 1;
                        var isRemain = true;
                        var rebatePayoutLimit = _rebateBll.GetRebatePayoutLimit();

                        while (isRemain)
                        {
                            var rebateInfoList = _rebateBll.GetVipRebateCustList(rebateTask.SiteId, rebateTask.CurrencyId, (int)rebateTask.PeriodType, rebateTask.SpecificDate, rebateTask.TimeZone, pageNumber, pageSize);
                            isRemain = rebateInfoList.Count() > pageSize;
                            foreach (var rebateInfo in rebateInfoList)
                            {
                                var rebateTpBetRecordList = _rebateBll.GetVipRebateTpBetRecord((int)rebateTask.PeriodType, rebateInfo.CustId, rebateTask.SpecificDate, rebateTask.TimeZone, 0).ToList();

                                if (rebateTpBetRecordList.Count > 0)
                                {
                                    _rebateBll.PayoutVipRebate(rebateTask.SiteId, rebateInfo, rebateTpBetRecordList, rebatePayoutLimit);
                                }
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

                    _logger.LogInformation($"Claim Vip Rebate Task End. isError={isError}.\n" +
                      $"Next Task Will Start At {DateTime.Now.Add(dueTimeSpan).ToString("yyyy-MM-ddTHH:mm:ss")}");
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
