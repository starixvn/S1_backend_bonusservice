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
    }
}
