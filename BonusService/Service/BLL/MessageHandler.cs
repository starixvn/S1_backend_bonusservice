using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Newtonsoft.Json;
using Serilog.Context;

namespace BonusService.Service.BLL
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(string key, string message);
    }

    public class MessageHandler : IMessageHandler
    {
        private readonly BonusAPIBLL _bonusAPIBLL;
        private readonly ILogger<MessageHandler> _logger;

        public MessageHandler(BonusAPIBLL bonusAPIBLL, ILogger<MessageHandler> logger)
        {
            _bonusAPIBLL = bonusAPIBLL;
            _logger = logger;
        }

        public async Task HandleMessageAsync(string key, string message)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            _logger.LogInformation($"key:{key} message:{message}");
            var result = new ApiResult();
            switch (key) 
            {
                case "verify.phone":
                case "verify.email":
                    var verify = JsonConvert.DeserializeObject<SetVerificationRewardReq>(message);
                    if (verify != null) 
                    {
                        result = _bonusAPIBLL.SetVerificationReward(verify.CustId, verify.OTPType, false);
                        _logger.LogInformation($"SetVerificationReward {verify.OTPType} result:{result.ErrorCode}, {result.Message}");
                    }
                    
                    break;
                case "deposit.payment":
                case "deposit.local":
                    var deposit = JsonConvert.DeserializeObject<SetVerificationRewardByDeposit>(message);
                    if (deposit != null) 
                    {
                        result = _bonusAPIBLL.SetVerificationReward(deposit.CustId, OTPTypeEnum.SMS, false);
                        _logger.LogInformation($"SetVerificationReward SMS result:{result.ErrorCode}, {result.Message}");
                        result = _bonusAPIBLL.SetVerificationReward(deposit.CustId, OTPTypeEnum.Email, false);
                        _logger.LogInformation($"SetVerificationReward Email result:{result.ErrorCode}, {result.Message}");
                    }
                    break;
                default:
                    _logger.LogError($"No this key:{key}");
                    break;
            }

            await Task.CompletedTask;
        }
    }
}
