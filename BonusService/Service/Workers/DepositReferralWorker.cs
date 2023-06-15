using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;
using System.Diagnostics;

namespace BonusService.Service.Workers
{
    public class DepositReferralWorker : BackgroundService
    {
        private readonly ReferralBLL _referralBLL;
        private Timer _timer;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<DepositReferralWorker> _logger;

        public DepositReferralWorker(IServiceProvider provider)
        {
            _referralBLL = provider.GetService<ReferralBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<DepositReferralWorker>>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            bool isError = false;
            try
            {
                if (VerifyExecute(DateTime.Now.Minute))
                {
                    _logger.LogInformation($"DepositReferral Wroker：Verify Task Start.");

                    var doTaskList = _referralBLL.VerifyTask(WorkerName);

                    _logger.LogInformation($"DepositReferral Wroker：Verify Task End. Count = {doTaskList.Count()}");

                    foreach (var task in doTaskList)
                    {
                        _logger.LogInformation($"DepositReferral Wroker：Do Task Start. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId}. ");

                        var sw = Stopwatch.StartNew();

                        var custInfoList = _angPaoEventBLL.GetCustList(task.SiteId, task.CurrencyId, task.TimeZone);

                        foreach (var custInfo in custInfoList)
                        {
                            var drawEventTpBetRecord = _angPaoEventBLL.GetTpBetRecord(custInfo.CustId, task.TimeZone);

                            if (drawEventTpBetRecord != null)
                            {
                                _angPaoEventBLL.GenTicket(task.SiteId, task.CurrencyId, custInfo, drawEventTpBetRecord);
                            }
                        }
                        _angPaoEventBLL.SetTaskDone(task.SiteId, task.CurrencyId, WorkerName);

                        _logger.LogInformation($"DepositReferral Wroker：Do Task End. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId} . \n"
                        + $"Finish Time (m:s.ms): {Math.Round(sw.Elapsed.TotalMinutes)}:{Math.Round(sw.Elapsed.TotalMinutes)}.{Math.Round(sw.Elapsed.TotalMilliseconds)}.");
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = isError ? TimeSpan.FromMinutes(10)
                    : TimeSpan.FromMinutes(CaculateNextExecuteTime(DateTime.Now.Minute));

                _logger.LogInformation($"{WorkerName} Task End. isError = {isError}.\n" +
                  $"Next Task Will Start At {DateTime.Now.Add(dueTimeSpan).AddHours(8).ToString("yyyy-MM-ddTHH:mm:ss")}");
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 每小時的0~20分不跑排程
        /// </summary>
        /// <returns></returns>
        private bool VerifyExecute(int mins)
        {
            if (mins > 0 && mins < 20)
                return false;
            return true;
        }

        private int CaculateNextExecuteTime(int mins)
        {
            if (mins > 0 && mins < 10)
                return 20;
            else if (mins > 10 && mins < 20)
                return 10;
            else
                return 30;
        }
    }
}
