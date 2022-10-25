using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using System.Data;
using System.Text.RegularExpressions;
using BonusService.Service.Library;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class PromotionBll
    {
        private readonly MySqlClient _mainDbClient;
        private readonly Common _common;
        private readonly CustInfoService _custInfoService;
        private readonly PromotionService _promotionService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly DepositBonusBLL _depositBonusBll;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly BonusWalletService _bonusWalletService;
        private readonly ILogger<PromotionBll> _logger;
        private readonly ICacheHelper _redisHelper;

        public PromotionBll(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _common = provider.GetRequiredService<Common>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _promotionService = provider.GetRequiredService<PromotionService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            _depositBonusBll = provider.GetRequiredService<DepositBonusBLL>();
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _bonusWalletService = provider.GetRequiredService<BonusWalletService>();
            _logger = provider.GetRequiredService<ILogger<PromotionBll>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        private ApiResult MakeErrorMessage(ErrorMessageMakerInput input, bool isMessageUserLang = false)
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

            if (input.CustInfo.SiteId is (int)SiteId.Goal123 or (int)SiteId.Bwing)
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
                case (int)BonusErrorCode.DuplicateClaimBonus:
                    apiResult1.RawMessage = "Duplicate claim bonus.";
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

                        if (input.CustInfo.SiteId is (int)SiteId.Goal123 or (int)SiteId.Bwing)
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

        private void CancelAllWaitingBonus(int custId)
        {//當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉

            var BonusList = _promotionService.GetDepositBonusQueueByCustId(custId, true);//改變狀態用mainDb取得資料，避免資料不同步

            foreach (var depositBonus in BonusList)
            {
                _depositBonusBll.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
            }
        }

        private void AfterRewardSuccess(AfterRewardSuccessInput input)
        {
            if(input.atPage != BonusInfoAtPage.Deposit) 
                CancelAllWaitingBonus(input.CustId);
        }

        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIp,
            decimal lockedAmount, int deviceType, BonusInfoAtPage atPage, bool isMessageUserLang,
            string depositTransId = null)
        {
            var apiResult = new ApiResult();
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            var errorInfo = new ErrorMessageMakerInput
            {
                Result = (int)BonusErrorCode.PROMOTION_NOT_FOUND,
                InputBonusCode = bonusCode,
                LockedAmount = lockedAmount,
                CustInfo = custInfo,
                ClaimPromotion = new ClaimPromotion { }
            };

            var claimPromotion = _promotionService.GetPromotion<ClaimPromotion>(custId, custInfo.CurrencyId,
                custInfo.SiteId, bonusCode, deviceType, false);

            var result = VerifyPromotion(custId, custInfo.SiteId, bonusCode, custIp, claimPromotion);
            if (result != (int)BonusErrorCode.SUCCESS)
            {
                errorInfo.Result = result;
                return MakeErrorMessage(errorInfo,isMessageUserLang);
            }

            var custConditionInfo = _promotionService.GetCustConditionInfo(custId, custInfo.CurrencyId, custInfo.SiteId,
                bonusCode, deviceType);

            if (custConditionInfo.CanJoin != 1)
            {
                errorInfo.Result = custConditionInfo.Result;
                return MakeErrorMessage(errorInfo,isMessageUserLang);
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var calculateBonus = CalculateBonus(claimPromotion, custConditionInfo.Amount,
                lockedAmount, custId, custInfo.SiteId, custInfo.CurrencyId);
            if (calculateBonus.result <= 0)
            {
                errorInfo.Result = calculateBonus.result;
                return MakeErrorMessage(errorInfo,isMessageUserLang);
            }

            if (atPage == BonusInfoAtPage.Deposit) //由Deposit頁面進來的 reference紀錄指定TransId、Amount
            {
                if (claimPromotion.PromotionType == PromotionType.FstDeposit &&
                    depositTransId != custConditionInfo.RefId)
                {
                    errorInfo.Result = custConditionInfo.Result;
                    return MakeErrorMessage(errorInfo,isMessageUserLang);
                }

                custConditionInfo.reference = new List<Ref>
                {
                    new Ref
                    {
                        DepositId = depositTransId,
                        Amount = lockedAmount
                    }
                };
            }

            var bonusRewardInput = new BonusRewardInput
            {
                PromotionType = claimPromotion.PromotionType,
                BonusCode = bonusCode,
                CategoryId = claimPromotion.CategoryId,
                TpId = claimPromotion.TpId,
                BonusInfo = calculateBonus.BonusInfo,
                BonusAmount = calculateBonus.BonusAmount,
                LockedAmount = calculateBonus.LockedAmount,
                RollOver = calculateBonus.RollOver,
                FullFillType = claimPromotion.FullFillType,
                ExpiryTime = calculateBonus.ExpiryTime,
                ExpiryDays = claimPromotion.ExpiryDays,
                CustId = custId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                SiteId = custInfo.SiteId,
                Memo = null,
                Prefix = $"B{_common.GetEnv()}",
                RefList = custConditionInfo.reference.Any() ? JsonConvert.SerializeObject(custConditionInfo.reference) : null,
                SetBalanceMemo = $"Claim {Enum.GetName(typeof(PromotionType), claimPromotion.PromotionType)} BonusCode: {bonusCode}, Locked Amount: {calculateBonus.LockedAmount:#,##0.##}",
                RewardBonusMemo = $"Claim {Enum.GetName(typeof(PromotionType), claimPromotion.PromotionType)} BonusCode: {bonusCode}",
                FinancialType = FinancialType.Bonusreward,
                AtPage = atPage
            };
            result = Reward(bonusRewardInput, trans);

            if (result > 0)
            {
                result = _promotionService.AfterReward(claimPromotion, custInfo.SiteId, bonusCode, custInfo.CurrencyId,
                    custIp, trans);
                if (result > 0)
                    trans.Commit();
            }

            if (result > 0)
            {
                if (claimPromotion.TpId > 0)
                {
                    var tpBonusTransfer = new TpBonusTransfer
                    {
                        TpId = claimPromotion.TpId,
                        CustId = custId,
                        Bonus = calculateBonus.BonusAmount + lockedAmount,
                        BonusAmount = calculateBonus.BonusAmount,
                        LockedAmount = lockedAmount,
                        RolloverAmount = calculateBonus.RollOver,
                        ExpiryTime = calculateBonus.ExpiryTime,
                        inputs = new Dictionary<string, string>()
                    };
                    var transferResult = await _promotionService.TransferTpBonus(tpBonusTransfer);
                    if (transferResult.ErrorCode != 0)
                    {
                        //ErrorCode:1205 TPService Pending
                        apiResult = transferResult;
                    }
                    else
                    {
                        apiResult.ErrorCode = 0;
                    }
                }
                else
                {
                    apiResult.ErrorCode = 0;
                }

                if (apiResult.ErrorCode == 0 && claimPromotion is { PromotionType: PromotionType.Cashback })
                {
                    _ = Task.Run(async () =>
                    {
                        var content = string.Format(_selLangBll.GetMsg("msg_ClaimCashBackBonus", custInfo.SiteId,
                            custInfo.SiteName, custInfo.UserLang), calculateBonus.BonusAmount.ToString("#,##0.##"));
                        await _messageService.SendMessageAsync(new Message(custInfo.SiteId, "System", content, content,
                            custInfo.CustId, custInfo.UserName, custInfo.UserLang));
                    });
                }

                if (apiResult.ErrorCode != 0) return apiResult;
                var input = new AfterRewardSuccessInput
                {
                    CustId = custId,
                    atPage = atPage
                };
                AfterRewardSuccess(input);

                return apiResult;
            }

            errorInfo.Result = result;
            errorInfo.ClaimPromotion = claimPromotion;
            return MakeErrorMessage(errorInfo, isMessageUserLang);
        }

        public async Task<ApiResult> SetAdjustmentBonusV2Async(int custId, string bonusCode, string memo,
            decimal amount, int promotionType, string depositId)
        {
            var apiResult = new ApiResult();
            var lockedAmount = (decimal)0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);

            var errorInfo = new ErrorMessageMakerInput
            {
                Result = (int)BonusErrorCode.PROMOTION_NOT_FOUND,
                InputBonusCode = bonusCode,
                LockedAmount = lockedAmount,
                InputPromotionType = promotionType,
                InputAmount = amount,
                CustInfo = custInfo,
                ClaimPromotion = new ClaimPromotion { }
            };

            var claimPromotion = _promotionService.GetPromotion<ClaimPromotion>(custId, custInfo.CurrencyId,
                custInfo.SiteId, bonusCode, (int)PromotionDeviceType.All, false);

            var result = VerifyAdjustmentBonus(custId, custInfo.SiteId, amount, promotionType, claimPromotion);
            if (result != (int)BonusErrorCode.SUCCESS)
            {
                errorInfo.Result = result;
                apiResult = MakeErrorMessage(errorInfo);
                return apiResult;
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var calculateBonus = CalculateBonus(claimPromotion, amount, lockedAmount, custId,
                custInfo.SiteId, custInfo.CurrencyId, depositId);
            if (calculateBonus.result <= 0)
            {
                errorInfo.Result = calculateBonus.result;
                return MakeErrorMessage(errorInfo);
            }

            var bonusRewardInput = new BonusRewardInput
            {
                PromotionType = claimPromotion.PromotionType,
                BonusCode = bonusCode,
                CategoryId = claimPromotion.CategoryId,
                TpId = claimPromotion.TpId,
                BonusInfo = calculateBonus.BonusInfo,
                BonusAmount = calculateBonus.BonusAmount,
                LockedAmount = calculateBonus.LockedAmount,
                RollOver = calculateBonus.RollOver,
                FullFillType = claimPromotion.FullFillType,
                ExpiryTime = calculateBonus.ExpiryTime,
                ExpiryDays = claimPromotion.ExpiryDays,
                CustId = custId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                SiteId = custInfo.SiteId,
                Memo = memo,
                Prefix = $"B{_common.GetEnv()}",
                RefList = !string.IsNullOrEmpty(depositId) ? JsonConvert.SerializeObject(new { DepositId = depositId } ) : null,
                SetBalanceMemo = $"Claim {Enum.GetName(typeof(PromotionType), claimPromotion.PromotionType)} BonusCode: {bonusCode}, Locked Amount: {calculateBonus.LockedAmount:#,##0.##}",
                RewardBonusMemo = $"Claim {Enum.GetName(typeof(PromotionType), claimPromotion.PromotionType)} BonusCode: {bonusCode}",
                FinancialType = FinancialType.Bonusreward,
                AtPage = BonusInfoAtPage.Promotion
            };

            result = Reward(bonusRewardInput, trans);
            if (result > 0)
            {
                result = _promotionService.AfterReward(claimPromotion, custInfo.SiteId, bonusCode, custInfo.CurrencyId,
                    null, trans);
                if (result > 0)
                    trans.Commit();
            }

            if (result > 0)
            {
                if (claimPromotion.TpId > 0)
                {
                    var tpBonusTransfer = new TpBonusTransfer
                    {
                        TpId = claimPromotion.TpId,
                        CustId = custId,
                        Bonus = calculateBonus.BonusAmount + lockedAmount,
                        BonusAmount = calculateBonus.BonusAmount,
                        LockedAmount = lockedAmount,
                        RolloverAmount = calculateBonus.RollOver,
                        ExpiryTime = calculateBonus.ExpiryTime,
                        inputs = new Dictionary<string, string>()
                    };
                    var transferResult = await _promotionService.TransferTpBonus(tpBonusTransfer);
                    if (transferResult.ErrorCode != 0)
                    {
                        //ErrorCode:1205 TPService Pending
                        apiResult = transferResult;
                    }
                    else
                    {
                        apiResult.ErrorCode = 0;
                    }
                }
                else
                {
                    apiResult.ErrorCode = 0;
                }

                if (apiResult.ErrorCode != 0) return apiResult;
                var input = new AfterRewardSuccessInput
                {
                    CustId = custId,
                    atPage = BonusInfoAtPage.Promotion
                };
                AfterRewardSuccess(input);

                return apiResult;
            }

            errorInfo.Result = result;
            errorInfo.ClaimPromotion = claimPromotion;
            return MakeErrorMessage(errorInfo);
        }

        private int VerifyPromotion(int custId, int siteId, string bonusCode, string custIp, ClaimPromotion claimPromotion)
        {
            if (claimPromotion == null)
                return (int)BonusErrorCode.PROMOTION_NOT_FOUND;

            if (_promotionService.IsBlockIP(claimPromotion, bonusCode, siteId, custIp))
                return (int)BonusErrorCode.IP_IS_BLOCKED;
            if (!CheckProfileFinish(custId, siteId)) //Profile Finish & Phone Verified
                return (int)BonusErrorCode.PROFILE_OR_PHONE_NOT_FINISH;
            
            return (int)BonusErrorCode.SUCCESS;
        }
        
        private int VerifyAdjustmentBonus(int custId, int siteId, decimal amount, int promotionType,
            ClaimPromotion claimPromotion)
        {
            if (claimPromotion == null)
            {
                return (int)BonusErrorCode.PROMOTION_NOT_FOUND;
            }

            if (claimPromotion.PromotionType != (PromotionType)promotionType)
            {
                return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTIONTYPE_NOT_MATCH;
            }

            if (claimPromotion.PromotionType == PromotionType.SpecialBonus)
            {
                if (claimPromotion.PromotionAction != 1)
                    return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_NOT_ACTIVE;
                if (amount > claimPromotion.MaxBonus)
                    return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_AMOUNT_OVER_MAXBONUS;
            }

            if (!CheckProfileFinish(custId, siteId)) //Profile Finish & Phone Verified
                return (int)BonusErrorCode.PROFILE_OR_PHONE_NOT_FINISH;

            if (_promotionService.GetCustRunningBonus(custId).Any())
            {
                return (int)BonusErrorCode.ADJUSTMENT_BONUS_CUSTOMER_HAS_RUNNING_BONUS;
            }

            return (int)BonusErrorCode.SUCCESS;
        }
        
        private bool CheckProfileFinish(int custId, int siteId) //Profile Finish & Phone Verified
        {
            var custInfoProfile = _custInfoService.GetCustInfoProfile( custId, siteId);
            return custInfoProfile.PhoneVerified 
                   && !string.IsNullOrEmpty(custInfoProfile.AccountName) 
                   && !string.IsNullOrEmpty(custInfoProfile.Birth_y) 
                   && !string.IsNullOrEmpty(custInfoProfile.Birth_m) 
                   && !string.IsNullOrEmpty(custInfoProfile.Birth_d);
        }

        private CalculateBonusOutput CalculateBonus(ClaimPromotion claimPromotion, decimal amount, decimal lockedAmount, int custId, int siteId, int currencyId, string depositId = null)
        {
            var output = new CalculateBonusOutput
            {
                result = (int)BonusErrorCode.SUCCESS
            };

            decimal bonusAmount;
            var lockAmount = amount;
            decimal rollover;

            switch (claimPromotion.PromotionType)
            {
                case PromotionType.FstDeposit:
                case PromotionType.ReDeposit:
                    {
                        //帶DepositId使用存款金額計算TierAmount
                        if (claimPromotion.PromotionType == PromotionType.ReDeposit && depositId != null)
                        {
                            return CalculateTierRedepositBonus(claimPromotion, custId, siteId, currencyId, depositId);     
                        }

                        if (claimPromotion.PromotionType == PromotionType.ReDeposit ||
                           (claimPromotion.IsSpecial && claimPromotion.PromotionType == PromotionType.FstDeposit))
                        {
                            amount = claimPromotion.BonusType == BonusType.Amount ? claimPromotion.RequiredAmount : lockedAmount;

                            if (amount <= 0)
                            {
                                output.result = (int)BonusErrorCode.CALCULATE_AMOUNT_IS_NEGATIVE;
                                return output;
                            }
                            lockAmount = amount;
                        }

                        if (amount < claimPromotion.RequiredAmount)
                        {
                            output.result = (int)BonusErrorCode.DEPOSIT_AMOUNT_NOT_ENOUGH;
                            return output;
                        }

                        if (claimPromotion.BonusType == BonusType.Amount)
                        {
                            if (claimPromotion.RequiredAmount == 0)
                            {
                                throw new Exception("Required Amount can't be 0");
                            }

                            lockAmount = claimPromotion.RequiredAmount;
                            bonusAmount = claimPromotion.Bonus;
                        }
                        else
                        {
                            bonusAmount = Math.Ceiling(claimPromotion.Bonus * amount / 100);
                            if (claimPromotion.MaxBonus != 0 && bonusAmount > claimPromotion.MaxBonus)
                            {
                                bonusAmount = claimPromotion.MaxBonus;
                                lockAmount = claimPromotion.MaxBonus * 100 / claimPromotion.Bonus;
                            }
                        }

                        rollover = (lockAmount + bonusAmount) * (claimPromotion.FullFillType == (int)FullFillType.RollOver ? claimPromotion.RollOver : claimPromotion.WinOver);
                        break;
                    }

                case PromotionType.Cashback:
                    var custPromotion = new CustPromotion
                    {
                        BonusCode = claimPromotion.BonusCode,
                        CategoryId = claimPromotion.CategoryId
                    };
                    GetCashbackInfo(custId, siteId, currencyId, custPromotion, true);
                    if (!(custPromotion.canJoin && custPromotion.CashBackDetails?.TotalBonus > claimPromotion.MinPayout))
                    {
                        output.result = (int)BonusErrorCode.CALCULATE_CASHBACK_TOTALBONUS_LESS_THEN_MINPAY;
                        return output;
                    }

                    bonusAmount = custPromotion.CashBackDetails?.TotalBonus ?? 0;
                    if (claimPromotion.MaxBonus != 0 && bonusAmount > claimPromotion.MaxBonus)
                    {
                        bonusAmount = claimPromotion.MaxBonus;
                    }

                    rollover = ((lockAmount + bonusAmount) * claimPromotion.RollOver);
                    lockAmount = 0;
                    break;

                case PromotionType.FreeBet:
                    bonusAmount = claimPromotion.Bonus;
                    rollover = bonusAmount * claimPromotion.RollOver;

                    lockAmount = 0;
                    break;

                case PromotionType.Rebate:
                    return output;
                
                case PromotionType.SpecialBonus:
                    bonusAmount = amount;
                    lockAmount = 0;
                    rollover = amount * claimPromotion.RollOver;
                    break;
                
                case PromotionType.RetentionFreeBet:
                    bonusAmount = claimPromotion.Bonus;
                    lockAmount = 0;         
                    rollover = bonusAmount * claimPromotion.RollOver;
                    break;                  

                default:
                    output.result = (int)BonusErrorCode.CALCULATE_UNKNOW_PROMOTION_TYPE;
                    return output;
            }
            output.BonusInfo = JsonConvert.SerializeObject(claimPromotion,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            output.ExpiryTime = DateTimeOffset.Now.AddDays(claimPromotion.ExpiryDays).ToUnixTimeSeconds();
            output.BonusAmount = bonusAmount;
            output.LockedAmount = lockAmount;
            output.RollOver = rollover;
            return output;
        }

        private int Reward(BonusRewardInput bonusRewardInput, MySqlTransaction trans)
        {
            int result;
            switch (bonusRewardInput.PromotionType)
            {
                case PromotionType.FstDeposit:
                case PromotionType.ReDeposit:
                case PromotionType.FreeBet:
                case PromotionType.Cashback:
                case PromotionType.SpecialBonus:
                case PromotionType.RetentionFreeBet:
                    {
                        result = BonusReward(bonusRewardInput, trans);
                        break;
                    }

                case PromotionType.Rebate:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }
        
        private int BonusReward(BonusRewardInput bonusRewardInput, MySqlTransaction trans)
        {
            var key = $"BonusReward:{bonusRewardInput.CustId}";
            if (_redisHelper.KeyExists(key))
            {
                return (int)BonusErrorCode.DuplicateClaimBonus;
            }
            _redisHelper.StringSet(key, bonusRewardInput.CustId, TimeSpan.FromMinutes(1));
            
            var p = new DynamicParameters(new
            {
                PromotionType = (int)bonusRewardInput.PromotionType,
                bonusRewardInput.BonusCode,
                bonusRewardInput.CustId,
                bonusRewardInput.UserName,
                bonusRewardInput.CurrencyId,
                bonusRewardInput.SiteId,
                bonusRewardInput.BonusInfo,
                bonusRewardInput.LockedAmount,
                bonusRewardInput.BonusAmount,
                bonusRewardInput.ExpiryDays,
                bonusRewardInput.Memo,
                bonusRewardInput.Prefix,
                bonusRewardInput.RefList,
                AtPage = (int)bonusRewardInput.AtPage
            });
            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Member_BonusInfo_Ins_V3", trans, p);
            var transId = p.Get<string>("TransId");

            if (result > 0 && bonusRewardInput.LockedAmount > 0)
            {

                result = _walletService.SetBalance(bonusRewardInput.CustId, bonusRewardInput.SiteId, bonusRewardInput.UserName, bonusRewardInput.LockedAmount * -1, (int)bonusRewardInput.FinancialType,
                    bonusRewardInput.SetBalanceMemo, transId,
                    out _, out _, trans);

                if (result > 0)
                {
                    _withdrawalLimitService.SetWithdrawalLimit(bonusRewardInput.CustId, bonusRewardInput.UserName, bonusRewardInput.SiteId, bonusRewardInput.LockedAmount * -1,
                        null, transId, out _, trans);
                }
                else
                {
                    return (int)BonusErrorCode.MAINWALLET_AMOUNT_NOT_ENOUGH;
                }
            }

            if (result > 0)
            {
                var m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out var bonusId);

                result = _bonusWalletService.RewardBonus(bonusId, bonusRewardInput.CategoryId, bonusRewardInput.TpId, bonusRewardInput.CustId,
                    bonusRewardInput.SiteId, bonusRewardInput.UserName, bonusRewardInput.BonusAmount, bonusRewardInput.LockedAmount, bonusRewardInput.RollOver, bonusRewardInput.ExpiryTime, (int)bonusRewardInput.FinancialType,
                    bonusRewardInput.RewardBonusMemo, transId, trans, bonusRewardInput.FullFillType)
                    ? 1
                    : 0;
            }

            if (result > 0 && bonusRewardInput.PromotionType == PromotionType.Cashback)
            {
                var offset = GetOffset(bonusRewardInput.SiteId);
                var currentTime = DateTime.UtcNow.AddHours(offset);
                var calculateDate = StartOfWeek(currentTime, DayOfWeek.Monday).ToString("yyyy-MM-dd");

                result = _bonusWalletService.RewardCashBack(
                    new CustCashBackInput(bonusRewardInput.CustId, bonusRewardInput.BonusCode, bonusRewardInput.CategoryId, calculateDate),
                    trans);
            }
            return result;
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
                else if (promo.PromotionType == PromotionType.Cashback)
                {
                    var offset = GetOffset(request.SiteId);
                    ProcessPeriodTime(promo, DateTime.UtcNow.AddHours(offset), offset);
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
            if (promo.PromotionType is PromotionType.FstDeposit or PromotionType.ReDeposit
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
                GetCashbackInfo(custId, siteId, currencyId, promo, getCashbackDetail);
            }
        }
        
        private void SetPromotionDetail(string langId, string defLang, bool clearDetails, CustPromotion promo)
        {
            if (!string.IsNullOrEmpty(promo.Details))
            {
                var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                Dictionary<string, string> settings = null;
                if (details != null)
                {
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

        private void GetCashbackInfo(int custId, int siteId, int currencyId, CustPromotion promotion, bool getDetail)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var offset = GetOffset(siteId);
            var currentTime = DateTime.UtcNow.AddHours(offset);
            var calculateDate = StartOfWeek(currentTime, DayOfWeek.Monday).ToString("yyyy-MM-dd");

            var result = _promotionService.GetCustCashBackCheck(custId, promotion, calculateDate).ToList();

            promotion.isJoin = result.FirstOrDefault()?.Claimed ?? false;
            promotion.canJoin = result.Any();
            
            ProcessPeriodTime(promotion, currentTime, offset);

            if (!getDetail) return;
            var lastMonday = StartOfWeek(currentTime.AddDays(-7), DayOfWeek.Monday).Date;
            var lastSunday = lastMonday.AddDays(6);
            var detail = _promotionService.GetCashBackInfo(custId, promotion, lastMonday, lastSunday).ToList();

            if (detail.Any() && detail.First().TpId == (int)Tp.Saba)
            {
                foreach (var d in detail)
                {
                    d.TpName = d.CategoryId switch
                    {
                        (int)TpCategoryId.Sports => _selLangBll.GetResourceValue("lbl_Cashback_Sports", custInfo.UserLang),
                        (int)TpCategoryId.Casino => _selLangBll.GetResourceValue("lbl_Cashback_RNG", custInfo.UserLang),
                        (int)TpCategoryId.Lottery => _selLangBll.GetResourceValue("lbl_Cashback_Lottery", custInfo.UserLang),
                        (int)TpCategoryId.VirtualSports => _selLangBll.GetResourceValue("lbl_Cashback_Virtual_Sports", custInfo.UserLang),
                        _ => d.TpName
                    };
                }
            }

            var details = _promotionService.GetCustCashBack(custId, siteId, currencyId, promotion, calculateDate).ToList();

            if (!details.Any()) return;
            details.First().CashBackDetail = detail;
            promotion.CashBackDetails = details.First();
        }

        private void ProcessPeriodTime(CustPromotion promotion, DateTime currentTime, int offset)
        {
            var utcMonday = StartOfWeek(currentTime, DayOfWeek.Monday).AddHours(-offset);
            var utcSunday = utcMonday.AddDays(7).AddTicks(-1);
            promotion.PeriodFrom = ((DateTimeOffset)utcMonday).ToUnixTimeSeconds();
            promotion.PeriodTo = ((DateTimeOffset)utcSunday).ToUnixTimeSeconds();
        }

        private int GetOffset(int siteId)
        {
            var offset = 8;
            if (siteId is (int)SiteId.Goal123 or (int)SiteId.Bwing)
            {
                offset = 7;
            }

            return offset;
        }

        public DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek)
        {
            var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }

        private CheckTierRedepositBonusRep CheckTierRedepositBonus(CheckTierRedepositBonusReq request)
        {
            return _mainDbClient.QueryFirst<CheckTierRedepositBonusRep>("Admin_CheckTierRedepositBonus_Get", new DynamicParameters(request));
        }

        private CalculateBonusOutput CalculateTierRedepositBonus(ClaimPromotion claimPromotion, int custId, int siteId, int currencyId, string depositId)
        {
            var checkResult = CheckTierRedepositBonus(new CheckTierRedepositBonusReq
            {
                CustId = custId,
                BonusCode = claimPromotion.BonusCode,
                PromotionType = (int)claimPromotion.PromotionType,
                SiteId = siteId,
                CurrencyId = currencyId,
                DepositId = depositId
            });
            if (!checkResult.IsSuccess)
            {
                throw new Exception("TierAmount check fail.");
            }

            var tierAmount = claimPromotion.CalculateTierAmount(checkResult.ActualDeposit);
            if (tierAmount == null)
            {
                throw new Exception("TierAmount no found.");
            }

            claimPromotion.MaxBonus = tierAmount.MaxBonus;
            claimPromotion.RequiredAmount = tierAmount.RequiredAmount;
            claimPromotion.Bonus = tierAmount.Bonus;
            claimPromotion.RollOver = tierAmount.RollOver;
            claimPromotion.WinOver = tierAmount.WinOver;

            decimal bonusAmount;
            decimal lockAmount;
            if (claimPromotion.BonusType == BonusType.Amount)
            {
                bonusAmount = tierAmount.Bonus;
                lockAmount = tierAmount.RequiredAmount;
            }
            else
            {
                bonusAmount = Math.Ceiling(tierAmount.Bonus * checkResult.ActualDeposit / 100);
                lockAmount = bonusAmount * 100 / tierAmount.Bonus;
                if (tierAmount.MaxBonus != 0 && bonusAmount > tierAmount.MaxBonus)
                {
                    bonusAmount = tierAmount.MaxBonus;
                    lockAmount = tierAmount.MaxBonus * 100 / tierAmount.Bonus;
                }
            }

            return new CalculateBonusOutput
            {
                result = (int)BonusErrorCode.SUCCESS,
                BonusInfo = JsonConvert.SerializeObject(claimPromotion,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                ExpiryTime = DateTimeOffset.Now.AddDays(claimPromotion.ExpiryDays).ToUnixTimeSeconds(),
                BonusAmount = bonusAmount,
                LockedAmount = lockAmount,  
                RollOver = (lockAmount + bonusAmount) * (claimPromotion.FullFillType == (int)FullFillType.RollOver ? claimPromotion.RollOver : claimPromotion.WinOver)
            };
        }


    }
}
