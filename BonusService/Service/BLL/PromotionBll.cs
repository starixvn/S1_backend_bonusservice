using System;
using System.Data;

using System.Text.RegularExpressions;

using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;

using Newtonsoft.Json;
using Enum = System.Enum;

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
        private readonly CacheHelper _cacheHelper;
        private readonly Dictionary<int, IEnumerable<string>> _specialRebateDisplay;
        private readonly ICommonService _commonService;
        private readonly IEnumerable<IPromotionService> _promotionListService;
        //private readonly Dictionary<PromotionType, Type> _promotionServiceMap;
        //private readonly IServiceProvider _serviceProvider;
        //private readonly IPromotionService _iPromotionService;
        private readonly Func<PromotionType, IPromotionService> _promotionServiceFactory;


        public PromotionBll(IServiceProvider provider, Func<PromotionType, IPromotionService> promotionServiceFactory)
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
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _specialRebateDisplay = configBll.GetConfigValue<Dictionary<int, IEnumerable<string>>>("RebateExcludeGame");
            _commonService = provider.GetService<ICommonService>();
            //_iPromotionService = provider.GetService<IPromotionService>();
            //_promotionServiceMap = new Dictionary<PromotionType, Type>
            //{
            //    { PromotionType.FstDeposit, typeof(FirstDepositService) },
            //    { PromotionType.ReDeposit, typeof(ReDepositService) },
            //    { PromotionType.SpecialBonus, typeof(SpecialBonusService) },
            //};
            _promotionListService = provider.GetService<IEnumerable<IPromotionService>>();
            _promotionServiceFactory = promotionServiceFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="writeErrorLog">是否要寫 errorLog，(後台手動發放不紀錄，強制取得 en 語系錯誤也不記錄)</param>
        /// <returns></returns>
        private ApiResult MakeErrorMessage(ErrorMessageMakerInput input, bool writeErrorLog = true)
        {
            var apiResult1 = new ApiResult
            {
                ErrorCode = input.Result
            };

            var showLangId = string.IsNullOrWhiteSpace(input.CustInfo.UserLang)
                ? "en"
                : input.CustInfo.UserLang;

            switch (input.Result)
            {
                //API error -10xx
                case (int)BonusErrorCode.PromotionNotFound:
                case (int)BonusErrorCode.IpIsBlocked:
                case (int)BonusErrorCode.ProfileOrPhoneNotFinish:
                case (int)BonusErrorCode.CalculateAmountIsNegative:
                case (int)BonusErrorCode.CalculateCashbackTotalBonusLessThenMinPay:
                case (int)BonusErrorCode.CalculateUnknownPromotionType:
                case (int)BonusErrorCode.DuplicateClaimBonus:
                case (int)BonusErrorCode.TierAmountNoFound:
                case (int)BonusErrorCode.SameIp:
                case (int)BonusErrorCode.SameRealName:
                case (int)BonusErrorCode.SameDeviceId:
                case (int)BonusErrorCode.BlockTag:

                //SP Return ERROR -11XX
                case (int)BonusErrorCode.SpBonusNotFound:
                case (int)BonusErrorCode.SpCustomerNotJoinSpecialCode:
                case (int)BonusErrorCode.SpCustomerJoinThisPromotionBefore:
                case (int)BonusErrorCode.SpCustomerBonusStillRunning:
                case (int)BonusErrorCode.SpCustomerDepositNotExceedRequiredAmount:
                case (int)BonusErrorCode.SpCustomerDontHaveFirstDeposit:
                case (int)BonusErrorCode.SpCustomerFirstDepositRuleDontMatch:
                case (int)BonusErrorCode.SpCustomerWithdrawalBefore:
                case (int)BonusErrorCode.SpCustomerAddOtherBonusInPeriodTime:
                case (int)BonusErrorCode.SpCustomerBonusStillRunningOrAddOtherBonusWhichPromotionTypeIn135:
                case (int)BonusErrorCode.SpCustomerDepositApproveBefore:
                case (int)BonusErrorCode.SpCustomerBonusStillRunningOrAddThisBonusBefore:
                case (int)BonusErrorCode.SpCustomerJoinThisRebateBefore:
                case (int)BonusErrorCode.SpCustomerHaveRunningBonusOrJoinThisBonusInPeriodTime:
                case (int)BonusErrorCode.SpUndefinedPromotionType:
                case (int)BonusErrorCode.SpCustomerNotHaveNewScratchCardToJoin:
                    apiResult1.Message = _selLangBll.GetMsg($"msg_{(BonusErrorCode)input.Result}", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                    break;
                //API error -10xx
                case (int)BonusErrorCode.DepositAmountNotEnough:
                    {
                        var dRequiredAmount = input.ClaimPromotion.RequiredAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.ClaimPromotion.RequiredAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}",
                                input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId), dRequiredAmount);
                    }
                    break;
                //API error -10xx
                case (int)BonusErrorCode.MainWalletAmountNotEnough://中心錢包餘額不可少於 {0}
                    {
                        var dRequiredAmount = input.LockedAmount.ToString("#,##0");
                        if (_custInfoService.IsConvertRequired(input.CustInfo.CurrencyId))
                        {
                            dRequiredAmount = $"{input.LockedAmount / 1000:#,##0} ('000)";
                        }

                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}",
                                input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId), dRequiredAmount);
                    }
                    break;
                //AdjustmentBonus Error -12XX
                case (int)BonusErrorCode.AdjustmentBonusPromotionTypeNotMatch:
                    {
                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}", input.CustInfo.SiteId,
                                input.CustInfo.SiteName, showLangId), input.InputBonusCode,
                            Enum.GetName(typeof(PromotionType), (PromotionType)input.InputPromotionType));
                    }
                    break;
                case (int)BonusErrorCode.AdjustmentBonusPromotionNotActive:
                    {
                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}", input.CustInfo.SiteId,
                                input.CustInfo.SiteName, showLangId), input.InputBonusCode);
                    }
                    break;
                case (int)BonusErrorCode.AdjustmentBonusPromotionAmountOverMaxBonus:
                    {
                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}", input.CustInfo.SiteId,
                                input.CustInfo.SiteName, showLangId), input.InputAmount, input.ClaimPromotion.MaxBonus,
                            input.InputBonusCode);
                    }
                    break;
                case (int)BonusErrorCode.AdjustmentBonusCustomerHasRunningBonus:
                    {
                        apiResult1.Message = string.Format(
                            _selLangBll.GetMsg($"msg_{((BonusErrorCode)input.Result).ToString()}", input.CustInfo.SiteId,
                                input.CustInfo.SiteName, showLangId), input.CustInfo.CustId);
                    }
                    break;
                case (int)BonusErrorCode.Error:
                case (int)BonusErrorCode.UnknownError:
                default://您不符合領取此優惠，請聯繫客服以了解詳情。
                    {
                        apiResult1.ErrorCode = (int)BonusErrorCode.UnknownError;
                        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);

                        if (input.CustInfo.SiteId is 2 or 3)
                        {
                            if (input.ClaimPromotion.PromotionType == PromotionType.FreeBet)
                            {
                                apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet", input.CustInfo.SiteId, input.CustInfo.SiteName, showLangId);
                            }
                        }
                    }
                    break;
            }

            var writeToLogErrorMsg = GetWriteToLogErrorMsg(input, apiResult1);
            if (writeErrorLog)
            {
                _logger.LogInformation($"CustId={input.CustInfo.CustId} BonusCode={input.InputBonusCode} apiResult={JsonConvert.SerializeObject(apiResult1)}");
                InsertPromotionFailLog(input.CustInfo, input.ClaimPromotion, (BonusErrorCode)input.Result, writeToLogErrorMsg);
            }

            return apiResult1;
        }

        private string GetWriteToLogErrorMsg(ErrorMessageMakerInput input, ApiResult apiResult1)
        {
            // 自訂的ErrorMsg 如果為空，就使用語系檔內的 ErrorMsg，後台 ErrorMsg 一律寫入 en 語系的訊息
            string writeToLogErrorMsg;
            if (!string.IsNullOrEmpty(input.CustomizeErrorMsg))
            {
                writeToLogErrorMsg = input.CustomizeErrorMsg;
            }
            else
            {
                if (input.CustInfo.UserLang == "en")
                {
                    writeToLogErrorMsg = apiResult1.Message;
                }
                else
                {
                    input.CustInfo.UserLang = "en";
                    var error = MakeErrorMessage(input, false);
                    writeToLogErrorMsg = error.Message;
                }
            }

            return writeToLogErrorMsg;
        }
        private void CancelAllWaitingBonus(int custId, int limit)
        {
            //當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉
            var BonusList = _promotionService.GetDepositBonusQueueByCustId(custId, limit);
            foreach (var depositBonus in BonusList)
            {
                _depositBonusBll.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
            }
        }

        private void AfterRewardSuccess(AfterRewardSuccessInput input)
        {
            if (input.atPage != BonusInfoAtPage.Deposit)
                CancelAllWaitingBonus(input.CustId, 50);
        }

        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, string custIp,
            decimal lockedAmount, int deviceType, BonusInfoAtPage atPage, bool IsFromPromotionUploadCustList = false, string depositTransId = null)
        {
            bonusCode = bonusCode.Trim();
            Customer custInfo;
            CustPromotionWhiteListSettingDto custPromotionWhiteSetting;
            CheckCustInfo(custId, custIp, out custInfo, out custPromotionWhiteSetting);
            ClaimPromotion claimPromotion;
            CustCondition custConditionInfo;
            // 取 Promotion 資料
            if (!IsFromPromotionUploadCustList)
            {
                claimPromotion = _promotionService.GetPromotion<ClaimPromotion>(custId,
                    custInfo.CurrencyId,
                    custInfo.SiteId,
                    bonusCode,
                    deviceType,
                    getDetail: false);
            }
            else
            {
                claimPromotion = _promotionService.GetPromotionForUploadCustList<ClaimPromotion>(custId,
                    custInfo.CurrencyId,
                    custInfo.SiteId,
                    bonusCode,
                    deviceType,
                    getDetail: false);
            }

            var errorInfo = new ErrorMessageMakerInput
            {
                Result = (int)BonusErrorCode.PromotionNotFound,
                InputBonusCode = bonusCode,
                LockedAmount = lockedAmount,
                CustInfo = custInfo,
                ClaimPromotion = claimPromotion,
                CustomizeErrorMsg = string.Empty
            };

            if (claimPromotion == null)
            {
                return MakeErrorMessage(errorInfo);
            }

            if (IsFromPromotionUploadCustList && claimPromotion.PromotionType == PromotionType.SpecialBonus)
            {
                var tempResult = await SetAdjustmentBonusProcessAsync(custId, bonusCode, null, claimPromotion.MaxBonus, (int)PromotionType.SpecialBonus, "", errorInfo, claimPromotion, IsFromPromotionUploadCustList);
                return tempResult;
            }

            // 檢核是否可以領取
            var verifyResult = VerifyPromotion(custInfo, claimPromotion, custPromotionWhiteSetting);
            if (verifyResult.BonusErrorCode != BonusErrorCode.Success)
            {
                errorInfo.Result = (int)verifyResult.BonusErrorCode;
                errorInfo.CustomizeErrorMsg = verifyResult.ErrorMsg;
                return MakeErrorMessage(errorInfo);
            }

            if (!IsFromPromotionUploadCustList)
            {
                custConditionInfo = _promotionService.GetCustConditionInfo(custId, custInfo.CurrencyId, custInfo.SiteId, bonusCode, deviceType);
            }
            else
            {
                var currencyUTC = GetCurrencyUTC(claimPromotion.CurrencyId);
                IPromotionService service = _promotionServiceFactory(claimPromotion.PromotionType);
                custConditionInfo = service.GetCustConditionInfo(custId, custInfo.CurrencyId, custInfo.SiteId, deviceType, bonusCode, (int)claimPromotion.PeriodType, (int)claimPromotion.PromotionType, claimPromotion.RequiredAmount, currencyUTC, true, claimPromotion.CategoryId);
            }

            if (!custConditionInfo.CanJoin)
            {
                errorInfo.Result = custConditionInfo.Result;
                return MakeErrorMessage(errorInfo);
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var calculateBonus = CalculateBonus(claimPromotion, custConditionInfo.Amount, lockedAmount, custId, custInfo.SiteId, custInfo.CurrencyId);
            if (calculateBonus.result <= 0)
            {
                errorInfo.Result = calculateBonus.result;
                return MakeErrorMessage(errorInfo);
            }

            // 計算要join的存款單
            if (claimPromotion.PromotionType == PromotionType.MissionBonus)
            {
                CalculateRefList(custConditionInfo, claimPromotion.RequiredAmount);
            }

            if (atPage == BonusInfoAtPage.Deposit) //由Deposit頁面進來的 reference紀錄指定TransId、Amount
            {
                if (claimPromotion.PromotionType == PromotionType.FstDeposit &&
                    depositTransId != custConditionInfo.RefId)
                {
                    errorInfo.Result = custConditionInfo.Result;
                    return MakeErrorMessage(errorInfo);
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
                IsSpecial = claimPromotion.IsSpecial,
                SpecialCode = claimPromotion.SpecialCode,
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
                AtPage = atPage,
                PeriodType = claimPromotion.PeriodType
            };
            var result = Reward(bonusRewardInput, trans);

            if (result > 0)
            {
                if (bonusRewardInput.PromotionType == PromotionType.RetentionFreeBet)
                {
                    var bonusCustUsingPara = new DynamicParameters(new
                    {
                        _CustId = bonusRewardInput.CustId,
                        _SiteId = bonusRewardInput.SiteId,
                        _BonusCode = bonusRewardInput.BonusCode
                    });
                    result = _mainDbClient.Execute("Service_BonusCustUsingDetail_Set", trans, bonusCustUsingPara);
                }

                if (IsFromPromotionUploadCustList)
                {
                    var promotionUploadCustListPara = new DynamicParameters(new
                    {
                        BonusCode = bonusRewardInput.BonusCode,
                        SiteId = bonusRewardInput.SiteId,
                        CustId = bonusRewardInput.CustId
                    });
                    result = _mainDbClient.Execute("Service_PromotionUploadCustListStatus_Set", trans, promotionUploadCustListPara);
                }

                if (result > 0)
                {
                    result = _promotionService.AfterReward(claimPromotion, custInfo.SiteId, bonusCode, custInfo.CurrencyId,
                        custIp, trans);
                    if (result > 0)
                    {
                        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                        InsertPromotionSuccessLog(custInfo, claimPromotion, verifyResult.UseWhitelist);
                    }
                }
            }

            if (result <= 0)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            }

            var apiResult = new ApiResult();

            if (result > 0)
            {
                if (claimPromotion.WalletType == WalletType.Transfer)
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
                        await _messageService.SendMessageAsync(new Message("System", custInfo.CustId, custInfo.UserLang, content, content));
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
            return MakeErrorMessage(errorInfo);
        }
        private async Task<ApiResult> SetAdjustmentBonusProcessAsync(int custId, string bonusCode, string memo,
            decimal amount, int promotionType, string depositId, ErrorMessageMakerInput errorInfo, ClaimPromotion claimPromotion, bool isFromPromotionUploadCustList)
        {
            var apiResult = new ApiResult();
            var lockedAmount = (decimal)0;
            bonusCode = bonusCode.Trim();
            var custInfo = _custInfoService.GetCustInfo(custId);
            errorInfo.ClaimPromotion = claimPromotion;

            var result = VerifyAdjustmentBonus(custId, custInfo.SiteId, amount, promotionType, claimPromotion);
            if (result != (int)BonusErrorCode.Success)
            {
                errorInfo.Result = result;
                apiResult = MakeErrorMessage(errorInfo, false);
                return apiResult;
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var calculateBonus = CalculateBonus(claimPromotion, amount, lockedAmount, custId,
                custInfo.SiteId, custInfo.CurrencyId, depositId);
            if (calculateBonus.result <= 0)
            {
                errorInfo.Result = calculateBonus.result;
                return MakeErrorMessage(errorInfo, false);
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
                RefList = !string.IsNullOrEmpty(depositId) ? JsonConvert.SerializeObject(new List<Ref>()
                {
                    new Ref ()
                    {
                        DepositId = depositId,
                        Amount = calculateBonus.DepositAmount
                    }
                }) : null,
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
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
            }

            if (result <= 0)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            }

            if (result > 0)
            {
                if (claimPromotion.WalletType == WalletType.Transfer)
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

                if (isFromPromotionUploadCustList)
                {
                    var promotionUploadCustListPara = new DynamicParameters(new
                    {
                        BonusCode = bonusCode,
                        SiteId = custInfo.SiteId,
                        CustId = custId
                    });
                    result = _mainDbClient.Execute("Service_PromotionUploadCustListStatus_Set", trans, promotionUploadCustListPara);
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
            return MakeErrorMessage(errorInfo, false);
        }

        private void CheckCustInfo(int custId, string custIp, out Customer custInfo, out CustPromotionWhiteListSettingDto custPromotionWhiteSetting)
        {
            // 取玩家個人資訊、裝置、白名單設定
            custInfo = _custInfoService.GetCustInfo(custId);
            var custDeviceHistory = _custInfoService.GetCustDeviceId(new List<int>() { custInfo.CustId }, custInfo.SiteId).ToList();
            custInfo.DeviceIds = custDeviceHistory.Select(r => r.DeviceId);
            custInfo.CurrentUseDeviceId = custDeviceHistory.Any() ?
                custDeviceHistory.OrderByDescending(x => x.UpdateTime).Select(x => x.DeviceId).First()
                : string.Empty;
            custInfo.CustIp = custIp;
            custPromotionWhiteSetting = _custInfoService.GetCustPromotionWhiteSetting(custId);
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
                Result = (int)BonusErrorCode.PromotionNotFound,
                InputBonusCode = bonusCode,
                LockedAmount = lockedAmount,
                InputPromotionType = promotionType,
                InputAmount = amount,
                CustInfo = custInfo,
                ClaimPromotion = new ClaimPromotion { }
            };

            var claimPromotion = _promotionService.GetPromotion<ClaimPromotion>(custId, custInfo.CurrencyId,
                custInfo.SiteId, bonusCode, (int)PromotionDeviceType.All, false);
            errorInfo.ClaimPromotion = claimPromotion;

            var tempResult = await SetAdjustmentBonusProcessAsync(custId, bonusCode, memo, amount, promotionType, depositId, errorInfo, claimPromotion, false);
            return tempResult;
        }

        private CheckPromotionResultBaseDto VerifyPromotion(Customer custInfo, ClaimPromotion claimPromotion, CustPromotionWhiteListSettingDto custPromotionWhiteListListSetting)
        {
            if (claimPromotion == null)
            {
                return new CheckPromotionResultBaseDto() { BonusErrorCode = BonusErrorCode.PromotionNotFound };
            }

            if (!CheckProfileFinish(custInfo.CustId, custInfo.SiteId))
            {
                return new CheckPromotionResultBaseDto() { BonusErrorCode = BonusErrorCode.ProfileOrPhoneNotFinish };
            }

            // 檢查 Tag
            var checkPromotionBlockTagResult = CheckPromotionBlockTag(custInfo.Tags, claimPromotion.BlockTags);
            if (checkPromotionBlockTagResult.IsBlockTag)
            {
                return checkPromotionBlockTagResult;
            }

            //檢查IP、Device、RealName
            var useWhitelist = false;
            if (claimPromotion.CheckIPDeviceRealName())
            {
                var checkIpResult = CheckBlockIp(custInfo, claimPromotion, custPromotionWhiteListListSetting);
                if (checkIpResult.IsBlockIp)
                {
                    if (custInfo.CustIp != "18.139.199.197") // 上雲 cloudfront 標頭會誤判IP，先暫時把 PRD Nginx ip 忽略
                    {
                        return checkIpResult;
                    }
                }

                var checkDeviceIdResult = CheckBlockDeviceId(custInfo, claimPromotion, custPromotionWhiteListListSetting);
                if (checkDeviceIdResult.IsBlockDeviceId)
                {
                    return checkDeviceIdResult;
                }

                var checkRealNameResult = CheckBlockRealName(custInfo, claimPromotion, custPromotionWhiteListListSetting);
                if (checkRealNameResult.IsBlockRealName)
                {
                    return checkRealNameResult;
                }

                //IP、DeviceID、RealName 其中有一個是因為白名單通過檢查的，都算有使用白名單
                useWhitelist = (checkIpResult.UseWhitelist || checkDeviceIdResult.UseWhitelist || checkRealNameResult.UseWhitelist);
            }

            return new CheckPromotionResultBaseDto()
            {
                BonusErrorCode = BonusErrorCode.Success,
                UseWhitelist = useWhitelist
            };
        }

        private int VerifyAdjustmentBonus(int custId, int siteId, decimal amount, int promotionType,
            ClaimPromotion claimPromotion)
        {
            if (claimPromotion == null)
            {
                return (int)BonusErrorCode.PromotionNotFound;
            }

            if (claimPromotion.PromotionType != (PromotionType)promotionType)
            {
                return (int)BonusErrorCode.AdjustmentBonusPromotionTypeNotMatch;
            }

            if (claimPromotion.PromotionType == PromotionType.SpecialBonus)
            {
                if (claimPromotion.PromotionAction != 1)
                    return (int)BonusErrorCode.AdjustmentBonusPromotionNotActive;
                if (amount > claimPromotion.MaxBonus)
                    return (int)BonusErrorCode.AdjustmentBonusPromotionAmountOverMaxBonus;
            }

            if (!CheckProfileFinish(custId, siteId)) //Profile Finish & Phone Verified
                return (int)BonusErrorCode.ProfileOrPhoneNotFinish;

            if (_promotionService.GetCustRunningBonus(custId).Any())
            {
                return (int)BonusErrorCode.AdjustmentBonusCustomerHasRunningBonus;
            }

            return (int)BonusErrorCode.Success;
        }

        private bool CheckProfileFinish(int custId, int siteId) //Profile Finish & Phone Verified
        {
            var custInfoProfile = _custInfoService.GetCustInfoProfile(custId, siteId);

            return siteId switch
            {
                7 => !string.IsNullOrEmpty(custInfoProfile.AccountName),

                _ => custInfoProfile.PhoneVerified
                     && !string.IsNullOrEmpty(custInfoProfile.AccountName)
                     && !string.IsNullOrEmpty(custInfoProfile.Birth_y)
                     && !string.IsNullOrEmpty(custInfoProfile.Birth_m)
                     && !string.IsNullOrEmpty(custInfoProfile.Birth_d)
            };
        }

        private CalculateBonusOutput CalculateBonus(ClaimPromotion claimPromotion, decimal amount, decimal applyLockedAmount, int custId, int siteId, int currencyId, string depositId = null)
        {
            var output = new CalculateBonusOutput
            {
                result = (int)BonusErrorCode.Success
            };

            decimal bonusAmount;
            var lockAmount = amount;
            decimal rollover;

            switch (claimPromotion.PromotionType)
            {
                case PromotionType.ReDeposit or PromotionType.FstDeposit:
                    return CalculateTierDepositBonus(claimPromotion, custId, siteId, currencyId, applyLockedAmount, depositId);

                case PromotionType.Cashback:
                    var custPromotion = new CustPromotion
                    {
                        BonusCode = claimPromotion.BonusCode,
                        CategoryId = claimPromotion.CategoryId,
                        PeriodType = claimPromotion.PeriodType,
                    };
                    GetCashBackInfo(custId, siteId, currencyId, custPromotion, true);
                    if (!(custPromotion.canJoin && custPromotion.CashBackDetails?.TotalBonus > claimPromotion.MinPayout))
                    {
                        output.result = (int)BonusErrorCode.CalculateCashbackTotalBonusLessThenMinPay;
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
                case PromotionType.RetentionFreeBet:
                case PromotionType.DeviceBonus:
                case PromotionType.PredictionEvent:
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

                case PromotionType.MissionBonus:
                    return CalculateTierMissionBonus(claimPromotion, custId, siteId, currencyId, amount, applyLockedAmount, depositId);

                default:
                    output.result = (int)BonusErrorCode.CalculateUnknownPromotionType;
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
            //Rebate發放是透過worker，沒辦法手動領取
            if (bonusRewardInput.PromotionType is PromotionType.Rebate)
            {
                throw new ArgumentOutOfRangeException();
            }

            return BonusReward(bonusRewardInput, trans);
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

            if (result > 0 && bonusRewardInput.IsSpecial)
            {
                var specialCodeInput = new DynamicParameters(new
                {
                    bonusRewardInput.CustId,
                    bonusRewardInput.SpecialCode,
                    BonusId = Convert.ToInt32(transId.Replace(bonusRewardInput.Prefix, ""))
                });
                result = _mainDbClient.Execute("Member_SpecialScratchCardTable_ClaimPromotion_Set", trans, specialCodeInput);
            }

            if (result > 0 && bonusRewardInput.LockedAmount > 0)
            {
                result = _walletService.SetBalance(bonusRewardInput.CustId, bonusRewardInput.SiteId, bonusRewardInput.UserName, bonusRewardInput.LockedAmount * -1, (int)bonusRewardInput.FinancialType,
                    bonusRewardInput.SetBalanceMemo, transId,
                    out _, out _, trans);

                if (result > 0)
                {
                    _withdrawalLimitService.SetWithdrawalLimit(bonusRewardInput.CustId, bonusRewardInput.UserName, bonusRewardInput.SiteId, bonusRewardInput.LockedAmount * -1,
                        0, null, transId, (int)WithdrawalLimitLogCreateFromEnum.Bonus, "System", out _, trans);
                }
                else
                {
                    return (int)BonusErrorCode.MainWalletAmountNotEnough;
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

                var calculateDate = bonusRewardInput.PeriodType == VipPeriodType.Weekly
                    ? StartOfWeek(currentTime, DayOfWeek.Monday).ToString("yyyy-MM-dd")
                    : currentTime.ToString("yyyy-MM-dd");

                result = _bonusWalletService.RewardCashBack(
                    new CustCashBackInput(bonusRewardInput.CustId, bonusRewardInput.BonusCode, bonusRewardInput.CategoryId, calculateDate),
                    trans);
            }
            return result;
        }

        public List<CustPromotion> GetPromotionList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetPromotionList(request);
            var promotion = new List<CustPromotion>();

            Customer custInfo = null;
            if (request.IsLogin) custInfo = _custInfoService.GetCustInfo(request.CustId);

            foreach (var promo in promotionList)
            {
                if (promo.PromotionType == PromotionType.RetentionFreeBet)
                {
                    var usable = GetBonusUsingUsable(request.SiteId, request.CustId, promo);
                    if (!usable) //未在清單內則不顯示
                    {
                        continue;
                    }
                }

                if (request.IsLogin)
                {
                    // 加入BlockTag 檢核過濾，如果有Tag命中，列表就不會有這筆
                    if (BlockTagIgnorePromotion(custInfo.Tags, promo.BlockTags))
                    {
                        continue;
                    }

                    var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                        request.SiteId, promo.BonusCode, request.DeviceType);
                    SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);
                }
                else
                {
                    if (promo.HideForUnlogin) // HideForUnlogin = Ture話未登入狀態不顯示
                    {
                        continue;
                    }
                }

                // 調整Cashback時間
                if (promo.PromotionType == PromotionType.Cashback)
                {
                    var offset = GetOffset(request.SiteId);
                    ProcessPeriodTime(promo, DateTime.UtcNow.AddHours(offset), offset);
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
                    if (promo.IsPromotionUploadCustList)
                    {
                        var promotionUploadCustList = _promotionService.GetPromotionUploadCustListByBonusCode(request.SiteId, request.CustId, request.BonusCode);
                        if(promotionUploadCustList != null)
                        {
                            if (promo.PromotionType == PromotionType.SpecialBonus)
                            {
                                promo.RollOver = promotionUploadCustList.RollOver;
                                promo.MaxBonus = promotionUploadCustList.Bonus;
                            }
                            var currencyUTC = GetCurrencyUTC(request.CurrencyId);

                            if (promotionUploadCustList.Status == true)
                            {
                                promo.isJoin = true;
                            }

                            if(promotionUploadCustList.ExpiryDate < currencyUTC.CurrentTime)
                            {
                                promo.canJoin = false;
                            }

                            IPromotionService service = _promotionServiceFactory(promo.PromotionType);
                            var custCondition = service.GetCustConditionInfo(request.CustId, request.CurrencyId, request.SiteId, request.DeviceType, promo.BonusCode, (int)promo.PeriodType, (int)promo.PromotionType, promo.RequiredAmount, currencyUTC, true, promo.CategoryId);
                            SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);
                        }
                        else
                        {
                            //沒有在領取名單內
                            promo.canJoin = false;
                            var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                            request.SiteId, promo.BonusCode, request.DeviceType);
                            SetPromotion(request.SiteId, request.CustId, request.CurrencyId, true, promo, custCondition);
                        }
                    }
                    else
                    {
                        var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                            request.SiteId, promo.BonusCode, request.DeviceType);
                        SetPromotion(request.SiteId, request.CustId, request.CurrencyId, true, promo, custCondition);
                    }
                }
                else
                {
                    if (promo.HideForUnlogin) // HideForUnlogin = Ture話未登入狀態不顯示
                    {
                        promo.canJoin = false;
                    }
                }

                // 調整Cashback時間
                if (promo.PromotionType == PromotionType.Cashback)
                {
                    var offset = GetOffset(request.SiteId);
                    ProcessPeriodTime(promo, DateTime.UtcNow.AddHours(offset), offset);

                    // 調整顯示
                }
            }

            return promo;
        }

        public List<CustPromotion> GetSpecialPromotionList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetSpecialPromotionList(request);
            var custInfo = _custInfoService.GetCustInfo(request.CustId);

            var promotion = new List<CustPromotion>();
            foreach (var promo in promotionList)
            {
                if (BlockTagIgnorePromotion(custInfo.Tags, promo.BlockTags))
                {
                    continue;
                }

                var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                    request.SiteId, promo.BonusCode, request.DeviceType);
                SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);
                SetPromotionDetail(request.LangId, request.DefLang, true, promo);

                promotion.Add(promo);
            }

            return promotion;
        }

        public List<CustPromotion> GetDepositPromotionList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetDepositPromotionList(request);
            var custInfo = _custInfoService.GetCustInfo(request.CustId);

            var promotion = new List<CustPromotion>();
            foreach (var promo in promotionList)
            {
                if (BlockTagIgnorePromotion(custInfo.Tags, promo.BlockTags)) continue;
                SetPromotionDetail(request.LangId, request.DefLang, true, promo);
                promotion.Add(promo);
            }

            return promotion;
        }

        private void SetPromotion(int siteId, int custId, int currencyId, bool getCashbackDetail, CustPromotion promo, CustCondition custCondition)
        {
            promo.canJoin = custCondition.CanJoin;
            promo.Result = custCondition.Result;

            if (promo.PromotionType is PromotionType.FstDeposit or PromotionType.ReDeposit
                && promo.canJoin)
            {
                promo.DepositId = custCondition.RefId;
                promo.DepositAmount = custCondition.Amount;
                promo.RefList = custCondition.reference;

                promo.MaxAmount = promo.PromotionType == PromotionType.FstDeposit
                    ? _walletService.GetBalance(custId)
                    : promo.DepositAmount;

                if (promo.PromotionType == PromotionType.FstDeposit && promo.MaxAmount > promo.DepositAmount)
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

                if (promo.PromotionType is PromotionType.FstDeposit or PromotionType.ReDeposit && promo.TierAmountList.Any())
                {
                    promo.CalculateCanJoinTierAmount(promo.DepositAmount, 0);
                    promo.canJoin = promo.CanJoinTierAmountList.Any();
                }
            }

            if (promo.PromotionType == PromotionType.MissionBonus)
            {
                var progressData = _promotionService.GetCustPromotionMissionProgress(custId, currencyId, promo.TurnoverTpGameTypes, (int)promo.PeriodType);
                promo.MissionBonusProgress = new MissionBonusProgress
                {
                    DepositAmountSum = custCondition.Amount,
                    TurnoverSum = progressData.TurnoverSum
                };

                if (promo.canJoin)
                {
                    promo.DepositId = custCondition.RefId;
                    promo.DepositAmount = custCondition.Amount;
                    promo.RefList = custCondition.reference;

                    if (promo.TierAmountList.Any())
                    {
                        promo.CalculateCanJoinTierAmount(promo.DepositAmount, progressData.TurnoverSum);
                        promo.canJoin = promo.CanJoinTierAmountList.Any();
                    }
                }
            }

            if (promo.PromotionType == PromotionType.RetentionFreeBet)
            {
                var isUsed = GetCustBonusUsing(siteId, custId, promo);
                if (!isUsed) return;
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
                GetCashBackInfo(custId, siteId, currencyId, promo, getCashbackDetail);
            }
        }

        private bool GetCustBonusUsing(int siteId, int custId, Promotion promo)
        {
            var isUsed = _promotionService.GetBonusCustUsingDetail(siteId, promo.BonusCode)
                .Any(x => x.Status == (int)BonusUsingStatusEnum.Used && x.CustId == custId);

            return isUsed;
        }

        private bool GetBonusUsingUsable(int siteId, int custId, Promotion promo)
        {
            var usingList = _promotionService.GetBonusCustUsingDetail(siteId, promo.BonusCode);
            var usable = usingList.Any(x => x.CustId == custId);

            return usable;
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

            var resultCode = _promotionService.SetScratchCardCode(request);
            _logger.LogInformation("SetScratchCardCode result=" + resultCode);

            return resultCode;
        }

        public bool SetSpecialCode(SpecialCodeRequest request)
        {
            var result = _promotionService.SetSpecialCode(request);

            if (!result)
            {
                _logger.LogInformation("SetSpecialCode failed.");
            }

            return result;
        }

        public bool CheckSpecialCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length > 20)
            {
                _logger.LogInformation($"{specialCode} is empty or length longer than 20.");
                return false;
            }
            var regex = new Regex("^[- !@#$%^&*a-zA-Z0-9]{1,20}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);
            var result = mPwd.Success;

            if (!result)
            {
                _logger.LogInformation($"{specialCode} is not match Regex:^[- !@#$%^&*a-zA-Z0-9]{1,20}$");
            }

            return result;
        }

        public bool CheckScratchCardCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length != 12)
            {
                _logger.LogInformation($"{specialCode} is empty or length is not match (length=12).");
                return false;
            }

            var regex = new Regex("^[a-zA-Z]{5}-[a-zA-Z0-9]{6}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);
            var result = mPwd.Success;

            if (!result)
            {
                _logger.LogInformation($"{specialCode} is not match Regex:^[a-zA-Z]{5}-[a-zA-Z0-9]{6}$");
            }

            return result;
        }

        public bool CheckVipLevelWithBlockTags(int custId, string specialCode)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var vipLevelWithBlockTagsSetting = _promotionService.GetVipLevelWithBlockTags(custInfo.SiteId, specialCode);

            if (vipLevelWithBlockTagsSetting is { BlockTags: { } } && vipLevelWithBlockTagsSetting.BlockTags.Any())
            {
                // 取交集
                var intersectTagResult = custInfo.Tags.Intersect(vipLevelWithBlockTagsSetting.BlockTags).ToList();
                // 如果有相同 tag 表示玩家不能領取
                if (intersectTagResult.Any())
                {
                    _logger.LogInformation($"custId={custId} has block tags: {JsonConvert.SerializeObject(intersectTagResult)}");
                    return false;
                }
            }

            if (vipLevelWithBlockTagsSetting is { VipLevels: { } } && vipLevelWithBlockTagsSetting.VipLevels.Any())
            {
                // 如果 Vip Level 命中就可以領
                var checkVip = vipLevelWithBlockTagsSetting.VipLevels.Any(x => x.Equals(custInfo.UserLevel));

                if (!checkVip)
                {
                    _logger.LogInformation(
                        $"custId={custId}, vipLevel={custInfo.UserLevel} not match vip setting: {JsonConvert.SerializeObject(vipLevelWithBlockTagsSetting.VipLevels)}");
                }

                return checkVip;
            }

            return true;
        }

        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialCode(siteId, SpecialCode);
        }

        public PromotionBySpecialScratchCard GetPromotionBySpecialScratchCard(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialScratchCard(siteId, SpecialCode);
        }

        public BonusInfoByBonusCode GetBonusInfoByBonusCode(int CustId, string BonusCode)
        {
            return _promotionService.GetBonusInfoByBonusCode(CustId, BonusCode);
        }

        public IEnumerable<RebateExcludeGameDetail> GetRebateExcludeGameList(GetRebateExcludeGameListRequest request)
        {
            var source = GetRebateExcludeGameSource(request);

            return SetLiveCasino(source);
        }

        private IEnumerable<RebateExcludeGameDetail> GetRebateExcludeGameSource(GetRebateExcludeGameListRequest request)
        {
            var key = $"GetRebateExcludeGameList_{request.SiteId}_{request.CurrencyId}_{request.VipLevel}_{request.LangId}";
            return _cacheHelper.TryGetOrCreate(key,
               new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
               i => _promotionService.GetRebateExcludeGameList(request));
        }

        private IEnumerable<RebateExcludeGameDetail> SetLiveCasino(IEnumerable<RebateExcludeGameDetail> data)
        {
            var result = data.Select(item =>
            {
                if (_specialRebateDisplay.ContainsKey(item.TpGameType) && _specialRebateDisplay[item.TpGameType].Contains(item.GameId))
                    return item;

                if (item.TpCategoryId is 3)
                {
                    item.GameId = null;
                    item.GameName = "All Blackjack";
                }
                return item;
            });
            return result
                .GroupBy(x => new { x.TpName, x.TpCategoryName,x.TpGameType ,x.TpGameTypeName, x.GameName })
                .Select(s => new RebateExcludeGameDetail(s.Key.TpName, s.Key.TpCategoryName,s.Key.TpGameType ,s.Key.TpGameTypeName, s.Key.GameName));
        }


        private void GetCashBackInfo(int custId, int siteId, int currencyId, CustPromotion promotion, bool getDetail)
        {
            var offset = GetOffset(siteId);
            var currentTime = DateTime.UtcNow.AddHours(offset);
            var calculateDate = promotion.PeriodType == VipPeriodType.Weekly
                ? StartOfWeek(currentTime, DayOfWeek.Monday).ToString("yyyy-MM-dd")
                : currentTime.ToString("yyyy-MM-dd");

            // 用 calculateDate 去 CustCashBack 資料表找有沒有相對應的 Bonus
            var result = _promotionService.GetCustCashBackCheck(custId, promotion, calculateDate).ToList();

            // 判斷領取時間是 null，並且有 CustCashBack 資料
            promotion.isJoin = result.FirstOrDefault()?.Claimed ?? false;
            promotion.canJoin = result.Any();

            // SET promotion.PeriodFrom & promotion.PeriodTo
            ProcessPeriodTime(promotion, currentTime, offset);

            if (!getDetail) return;

            var dateRange = GetCashBackInfoDateRange(promotion.PeriodType, currentTime);

            // 用 BonusCode 取得該名玩家(上周 or 昨日) CashBackInfo 的第三方遊戲輸贏明細
            var cashBackDetails = _promotionService
                .GetCustCashBackDetails(custId, promotion, dateRange.StartDate, dateRange.EndDate)
                .ToList();

            if (cashBackDetails.Any())
            {
                // 取得該 Promotion 的設定
                var promotionCashBackSetting = _promotionService.GetCashBackSetting(custId, promotion).ToList();

                // 將Promotion 設定的 MinLossAmount，填入玩家第三方遊戲明細紀錄
                foreach (var detail in cashBackDetails)
                {
                    var cashBackSetting = promotionCashBackSetting.FirstOrDefault(setting => setting.TpId == detail.TpId && setting.CategoryId == detail.CategoryId && setting.Percentage == detail.Percentage);
                    if (cashBackSetting is not null)
                    {
                        detail.MinLossAmount = cashBackSetting.MinLossAmount;
                    }
                }
            }

            // 取得該 Promotion 計算發放的總金額 => CustCashBack
            var custCashBack = _promotionService.GetCustCashBack(custId, siteId, currencyId, promotion, calculateDate);
            if (custCashBack is null)
            {
                return;
            }

            custCashBack.CashBackDetail = cashBackDetails;
            promotion.CashBackDetails = custCashBack;
        }

        private CashBackInfoDateRange GetCashBackInfoDateRange(VipPeriodType periodType, DateTime currentTime)
        {
            switch (periodType)
            {
                case VipPeriodType.Weekly:
                    var lastMonday = StartOfWeek(currentTime.AddDays(-7), DayOfWeek.Monday).Date;
                    var lastSunday = lastMonday.AddDays(6);
                    return new CashBackInfoDateRange(lastMonday, lastSunday);
                case VipPeriodType.Daily:
                    return new CashBackInfoDateRange(currentTime.AddDays(-2), currentTime.AddDays(-1));
                default:
                    throw new NotImplementedException("GetCashBackInfoDateRange VipPeriodType Implemented");
            }
        }

        private void ProcessPeriodTime(CustPromotion promotion, DateTime currentTime, int offset)
        {
            var checkTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            if (promotion.PeriodFrom <= checkTime && checkTime <= promotion.PeriodTo)
            {
                switch (promotion.PeriodType)
                {
                    case VipPeriodType.Weekly:
                        var utcMonday = StartOfWeek(currentTime, DayOfWeek.Monday).AddHours(-offset);
                        var utcSunday = utcMonday.AddDays(7).AddTicks(-1);
                        promotion.PeriodFrom = ((DateTimeOffset)utcMonday).ToUnixTimeSeconds();
                        promotion.PeriodTo = ((DateTimeOffset)utcSunday).ToUnixTimeSeconds();
                        break;
                    case VipPeriodType.Daily:
                        var today = currentTime.Date;
                        var startTime = new DateTimeOffset(new DateTime(today.Year, today.Month, today.Day), TimeSpan.FromHours(offset));
                        var endTime = new DateTimeOffset(startTime.DateTime.AddDays(1).AddTicks(-1), TimeSpan.FromHours(offset));
                        promotion.PeriodFrom = startTime.ToUnixTimeSeconds();
                        promotion.PeriodTo = endTime.ToUnixTimeSeconds();
                        break;
                }
            }
        }

        private int GetOffset(int siteId)
        {
            var offset = 8;
            if (siteId is 2 or 3)
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

        private CalculateBonusOutput CalculateTierDepositBonus(ClaimPromotion claimPromotion, int custId, int siteId, int currencyId, decimal amount, string depositId = null)
        {
            var depositAmount = amount;

            //帶DepositId使用存款金額計算TierAmount
            if (depositId != null)
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
                depositAmount = checkResult.ActualDeposit;
            }

            var tierAmount = claimPromotion.FindTierAmountLevel(depositAmount, 0, 0);
            if (tierAmount == null)
            {
                return new CalculateBonusOutput
                {
                    result = (int)BonusErrorCode.TierAmountNoFound
                };
            }

            claimPromotion.MaxBonus = tierAmount.MaxBonus;
            claimPromotion.RequiredAmount = tierAmount.RequiredAmount;
            claimPromotion.Bonus = tierAmount.Bonus;
            claimPromotion.RollOver = tierAmount.RollOver;
            claimPromotion.WinOver = tierAmount.WinOver;

            var canJoinTierAmount = tierAmount.CalculateTierAmount(claimPromotion.PromotionType, claimPromotion.BonusType, depositAmount);
            var bonusAmount = canJoinTierAmount.BonusAmount;
            var lockAmount = canJoinTierAmount.LockedAmount;

            return new CalculateBonusOutput
            {
                result = (int)BonusErrorCode.Success,
                BonusInfo = JsonConvert.SerializeObject(claimPromotion, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                ExpiryTime = DateTimeOffset.Now.AddDays(claimPromotion.ExpiryDays).ToUnixTimeSeconds(),
                BonusAmount = bonusAmount,
                LockedAmount = lockAmount,
                RollOver = (lockAmount + bonusAmount) * (claimPromotion.FullFillType == (int)FullFillType.RollOver ? claimPromotion.RollOver : claimPromotion.WinOver),
                DepositAmount = depositAmount
            };
        }

        private CalculateBonusOutput CalculateTierMissionBonus(ClaimPromotion claimPromotion, int custId, int siteId, int currencyId, decimal depositAmountSum, decimal bonus, string depositId = null)
        {
            //帶DepositId使用存款金額計算TierAmount
            if (depositId != null)
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
            }

            var progressData = _promotionService.GetCustPromotionMissionProgress(custId, currencyId, claimPromotion.TurnoverTpGameTypes, (int)claimPromotion.PeriodType);

            var tierAmount = claimPromotion.FindTierAmountLevel(depositAmountSum, progressData.TurnoverSum, bonus);
            if (tierAmount == null)
            {
                return new CalculateBonusOutput
                {
                    result = (int)BonusErrorCode.TierAmountNoFound
                };
            }

            claimPromotion.Bonus = tierAmount.Bonus;
            claimPromotion.RollOver = tierAmount.RollOver;
            claimPromotion.WinOver = tierAmount.WinOver;
            claimPromotion.RequiredAmount = tierAmount.RequiredAmount;
            return new CalculateBonusOutput
            {
                result = (int)BonusErrorCode.Success,
                BonusInfo = JsonConvert.SerializeObject(claimPromotion, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                ExpiryTime = DateTimeOffset.Now.AddDays(claimPromotion.ExpiryDays).ToUnixTimeSeconds(),
                BonusAmount = tierAmount.Bonus,
                RollOver = tierAmount.Bonus * (claimPromotion.FullFillType == (int)FullFillType.RollOver ? claimPromotion.RollOver : claimPromotion.WinOver)
            };
        }

        public PopUpPromotion GetPopUpPromotion(CustPromotionRequest request)
        {
            PopUpPromotion result = new PopUpPromotion();
            Customer custInfo = _custInfoService.GetCustInfo(request.CustId);
            List<TempPopUpPromotion> tempPopUpPromotions = new List<TempPopUpPromotion>();
            var promotionList = _promotionService.GetPromotionList(request).Where(x => x.IsPopUp);

            foreach (var promotion in promotionList)
            {
                if (promotion.PromotionType == PromotionType.RetentionFreeBet)
                {
                    var bonusCustUsingDetail = GetBonusCustUsingDetail(request.SiteId, promotion);

                    //Status為0代表還沒申請過
                    if (bonusCustUsingDetail.Any(x => x.CustId == request.CustId && x.Status == 0))
                    {
                        tempPopUpPromotions.Add(new TempPopUpPromotion()
                        {
                            BonusCode = promotion.BonusCode,
                            Bonus = promotion.Bonus,
                            MaxBonus = promotion.MaxBonus,
                            BlockTags = promotion.BlockTags
                        });
                    }
                }
            }

            //檢核BlockTag
            tempPopUpPromotions = tempPopUpPromotions.Where(x => !BlockTagIgnorePromotion(custInfo.Tags, x.BlockTags)).ToList();

            if (tempPopUpPromotions.Any())
            {
                //取金額最高的
                result.BonusCode = tempPopUpPromotions.FirstOrDefault(x => x.Bonus == tempPopUpPromotions.Max(y => y.Bonus)).BonusCode;
            }

            return result;
        }

        public PopUpPromotion GetRetentionPromotion(CustPromotionRequest request)
        {
            var result = new PopUpPromotion();
            var promotionList = _promotionService.GetPromotionList(request)
                .Where(x => x.IsPopUp && x.PromotionType == PromotionType.RetentionFreeBet);

            // 檢查是否有進行中RetentionFreeBet Bonus
            var checkRunningBonus = _promotionService.CheckRunningBonus(request.CustId);
            if (promotionList.Any(i => i.BonusCode == checkRunningBonus?.BonusCode))
            {
                result.BonusCode = checkRunningBonus.BonusCode;
                return result;
            }

            var tempPromotions = new List<TempPopUpPromotion>();
            foreach (var promotion in promotionList)
            {
                var bonusCustUsingDetail = GetBonusCustUsingDetail(request.SiteId, promotion);

                //Status為0代表還沒申請過
                if (bonusCustUsingDetail.Any(x => x.CustId == request.CustId && x.Status == 0))
                {
                    tempPromotions.Add(new TempPopUpPromotion()
                    {
                        BonusCode = promotion.BonusCode,
                        Bonus = promotion.Bonus,
                        MaxBonus = promotion.MaxBonus,
                        BlockTags = promotion.BlockTags
                    });
                }
            }

            //檢核BlockTag，並且用Bonus降排
            var custInfo = _custInfoService.GetCustInfo(request.CustId);
            //檢核BlockTag
            tempPromotions = tempPromotions.Where(x => !BlockTagIgnorePromotion(custInfo.Tags, x.BlockTags)).ToList();

            if (tempPromotions.Any())
            {
                //取金額最高的
                result.BonusCode = tempPromotions.FirstOrDefault(x => x.Bonus == tempPromotions.Max(y => y.Bonus)).BonusCode;
            }

            return result;
        }

        public List<CustPromotion> GetCustPrivatePromotionList(CustPrivatePromotionRequest request)
        {
            var custPrivatePromotionList = _promotionService.GetCustPrivatePromotionList(request).ToList();
            var custInfo = _custInfoService.GetCustInfo(request.CustId);
            var promotion = new List<CustPromotion>();

            foreach (var privatePromotion in custPrivatePromotionList)
            {
                var promo = _promotionService.GetPromotion<CustPromotion>(request.CustId, request.CurrencyId, request.SiteId, privatePromotion.BonusCode, request.DeviceType, false);
                var custCondition = _promotionService.GetCustConditionInfo(request.CustId, request.CurrencyId,
                        request.SiteId, promo.BonusCode, request.DeviceType);
                SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);

                // 調整Cashback時間
                if (promo.PromotionType == PromotionType.Cashback)
                {
                    var offset = GetOffset(request.SiteId);
                    ProcessPeriodTime(promo, DateTime.UtcNow.AddHours(offset), offset);
                }

                promotion.Add(promo);
            }

            return promotion;
        }

        public List<CustPromotion> GetPromotionUploadCustList(CustPromotionRequest request)
        {
            var promotionList = _promotionService.GetPromotionUploadCustList(request).ToList();
            var allUploadSpecialBonusList = AllUploadSpecialBonusList(request);
            var notInCustList = allUploadSpecialBonusList.Where(b => !promotionList.Any(a => a.BonusCode == b.BonusCode)).ToList();
            promotionList.AddRange(notInCustList);

            var promotion = new List<CustPromotion>();

            var currencyUTC = GetCurrencyUTC(request.CurrencyId);

            foreach (var promo in promotionList)
            {
                if (request.IsLogin)
                {
                    IPromotionService service = _promotionServiceFactory(promo.PromotionType);
                    var custCondition = service.GetCustConditionInfo(request.CustId, request.CurrencyId, request.SiteId, request.DeviceType, promo.BonusCode, (int)promo.PeriodType, (int)promo.PromotionType, promo.RequiredAmount, currencyUTC, promo.InPromotionUploadCustList, promo.CategoryId);

                    SetPromotion(request.SiteId, request.CustId, request.CurrencyId, false, promo, custCondition);

                }
                else
                {
                    if (promo.HideForUnlogin) // HideForUnlogin = Ture話未登入狀態不顯示
                    {
                        continue;
                    }
                }
                
                promotion.Add(promo);
            }

            return promotion;
        }

        #region PrivateMethod

        private CheckPromotionBlockIp CheckBlockIp(Customer custInfo, ClaimPromotion claimPromotion, CustPromotionWhiteListSettingDto whiteListSetting)
        {
            var result = new CheckPromotionBlockIp
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockIp = false,
                UseWhitelist = false
            };

            // 判斷 Promotion 是否有設定要檢查 IP
            if (!claimPromotion.SameIpRestrict) return result;

            if (whiteListSetting is { IpEnable: true }) // 判斷玩家身上有沒有白名單可以跳過檢查
            {
                result.BonusErrorCode = BonusErrorCode.Success;
                result.IsBlockIp = false;
                result.UseWhitelist = true;
                return result;
            }

            // 沒有 IP 就跳過檢查，不進 db
            if (string.IsNullOrEmpty(custInfo.CustIp)) return result;

            // check db 相同 IP 領取是否達到上限
            result = _promotionService.CheckBlockIpFromDb(claimPromotion, custInfo.SiteId, custInfo.CustIp);
            return result;
        }

        private CheckPromotionBlockDeviceId CheckBlockDeviceId(Customer custInfo, ClaimPromotion claimPromotion, CustPromotionWhiteListSettingDto whiteListSetting)
        {
            var result = new CheckPromotionBlockDeviceId
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockDeviceId = false,
                UseWhitelist = false
            };

            // 如果玩家沒有 DeviceId 不檢查
            if (!custInfo.DeviceIds.Any()) return result;

            // 判斷 Promotion 是否有設定要檢查 Device
            if (!claimPromotion.SameDeviceIdRestrict) return result;

            if (whiteListSetting is { DeviceEnable: true }) // 判斷玩家身上有沒有白名單可以跳過檢查
            {
                result.BonusErrorCode = BonusErrorCode.Success;
                result.IsBlockDeviceId = false;
                result.UseWhitelist = true;
                return result;
            }

            // check db 相同 DeviceId 領取是否達到上限
            result = _promotionService.CheckBlockDeviceIdFromDb(claimPromotion, custInfo.SiteId, custInfo.DeviceIds);
            return result;
        }

        private CheckPromotionBlockTags CheckPromotionBlockTag(List<int> custTags, List<int> promotionBlockTags)
        {
            var result = new CheckPromotionBlockTags
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockTag = false,
                UseWhitelist = false
            };

            // 如果 promotion 沒有設定 tag 就不用檢查
            if (!promotionBlockTags.Any()) return result;

            // 取交集
            var intersectTagResult = custTags.Intersect(promotionBlockTags).ToList();

            // 如果沒有相同 tag 表示可以領
            if (!intersectTagResult.Any()) return result;

            var firstTagId = intersectTagResult.First();

            // db 取得 tagName 
            var tagName = _promotionService.GetTagName(firstTagId).TagName;

            result.BonusErrorCode = BonusErrorCode.BlockTag;
            result.IsBlockTag = true;
            result.ErrorMsg = $"Block Tag：{tagName}";
            return result;
        }

        private bool BlockTagIgnorePromotion(List<int> custTags, List<int> promotionBlockTags)
        {
            // 如果 promotion 沒有設定 tag 就不用檢查
            if (!promotionBlockTags.Any()) return false;

            // 取交集
            var intersectTagResult = custTags.Intersect(promotionBlockTags).ToList();

            return intersectTagResult.Any();
        }

        private CheckPromotionBlockRealName CheckBlockRealName(Customer custInfo, ClaimPromotion claimPromotion, CustPromotionWhiteListSettingDto whiteListSetting)
        {
            var result = new CheckPromotionBlockRealName
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockRealName = false,
                UseWhitelist = false
            };

            // 判斷 Promotion 是否有設定要檢查 RealName
            if (!claimPromotion.SameRealNameRestrict) return result;

            if (whiteListSetting is { RealNameEnable: true })  // 判斷玩家身上有沒有白名單可以跳過檢查
            {
                result.BonusErrorCode = BonusErrorCode.Success;
                result.IsBlockRealName = false;
                result.UseWhitelist = true;
                return result;
            }

            // check db 相同 RealName 領取是否達到上限
            result = _promotionService.CheckBlockRealNameFromDb(claimPromotion, custInfo.SiteId, custInfo.AccountName);
            return result;
        }

        private int InsertPromotionFailLog(Customer custInfo, ClaimPromotion promotion, BonusErrorCode bonusErrorCode, string errorMsg)
        {
            return _promotionService.InsertPromotionFailLog(
                bonusCode: promotion.BonusCode,
                promotionType: promotion.PromotionType,
                errorCode: bonusErrorCode,
                errorMsg: errorMsg,
                siteId: custInfo.SiteId,
                custId: custInfo.CustId,
                currencyId: custInfo.CurrencyId);
        }

        private int InsertPromotionSuccessLog(Customer custInfo, ClaimPromotion promotion, bool useWhitelist)
        {
            return _promotionService.InsertPromotionSuccessLog(
                bonusCode: promotion.BonusCode,
                promotionType: promotion.PromotionType,
                ip: custInfo.CustIp,
                deviceId: custInfo.CurrentUseDeviceId,
                realName: custInfo.AccountName,
                siteId: custInfo.SiteId,
                custId: custInfo.CustId,
                userName: custInfo.UserName,
                currencyId: custInfo.CurrencyId,
                useWhitelist: useWhitelist
            );
        }

        private IEnumerable<BonusCustUsingList> GetBonusCustUsingDetail(int siteId, Promotion promo)
        {
            return _promotionService.GetBonusCustUsingDetail(siteId, promo.BonusCode);
        }

        private void CalculateRefList(CustCondition custCondition, decimal requiredAmount)
        {
            var refList = new List<Ref>();
            var amount = (decimal)0;
            foreach (var refModel in custCondition.reference)
            {
                amount += refModel.Amount;
                refList.Add(new Ref
                {
                    DepositId = refModel.DepositId,
                    Amount = refModel.Amount
                });

                if (amount >= requiredAmount)
                {
                    break;
                }
            }

            custCondition.reference = refList;
            custCondition.RefList = JsonConvert.SerializeObject(refList);
        }

        public bool CheckPromotionRuleBonusInfo(int custId, DateTime todayUtcStart, DateTime todayUtcEnd)
        {
            return _promotionService.CheckPromotionRuleBonusInfo(custId, todayUtcStart, todayUtcEnd);
        }
        public bool CheckPromotionRuleWithdrawalInfo(int custId)
        {
            return _promotionService.CheckPromotionRuleWithdrawalInfo(custId);
        }
        public IEnumerable<PromotionRuleDepositInfo> GetPromotionRuleDepositInfo(int custId)
        {
            return _promotionService.GetPromotionRuleDepositInfo(custId);
        }

        private CurrencyUTC GetCurrencyUTC(int currencyId)
        {
            var timeZone = _commonService.GetCurrencyTimeZones().FirstOrDefault(x => x.CurrencyId == currencyId).TimeZone;

            int hoursOffset = 0;
            int minutesOffset = 0;

            if ((timeZone.StartsWith("+") || timeZone.StartsWith("-")) && timeZone.Length == 6)
            {
                // 提取小时和分钟部分（跳过符号部分）
                string numericPart = timeZone.Substring(1);

                // 使用冒号分割小时和分钟
                string[] parts = numericPart.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out hoursOffset) && int.TryParse(parts[1], out minutesOffset))
                {
                    // 处理正负符号
                    if (timeZone.StartsWith("-"))
                    {
                        hoursOffset = -hoursOffset;
                    }
                }
            }

            TimeSpan timeOffset = new TimeSpan(hoursOffset, minutesOffset, 0);

            // 获取当前 UTC 时间
            DateTime utcNow = DateTime.UtcNow;

            // 将 UTC 时间根据偏移量转换为目标时区时间
            DateTime currentTime = utcNow + timeOffset;  // 当前时区的时间
            DateTime todayStart = currentTime.Date;      // 当地日期（忽略时间部分）
            DateTime nextDayStart = todayStart.AddDays(1); // 第二天的开始时间

            // 将当地时间转换回 UTC 时间
            DateTime todayUtcStart = todayStart - timeOffset;  // 将当地时间转换为 UTC 时间
            DateTime todayUtcEnd = nextDayStart - timeOffset;  // 将第二天的开始时间转换为 UTC 时间

            // 将 UTC 时间加上 1 秒，表示当天的最后一刻
            todayUtcEnd = todayUtcEnd.AddSeconds(-1);

            return new CurrencyUTC
            {
                CurrentTime = currentTime,
                TodayUtcStart = todayUtcStart,
                TodayUtcEnd = todayUtcEnd,
                TimeZone = timeZone
            };
        }
        public void CalculatePeriodRange(int periodType, CurrencyUTC range)
        {
            DateTime currentTime = range.CurrentTime;
            DateTime startDate, endDate;

            switch (periodType)
            {
                case 0: // PeriodType = 0 or 1
                case 1:
                    range.UTC_StartTime = range.TodayUtcStart;
                    range.UTC_EndTime = range.TodayUtcEnd;
                    break;

                case 2: // PeriodType = 2
                    startDate = currentTime.Date.AddDays(-((int)currentTime.DayOfWeek));
                    endDate = startDate.AddDays(7);
                    range.UTC_StartTime = startDate;
                    range.UTC_EndTime = endDate.AddSeconds(-1);
                    break;

                case 3: // PeriodType = 3
                    startDate = new DateTime(currentTime.Year, currentTime.Month, 1);
                    endDate = startDate.AddMonths(1);
                    range.UTC_StartTime = startDate;
                    range.UTC_EndTime = endDate.AddSeconds(-1);
                    break;
            }
        }
        public bool GetPromotionRuleBonusInfoByBonusCode(string bonusCode)
        {
            return _promotionService.CheckPromotionRuleBonusInfoByBonusCode(bonusCode);
        }

        public bool CheckPromotionRuleBonusInfoByPeriodType(int promotionType, int periodType, DateTime todayUtcStart, DateTime todayUtcEnd, DateTime uTC_StartTime, DateTime uTC_EndTime, int categoryId, int custId)
        {
            return _promotionService.CheckPromotionRuleBonusInfoByPeriodType(promotionType, periodType, todayUtcStart, todayUtcEnd, uTC_StartTime, uTC_EndTime, categoryId, custId);
        }
        public PromotionRuleDepositInfo GetPromotionRuleDepositInfoByTime(DateTime currentTime, string timezone, int custId)
        {
            return _promotionService.GetPromotionRuleDepositInfoByTime(currentTime, timezone, custId);
        }
        private IEnumerable<UploadCustPromotion> AllUploadSpecialBonusList(CustPromotionRequest request)
        {
            return _promotionService.AllUploadSpecialBonusList(request);
        }
        public bool CheckPromotionRuleRunningBonus(int custId)
        {
            return _promotionService.CheckPromotionRuleRunningBonus(custId);
        }

        #endregion
    }
}
