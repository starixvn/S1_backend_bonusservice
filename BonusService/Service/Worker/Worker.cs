using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.BLL;
using BonusService.Service.DAL;

namespace BonusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly BonusCashBackBLL bonusCashBackBLL;
        private readonly WalletService walletService;

        public Worker(ILogger<Worker> logger , BonusCashBackBLL bonusCashBackBLL, WalletService walletService)
        {
            _logger = logger;
            this.bonusCashBackBLL = bonusCashBackBLL;
            this.walletService = walletService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            bonusCashBackBLL.BonusCashBackInfo();
            //foreach(var cashBack in bonusCashBackBLL.BonusCashBackInfo())
            //{
            //    walletService.SetBalance()
            //}


            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
