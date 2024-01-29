using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class BonusCancelResetWorker : BackgroundService
    {
        private readonly ILogger<BonusCancelResetWorker> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private Timer timer;
        private readonly Random _random;

        public BonusCancelResetWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<BonusCancelResetWorker>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            try
            {
                var bonusCancelResetList =  _reportDbClient.Query<BonusCancelResetDto>("Service_CancelBonus_AmountCheck_Get", new { Limit = _random.Next(50, 100) });
                foreach (var cancelBonus in bonusCancelResetList)
                {
                    _mainDbClient.Execute("Service_SeamlessBonus_CancelReset", cancelBonus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BonusCancelResetWorker");
            }
            finally
            {
                timer.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
