using BonusService.Service.DAL;
using BonusService.Service.Model;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BonusService.Service.Library;

namespace BonusService.Service.BLL
{
    public class DepositBonusBLL
    {
        private readonly DepositBonusService depositBonusService;
        private readonly string apiUrl;
        private readonly CourierAdapter courierAdapter;
        private readonly Dictionary<int, bool> isConvert = new Dictionary<int, bool>()
        {
            {51,false },{15,false }
        };

        public DepositBonusBLL(DepositBonusService depositBonusService, IConfiguration configuration, CourierAdapter courierAdapter)
        {
            this.depositBonusService = depositBonusService;
            this.apiUrl = configuration.GetValue<string>("TpService");
            this.courierAdapter = courierAdapter;
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            return depositBonusService.GetDepositBonusQueue();
        }
        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            return depositBonusService.GetDepositBonusQueueCustList();
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId)
        {
                return depositBonusService.GetDepositBonusQueueByCustId(custId);
        }
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
        }
        
        public async Task<Boolean> SetClaimPromotionAsync(DepositBonus depositBonus)
        {
            var apiResult1 = new ApiResult();
            var lockedAmount = depositBonus.LockedAmount;
            var result = depositBonusService.SetClaimPromotion(depositBonus, out var bonusAmount, ref lockedAmount,
                out var rollOver, out var expiryTime, out var promotion);

            if (result > 0)
            {
                if (promotion?.TpId > 0)
                {
                    var myUrl = $"{apiUrl}BonusTransfer";
                    var tpBonusTransfer = new TpBonusTransfer
                    {
                        TpId = promotion.TpId,
                        CustId = depositBonus.CustId,
                        Bonus = bonusAmount + lockedAmount,
                        BonusAmount = bonusAmount,
                        LockedAmount = lockedAmount,
                        RolloverAmount = rollOver,
                        ExpiryTime = expiryTime,
                        inputs = new Dictionary<string, string>()
                    };

                    var apiResult = await courierAdapter.MyPostAsync<TransferResult, TpBonusTransfer>(myUrl, tpBonusTransfer);

                    if (apiResult.ErrorCode != 0)
                    {
                        apiResult1.ErrorCode = apiResult.ErrorCode;
                        apiResult1.RawErrorCode = apiResult.RawErrorCode;
                        apiResult1.RawMessage = apiResult.RawMessage;
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
            else if (result == -2)
            {
                apiResult1.ErrorCode = -2;
                //var dRequiredAmount = lockedAmount.ToString("#,##0");
                //if (IsConvertRequired(currencyId))
                //{
                //    dRequiredAmount = $"{lockedAmount / 1000:#,##0} ('000)";
                //}

                //apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_LockedAmountInsufficient"), dRequiredAmount);
            }
            else if (result == -3)
            {
                apiResult1.ErrorCode = -3;
                //apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible");

                //if ((custInfo.SiteId == 2 || custInfo.SiteId == 3) && promotion?.PromotionType == PromotionType.FreeBet)
                //{
                //    apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet");
                //}
            }
            else if (result == -4)
            {
                apiResult1.ErrorCode = -4;
                //var dRequiredAmount = promotion.RequiredAmount.ToString("#,##0");
                //if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
                //{
                //    dRequiredAmount = $"{promotion.RequiredAmount / 1000:#,##0} ('000)";
                //}

                //apiResult1.Message = string.Format(_selLangBll.GetMsg("msg_RequiredAmountInsufficient"), dRequiredAmount);
            }
            else if (result == -5)
            {
                apiResult1.ErrorCode = result;
                //apiResult1.Message = $"{_selLangBll.GetMsg("msg_OTPHint1")}\r\n{_selLangBll.GetMsg("msg_OTPHint2")}";
            }
            else
            {
                //apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible");

                //if (custInfo.SiteId == (int)SiteId.Goal123 || custInfo.SiteId == (int)SiteId.Bwing)
                //{
                //    var custPromotion = GetPromotionDetail(custInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, false, custInfo.UserLang, custInfo.UserLang, bonusCode, deviceType);
                //    if (custPromotion?.PromotionType == (int)PromotionType.FreeBet)
                //    {
                //        apiResult1.Message = _selLangBll.GetMsg("msg_Ineligible_FreeBet");
                //    }
                //}
            }
            if (apiResult1.ErrorCode == 0)
                return true;
            return false;
        }

        public void CheckDepositBonusExpired()
        {
            depositBonusService.CheckDepositBonusExpired();
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            return depositBonusService.SetDepositBonusStatus( transId, status);
        }
        public int SetDepositBonusFinish(string transId, int custId)
        {
            var BonusList = GetDepositBonusQueueByCustId(custId);
            foreach (var depositBonus in BonusList)
            {
                if (depositBonus.TransId == transId)
                {
                    SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Finish);
                }else
                {
                    SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
                }

            }
            return 1;
        }
    }
}
