using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

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
        private readonly Currency _currency;
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
            _currency = provider.GetRequiredService<Currency>();
        }
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var customer = _custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);
            return customer;
        }
        public ApiResult CheckRewardCondination(int custId, OTPTypeEnum inputType, out decimal minDeposit ,Customer custInfo = null, VerificationRewardData verInfo = null, CustVerificationRewardData custRewardInfo = null )
        {
            var result = new ApiResult();
            if(custInfo == null)
                custInfo = _custInfoService.GetCustInfo(custId);
            if(verInfo == null)
                verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            if (custRewardInfo == null)
                custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
            var rewardLimit = _bonusAPIService.GetVerificationRewardLimit(custInfo.SiteId, custInfo.CurrencyId, inputType);

            minDeposit = rewardLimit.MinDeposit;
            
            if (verInfo == null)
            {
                result.ErrorCode = 103;
                result.Message = $"SiteId:{custInfo.SiteId} CurrencyId:{custInfo.CurrencyId} setting not found";
                return result;
            }
            
            if (custRewardInfo.SumDepositAmount < minDeposit)
            {
                result.ErrorCode = 104;
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
        public ApiResult SetVerificationReward(int custId, OTPTypeEnum inputType , bool isWorker)
        {
            var result = new ApiResult();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            var custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
           
            result = CheckRewardCondination(custId, inputType,out var minDeposit, custInfo, verInfo, custRewardInfo);//領獎條件規則
            if (result.ErrorCode != 0)
            {
                if (result.ErrorCode == 104 && !isWorker)
                {
                    //Personal Msg - Failed
                    _currency.GetCurrency().TryGetValue(custInfo.CurrencyId, out var currencyName);
                    var leftAmountLimit = minDeposit - custRewardInfo.SumDepositAmount;
                    var title = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimitTitle", custInfo.UserLang));
                    var content = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimit", custInfo.UserLang),
                        currencyName,leftAmountLimit.ToString("#,##0.##"));
                    _messageService.SendMessage(custInfo.SiteId, "System", title, content, custInfo.CustId, custInfo.UserName, custInfo.UserLang);

                }
                return result;
            } 
            
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

            if (result.ErrorCode == 0)
            {
                //Personal Msg - Success
                _currency.GetCurrency().TryGetValue(custInfo.CurrencyId, out var currencyName);
                var title = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimitTitle", custInfo.UserLang));
                var content = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardContent", custInfo.UserLang),
                    currencyName,
                    (inputType == OTPTypeEnum.Email) ? 
                        verInfo.EmailReward.ToString("#,##0.##") : verInfo.PhoneReward.ToString("#,##0.##"));
                _messageService.SendMessage(custInfo.SiteId, "System", title, content, custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }
            
            return result;
        }
    }
}
