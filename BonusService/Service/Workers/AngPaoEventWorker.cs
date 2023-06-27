using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;
using System.Diagnostics;

namespace BonusService.Service.Workers;

public class AngPaoEventWorker : BackgroundService
{
    private readonly AngPaoEventBLL _angPaoEventBLL;
    private readonly ILogger<AngPaoEventWorker> _logger;
    private Timer timer;
    private int execCount = 0;
    private readonly string WorkerName = "AngPaoEvent";

    public AngPaoEventWorker(IServiceProvider provider)
    {
        _angPaoEventBLL = provider.GetRequiredService<AngPaoEventBLL>();
        _logger = provider.GetRequiredService<ILogger<AngPaoEventWorker>>();
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
                if (VerifyExecute(DateTime.Now.Minute))
                {
                    _logger.LogInformation($"{WorkerName} Wroker：Verify Task Start.");

                    var doTaskList = _angPaoEventBLL.VerifyTask(WorkerName);

                    _logger.LogInformation($"{WorkerName} Wroker：Verify Task End. Count = {doTaskList.Count()}");

                    foreach (var task in doTaskList)
                    {
                        _logger.LogInformation($"{WorkerName} Wroker：Do Task Start. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId}. ");

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

                        _logger.LogInformation($"{WorkerName} Wroker：Do Task End. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId} . \n"
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
                var dueTimeSpan = isError ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(CaculateNextExecuteTime(DateTime.Now.Minute));

                _logger.LogInformation($"{WorkerName} Task End. isError = {isError}.\n" +
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
