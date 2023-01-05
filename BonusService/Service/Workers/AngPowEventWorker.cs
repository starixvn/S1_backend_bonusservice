using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;
using System.Diagnostics;

namespace BonusService.Service.Workers;

public class AngPowEventWorker : BackgroundService
{
    private readonly AngPowEventBLL _angPowEventBLL;
    private readonly ILogger<VipRebateWorker> _logger;
    private Timer timer;
    private int execCount = 0;

    public AngPowEventWorker(IServiceProvider provider)
    {
        _angPowEventBLL = provider.GetRequiredService<AngPowEventBLL>();
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
                if (VerifyExecute(DateTime.Now.Minute))
                {
                    _logger.LogInformation("AngPowEvent Wroker：Verify Task Start.");

                    var doTaskList = _angPowEventBLL.VerifyTask("AngPowEvent");

                    _logger.LogInformation($"AngPowEvent Wroker：Verify Task End. Count = {doTaskList.Count()}");

                    foreach (var task in doTaskList)
                    {
                        _logger.LogInformation($"AngPowEvent Wroker：Do Task Start. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId}. ");

                        var sw = Stopwatch.StartNew();
                        int pageSize = 1000, pageNumber = 1;
                        var isRemain = true;

                        while (isRemain)
                        {
                            var custInfoList = _angPowEventBLL.GetCustList(task.SiteId, task.CurrencyId, task.TimeZone, pageNumber, pageSize);
                            isRemain = custInfoList.Count() >= pageSize;
                            foreach (var custInfo in custInfoList)
                            {
                                var drawEventTpBetRecord = _angPowEventBLL.GetTpBetRecord(custInfo.CustId, task.TimeZone);

                                if (drawEventTpBetRecord != null)
                                {
                                    _angPowEventBLL.GenTicket(task.SiteId, task.CurrencyId, custInfo, drawEventTpBetRecord);
                                }
                            }
                        }
                        _angPowEventBLL.SetTaskDone(task.SiteId, task.CurrencyId, "AngPowEvent");

                        _logger.LogInformation($"AngPowEvent Wroker：Do Task End. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId} . \n"
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
                //TODO：要去計算下一個排成的時間(校時)
                var dueTimeSpan = isError ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(CaculateNextExecuteTime(DateTime.Now.Minute));

                _logger.LogInformation($"AngPowEvent Task End. isError = {isError}.\n" +
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
