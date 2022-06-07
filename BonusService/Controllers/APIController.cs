using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BonusService.Service.Model;

namespace BonusService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly BonusAPIBLL _bonusAPIBLL;
        private readonly BonusAPIBLLV2 _bonusAPIBLLV2;
        private readonly ILogger<APIController> _logger;

        public APIController(IServiceProvider provider, ILogger<APIController> logger)
        {
            _bonusAPIBLL = provider.GetRequiredService<BonusAPIBLL>();
            _bonusAPIBLLV2 = provider.GetRequiredService<BonusAPIBLLV2>();
            this._logger = logger;
        }

        public class SetAdjustBonusAsyncReq
        {
            public int CustId { get; set; }
            public string BonusCode { get; set; }
            public string Operator { get; set; }
            public string Memo { get; set; }
            public decimal Amount { get; set; }
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
                var Memo = JsonConvert.SerializeObject(new { setAdjustBonusReq.Operator }); 

                result = await _bonusAPIBLLV2.SetAdjustmentBonusV2Async(setAdjustBonusReq.CustId, setAdjustBonusReq.BonusCode, Memo, setAdjustBonusReq.Amount, setAdjustBonusReq.PromotionType);

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
            public int OTPType { get; set; }//0.SMS 1.EMAIL

        }
        [HttpPost]
        public ApiResult SetVerificationReward(SetVerificationRewardReq verificationRewardReq)
        {
            var item = OTPTypeEnum.SMS;
            if(verificationRewardReq.OTPType == 1)
                item = OTPTypeEnum.Email;

            return _bonusAPIBLL.SetVerificationReward(verificationRewardReq.CustId, item , false);
        }

        [HttpPost]
        public ApiResult CheckRewardCondination(SetVerificationRewardReq verificationRewardReq)
        {
            var item = OTPTypeEnum.SMS;
            if (verificationRewardReq.OTPType == 1)
                item = OTPTypeEnum.Email;

            return _bonusAPIBLL.CheckRewardCondination(verificationRewardReq.CustId, item ,out _ );
        }

        public class PromotionReq
        {
            public string BonusCode { set; get; }
            public int CustId { set; get; }
            public string custIP { set; get; }
            public decimal? LockedAmount { set; get; }
            public int? DeviceType { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> SetClaimPromotionAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult();
            try
            {
                int? deviceType = 1;
                if (promotionReq.DeviceType != null)
                {
                    deviceType = promotionReq.DeviceType;
                }
                var lockedAmount = promotionReq.LockedAmount ?? 0;
                
                apiResult = await _bonusAPIBLLV2.SetClaimPromotionV2Async(promotionReq.CustId, promotionReq.BonusCode, promotionReq.custIP, lockedAmount, deviceType, BonusInfoAtPage.Promotion,true);
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
                apiResult.Message = ex.Message;
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }

            return apiResult;
        }
        
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetPromotionListV3(CustPromotionRequest request)
        {
            var result = new ApiResult<List<CustPromotion>>
            {
                ErrorCode = 0
            };
            
            try
            {
                result.Data = _bonusAPIBLLV2.GetPromotionList(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return result;
        }
        
        [HttpPost]
        public ApiResult<CustPromotion> GetPromotionDetail(PromotionDetailRequest request)
        {
            var result = new ApiResult<CustPromotion>
            {
                ErrorCode = 0
            };

            try
            {
                result.Data = _bonusAPIBLLV2.GetPromotionDetail(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public ApiResult<List<CustPromotion>> GetSpecialPromotionListV2(CustPromotionRequest request)
        {
            var result = new ApiResult<List<CustPromotion>>
            {
                ErrorCode = 0
            };
            
            try
            {
                result.Data = _bonusAPIBLLV2.GetSpecialPromotionList(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return result;
        }
    }


}
