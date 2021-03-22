using BonusService.Service.BLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Extensions;

namespace BonusService.Service.Workers
{
    public partial class VipWorker : BackgroundService
    {
        private Timer _timer;
        private readonly IHostEnvironment _environment;
        private readonly VipBLL _vipBll;
        private readonly ILogger<VipWorker> _logger;

        public VipWorker(ILogger<VipWorker> logger, IHostEnvironment environment, VipBLL vipBll)
        {
            _environment = environment;
            _vipBll = vipBll;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(@"VIP Worker Start");
            _timer = new Timer(VipDoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private void VipDoWork(object state)
        {
            var success = true;
                    
            try
            {
                Console.WriteLine(@"GetVipSettings Start");
                var vipSettings = _vipBll.GetVipSettings().ToList();
                var currentTime = DateTime.UtcNow.AddHours(8);
                _vipBll.UpgradeReview(vipSettings, currentTime, out var upgradeReviewSuccess);
                _vipBll.UpgradeCustomer(vipSettings, currentTime, out var upgradeCustomerSuccess);
                
                if (!upgradeReviewSuccess || !upgradeCustomerSuccess)
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"VIPWorker Exception:{ex.Message}");
                success = false;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                Console.WriteLine(@"VIPWorker is done");
                var dueTimeSpan = TimeSpan.FromHours(4);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (!success)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
