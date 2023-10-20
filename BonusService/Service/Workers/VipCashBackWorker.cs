using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class VipCashBackWorker : BackgroundService
    {
        private Timer _cashBackWeekTimer;
        private Timer _cashBackDailyTimer;
        private readonly IHostEnvironment _environment;
        private readonly VipCashBackBLL _vipCashBackBLL;
        private readonly ILogger<VipCashBackWorker> _logger;

        public VipCashBackWorker(ILogger<VipCashBackWorker> logger, IHostEnvironment environment, VipCashBackBLL vipCashBackBll)
        {
            _environment = environment;
            _vipCashBackBLL = vipCashBackBll;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cashBackWeekTimer = new Timer(DoCashBackByWeek, null, TimeSpan.Zero, TimeSpan.Zero); //週返
            _cashBackDailyTimer = new Timer(DoCashBackByDaily, null, TimeSpan.Zero, TimeSpan.Zero); //日返
            return Task.CompletedTask;
        }

        private void DoCashBackByWeek(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var success = false;

            try
            {
                success = _vipCashBackBLL.CalculateCashBack(VipPeriodType.Weekly);
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (!success)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }

                _cashBackWeekTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoCashBackByDaily(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var success = false;

            try
            {
                success = _vipCashBackBLL.CalculateCashBack(VipPeriodType.Daily);
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (!success)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }

                _cashBackDailyTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}