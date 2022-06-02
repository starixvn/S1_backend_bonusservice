using BonusService.Service.BLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;

namespace BonusService.Service.Workers
{
    public class DepositBonusWorker : BackgroundService
    {
        private readonly ILogger<DepositBonusWorker> logger;
        private readonly DepositBonusBLL depositBonusBLL;
        private readonly BonusAPIBLLV2 bonusAPIBLLV2;
        private Timer timerDepositBonus;
        private DateTime LastChickDate;

        public DepositBonusWorker(ILogger<DepositBonusWorker> logger, DepositBonusBLL depositBonusBLL, BonusAPIBLLV2 bonusAPIBLLV2)
        {
            this.logger = logger;
            this.depositBonusBLL = depositBonusBLL;
            this.bonusAPIBLLV2 = bonusAPIBLLV2;
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
                if (LastChickDate != GetUTC8Time().Date)
                {
                    LastChickDate = GetUTC8Time().Date;
                    depositBonusBLL.CheckDepositBonusExpired();
                }

                var depositBonusQueueCustList = depositBonusBLL.GetDepositBonusQueueCustList();
                foreach (var depositBonusQueue in depositBonusQueueCustList)
                {
                    var depositBonusList = depositBonusBLL.GetDepositBonusQueueByCustId(depositBonusQueue.CustId);
                    foreach (var depositBonus in depositBonusList)
                    {
                        
                        if (depositBonus.DepositStatus == (int)DepositStatusEnum.Approved)
                        {
                            var result = await bonusAPIBLLV2.SetClaimPromotionV2Async(depositBonus.CustId, depositBonus.BonusCode, depositBonus.IP,
                                depositBonus.LockedAmount, 1, BonusInfoAtPage.Deposit, false, depositBonus.TransId);
                            if (result.ErrorCode == 0)
                            {
                                logger.LogInformation($"DoDepositBonus success TransId: {depositBonus.TransId}");
                                depositBonusBLL.SetDepositBonusFinish(depositBonus.TransId, depositBonusQueue.CustId);
                                break;
                            }

                            depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_not_eligible);
                        }
                       
                        else if (depositBonus.DepositStatus == (int)DepositStatusEnum.Rejected || depositBonus.DepositStatus == (int)DepositStatusEnum.Canceled || depositBonus.DepositStatus == (int)DepositStatusEnum.Expired || depositBonus.DepositStatus == (int)DepositStatusEnum.Error )
                        {
                            depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_Rejected);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                timerDepositBonus.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
            }

        }
 
        private DateTime GetUTC8Time()
        {
            return DateTime.UtcNow.AddHours(8);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerDepositBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
