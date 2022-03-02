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
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly string _apiUrl;
        private readonly Dictionary<int, bool> isConvert = new Dictionary<int, bool>()
        {
            {51,false },{15,false }
        };
        
        public  BonusAPIBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _bonusAPIService = provider.GetRequiredService<BonusAPIService>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _apiUrl = _configuration.GetValue<string>("TpService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
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

        
        public async Task<ApiResult> SetClaimPromotionAsync(int custId, string bonusCode, string custIP, decimal? dLockedAmount, int? deviceType)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = dLockedAmount ?? 0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var result = _bonusAPIService.SetClaimPromotion(custId, custInfo.UserName, custInfo.SiteId, bonusCode, custInfo.CurrencyId, custIP, deviceType, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion);

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

                if (apiResult1.ErrorCode == 0 && promotion is { PromotionType: PromotionType.Cashback })
                {
                    var content = string.Format(_selLangBll.GetMsg("msg_ClaimCashBackBonus", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang),
                        bonusAmount.ToString("#,##0.##"));
                    _messageService.SendMessage(custInfo.SiteId, "System", content, content, custInfo.CustId, custInfo.UserName, custInfo.UserLang);
                   

                }
            }
            else if (result == -2)
            {
                apiResult1.ErrorCode = -2;
                var dRequiredAmount = lockedAmount.ToString("#,##0");
                if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dRequiredAmount = $"{lockedAmount / 1000:#,##0} ('000)";
                }

                apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_LockedAmountInsufficient", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang), dRequiredAmount);
            }
            else if (result == -3)
            {
                apiResult1.ErrorCode = -3;
                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);

                if ((custInfo.SiteId == 2 || custInfo.SiteId == 3) && promotion?.PromotionType == PromotionType.FreeBet)
                {
                    apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);
                }
            }
            else if (result == -4)
            {
                apiResult1.ErrorCode = -4;
                var dRequiredAmount = promotion.RequiredAmount.ToString("#,##0");
                if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dRequiredAmount = $"{promotion.RequiredAmount / 1000:#,##0} ('000)";
                }

                apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_RequiredAmountInsufficient", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang), dRequiredAmount);
            }
            else if (result == -5)
            {
                apiResult1.ErrorCode = result;
                apiResult1.Message = $"{_selLangBll.GetMsg("msg_OTPHint1", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang)}\r\n{_selLangBll.GetMsg("msg_OTPHint2", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang)}";
            }
            else
            {
                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);

                if (custInfo.SiteId == (int)SiteId.Goal123 || custInfo.SiteId == (int)SiteId.Bwing)
                {
                    if (promotion.PromotionType == PromotionType.FreeBet)
                    {
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);
                    }
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
        public ApiResult CheckRewardCondination(int custId, OTPTypeEnum inputType, Customer custInfo = null, VerificationRewardData verInfo = null, CustVerificationRewardData custRewardInfo = null)
        {
            var result = new ApiResult();
            if(custInfo == null)
                custInfo = _custInfoService.GetCustInfo(custId);
            if(verInfo == null)
                verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            if (custRewardInfo == null)
                custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
            
            if (verInfo == null)
            {
                result.ErrorCode = 103;
                result.Message = $"SiteId:{custInfo.SiteId} CurrencyId:{custInfo.CurrencyId} setting not found";
                return result;
            }
            if (verInfo.VerificationType == 1) //email & phone both
            {
                if (custRewardInfo.EmailReward != null || custRewardInfo.PhoneReward != null)
                {
                    //payout before
                    result.ErrorCode = 101;
                    result.Message = "Reward already payout before";
                }
                else if (custRewardInfo.PhoneVerified && custRewardInfo.EmailVerified)
                {
                    result.ErrorCode = 0;
                    result.Message = $"{verInfo.CurrencyName} {verInfo.PhoneReward:#,##0.##}";
                }
                else
                {
                    result.ErrorCode = 102;
                    result.Message = "Condition not fulfilled";
                    // Condition not fulfilled
                }
            }
            else if (verInfo.VerificationType == 0)//email & phone each
            {
                if (inputType == OTPTypeEnum.SMS)
                {
                    if (custRewardInfo.PhoneReward != null)
                    {
                        //payout before
                        result.ErrorCode = 101;
                        result.Message = "Reward already payout before";
                    }
                    else
                    {
                        result.ErrorCode = 0;
                        result.Message = $"{verInfo.CurrencyName} {verInfo.PhoneReward:#,##0.##}";
                    }
                }
                else
                {
                    if (custRewardInfo.EmailReward != null)
                    {
                        result.ErrorCode = 101;
                        result.Message = "Reward already payout before";
                    }
                    else
                    {
                        result.ErrorCode = 0;
                        result.Message = $"{verInfo.CurrencyName} {verInfo.EmailReward:#,##0.##}";
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
        public ApiResult SetVerificationReward(int custId, OTPTypeEnum inputType)
        {
            var result = new ApiResult();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            var custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
           
            result = CheckRewardCondination(custId, inputType, custInfo, verInfo, custRewardInfo);//領獎條件規則
            if (result.ErrorCode != 0) // 規則不符
                return result;

            if (verInfo.VerificationType == 1) //email & phone both
            {
                result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.All, verInfo.PhoneReward);
            }
            else if (verInfo.VerificationType == 0)//email & phone each
            {
                if (inputType == OTPTypeEnum.SMS)
                {
                    result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.SMS, verInfo.PhoneReward);
                }
                else
                {
                    result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.Email, verInfo.EmailReward);
                }
            }
            return result;
        }
    }
}
