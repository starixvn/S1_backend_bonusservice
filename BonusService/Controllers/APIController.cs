using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BonusService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly BonusAPIBLL _bonusAPIBLL;
        private readonly ILogger<APIController> _logger;

        public APIController(IServiceProvider provider, ILogger<APIController> logger)
        {
            _bonusAPIBLL = provider.GetRequiredService<BonusAPIBLL>();
            this._logger = logger;
        }

        public class SetAdjustBonusAsyncReq
        {
            public int CustId { get; set; }
            public string BonusCode { get; set; }
            public string Operator { get; set; }
            public string Memo { get; set; }
            public decimal Amount { get; set; }
            public int RollOver { get; set; }
            public int PromotionType { get; set; }

        }
        [HttpPost]
        public async Task<ApiResult> SetAdjustBonusAsync(SetAdjustBonusAsyncReq setAdjustBonusReq)
        {
            var result = new ApiResult();
            try
            {
                //檢核PhoneVerified,AccountName,Birth
                var custInfoProfile = _bonusAPIBLL.GetCustInfoProfile(setAdjustBonusReq.CustId);
                if (!custInfoProfile.PhoneVerified
                || string.IsNullOrEmpty(custInfoProfile.AccountName)
                || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    result.ErrorCode = 999;
                    result.Message = "Please complete your profile and verify your phone number to enjoy our promotions.";
                    return result;
                }
                result = await _bonusAPIBLL.SetAdjustmentBonusAsync(setAdjustBonusReq.CustId, setAdjustBonusReq.BonusCode, setAdjustBonusReq.Operator, setAdjustBonusReq.Memo, setAdjustBonusReq.Amount, setAdjustBonusReq.RollOver, setAdjustBonusReq.PromotionType);
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            if (result.ErrorCode != 0)
            {
                _logger.LogError($"SetAdjustBonusAsync { Newtonsoft.Json.JsonConvert.SerializeObject(result)}");
            }
            return result;
        }



        public class SetVerificationRewardReq
        {
            public int CustId { get; set; }
            public int VerificationType { get; set; }

        }
        [HttpPost]
        public ApiResult SetVerificationReward(SetVerificationRewardReq verificationRewardReq)
        {
            VerificationItem item = VerificationItem.SMS;
            if(verificationRewardReq.VerificationType == 1)
                item = VerificationItem.Email;

            return _bonusAPIBLL.SetVerificationReward(verificationRewardReq.CustId, item);
        }

    }


}
