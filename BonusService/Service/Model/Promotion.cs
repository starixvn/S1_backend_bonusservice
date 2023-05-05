using BonusService.Service.Model.enums;
using Newtonsoft.Json;

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
        public WalletType WalletType { get; set; }

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

        /// <summary>
        /// 啟用未登入隱藏
        /// </summary>
        public bool HideForUnlogin { get; set; }
        
        /// 判斷PromotionType是否要檢查 IP、Device、RealName 次數
        /// </summary>
        /// <returns></returns>
        public bool CheckIPDeviceRealName()
        {
            return 
            PromotionType is PromotionType.FreeBet ||
            PromotionType is PromotionType.FstDeposit ||
            PromotionType is PromotionType.DeviceBonus;    
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

        /// <summary>
        /// 是否有相同 IP 限制
        /// </summary>
        public bool SameIpRestrict { get; set; }

        /// <summary>
        /// 相同 IP 能使用的次數
        /// </summary>
        public int SameIpRestrictAvailableCount { get; set; }

        /// <summary>
        /// 是否有相同 DeviceId 限制
        /// </summary>
        public bool SameDeviceIdRestrict { get; set; }

        /// <summary>
        /// 相同 Device 能使用的次數
        /// </summary>

        public int SameDeviceIdAvailableCount { get; set; }

        /// <summary>
        /// 是否有相同 真實姓名 限制
        /// </summary>
        public bool SameRealNameRestrict { get; set; }

        /// <summary>
        /// 相同 真實姓名 能使用的次數
        /// </summary>
        public int SameRealNameRestrictAvailableCount { get; set; }


        public string BlockTagList { get; set; }

        public List<int> BlockTags {
            get
            {
                var tags = string.IsNullOrEmpty(this.BlockTagList)
                    ? new List<int>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<int>>(this.BlockTagList);

                return tags;
            }
        }
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
                lockAmount = depositAmount;
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
        public int PromotionType { set; get; }
    }

    public class PromotionDetailRequest : CustPromotionRequest
    {
        public string BonusCode { set; get; }
    }

    public class CustPromotion : Promotion
    {
        public string Banner { get; set; }
        
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

        public string BlockTagList { get; set; }

        public List<int> BlockTags
        {
            get
            {
                var tags = string.IsNullOrEmpty(this.BlockTagList)
                    ? new List<int>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<int>>(this.BlockTagList);

                return tags;
            }
        }
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
        public decimal TotalLossAmount => CashBackDetail.Any() ? CashBackDetail.Sum(i => i.LossAmount) : 0;
        public decimal MinLossAmount => CashBackDetail.Any() ? CashBackDetail.Max(i => i.MinLossAmount) : 0;
        public decimal OriginalTotalBonus => CashBackDetail.Any() ? CashBackDetail.Max(i => i.Percentage) / 100 * TotalLossAmount : 0;
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

        /// <summary>
        /// 自定義錯誤訊息
        /// </summary>
        public string CustomizeErrorMsg { get; set; }
    }

    public class VipLevelWithBlockTagsDto
    {
        public string BlockTagList { get; set; }

        public string VipLevelList { get; set; }

        public List<int> BlockTags
        {
            get
            {
                var tags = string.IsNullOrEmpty(this.BlockTagList)
                    ? new List<int>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<int>>(this.BlockTagList);

                return tags;
            }
        }

        public List<int> VipLevels
        {
            get
            {
                if (string.IsNullOrEmpty(this.VipLevelList))
                {
                    return new List<int>();
                }
                else
                {
                    var vipDto = System.Text.Json.JsonSerializer.Deserialize<PromotionVipLevelSettingDto>(this.VipLevelList);
                    return vipDto.VipLevel;
                }
            }
        }
    }

    public class PromotionVipLevelSettingDto
    {
        public List<int> VipLevel { get; set; }
    }
}
