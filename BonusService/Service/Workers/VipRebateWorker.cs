using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class VipRebateWorker : BackgroundService
    {
        private readonly RebateBLL rebateBLL;
        private readonly ILogger<VipRebateWorker> _logger;
        private Timer timer;

        public VipRebateWorker(RebateBLL rebateBLL, ILogger<VipRebateWorker> logger)
        {
            this.rebateBLL = rebateBLL;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            bool isError = false;
            int startTick = 0;
            try
            {
                var rebateTaskList = rebateBLL.GetVipRebateTask();
                foreach (var rebateTask in rebateTaskList)
                {
                    startTick = Environment.TickCount;
                    _logger.LogInformation($"VipRebate SiteId:{rebateTask.SiteId} start ");
                    int pageSize = 1000, pageNumber = 1;
                    var rebatePayoutLimit = rebateBLL.GetRebatePayoutLimit();

                  
                    var rebateInfoList = rebateBLL.GetVipRebateCustList(rebateTask.SiteId, pageSize, pageNumber, rebateTask.SpecificDate);
                    foreach (var rebateInfo in rebateInfoList)
                    {
                            var rebateTpBetRecordList = rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, rebateTask.SpecificDate, 0).ToList();//.ToDictionary<string, RebateTpBetRecord>(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                        if (rebateTask.PeriodType == 3)
                        {
                            var dd = rebateTpBetRecordList.ToDictionary(x => $"{x.SettledDate:yyyyMMdd}_{x.GameType}", x => x);

                            foreach (var rebateTpBetRecord14 in rebateBLL.GetVipRebateTpBetRecord(rebateTask.SiteId, rebateInfo.CustId, rebateTask.SpecificDate, 1))
                            {
                                if (dd.ContainsKey($"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"))
                                {
                                    dd[$"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"].ValidBet += rebateTpBetRecord14.ValidBet;
                                    dd[$"{rebateTpBetRecord14.SettledDate:yyyyMMdd}_{rebateTpBetRecord14.GameType}"].WinlostAmount += rebateTpBetRecord14.WinlostAmount;
                                }
                                else
                                {
                                    rebateTpBetRecordList.Add(rebateTpBetRecord14);
                                }
                            }
                        }

                        if (rebateTpBetRecordList.Count > 0)
                        {
                            rebateBLL.PayoutVipRebate(rebateTask.SiteId, rebateInfo.CustId, rebateInfo.UserName, rebateInfo, rebateTpBetRecordList, rebatePayoutLimit);
                        }
                    }

                    rebateBLL.SetVipRebateTaskDone(rebateTask.SiteId);
                    int spendTime = Environment.TickCount - startTick;
                        
                    _logger.LogInformation($"VipRebateTask SiteId:{rebateTask.SiteId} SpecificDate:{rebateTask.SpecificDate} finish time: {spendTime / 60000}:{(spendTime % 60000) / 1000}.{(spendTime % 60000) % 1000}");
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(60);
                //  if (environment.IsDevelopment())
                //  {
                //      dueTimeSpan = TimeSpan.FromMinutes(1);
                //  }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
