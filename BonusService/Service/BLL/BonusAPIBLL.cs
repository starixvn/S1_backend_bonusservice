using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using MemberSite.Service.Model;

namespace BonusService.Service.BLL
{
    public class BonusAPIBLL
    {
        private readonly CustInfoService _custInfoService;
        private readonly BonusAPIService _bonusAPIService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly Currency _currency;
        private readonly ICacheHelper _redisHelper;

        public BonusAPIBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _bonusAPIService = provider.GetRequiredService<BonusAPIService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _currency = provider.GetRequiredService<Currency>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var customer = _custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);
            return customer;
        }
        public ApiResult CheckRewardCondition(int custId, OTPTypeEnum inputType, Customer custInfo = null, VerificationRewardData verInfo = null, CustVerificationRewardData custRewardInfo = null)
        {
            var result = new ApiResult();
            if (custInfo == null)
                custInfo = _custInfoService.GetCustInfo(custId);
            if (verInfo == null)
                verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            if (custRewardInfo == null)
                custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);
            //var rewardLimit = _bonusAPIService.GetVerificationRewardLimit(custInfo.SiteId, custInfo.CurrencyId, inputType);

            var verifyTime = _bonusAPIService.GetVerifyTime(custInfo.CustId, inputType);
            if (verInfo == null)
            {
                result.ErrorCode = 103;
                result.Message = $"SiteId:{custInfo.SiteId} CurrencyId:{custInfo.CurrencyId} setting not found";
                return result;
            }

            if (!verifyTime.HasValue)
            {
                result.ErrorCode = 105;
                result.Message = $"{inputType} verifyTime is null";
                return result;
            }

            if (custRewardInfo.SumDepositAmount < verInfo.MinDeposit)
            {
                result.ErrorCode = 104;
                result.Message = "Condition not fulfilled.";
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
                    if (!custRewardInfo.PhoneVerified)
                    {
                        result.ErrorCode = 102;
                        result.Message = "Condition not fulfilled";
                    }
                    else if (custRewardInfo.PhoneReward != null)
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
                    if (!custRewardInfo.EmailVerified)
                    {
                        result.ErrorCode = 102;
                        result.Message = "Condition not fulfilled";
                    }
                    else if (custRewardInfo.EmailReward != null)
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
        public ApiResult SetVerificationReward(int custId, OTPTypeEnum inputType, bool isWorker)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var verInfo = _bonusAPIService.GetVerificationReward(custInfo.SiteId, custInfo.CurrencyId);
            var custRewardInfo = _bonusAPIService.GetCustVerificationReward(custId, true);

            var result = CheckRewardCondition(custId, inputType, custInfo, verInfo, custRewardInfo); //領獎條件規則

            if (result.ErrorCode != 0)
            {
                if (result.ErrorCode == 104 && !isWorker)
                {
                    //Personal Msg - Failed
                    _currency.GetCurrency().TryGetValue(custInfo.CurrencyId, out var currencyName);
                    var leftAmountLimit = verInfo.MinDeposit - custRewardInfo.SumDepositAmount;
                    var title = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimitTitle", custInfo.UserLang));
                    var content = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimit", custInfo.UserLang),
                        currencyName, leftAmountLimit.ToString("#,##0.##"));
                    _messageService.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, content));

                }
                return result;
            }
            var reward = 0m;
            if (verInfo.VerificationType == 1) //email & phone both
            {
                reward = verInfo.PhoneReward + verInfo.EmailReward;
                result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.All, reward);
            }
            else if (verInfo.VerificationType == 0)//email & phone each
            {
                if (inputType == OTPTypeEnum.SMS)
                {
                    reward = verInfo.PhoneReward;
                    result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.SMS, reward);
                }
                else
                {
                    reward = verInfo.EmailReward;
                    result = _bonusAPIService.PayoutVerificationReward(custInfo.CustId, custInfo.SiteId, custInfo.CurrencyId, custInfo.UserName, OTPVerificationType.Email, reward);
                }
            }

            if (result.ErrorCode == 0)
            {
                //Personal Msg - Success
                _currency.GetCurrency().TryGetValue(custInfo.CurrencyId, out var currencyName);
                var title = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardLimitTitle", custInfo.UserLang));
                var content = string.Format(_selLangBll.GetResourceValue("lbl_VerificationRewardContent", custInfo.UserLang),
                    currencyName,
                    reward.ToString("#,##0.##"));
                _messageService.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, content));
            }

            return result;
        }
    }
}
