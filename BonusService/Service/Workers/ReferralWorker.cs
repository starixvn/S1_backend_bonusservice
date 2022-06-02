using BonusService.Service.BLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BonusService.Service.Model;
using Newtonsoft.Json;

namespace BonusService.Service.Workers
{
    public class ReferralWorker : BackgroundService
    {
        private readonly ILogger<ReferralWorker> _logger;
        private readonly IHostEnvironment environment;
        private readonly ReferralBLL referralBLL;
        private Timer timer;
        public ReferralWorker(ILogger<ReferralWorker> logger, IHostEnvironment environment, ReferralBLL referralBLL)
        {
            _logger = logger;
            this.environment = environment;
            this.referralBLL = referralBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //  Console.WriteLine(selLangBLL.GetResourceValue("lbl_HelloWorld", "en"));
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            var isError = false;

            //_logger.LogCritical($"Worker running at: {environment.EnvironmentName}");

            try
            {
                var settlementList = new Dictionary<int, List<ReferralInfo>>();

                foreach(var referralInfo in referralBLL.GetReferralSettlement())
                {
                    if (!settlementList.ContainsKey(referralInfo.AgentId))
                    {
                        settlementList.Add(referralInfo.AgentId, new List<ReferralInfo>());
                    }

                    settlementList[referralInfo.AgentId].Add(referralInfo);
                }

                foreach(var rr in settlementList)
                {
                    if(referralBLL.PayoutReferral(rr.Value, out var payAmount, out var paidReferralList) > 0)
                    {
                        _logger.LogInformation($"Referral:{JsonConvert.SerializeObject(paidReferralList)}");
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, "ReferralWorker");
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(30);
                if (environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
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
