using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class VipDownGradeWorker : BackgroundService
    {
        #region Fields

        private Timer _timer;
        private ILogger<VipDownGradeWorker> _logger;
        private IHostEnvironment _environment;
        private VipBLL _vipBLL;

        private int execCount = 0;

        #endregion

        #region Constructors

        public VipDownGradeWorker(ILogger<VipDownGradeWorker> logger,
                                  IHostEnvironment environment,
                                  VipBLL vipBLL)
        {
            _logger = logger;
            _environment = environment;
            _vipBLL = vipBLL;
        }

        #endregion

        #region Actions

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoVipDownGradeWorker, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoVipDownGradeWorker(object state)
        {
            Interlocked.Increment(ref execCount);

            if (execCount == 1)
            {
                var success = true;

                try
                {
                    var vipSettings = _vipBLL.GetVipSettings(VipTypeEnum.Downgrade);
                    var currentTime = DateTime.UtcNow.AddHours(8);

                    _vipBLL.DowngradeReview(vipSettings,
                                            currentTime,
                                            out var downgradeReviewSuccess);

                    _vipBLL.DowngradeCustomer(vipSettings,
                                              currentTime,
                                              out var downgradeCustomerSuccess);

                    if (!downgradeReviewSuccess || !downgradeCustomerSuccess)
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
            Interlocked.Decrement(ref execCount);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        #endregion

    }
}
