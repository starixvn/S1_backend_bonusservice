using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using MemberSite.Service.Model;

using Microsoft.AspNetCore.Mvc;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

namespace BonusService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly BonusAPIBLL _bonusAPIBLL;
        private readonly PromotionBll _promotionBll;
        private readonly ILogger<APIController> _logger;

        public APIController(IServiceProvider provider)
        {
            _bonusAPIBLL = provider.GetRequiredService<BonusAPIBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _logger = provider.GetRequiredService<ILogger<APIController>>();
        }

        public class SpecialCodeResponse
        {
            public string BonusCode { get; set; }
        }

        public class SetAdjustBonusAsyncReq
        {
            public int CustId { get; set; }
            public string BonusCode { get; set; }
            public string Operator { get; set; }
            public string Memo { get; set; }
            public decimal Amount { get; set; }
            public int PromotionType { get; set; }
            public string DepositId { get; set; }
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

                result = await _promotionBll.SetAdjustmentBonusV2Async(setAdjustBonusReq.CustId, setAdjustBonusReq.BonusCode, Memo, setAdjustBonusReq.Amount, setAdjustBonusReq.PromotionType, setAdjustBonusReq.DepositId);

            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            if (result.ErrorCode != 0)
            {
                _logger.LogInformation($"SetAdjustBonusAsync {JsonConvert.SerializeObject(result)}");
            }
            return result;
        }

        [HttpPost]
        public ApiResult SetVerificationReward(SetVerificationRewardReq verificationRewardReq)
        {
            return _bonusAPIBLL.SetVerificationReward(verificationRewardReq, false);
        }

        [HttpPost]
        public ApiResult CheckRewardCondination(SetVerificationRewardReq verificationRewardReq)
        {
            return _bonusAPIBLL.CheckRewardCondition(verificationRewardReq.CustId, verificationRewardReq.OTPType);
        }

        public class PromotionReq
        {
            public string BonusCode { set; get; }
            public int CustId { set; get; }
            public string custIP { set; get; }
            public decimal? LockedAmount { set; get; }
            public int DeviceType { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> SetClaimPromotionAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult();
            try
            {
                var lockedAmount = promotionReq.LockedAmount ?? 0;
                apiResult = await _promotionBll.SetClaimPromotionV2Async(promotionReq.CustId,
                    promotionReq.BonusCode,
                    promotionReq.custIP,
                    lockedAmount,
                    promotionReq.DeviceType,
                    BonusInfoAtPage.Promotion);
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
                result.Data = _promotionBll.GetPromotionList(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
                result.Data = new List<CustPromotion>();
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
                result.Data = _promotionBll.GetPromotionDetail(request);
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
                result.Data = _promotionBll.GetSpecialPromotionList(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return result;
        }

        public ApiResult<List<CustPromotion>> GetDepositPromotionList(CustPromotionRequest request)
        {
            var result = new ApiResult<List<CustPromotion>>
            {
                ErrorCode = 0
            };

            try
            {
                result.Data = _promotionBll.GetDepositPromotionList(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public ApiResult SetSpecialCode(SpecialCodeRequest request)
        {
            var result = new ApiResult
            {
                ErrorCode = (int)SpecialCode.SpecialCodeError
            };

            try
            {
                if (!_promotionBll.CheckSpecialCode(request.SpecialCode))
                {
                    result.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                    return result;
                }
                
                if (!_promotionBll.CheckVipLevelWithBlockTags(request.CustId, request.SpecialCode))
                {
                    result.ErrorCode = (int)SpecialCode.SpecialCodeVipLevelsOrBlockTagInvalid;
                    return result;
                }
                
                // 240215考古:
                // * 如果符合ScratchCard格式走SetScratchCardCode，其餘走SetSpecialCode
                // * SetSpecialCode這段是輸入後台設定的ScratchCode(也就是DB的Promotion.SpecialCode)會過，詢問市場說這個ScratchCode不會對外開放所以實際上沒用，但PM說可先保留
                if (_promotionBll.CheckScratchCardCode(request.SpecialCode.ToUpper()))
                {
                    result.ErrorCode = _promotionBll.SetScratchCardCode(request);
                }
                else
                {
                    if (_promotionBll.SetSpecialCode(request))
                    {
                        result.ErrorCode = (int)SpecialCode.Success;
                    }
                    else
                    {
                        result.ErrorCode = (int)SpecialCode.SpecialCodeError;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == (int)MySqlErrorCode.DuplicateKeyEntry)/*Duplicate for key 'PRIMARY'*/
                {
                    result.ErrorCode = (int)SpecialCode.Success;
                }
                else
                {
                    result.ErrorCode = 9000;
                    result.Message = mySqlEx.ToString();
                    _logger.LogError(mySqlEx, MethodExtension.GetCurrentMethod());
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = 9000;
                result.Message = ex.ToString();
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public ApiResult<SpecialCodeResponse> CheckDepositSpecialCode(SpecialCodeRequest request)
        {
            var apiResult = new ApiResult<SpecialCodeResponse>();

            try
            {
                //檢核SpecialCode格式是否正確
                var isSpecialCode = _promotionBll.CheckSpecialCode(request.SpecialCode);
                var isScratchCardCode = _promotionBll.CheckScratchCardCode(request.SpecialCode.ToUpper());
                if (!isSpecialCode && !isScratchCardCode)
                {
                    apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                    return apiResult;
                }

                // 檢查 VIP Level & blockTag
                if (!_promotionBll.CheckVipLevelWithBlockTags(request.CustId, request.SpecialCode))
                {
                    apiResult.ErrorCode = (int)SpecialCode.SpecialCodeVipLevelsOrBlockTagInvalid;
                }

                if (isScratchCardCode)
                {
                    //檢核是否真有這筆ScratchCardCode
                    var promotion =
                        _promotionBll.GetPromotionBySpecialScratchCard(request.SiteId, request.SpecialCode);
                    if (promotion != null)
                    {
                        if (promotion.PromotionType != (int)PromotionType.FstDeposit &&
                            promotion.PromotionType != (int)PromotionType.ReDeposit)
                        {
                            apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                            return apiResult;
                        }

                        if (promotion.CustId != 0 && promotion.CustId != request.CustId)
                        {
                            apiResult.ErrorCode = (int)SpecialCode.PromoCodeHasBeenUsed;
                            return apiResult;
                        }

                        //寫入CustSpecialCode Table,並檢核是否輸入相同ScratchCard過
                        //有輸入相同ScratchCard過
                        if (_promotionBll.SetScratchCardCode(request) != 0)
                        {
                            //檢核是否領過這個BonusCode
                            if (_promotionBll.GetBonusInfoByBonusCode(request.CustId, promotion.BonusCode) != null)
                            {
                                apiResult.ErrorCode = (int)SpecialCode.PromoCodeHasBeenUsed;
                                return apiResult;
                            }

                            apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                            return apiResult;
                        }

                        apiResult.ErrorCode = (int)SpecialCode.Success;
                        apiResult.Data = new SpecialCodeResponse() { BonusCode = promotion.BonusCode };

                        return apiResult;
                    }
                }

                if (!isSpecialCode)
                {
                    apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                    return apiResult;
                }

                //檢核是否真有這筆specialCode
                var promotionBySpecialCode =
                    _promotionBll.GetPromotionBySpecialCode(request.SiteId, request.SpecialCode);
                if (promotionBySpecialCode != null)
                {
                    if (promotionBySpecialCode.PromotionType != (int)PromotionType.FstDeposit &&
                        promotionBySpecialCode.PromotionType != (int)PromotionType.ReDeposit)
                    {
                        apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                        return apiResult;
                    }

                    //寫入CustSpecialCode Table,並檢核是否輸入相同specialCode過
                    //有輸入相同specialCode過
                    if (!_promotionBll.SetSpecialCode(request))
                    {
                        //檢核是否領過這個BonusCode
                        if (_promotionBll.GetBonusInfoByBonusCode(request.CustId, promotionBySpecialCode.BonusCode) !=
                            null)
                        {
                            apiResult.ErrorCode = (int)SpecialCode.PromoCodeHasBeenUsed;
                            return apiResult;
                        }

                        apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
                        return apiResult;
                    }

                    apiResult.ErrorCode = (int)SpecialCode.Success;
                    apiResult.Data = new SpecialCodeResponse() { BonusCode = promotionBySpecialCode.BonusCode };
                    return apiResult;
                }

                apiResult.ErrorCode = (int)SpecialCode.SpecialCodeErrorForCheck;
            }
            catch (Exception e)
            {
                apiResult.ErrorCode = 9000;
                apiResult.Message = e.ToString();
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<RebateExcludeGameDetail>> GetRebateExcludeGameList(GetRebateExcludeGameListRequest request)
        {
            try
            {
                return new ApiResult<IEnumerable<RebateExcludeGameDetail>>
                {
                    Data = _promotionBll.GetRebateExcludeGameList(request),
                    ErrorCode = 0
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());

                return new ApiResult<IEnumerable<RebateExcludeGameDetail>>
                {
                    ErrorCode = -1,
                    RawMessage = e.Message
                };
            }
        }

        [HttpPost]
        public ApiResult<PopUpPromotion> GetPopUpPromotion(CustPromotionRequest request)
        {
            var result = new ApiResult<PopUpPromotion>();
            try
            {
                result.Data = _promotionBll.GetPopUpPromotion(request);
                result.ErrorCode = 0;
            }
            catch (Exception e)
            {
                result.Message = e.Message;
            }

            return result;
        }
    }
}
