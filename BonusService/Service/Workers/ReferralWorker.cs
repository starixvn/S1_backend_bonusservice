using BonusService.Service.BLL;
using BonusService.Service.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BonusService.Service.Model;

namespace BonusService.Service.Workers
{
    public class ReferralWorker : BackgroundService
    {
        private readonly ILogger<RebateWorker> _logger;
        private readonly Common tools;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private readonly SelLangBLL selLangBLL;
        private readonly ReferralBLL referralBLL;
        private Timer timer;
        public ReferralWorker(ILogger<RebateWorker> logger, IHostEnvironment environment, Common tools, Log2File log2File, SelLangBLL selLangBLL, ReferralBLL referralBLL)
        {
            _logger = logger;
            this.tools = tools;
            this.log2File = log2File;
            this.environment = environment;
            this.selLangBLL = selLangBLL;
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
            bool isError = false;

            //_logger.LogCritical($"Worker running at: {environment.EnvironmentName}");

            try
            {
                Dictionary<int, List<ReferralInfo>> settlementList = new Dictionary<int, List<ReferralInfo>>();

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
                    if(referralBLL.PayoutReferral(rr.Value, out decimal payAmount, out List<ReferralInfo> paidReferralList) > 0)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex.ToString());
                log2File.Error(ex.ToString(), "Referral");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);
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
