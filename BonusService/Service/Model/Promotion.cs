using BonusService.Service.Model.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BonusService.Service.Model
{
    public class Promotion
    {
        public string BonusCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public PromotionType PromotionType { get; set; }
        public int CategoryId { get; set; }
        public long PeriodFrom { get; set; }
        public long PeriodTo { get; set; }
        public long DisplayFrom { get; set; }
        public long DisplayTo { get; set; }
        public int RollOver { get; set; }
        public int WinOver { get; set; }
        public int FullFillType { get; set; }
        public decimal Bonus { get; set; }
        public BonusType BonusType { get; set; }
        public LockedType LockedType { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal MaxBonus { get; set; }
        public int Sort { get; set; }
        public int TpId { get; set; }
        public string Details { get; set; }
        public int PromotionAction { get; set; }
        public string TierAmount { get; set; }
        public IEnumerable<TierAmount> TierAmountList
        {
            get
            {
                if (string.IsNullOrEmpty(TierAmount))
                {
                    return new List<TierAmount>();
                }

                return JsonConvert.DeserializeObject<IEnumerable<TierAmount>>(TierAmount);
            }
        }

        /// <summary>
        /// 使用金額找到符合的級距紅利
        /// </summary>
        /// <param name="depositAmount"></param>
        /// <returns></returns>
        public TierAmount FindTierAmountLevel(decimal depositAmount)
        {
            if (!TierAmountList.Any())
            {
                return null;
            }

            var tierAmountList = TierAmountList.OrderByDescending(i => i.RequiredAmount);
            foreach (var item in tierAmountList)
            {
                if (depositAmount >= item.RequiredAmount)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 計算符合資格的級距紅利
        /// </summary>
        /// <param name="depositAmount"></param>
        public void CalculateCanJoinTierAmount(decimal depositAmount)
        {
            var canJoinTierAmount = TierAmountList.Where(i => depositAmount >= i.RequiredAmount);
            _canJoinTierAmountList = canJoinTierAmount.Select(i => i.CalculateTierAmount(BonusType, depositAmount));
        }

        private IEnumerable<CanJoinTierAmount> _canJoinTierAmountList { get; set; }

        public IEnumerable<CanJoinTierAmount> CanJoinTierAmountList
        {
            get
            {
                return _canJoinTierAmountList != null ? _canJoinTierAmountList : new List<CanJoinTierAmount>();
            }
        }
    }
    
    public class ClaimPromotion : Promotion
    {
        public string CategoryName { get; set; }
        public int ExpiryDays { get; set; }
        public int CurrencyId { get; set; }
        public decimal LockedAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int BonusInfoStatus { get; set; }
        public CustRollOver custRollOver { get; set; } 
        public bool IsSpecial { get; set; }
        public string SpecialCode { get; set; }
        public decimal MinPayout { get; set; }
    }

    public class TierAmount
    {
        public decimal Bonus { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal RequiredAmount { get; set; }
        public int RollOver { get; set; }
        public int WinOver { get; set; }
        public CanJoinTierAmount CalculateTierAmount(BonusType bonusType, decimal depositAmount)
        {       
            decimal bonusAmount;
            decimal lockAmount;
            if (bonusType == BonusType.Amount)
            {
                bonusAmount = Bonus;
                lockAmount = RequiredAmount;
            }
            else
            {
                bonusAmount = Math.Ceiling(Bonus * depositAmount / 100);
                lockAmount = bonusAmount * 100 / Bonus;
                if (MaxBonus != 0 && bonusAmount > MaxBonus)
                {
                    bonusAmount = MaxBonus;
                    lockAmount = MaxBonus * 100 / Bonus;
                }
            }

            return new CanJoinTierAmount()
            {
                RequiredAmount = RequiredAmount,
                Bonus = Bonus,
                BonusAmount = bonusAmount,
                LockedAmount = lockAmount,
            }; 
        }
    }

    public class CanJoinTierAmount
    {
        public decimal RequiredAmount { get; set; }
        public decimal Bonus { get; set; }       
        public decimal BonusAmount { get; set; }
        public decimal LockedAmount { get; set; }
    }

    public class CustPromotionRequest
    {
        public int SiteId { set; get; }
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public int DeviceType { set; get; }
        public bool IsLogin { set; get; }
        public string LangId { set; get; }
        public string DefLang { set; get; }
    }
    
    public class PromotionDetailRequest : CustPromotionRequest
    {
        public string BonusCode { set; get; }
    }
    
    public class CustPromotion : Promotion
    {
        public string Banner { get; set; }
        public int WalletType { get; set; }        
        public decimal CustRollOver { get; set; }
        public decimal CustTurnOver { get; set; }
        public int Status { get; set; }
        public DateTime? CustExpiryTime { get; set; }
        public bool canJoin { get; set; }
        public bool isJoin { get; set; }
        public int Result { get; set; }
        public string DepositId { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public IEnumerable<Ref> RefList { get; set; }
        public string PromotionLink { get; set; }
        public int? DeviceType { set; get; }
        public CashBackDetails CashBackDetails { get; set; }
        public bool BeenUsed { get; set; }
    }
    
    public class SpecialCodeRequest
    {
        public int CustId { set; get; }
        public string SpecialCode { set; get; }
        public int DeviceType { set; get; }
        public int SiteId { set; get; }
    }
    
    public class PromotionBySpecialCode
    {
        public string BonusCode { set; get; }
        public int PromotionType { get; set; }
    }
    
    public class PromotionBySpecialScratchCard : PromotionBySpecialCode
    {
        public int CustId { get; set; }
    }
    
    public class BonusInfoByBonusCode
    {
        public int Status { get; set; }
    }

    public class CashBackDetails
    {
        public decimal TotalBonus { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal TotalLossAmount => CashBackDetail.Sum(i => i.LossAmount);
        public decimal MinLossAmount => CashBackDetail.Max(i => i.MinLossAmount);
        public decimal OriginalTotalBonus => CashBackDetail.Max(i => i.Percentage) / 100 * TotalLossAmount;
        public string Percentage => CashBackDetail.Select(i => i.PercentageName).FirstOrDefault();
        public IEnumerable<CashBackDetail> CashBackDetail { get; set; }
    }

    public class CashBackDetail
    {
        public int CategoryId { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public decimal LossAmount
        {
            get => Math.Abs(_LossAmount);
            set => _LossAmount = value;
        }
        private decimal _LossAmount { get; set; }
        public decimal LossAmountWithBonus
        {
            get => Math.Abs(_LossAmountWithBonus);
            set => _LossAmountWithBonus = value;
        }
        private decimal _LossAmountWithBonus { get; set; }
        public decimal Percentage { get; set; }
        public string PercentageName => $"{Percentage:G29}%";
        public decimal BonusAmount { get; set; }
        public decimal MinLossAmount { get; set; }
    }

    public class Ref
    {
        public string DepositId { get; set; }
        public decimal Amount { get; set; }
    }
    public class DepositBonus
    {
        public string TransId { set; get; }
        public string BonusCode { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public decimal LockedAmount { set; get; }
        public int DeviceType { set; get; }
        public decimal Amount { set; get; }
        public int DepositStatus { set; get; }
        public string IP { set; get; }
    }
    public class DepositBonusCustList
    {
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
    }
    public enum TransferStatus
    {
        Success = 0, Fail = 1, Pending = 2, PendingD = 3
    }
    public class TransferResult
    {
        public TransferResult()
        {
            Status = TransferStatus.Pending;
        }
        public TransferStatus Status { get; set; }
        public string ThirdId { get; set; }
        public decimal Difference { get; set; }
        public decimal Bonus { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public int BonusStatus { get; set; }
    }
    public class TpBonusTransfer
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public decimal Bonus { set; get; }
        public decimal BonusAmount { set; get; }
        public decimal LockedAmount { set; get; }
        public decimal RolloverAmount { set; get; }
        public long ExpiryTime { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }
    public class ErrorMessageMakerInput
    {
        public int Result { set; get; }
        public string InputBonusCode { set; get; }
        public decimal LockedAmount { set; get; }
        public int InputPromotionType { set; get; }
        public decimal InputAmount { set; get; }
        public Customer CustInfo { set; get; }
        public ClaimPromotion ClaimPromotion { set; get; }
        
    }

}
