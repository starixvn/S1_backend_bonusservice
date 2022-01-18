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
        public ApiResult MakeErrorMessage(ErrorMessageMakerInput input)
        {
            var apiResult1 = new ApiResult();
            switch (input.Result)
            {
                case -2://中心錢包餘額不可少於 {0}
                    {
                        apiResult1.ErrorCode = input.Result;
                        var dRequiredAmount = input.LockedAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.LockedAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_LockedAmountInsufficient", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang), dRequiredAmount);
                    }
                    break;
                case -3://您不符合領取此優惠，請聯繫客服以了解詳情。
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang);

                        if ((input.CustInfo.SiteId == 2 || input.CustInfo.SiteId == 3) && input.Promotion?.PromotionType == PromotionType.FreeBet)
                        {
                            apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang);
                        }
                    }
                    break;
                case -4://最低存款{0}可以享受優惠
                    {
                        apiResult1.ErrorCode = input.Result;
                        var dRequiredAmount = input.Promotion.RequiredAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.Promotion.RequiredAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_RequiredAmountInsufficient", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang), dRequiredAmount);
                    }
                    break;
                case -5://您想享受我們完整的產品和服務嗎？現在驗證您的電話？
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"{_selLangBll.GetMsg("msg_OTPHint1", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang)}\r\n{_selLangBll.GetMsg("msg_OTPHint2", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang)}";
                    }
                    break;
                case -6://Please complete your profile and verify your phone number to enjoy our promotions.
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"{_selLangBll.GetMsg("msg_PhoneProfile_ERROR", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang)}";
                    }
                    break;
                //-2XX AdjustmentBonus Error Message
                case -201:
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} not found"; 
                    }
                    break;
                case -202:
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} is not {Enum.GetName(typeof(PromotionType), (PromotionType)input.InputPromotionType)}"; 
                    }
                    break;
                case -203:
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} not Action"; 
                    }
                    break;
                case -204:
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"Amount:{input.InputAmount} is over MaxBonus{input.Promotion.MaxBonus} BonusCode:{input.InputBonusCode} "; 
                    }
                    break;
                case -205:
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = $"CustId {input.CustInfo.CustId} has other running bonus"; 
                    }
                    break;

                default://您不符合領取此優惠，請聯繫客服以了解詳情。
                    {
                        apiResult1.ErrorCode = input.Result;
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang);

                        if (input.CustInfo.SiteId == (int)SiteId.Goal123 || input.CustInfo.SiteId == (int)SiteId.Bwing)
                        {
                            if (input.Promotion.PromotionType == PromotionType.FreeBet)
                            {
                                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, input.CustInfo.UserLang);
                            }
                        }
                    }
                    break;
            }
            return apiResult1;
        }

        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIP, decimal? dLockedAmount, int? deviceType, BonusInfoAtPage atPage, string DepositTransId = null)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = dLockedAmount ?? 0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var result = _bonusAPIServiceV2.SetClaimPromotionV2(custId, custInfo.UserName, custInfo.SiteId, bonusCode, custInfo.CurrencyId, custIP, deviceType, atPage, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion, DepositTransId);

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
                var errorInfo = new ErrorMessageMakerInput
                {
                    Result = result,
                    InputBonusCode = bonusCode,
                    LockedAmount = lockedAmount,
                    CustInfo = custInfo,
                    Promotion = promotion
                };
                apiResult1 = MakeErrorMessage(errorInfo);
            }
            return apiResult1;
        }
        public async Task<ApiResult> SetAdjustmentBonusV2Async(int custId, string bonusCode, string Operator, string memo, decimal amount, int promotionType)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = (decimal) 0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);

            DepositBonus depositBonus = new DepositBonus();
            depositBonus.CustId = custId;
            depositBonus.UserName = custInfo.UserName;
            depositBonus.SiteId = custInfo.SiteId;
            depositBonus.BonusCode = bonusCode;
            depositBonus.CurrencyId = custInfo.CurrencyId;
            depositBonus.DeviceType = 1;

            var result = _bonusAPIServiceV2.SetAdjustmentBonusV2(depositBonus, amount, promotionType, memo, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion);
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
            }
            else
            {
                var errorInfo = new ErrorMessageMakerInput
                {
                    Result = result,
                    InputBonusCode = bonusCode,
                    LockedAmount = lockedAmount,
                    InputPromotionType = promotionType,
                    InputAmount = amount,
                    CustInfo = custInfo,
                    Promotion = promotion
                };
                apiResult1 = MakeErrorMessage(errorInfo);
            }

            return apiResult1;
        }
    }
}
