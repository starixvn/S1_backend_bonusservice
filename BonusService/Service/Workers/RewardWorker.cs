using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
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
        private readonly ICacheHelper _redisHelper;

        public RewardWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RewardWorker>>();
            _bonusApiBll = provider.GetRequiredService<BonusAPIBLL>();
            _queueJobBll = provider.GetRequiredService<QueueJobBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerClaimReward = new Timer(DoClaimVerificationRewardTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoClaimVerificationRewardTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Claim Verification Reward Task Start");

                var list = _queueJobBll.GetQueueJob<ClaimVerificationRewardData>(QueueJob.Verification);

                foreach (var data in list)
                {
                    _logger.LogInformation($"Do SetVerificationReward ,CustId:{data.CustId}");

                    var sms = new SetVerificationRewardReq()
                    {
                        CustId = data.CustId,
                        OTPType = OTPTypeEnum.SMS
                    };
                    var resultSms = _bonusApiBll.SetVerificationReward(sms, true);
                    _logger.LogInformation($"SetVerificationReward SMS ,CustId:{data.CustId} ,result:{resultSms.ErrorCode}, {resultSms.Message}");

                    var email = new SetVerificationRewardReq()
                    {
                        CustId = data.CustId,
                        OTPType = OTPTypeEnum.Email
                    };
                    var resultEmail = _bonusApiBll.SetVerificationReward(email, true);
                    _logger.LogInformation($"SetVerificationReward Email ,CustId:{data.CustId} ,result:{resultEmail.ErrorCode}, {resultEmail.Message}");

                    _queueJobBll.DeleteQueueJobByCustId(data.CustId ,QueueJob.Verification);
                }

                var failList = _redisHelper.GetListByRange<SetVerificationRewardReq>("SetVerificationReward:ExceptionList", 0, -1);
                foreach (var fail in failList) 
                {
                    var resultFail = _bonusApiBll.SetVerificationReward(fail, true);
                    _logger.LogInformation($"SetVerificationReward SMS ,CustId:{fail.CustId} ,result:{resultFail.ErrorCode}, {resultFail.Message}");
                    if (resultFail.ErrorCode == 0)
                    {
                      await _redisHelper.ListLeftPopAsync("SetVerificationReward:ExceptionList");
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