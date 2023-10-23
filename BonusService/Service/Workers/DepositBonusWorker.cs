using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;

using Newtonsoft.Json;

using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class DepositBonusWorker : BackgroundService
    {
        private readonly ILogger<DepositBonusWorker> logger;
        private readonly DepositBonusBLL depositBonusBLL;
        private readonly PromotionBll _promotionBll;
        private Timer timerDepositBonus;
        private DateTime LastChickDate;
        private readonly Random _random;

        public DepositBonusWorker(ILogger<DepositBonusWorker> logger, DepositBonusBLL depositBonusBLL, PromotionBll promotionBll)
        {
            this.logger = logger;
            this.depositBonusBLL = depositBonusBLL;
            _promotionBll = promotionBll;
            _random = new Random();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerDepositBonus = new Timer(DoDepositBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoDepositBonusTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                if (LastChickDate != GetUTC8Time().Date)
                {
                    LastChickDate = GetUTC8Time().Date;
                    depositBonusBLL.CheckDepositBonusExpired();
                }

                var depositBonusQueueCustList = depositBonusBLL.GetDepositBonusQueueCustList(_random.Next(950, 1000));
                logger.LogInformation($"GetDepositBonusQueueCustList Data Count：{depositBonusQueueCustList?.Count()}");

                foreach (var depositBonusQueue in depositBonusQueueCustList)
                {
                    var depositBonusList = depositBonusBLL.GetDepositBonusQueueByCustId(depositBonusQueue.CustId, _random.Next(50, 100));
                    foreach (var depositBonus in depositBonusList)
                    {
                        try
                        {
                            if (depositBonus.DepositStatus == (int)DepositStatusEnum.Approved)
                            {
                                var result = await _promotionBll.SetClaimPromotionV2Async(depositBonus.CustId, depositBonus.BonusCode, depositBonus.IP,
                                    depositBonus.LockedAmount, (int)PromotionDeviceType.All, BonusInfoAtPage.Deposit, depositBonus.TransId);
                                if (result.ErrorCode == 0)
                                {
                                    logger.LogInformation($"CustId={depositBonusQueue.CustId} DoDepositBonus success TransId: {depositBonus.TransId}. SetClaimPromotion result={JsonConvert.SerializeObject(result)}");
                                    depositBonusBLL.SetDepositBonusFinish(depositBonus.TransId, depositBonusQueue.CustId, _random.Next(100, 150));
                                    break;
                                }

                                depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_not_eligible);
                            }
                            else if (depositBonus.DepositStatus is (int)DepositStatusEnum.Rejected or (int)DepositStatusEnum.Canceled or (int)DepositStatusEnum.Expired or (int)DepositStatusEnum.Error)
                            {
                                logger.LogInformation($"CustId={depositBonusQueue.CustId} TransId={depositBonus.TransId}. DepositStatus={Enum.GetName(typeof(DepositStatusEnum), depositBonus.DepositStatus)}. Change DepositBonusQueueStatus To Cancel_Rejected.");
                                depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_Rejected);
                            }
                            else
                                logger.LogInformation($"CustId={depositBonusQueue.CustId} TransId={depositBonus.TransId}. DepositStatus={Enum.GetName(typeof(DepositStatusEnum), depositBonus.DepositStatus)}. Do nothing.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"depositBonus：{JsonConvert.SerializeObject(depositBonus)}，exception：{ex.Message}");
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
                timerDepositBonus.Change(TimeSpan.FromSeconds(15), TimeSpan.Zero);
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
