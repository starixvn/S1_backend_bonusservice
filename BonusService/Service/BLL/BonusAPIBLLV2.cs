using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly DepositBonusService _depositBonusService;
        private readonly DepositBonusBLL _depositBonusBLL;
        ILogger<BonusAPIBLLV2> _logger;

        public BonusAPIBLLV2(IServiceProvider provider, ILogger<BonusAPIBLLV2> logger)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _bonusAPIServiceV2 = provider.GetRequiredService<BonusAPIServiceV2>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _depositBonusService = provider.GetRequiredService<DepositBonusService>();
            _depositBonusBLL = provider.GetRequiredService<DepositBonusBLL>();
            _logger = logger;
        }
        public ApiResult MakeErrorMessage(ErrorMessageMakerInput input, bool isMessageUserLang = false)
        {
            var apiResult1 = new ApiResult();
            //defult Message display
            apiResult1.ErrorCode = input.Result;
            string showLangId = "en";//default
            if(isMessageUserLang)
            {
                showLangId = input.CustInfo.UserLang;
            }

            apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);

            if (input.CustInfo.SiteId == (int)SiteId.Goal123 || input.CustInfo.SiteId == (int)SiteId.Bwing)
            {
                if (input.Promotion.PromotionType == PromotionType.FreeBet)
                {
                    apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                }
            }
            switch (input.Result)
            {
                case (int)BonusErrorCode.ERROR:
                    {
                        apiResult1.ErrorCode = 9000;//未知錯誤
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                        apiResult1.RawMessage = "Unknow Error";
                    }
                    break;
                //API error -10xx
                case (int)BonusErrorCode.PROMOTION_NOT_FOUND:
                    {
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} not found";
                        apiResult1.RawMessage = $"BonusCode:{input.InputBonusCode} not found";
                    }
                    break;
                case (int)BonusErrorCode.IP_IS_BLOCKED:
                    {
                        apiResult1.RawMessage = "Customer's IP is blocked";
                    }
                    break;
                case (int)BonusErrorCode.PROFILE_OR_PHONE_NOT_FINISH:
                    {
                        apiResult1.Message = $"{_selLangBll.GetMsg("msg_PhoneProfile_ERROR", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId)}";
                        apiResult1.RawMessage = "Customer profile incomplete or phone not verified. ";
                    }
                    break;
                case (int)BonusErrorCode.DEPOSIT_AMOUNT_NOT_ENOUGH:
                    {
                        var dRequiredAmount = input.Promotion.RequiredAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.Promotion.RequiredAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_RequiredAmountInsufficient", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId), dRequiredAmount);
                        apiResult1.RawMessage = $"Customer's deposit amount less then requiredamount {dRequiredAmount}.";
                    }
                    break;
                case (int)BonusErrorCode.MAINWALLET_AMOUNT_NOT_ENOUGH://中心錢包餘額不可少於 {0}
                    {
                        var dRequiredAmount = input.LockedAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.LockedAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_LockedAmountInsufficient", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId), dRequiredAmount);
                        apiResult1.RawMessage = $"Customer's mainwallet amount less then {dRequiredAmount}.";
                    }
                    break;
                case (int)BonusErrorCode.CALCULATE_AMOUNT_IS_NEGATIVE:
                    {
                        apiResult1.RawMessage = "Calculate amount is negiative.";
                    }
                    break;
                case (int)BonusErrorCode.CALCULATE_CASHBACK_TOTALBONUS_LESS_THEN_MINPAY:
                    {
                        apiResult1.RawMessage = "Calculate cashback bonus less then minpay.";
                    }break;
                case (int)BonusErrorCode.CALCULATE_UNKNOW_PROMOTION_TYPE:
                    {
                        apiResult1.RawMessage = "Calculate function unknow promotion type.";
                    }
                    break;
                //SP Return ERROR -11XX
                case (int)BonusErrorCode.SP_BONUS_NOT_FOUND:
                    {
                        apiResult1.RawMessage = $"SP can not found this bonus {input.InputBonusCode}";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_NOT_JOIN_SPECIALCODE:
                    {
                        apiResult1.RawMessage = "Customer did not join special code";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_JOIN_THIS_PROMOTION_BEFORE:
                    {
                        apiResult1.RawMessage = "Customer join this promotion before";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_BONUS_STILL_RUNNING:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_DEPOSITE_NOT_EXCEED_REQUIRED_AMOUNT:
                    {
                        apiResult1.RawMessage = "Customer deposit amount less then requiredamount.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_DONT_HAVE_FIRSTDEPOSIT:
                    {
                        apiResult1.RawMessage = "Can't not find Customer's firstdeposit";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_FIRSTDEPOSIT_RULT_DONT_MATCH:
                    {
                        apiResult1.RawMessage = "Customer does not match firstdeposit bonus rult";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTUMER_WITHDRAWAL_BEFORE:
                    {
                        apiResult1.RawMessage = "Customer withdrawal before";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_BONUS_STILL_RUNNING_OR_ADD_OTHER_BONUS_IN_PERIOD_TIME:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus or join other bonus in period time.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_BONUS_STILL_RUNNING_OR_ADD_OTHER_BONUS_WHICH_PROMOTIONTYPE_IN_135:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus or ever join other bonus which promotion type in 1,3,5.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_DEPOSIT_APPROVE_BEFORE:
                    {
                        apiResult1.RawMessage = "Customer has approved deposit before.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_BONUS_STILL_RUNNING_OR_ADD_THIS_BONUS_BEFORE:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus or join this bonus before.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_JOIN_THIS_REBATE_BEFORE:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus or join this bonus before.";
                    }
                    break;
                case (int)BonusErrorCode.SP_CUSTOMER_HAVE_RUNNING_BONUS_OR_JOIN_THIS_BONUS_IN_PROID_TIME:
                    {
                        apiResult1.RawMessage = "Customer has other running bonus or join other bonus in period time.";
                    }
                    break;
                case (int)BonusErrorCode.SP_UNDEFINE_PROMOTIONTYPE:
                    {
                        apiResult1.RawMessage = "Promotion Type not define.";
                    }
                    break;
                //AdjustmentBonus Error -12XX
                case (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTIONTYPE_NOT_MATCH:
                    {
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} is not {Enum.GetName(typeof(PromotionType), (PromotionType)input.InputPromotionType)}";
                        apiResult1.RawMessage = $"BonusCode:{input.InputBonusCode} is not {Enum.GetName(typeof(PromotionType), (PromotionType)input.InputPromotionType)}"; ;
                    }
                    break;
                case (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_NOT_ACTIVE:
                    {
                        apiResult1.Message = $"BonusCode:{input.InputBonusCode} not Action";
                        apiResult1.RawMessage = $"BonusCode:{input.InputBonusCode} not Action";
                    }
                    break;
                case (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_AMOUNT_OVER_MAXBONUS:
                    {
                        apiResult1.Message = $"Amount:{input.InputAmount} is over MaxBonus{input.Promotion.MaxBonus} BonusCode:{input.InputBonusCode} ";
                        apiResult1.RawMessage = $"Amount:{input.InputAmount} is over MaxBonus{input.Promotion.MaxBonus} BonusCode:{input.InputBonusCode} ";
                    }
                    break;
                case (int)BonusErrorCode.ADJUSTMENT_BONUS_CUSTOMER_HAS_RUNNING_BONUS:
                    {
                        apiResult1.Message = $"CustId {input.CustInfo.CustId} has other running bonus";
                        apiResult1.RawMessage = $"CustId {input.CustInfo.CustId} has other running bonus";
                    }
                    break;
                case (int)BonusErrorCode.UNKNOWERROR:
                default://您不符合領取此優惠，請聯繫客服以了解詳情。
                    {
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);

                        if (input.CustInfo.SiteId == (int)SiteId.Goal123 || input.CustInfo.SiteId == (int)SiteId.Bwing)
                        {
                            if (input.Promotion.PromotionType == PromotionType.FreeBet)
                            {
                                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                            }
                        }
                        apiResult1.RawMessage = "Unknow Error";
                    }
                    break;
            }
            _logger.LogInformation($"CustId {input.CustInfo.CustId}  BonusCode:{input.InputBonusCode} apiResult:{Newtonsoft.Json.JsonConvert.SerializeObject(apiResult1)}");
            return apiResult1;
        }
        
        public void CancelAllWaitingBonus(int custId)
        {//當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉

            var BonusList = _bonusAPIServiceV2.GetDepositBonusQueueByCustId(custId, true);//改變狀態用mainDb取得資料，避免資料不同步

            foreach (var depositBonus in BonusList)
            {
                _depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
            }
        }
        public void AfterRewardSuccess(AfterRewardSuccessInput input)
        {
            if(input.atPage != BonusInfoAtPage.Deposit) 
                CancelAllWaitingBonus(input.CustId);
        }
        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIP, decimal lockedAmount, int? deviceType, BonusInfoAtPage atPage, bool isMessageUserLang, string DepositTransId = null)
        {
            var apiResult1 = new ApiResult();
            
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
                if (apiResult1.ErrorCode == 0)
                {
                    var input = new AfterRewardSuccessInput
                    {
                        CustId = custId,
                        atPage = atPage
                    };
                    AfterRewardSuccess(input);
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
                apiResult1 = MakeErrorMessage(errorInfo, isMessageUserLang);
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

                if (apiResult1.ErrorCode == 0)
                {
                    var input = new AfterRewardSuccessInput
                    {
                        CustId = custId,
                        atPage = BonusInfoAtPage.Promotion
                    };
                    AfterRewardSuccess(input);
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
        public async Task<Boolean> SetClaimPromotionV2Async(DepositBonus depositBonus)
        {
            var apiResult1 = new ApiResult();
            int? deviceType = 1;
            apiResult1 = await SetClaimPromotionV2Async(depositBonus.CustId, depositBonus.BonusCode, null, depositBonus.LockedAmount, deviceType, BonusInfoAtPage.Deposit, false, depositBonus.TransId);
            if (apiResult1.ErrorCode == 0)
                return true;
            return false;
        }
    }
}
