using BonusService.Service.BLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.Workers
{
    public partial class VipUpgradeWorker : BackgroundService
    {
        private Timer _timer;
        private readonly IHostEnvironment _environment;
        private readonly VipBLL _vipBll;
        private readonly ILogger<VipUpgradeWorker> _logger;

        public VipUpgradeWorker(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _vipBll = provider.GetRequiredService<VipBLL>();
            _logger = provider.GetRequiredService<ILogger<VipUpgradeWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(VipDoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private void VipDoWork(object state)
        {
            var success = true;
                    
            try
            {
                var vipSettings = _vipBll.GetVipSettings(VipTypeEnum.Upgrade);
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
                success = false;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                if (_environment.IsProduction())
                {
                    dueTimeSpan = TimeSpan.FromHours(4);
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
