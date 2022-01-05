using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class BonusAPIBLLV2
    {
        private readonly CustInfoService _custInfoService;
        private readonly BonusAPIServiceV2 _bonusAPIServiceV2;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;

        public BonusAPIBLLV2(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _bonusAPIServiceV2 = provider.GetRequiredService<BonusAPIServiceV2>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();

        }
        public ApiResult MakeErrorMessage(int result, Customer custInfo, Promotion promotion, decimal lockedAmount)
        {
            var apiResult1 = new ApiResult();
            switch (result)
            {
                case -2://中心錢包餘額不可少於 {0}
                    {
                        apiResult1.ErrorCode = -2;
                        var dRequiredAmount = lockedAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{lockedAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_LockedAmountInsufficient", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang), dRequiredAmount);
                    }
                    break;
                case -3://您不符合領取此優惠，請聯繫客服以了解詳情。
                    {
                        apiResult1.ErrorCode = -3;
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);

                        if ((custInfo.SiteId == 2 || custInfo.SiteId == 3) && promotion?.PromotionType == PromotionType.FreeBet)
                        {
                            apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang);
                        }
                    }
                    break;
                case -4://最低存款{0}可以享受優惠
                    {
                        apiResult1.ErrorCode = -4;
                        var dRequiredAmount = promotion.RequiredAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{promotion.RequiredAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_RequiredAmountInsufficient", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang), dRequiredAmount);
                    }
                    break;
                case -5://您想享受我們完整的產品和服務嗎？現在驗證您的電話？
                    {
                        apiResult1.ErrorCode = result;
                        apiResult1.Message = $"{_selLangBll.GetMsg("msg_OTPHint1", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang)}\r\n{_selLangBll.GetMsg("msg_OTPHint2", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang)}";
                    }
                    break;
                case -6://Please complete your profile and verify your phone number to enjoy our promotions.
                    {
                        apiResult1.ErrorCode = result;
                        apiResult1.Message = $"{_selLangBll.GetMsg("msg_PhoneProfile_ERROR", custInfo.SiteId, custInfo.SiteName, custInfo.UserLang)}";
                    }
                    break;
                default://您不符合領取此優惠，請聯繫客服以了解詳情。
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
                    break;
            }
            return apiResult1;
        }

        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIP, decimal? dLockedAmount, int? deviceType)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = dLockedAmount ?? 0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var result = _bonusAPIServiceV2.SetClaimPromotionV2(custId, custInfo.UserName, custInfo.SiteId, bonusCode, custInfo.CurrencyId, custIP, deviceType, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion);

            if (result > 0)
            {
                if (promotion?.TpId > 0)
                {
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
                    var apiResult = await _bonusAPIServiceV2.TransferTpBonus(tpBonusTransfer);
                    if (apiResult.ErrorCode != 0)
                    {
                        //ErrorCode:1205 TPService Pending
                        apiResult1 = apiResult;
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
            else
            {
                apiResult1 = MakeErrorMessage(result, custInfo, promotion, lockedAmount);
            }
            return apiResult1;
        }
    }
}
