using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class VipCashBackWorker: BackgroundService
    {
        private Timer _timer;
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
            _timer = new Timer(CashBackDoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void CashBackDoWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var success = false;
                    
            try
            {
                //success = _vipCashBackBLL.CalculateCashBack();
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

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}