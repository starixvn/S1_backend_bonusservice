using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class PromotionBll
    {
        private readonly CustInfoService _custInfoService;
        private readonly PromotionService _promotionService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly DepositBonusBLL _depositBonusBll;
        private readonly WalletService _walletService;
        private readonly ILogger<PromotionBll> _logger;

        public PromotionBll(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _promotionService = provider.GetRequiredService<PromotionService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _depositBonusBll = provider.GetRequiredService<DepositBonusBLL>();
            _walletService = provider.GetRequiredService<WalletService>();
            _logger = provider.GetRequiredService<ILogger<PromotionBll>>();
        }
        public ApiResult MakeErrorMessage(ErrorMessageMakerInput input, bool isMessageUserLang = false)
        {
            var apiResult1 = new ApiResult
            {
                //defult Message display
                ErrorCode = input.Result
            };
            var showLangId = "en";//default
            if(isMessageUserLang)
            {
                showLangId = input.CustInfo.UserLang;
            }

            apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);

            if (input.CustInfo.SiteId == (int)SiteId.Goal123 || input.CustInfo.SiteId == (int)SiteId.Bwing)
            {
                if (input.ClaimPromotion.PromotionType == PromotionType.FreeBet)
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
                        var dRequiredAmount = input.ClaimPromotion.RequiredAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.ClaimPromotion.RequiredAmount / 1000:#,##0} ('000)";
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
                        apiResult1.Message = $"Amount:{input.InputAmount} is over MaxBonus{input.ClaimPromotion.MaxBonus} BonusCode:{input.InputBonusCode} ";
                        apiResult1.RawMessage = $"Amount:{input.InputAmount} is over MaxBonus{input.ClaimPromotion.MaxBonus} BonusCode:{input.InputBonusCode} ";
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
                            if (input.ClaimPromotion.PromotionType == PromotionType.FreeBet)
                            {
                                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                            }
                        }
                        apiResult1.RawMessage = "Unknow Error";
                    }
                    break;
            }
            _logger.LogInformation($"CustId={input.CustInfo.CustId} BonusCode={input.InputBonusCode} apiResult={JsonConvert.SerializeObject(apiResult1)}");
            return apiResult1;
        }
        
        public void CancelAllWaitingBonus(int custId)
        {//當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉

            var BonusList = _promotionService.GetDepositBonusQueueByCustId(custId, true);//改變狀態用mainDb取得資料，避免資料不同步

            foreach (var depositBonus in BonusList)
            {
                _depositBonusBll.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
            }
        }
        public void AfterRewardSuccess(AfterRewardSuccessInput input)
        {
            if(input.atPage != BonusInfoAtPage.Deposit) 
                CancelAllWaitingBonus(input.CustId);
        }
        
        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIp,
            decimal lockedAmount, int? deviceType, BonusInfoAtPage atPage, bool isMessageUserLang,
            string depositTransId = null)
        {
            var apiResult1 = new ApiResult();

            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var result = _promotionService.SetClaimPromotionV2(custId, custInfo.UserName, custInfo.SiteId, bonusCode,
                custInfo.CurrencyId, custIp, deviceType, atPage, out var bonusAmount, ref lockedAmount,
                out var rollOver, out var expiryTime, out var promotion, depositTransId);

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
                    var apiResult = await _promotionService.TransferTpBonus(tpBonusTransfer);
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
                    var content = string.Format(
                        _selLangBll.GetMsg("msg_ClaimCashBackBonus", custInfo.SiteId, custInfo.SiteName,
                            custInfo.UserLang),
                        bonusAmount.ToString("#,##0.##"));
                    _messageService.SendMessage(custInfo.SiteId, "System", content, content, custInfo.CustId,
                        custInfo.UserName, custInfo.UserLang);
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
                var P = new ClaimPromotion { }; //for fix NullReferenceException
                if (promotion != null)
                    P = promotion;
                var errorInfo = new ErrorMessageMakerInput
                {
                    Result = result,
                    InputBonusCode = bonusCode,
                    LockedAmount = lockedAmount,
                    CustInfo = custInfo,
                    ClaimPromotion = P
                };

                apiResult1 = MakeErrorMessage(errorInfo, isMessageUserLang);
            }

            return apiResult1;
        }

        public async Task<ApiResult> SetAdjustmentBonusV2Async(int custId, string bonusCode, string memo, decimal amount, int promotionType)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = (decimal) 0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);

            var depositBonus = new DepositBonus
            {
                CustId = custId,
                UserName = custInfo.UserName,
                SiteId = custInfo.SiteId,
                BonusCode = bonusCode,
                CurrencyId = custInfo.CurrencyId,
                DeviceType = 1
            };

            var result = _promotionService.SetAdjustmentBonusV2(depositBonus, amount, promotionType, memo, out var bonusAmount, ref lockedAmount, out var rollOver, out var expiryTime, out var promotion);
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
                    var apiResult = await _promotionService.TransferTpBonus(tpBonusTransfer);
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
                var P = new ClaimPromotion { }; //for fix NullReferenceException
                if (promotion != null)
                    P = promotion;
                var errorInfo = new ErrorMessageMakerInput
                {
                    Result = result,
                    InputBonusCode = bonusCode,
                    LockedAmount = lockedAmount,
                    InputPromotionType = promotionType,
                    InputAmount = amount,
                    CustInfo = custInfo,
                    ClaimPromotion = P
                };

                apiResult1 = MakeErrorMessage(errorInfo);
            }

            return apiResult1;
        }
        
        public List<CustPromotion> GetPromotionList(CustPromotionRequest request)
        {
            var promotionList =  _promotionService.GetPromotionList(request);
            var promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                if (request.IsLogin)
                {
                    var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                        request.SiteId, promo.BonusCode, request.DeviceType);
                    SetResult(promo, custCondition);
                    SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);
                }

                promotion.Add(promo);
            }

            return promotion;
        }

        public CustPromotion GetPromotionDetail(PromotionDetailRequest request)
        {
            var promo = _promotionService.GetPromotion<CustPromotion>(request.CustId, request.CurrencyId, request.SiteId, request.BonusCode, request.DeviceType, true);

            if (promo != null)
            {
                SetPromotionDetail(request.LangId, request.DefLang, false, promo);

                if (request.IsLogin)
                {
                    var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                        request.SiteId, promo.BonusCode, request.DeviceType);
                    SetResult(promo, custCondition);
                    SetPromotion(request.SiteId, request.CustId, request.CurrencyId, true, promo, custCondition);
                }
            }

            return promo;
        }
        
        public List<CustPromotion> GetSpecialPromotionList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetSpecialPromotionList(request);

            var promotion = new List<CustPromotion>();
            foreach (var promo in promotionList)
            {
                var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                    request.SiteId, promo.BonusCode, request.DeviceType);
                SetResult(promo, custCondition);
                SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);
                SetPromotionDetail(request.LangId, request.DefLang, true, promo);

                promotion.Add(promo);
            }

            return promotion;
        }
        
        public List<CustPromotion> GetDepositPromotionList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetDepositPromotionList(request);
            
            var promotion = new List<CustPromotion>();
            foreach (var promo in promotionList)
            { 
                SetPromotionDetail(request.LangId, request.DefLang, true, promo);
                promotion.Add(promo);
            }

            return promotion;
        }
        
        private static void SetResult(CustPromotion promo, CustCondition custCondition)
        {
            promo.canJoin = custCondition.CanJoin == 1;
            promo.Result = custCondition.Result;
        }
        
        private void SetPromotion(int siteId, int custId, int currencyId, bool getCashbackDetail, CustPromotion promo, CustCondition custCondition)
        {
            if ((promo.PromotionType == PromotionType.FstDeposit || promo.PromotionType == PromotionType.ReDeposit)
                && promo.canJoin)
            {
                promo.DepositId = custCondition.RefId;
                promo.DepositAmount = custCondition.Amount;
                promo.RefList = custCondition.reference;
        
                promo.MaxAmount = promo.PromotionType == PromotionType.FstDeposit
                    ? _walletService.GetBalance(custId)
                    : promo.DepositAmount;
                
                if (promo.PromotionType == PromotionType.FstDeposit
                    && promo.MaxAmount > promo.DepositAmount)
                {
                    promo.MaxAmount = promo.DepositAmount;
                }
        
                if (promo.RequiredAmount == 0)
                {
                    promo.RequiredAmount = 1;
                }
        
                if (promo.BonusType == BonusType.Amount)
                {
                    promo.MaxAmount = promo.RequiredAmount;
                }
                else
                {
                    if (promo.MaxBonus > 0)
                    {
                        var dBonus = Math.Ceiling(promo.Bonus * promo.DepositAmount / 100);
                        if (dBonus > promo.MaxBonus)
                        {
                            promo.MaxAmount = promo.MaxBonus * 100 / promo.Bonus;
                        }
                    }
                }
        
                if (promo.RequiredAmount > promo.DepositAmount)
                {
                    promo.canJoin = false;
                }
            }
        
            if (promo.PromotionType == PromotionType.Rebate)
            {
                promo.isJoin = _promotionService.GetRebateInfo(custId, promo);
        
            }
            else
            {
                var custBonusInfo = _promotionService.GetBonusInfoByCust(custId, promo);
                if (custBonusInfo != null)
                {
                    promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                    promo.CustRollOver = custBonusInfo.Rollover;
                    promo.CustTurnOver = custBonusInfo.Turnover < 0 ? 0 : custBonusInfo.Turnover;
                    promo.FullFillType = custBonusInfo.FullFillType;
                    promo.Status = custBonusInfo.Status;
                    promo.isJoin = true;
                }
                
                if (promo.PromotionType == PromotionType.Cashback)
                {
                    _promotionService.GetCashbackInfo(custId, siteId, currencyId, promo, getCashbackDetail);
                }
            }
        }
        
        private void SetPromotionDetail(string langId, string defLang, bool clearDetails, CustPromotion promo)
        {
            if (!string.IsNullOrEmpty(promo.Details))
            {
                var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                Dictionary<string, string> settings = null;
                if (details.ContainsKey(langId))
                {
                    settings = details[langId];
                }
                else if (details.ContainsKey(defLang))
                {
                    settings = details[defLang];
                }
                else
                {
                    foreach (var item in details)
                    {
                        settings = item.Value;
                        break;
                    }
                }

                if (clearDetails)
                {
                    promo.Details = null;
                }

                if (settings != null)
                {
                    settings.TryGetValue("title", out var title);
                    settings.TryGetValue("bannerUrl", out var bannerUrl);
                    settings.TryGetValue("description", out var description);
                    settings.TryGetValue("promotionLink", out var promotionLink);

                    promo.Title = title;
                    promo.Banner = bannerUrl;
                    promo.Description = description;
                    promo.PromotionLink = promotionLink;

                    if (!clearDetails)
                    {
                        settings.TryGetValue("detail", out var detail);
                        promo.Details = detail;
                    }
                }
            }
        }
        
        public int SetScratchCardCode(SpecialCodeRequest request)
        {
            request.SpecialCode = request.SpecialCode.ToUpper();
            return _promotionService.SetScratchCardCode(request);
        }
        
        public bool SetSpecialCode(SpecialCodeRequest request)
        {
            return _promotionService.SetSpecialCode(request);
        }
        
        public bool CheckSpecialCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length > 20)
                return false;
            var regex = new Regex("^[- !@#$%^&*a-zA-Z0-9]{1,20}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }
        
        public bool CheckScratchCardCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length != 12)
                return false;

            var regex = new Regex("^[a-zA-Z]{5}-[a-zA-Z0-9]{6}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }
        
        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialCode(siteId,SpecialCode);
        }
        
        public PromotionBySpecialScratchCard GetPromotionBySpecialScratchCard(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialScratchCard(siteId, SpecialCode);
        }
        
        public BonusInfoByBonusCode GetBonusInfoByBonusCode(int CustId, string BonusCode)
        {
            return _promotionService.GetBonusInfoByBonusCode(CustId, BonusCode);
        }
    }
}
