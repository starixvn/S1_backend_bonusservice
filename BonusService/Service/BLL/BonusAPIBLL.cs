using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class BonusAPIBLL
    {
        private readonly CustInfoService _custInfoService;
        private readonly BonusAPIService _bonusAPIService;
        private readonly IConfiguration _configuration;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _apiUrl;

        public  BonusAPIBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _bonusAPIService = provider.GetRequiredService<BonusAPIService>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _apiUrl = _configuration.GetValue<string>("TpService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        }

        public async Task<ApiResult> SetAdjustmentBonusAsync(int custId, string bonusCode, string Operator, string memo, decimal Amount, int PromotionType)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = (decimal)0;
            var custInfo = _custInfoService.GetCustInfo(custId);
           
            DepositBonus depositBonus = new DepositBonus();
            depositBonus.CustId = custId;
            depositBonus.UserName = custInfo.UserName;
            depositBonus.SiteId = custInfo.SiteId;
            depositBonus.BonusCode = bonusCode;
            depositBonus.CurrencyId = custInfo.CurrencyId;
            depositBonus.DeviceType = 1;

            var result = _bonusAPIService.SetAdjustmentBonus(depositBonus, Operator, memo, Amount, PromotionType, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion, out var errorCode, out var errorMessage);
            if (result > 0)
            {
                if (promotion?.TpId > 0)
                {
                    var myUrl = $"{_apiUrl}BonusTransfer";
                    var apiResult = new ApiResult<TransferResult>();
                    var tpBonusTransfer = new TpBonusTransfer
                    {
                        TpId = promotion.TpId,
                        CustId = custId,
                        Bonus = bonusAmount + lockedAmount,
                        BonusAmount = bonusAmount,
                        LockedAmount = lockedAmount,
                        RolloverAmount = rollOver,
                        ExpiryTime = expiryTime,
                        inputs = new Dictionary<string, string>()
                    };

                    var data = await _courierAdapter.MyPostAsync<TransferResult, TpBonusTransfer>(myUrl, tpBonusTransfer);
                    apiResult.Data = data.Data;
                    apiResult.ErrorCode = data.ErrorCode;
                    apiResult.RawErrorCode = data.RawErrorCode;
                    apiResult.RawMessage = data.RawMessage;

                    if (apiResult.ErrorCode != 0)
                    {
                        apiResult1.ErrorCode = apiResult.ErrorCode;
                        apiResult1.RawErrorCode = apiResult.RawErrorCode;
                        apiResult1.RawMessage = apiResult.RawMessage;
                        apiResult1.Message = apiResult.Message;
                    }
                    else
                    {
                        apiResult1.ErrorCode = 0;
                    }
                }
                else
                {
                    apiResult1.ErrorCode = 0;
                }
            }
            else
            {
                if(errorCode !=0 )
                {
                    apiResult1.ErrorCode = errorCode;
                    apiResult1.Message = errorMessage;
                }

            }

            return apiResult1;
        }

        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var customer = _custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);
            return customer;
        }

        public ApiResult SetVerificationReward(int custId, VerificationItem InputType)
        {
            var result = new ApiResult();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var VerInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            if(VerInfo == null)
            {
                result.ErrorCode = 103;
                result.Message = $"SiteId:{custInfo.SiteId} CurrencyId:{custInfo.CurrencyId} setting  not found";
                return result;
            }
            var CustRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
            if (VerInfo.VerificationType == 1) //email & phone both
            {
                if(CustRewardInfo.EmailReward!= null || CustRewardInfo.PhoneReward != null)
                {
                    //payout before
                    result.ErrorCode = 101;
                    result.Message = "Reward already payout before";
                }
                else if (CustRewardInfo.PhoneVerified && CustRewardInfo.EmailVerified)
                {
                    if(_bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, VerificationItem.All, VerInfo.PhoneReward)>0)
                    {
                        result.ErrorCode = 0;
                    }
                }
                else
                {
                    result.ErrorCode = 102;
                    result.Message = "Condition not fulfilled";
                    // Condition not fulfilled
                }
            }
            else if (VerInfo.VerificationType == 0)//email & phone each
            {
                if(InputType == VerificationItem.SMS)
                {
                    if (CustRewardInfo.PhoneReward != null)
                    {
                        //payout before
                        result.ErrorCode = 101;
                        result.Message = "Reward already payout before";
                    }
                    else if(CustRewardInfo.PhoneVerified)
                    {
                        if (_bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, VerificationItem.SMS, VerInfo.PhoneReward) > 0)
                        {
                            result.ErrorCode = 0;
                        }
                    }else
                    {
                        // Condition not fulfilled
                        result.ErrorCode = 102;
                        result.Message = "Condition not fulfilled";
                    }
                }
                else
                {
                    if (CustRewardInfo.EmailReward != null)
                    {
                        result.ErrorCode = 101;
                        result.Message = "Reward already payout before";
                    }
                    else if (CustRewardInfo.EmailVerified)
                    {
                        if(_bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, VerificationItem.Email, VerInfo.EmailReward) > 0)
                        {
                            result.ErrorCode = 0;
                        }
                    }
                    else
                    {
                        // Condition not fulfilled
                        result.ErrorCode = 102;
                        result.Message = "Condition not fulfilled";
                    }
                }
            }
            else
            {
                // out of Setting
                result.ErrorCode = 103;
                result.Message = "setting error";
            }
            return result;
        }

    }
}
