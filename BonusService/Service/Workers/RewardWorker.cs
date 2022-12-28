using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class RewardWorker : BackgroundService
    {
        private readonly ILogger<RewardWorker> _logger;
        private readonly BonusAPIBLL _bonusApiBll;
        private readonly RewardBLL _rewardBll;
        private Timer _timerClaimReward;

        public RewardWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RewardWorker>>();
            _bonusApiBll = provider.GetRequiredService<BonusAPIBLL>();
            _rewardBll = provider.GetRequiredService<RewardBLL>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerClaimReward = new Timer(DoClaimVerificationRewardTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoClaimVerificationRewardTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Claim Verification Reward Task Start");
                
                var list = _rewardBll.GetVerificationRewardData();

                foreach (var data in list)
                {
                    if (data.Reward == null)
                    {
                        _bonusApiBll.SetVerificationReward(data.CustId, data.Type , true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                _timerClaimReward.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
                _logger.LogInformation("Claim Verification Reward Task End");
            }
        }
        
    }
}