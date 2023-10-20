using System.Diagnostics;

using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model;

using Newtonsoft.Json;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class DepositReferralWorker : BackgroundService
    {
        private readonly CustInfoBLL _custInfoBLL;
        private readonly ReferralBLL _referralBLL;
        private Timer _timer;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<DepositReferralWorker> _logger;
        private readonly string WorkerName = "DepositReferral";

        public DepositReferralWorker(IServiceProvider provider)
        {
            _custInfoBLL = provider.GetService<CustInfoBLL>();
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

            _logger.LogInformation($"{WorkerName} Wroker Start.");

            bool isError = false;
            try
            {
                if (VerifyExecute(DateTime.Now.Minute))
                {
                    var doTaskList = _referralBLL.VerifyTask(WorkerName);

                    foreach (var task in doTaskList)
                    {
                        _logger.LogInformation($"{WorkerName} Wroker：Do Task Start. SiteId:{task.SiteId} CurrencyId:{task.CurrencyId}. ");

                        var sw = Stopwatch.StartNew();

                        var depositReferralCustList = _referralBLL.GetDepositReferralCustList(task.SiteId, task.CurrencyId, task.SpecificDate, task.TimeZone);

                        var referrerCustList = depositReferralCustList.Select(x => x.Referrer_CustId).Distinct();

                        foreach (var custId in referrerCustList)
                        {
                            var referrerInfo = _custInfoBLL.GetCustInfo(custId);
                            var referredDetails = depositReferralCustList.Where(x => x.Referrer_CustId == custId);

                            //檢查推薦人是否可以獲得
                            if (!_referralBLL.IsReferrerValid(referrerInfo)) continue;

                            //塞選可納入計算的被推薦人資料
                            _referralBLL.FilterReferredDetail(referrerInfo, referredDetails, out IEnumerable<DepositReferralCustInfo> filteredReferredDetail);

                            if (!filteredReferredDetail.Any()) continue;

                            //計算推薦獎金
                            var caculateResult = _referralBLL.CaculateDepositReferral(filteredReferredDetail);

                            //派發
                            if (caculateResult != null && _referralBLL.PayoutDepositReferral(referrerInfo, caculateResult) > 0)
                            {
                                _logger.LogInformation($"{WorkerName} : {referrerInfo.CustId}_{referrerInfo.UserName} Reward Success. \n" +
                                    $"Detail = {JsonConvert.SerializeObject(caculateResult)}");
                            }
                        }

                        _referralBLL.SetTaskDone(task.SiteId, task.CurrencyId, WorkerName);

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
                var dueTimeSpan = isError ? TimeSpan.FromMinutes(10)
                    : TimeSpan.FromMinutes(CaculateNextExecuteTime(DateTime.Now.Minute));

                _logger.LogInformation($"{WorkerName} End. isError = {isError}.\n" +
                  $"Next Will Start At {DateTime.Now.Add(dueTimeSpan).AddHours(8).ToString("yyyy-MM-ddTHH:mm:ss")}");
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
