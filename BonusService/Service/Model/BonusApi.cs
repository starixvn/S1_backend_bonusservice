using BonusService.Service.Model.enums;

namespace BonusService.Service.Model
{
    public class BonusApi
    {
    }

    public class CustRunningBonus
    {
        public int BonusId { set; get; }
        public string TransId { set; get; }
        public int PromotionType { set; get; }
        public string BonusCode { set; get; }
        public DateTime ExpiryTime { set; get; }
        public int Status { set; get; }
    }
    public class CustCondition
    {
        public bool CanJoin { set; get; }
        public decimal Amount { set; get; }
        public string RefId { set; get; }
        public int Result { set; get; }
        public string RefList { set; get; }
        public IEnumerable<Ref> reference { set; get; }
    }

    public class BonusRewardInput
    {
        public PromotionType PromotionType { set; get; }
        public string BonusCode { set; get; }
        public bool IsSpecial { get; set; }
        public string SpecialCode { get; set; }
        public int CategoryId { set; get; }
        public int TpId { set; get; }
        public string BonusInfo { set; get; }
        public decimal BonusAmount { set; get; }
        public decimal LockedAmount { set; get; }
        public decimal RollOver { set; get; }
        public int FullFillType { get; set; }
        public long ExpiryTime { set; get; }
        public int ExpiryDays { get; set; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public int SiteId { set; get; }
        public string Memo { set; get; }
        public string Prefix { set; get; }
        public string RefList { set; get; }
        public string SetBalanceMemo { set; get; }
        public string RewardBonusMemo { set; get; }
        public FinancialType FinancialType { set; get; }
        public BonusInfoAtPage AtPage { set; get; }

        public VipPeriodType PeriodType { get; set; }
    }
    public class CalculateBonusOutput
    {
        public decimal BonusAmount { set; get; }
        public decimal LockedAmount { set; get; }
        public decimal RollOver { set; get; }
        public string BonusInfo { set; get; }
        public long ExpiryTime { set; get; }
        public int result { set; get; }
        public decimal DepositAmount { set; get; }
    }

    public class AfterRewardSuccessInput
    {
        public int CustId { set; get; }
        public BonusInfoAtPage atPage { set; get; }
    }
    public class CurrencyUTC
    {
        public DateTime CurrentTime { set; get; }
        public DateTime TodayUtcStart { set; get; }
        public DateTime TodayUtcEnd { set; get; }
        public DateTime UTC_StartTime { get; set; }
        public DateTime UTC_EndTime { get; set; }
        public string TimeZone { get; set; }
        public TimeSpan timeOffset { get; set; }
    }
}

