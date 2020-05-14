using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.BLL;

namespace BonusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly BonusCashBackBLL bonusCashBackBLL;

        public Worker(ILogger<Worker> logger , BonusCashBackBLL bonusCashBackBLL)
        {
            _logger = logger;
            this.bonusCashBackBLL = bonusCashBackBLL;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            bonusCashBackBLL.BonusCashBackInfo();

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
