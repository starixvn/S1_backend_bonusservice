using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class RewardWorker : BackgroundService
    {
        private readonly ILogger<RewardWorker> _logger;
        private readonly BonusAPIBLL _bonusApiBll;
        private readonly QueueJobBLL _queueJobBll;
        private Timer _timerClaimReward;

        public RewardWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RewardWorker>>();
            _bonusApiBll = provider.GetRequiredService<BonusAPIBLL>();
            _queueJobBll = provider.GetRequiredService<QueueJobBLL>();
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

                var list = _queueJobBll.GetQueueJob<ClaimVerificationRewardData>(QueueJob.Verification);

                foreach (var data in list)
                {
                    _logger.LogInformation($"Do SetVerificationReward ,CustId:{data.CustId}");
                    
                    var resultSms = _bonusApiBll.SetVerificationReward(data.CustId, OTPTypeEnum.SMS, true);
                    _logger.LogInformation($"SetVerificationReward SMS ,CustId:{data.CustId} ,result:{resultSms.ErrorCode}, {resultSms.Message}");
                    
                    var resultEmail = _bonusApiBll.SetVerificationReward(data.CustId, OTPTypeEnum.Email, true);
                    _logger.LogInformation($"SetVerificationReward Email ,CustId:{data.CustId} ,result:{resultEmail.ErrorCode}, {resultEmail.Message}");

                    _queueJobBll.DeleteQueueJobByCustId(data.CustId);
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