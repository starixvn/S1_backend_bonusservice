using BonusService.Service.BLL;
using BonusService.Service.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class VipWorker : BackgroundService
    {
        private Timer timer;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private readonly SelLangBLL selLangBLL;
        private readonly VipBLL vipBLL;
        private readonly ILogger<VipWorker> logger;

        public VipWorker(ILogger<VipWorker> logger, IHostEnvironment environment, Common tools, Log2File log2File, SelLangBLL selLangBLL, VipBLL vipBLL)
        {
            this.environment = environment;
            this.log2File = log2File;
            this.selLangBLL = selLangBLL;
            this.vipBLL = vipBLL;
            this.logger = logger;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            bool isError = false;

            try
            {
                foreach(var vipSystem in vipBLL.GetVipSystem().OrderByDescending(x => x.Level))
                {
                    foreach (var custVip in vipBLL.GetCustUpgrade(vipSystem.Level, vipSystem.UpDeposit, vipSystem.SiteId, vipSystem.CurrencyId))
                    {
                        vipBLL.SetCustVip(custVip.VipLevel, vipSystem.Level, custVip.CustId, custVip.UserName, vipSystem.SiteId);
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                log2File.Error(ex.ToString(), "Vip");
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
