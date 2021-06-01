using BonusService.Service.BLL;
using BonusService.Service.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class DepositBonusWorker : BackgroundService
    {
        private readonly ILogger<DepositBonusWorker> logger;
        private readonly Log2File log2File;
        private readonly DepositBonusBLL depositBonusBLL;
        private Timer timerDepositBonus;
        private DateTime LastChickDate;

        public DepositBonusWorker(ILogger<DepositBonusWorker> logger, Log2File log2File, DepositBonusBLL depositBonusBLL)
        {
            this.logger = logger;
            this.log2File = log2File;
            this.depositBonusBLL = depositBonusBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerDepositBonus = new Timer(DoDepositBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoDepositBonusTaskAsync(object state)
        {
            try
            {
                //logger.LogInformation("DoDepositBonusTaskAsync start");
                if (LastChickDate != GetUTC8Time().Date)
                {
                    LastChickDate = GetUTC8Time().Date;
                    //logger.LogInformation("DoDepositBonus CheckExpired");
                    depositBonusBLL.CheckDepositBonusExpired();
                }
                var depositBonusList = depositBonusBLL.GetDepositBonusQueue();
                //logger.LogInformation($"DoDepositBonus  count: {depositBonusList.Count()}");
                foreach (var depositBonus in depositBonusList)
                {
                    var rtn = await depositBonusBLL.SetClaimPromotionAsync(depositBonus.CustId, depositBonus.BonusCode, depositBonus.SiteId, depositBonus.UserName, depositBonus.CurrencyId, depositBonus.LockedAmount, depositBonus.DeviceType);
                    if (rtn)
                    {
                        logger.LogInformation($"DoDepositBonus  success TransId: {depositBonus.TransId}");
                        depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, 2);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), "DepositBonus");
            }
            finally
            {
                timerDepositBonus.Change(TimeSpan.FromMinutes(3), TimeSpan.Zero);
            }

        }
        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerDepositBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
