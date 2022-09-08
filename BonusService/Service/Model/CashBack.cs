namespace BonusService.Service.Model
{
    public class CashBackSettingInput
    {
        public int SiteId { get; set; }
        public DateTime SettleTime { get; set; }
    }
    public class CashBackSettingOutput
    {
        public int SiteId { get; set; }
        public int PeriodType { get; set; }
        public int PeriodInterval { get; set; }
        public DateTime SettleTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public class TpGameTypeOutput
    {
        public int TpId { get; set; }
        public int CategoryId { get; set; }
        public int PromoCategoryId { get; set; }
        public int CurrencyId { get; set; }
    }
    
    public class PromotionCashBackSettingInputInsert
    {
        public PromotionCashBackSettingInputInsert(string bonusCode, int currencyId, int siteId, int tpId,
            int categoryId, int level, decimal percentage, int status, string lastUpdateUser)
        {
            BonusCode = bonusCode;
            CurrencyId = currencyId;
            SiteId = siteId;
            TpId = tpId;
            CategoryId = categoryId;
            Level = level;
            Percentage = percentage;
            Status = status;
            LastUpdateUser = lastUpdateUser;
        }

        public string BonusCode { get; }
        public int CurrencyId { get; }
        public int SiteId { get; }
        public int TpId { get; }
        public int CategoryId { get; }
        public int Level { get; }
        public decimal Percentage { get; }
        public int Status { get; }
        public string LastUpdateUser { get; }
    }
    
    public class CashBackPromotionInput
    {
        public int SiteId { get; set; }
        public int PromotionType { get; set; }
    }
    
    public class CashBackPromotionOutput
    {
        public string BonusCode { get; set; }
        public int PromotionType { get; set; }
        public int CategoryId { get; set; }
        public int ExpiryDays { get; set; }
        public int SiteId { get; set; }
        public int? WalletType { get; set; }
        public int? TpId { get; set; }
        public int? RedeemHours { get; set; }
        public int? PeriodType { get; set; }
        public int? PeriodDay { get; set; }
        public string SpecialCode { get; set; }
        public bool IsSpecial { get; set; }
        public int CurrencyId { get; set; }
        public decimal Bonus { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal MinPayout { get; set; }
    }

    public class PromotionCashBackSettingInput
    {
        public string BonusCode { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class PromotionCashBackSettingOutput
    {
        public int TpId { get; set; }
        public int CategoryId { get; set; }
        public int Level { get; set; }
        public decimal LossAmount { get; }

        public decimal Percentage { get; set; }
    }
    
    public class CashBackTpBetRecordInput
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CategoryIds { get; set; }
        public string TpIds { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class CashBackTpBetRecordOutput
    {
        public int CustId { get; set; }
        public int TpId { get; set; }
        public int CategoryId { get; set; }
        public decimal WinlostAmount { get; set; }
    }

    public class CustomerVIPInfoOutput
    {
        public int CustId { get; set; }
        public int VipLevel { get; set; }
    }

    public class CashBackInfoInput
    {
        public string BonusCode { get; set; }
        public int PromoCategoryId { get; set; }
        public int CategoryId { get; set; }
        public int TpId { get; set; }
        public int CustId { get; set; }
        public string PeriodFrom { get; set; }
        public string PeriodTo { get; set; }
        public int VipLevel { get; set; }
        public int CurrencyId { get; set; }
        public int SiteId { get; set; }
        public decimal Percentage { get; set; }
        public decimal WinlostAmount { get; set; }
        public decimal BonusAmount { get; set; }
    }

    public class CustCashBackInput
    {
        public CustCashBackInput(int custId, string bonusCode, int categoryId, int promoCategoryId,
            string calculateDate, decimal bonusAmount)
        {
            CustId = custId;
            BonusCode = bonusCode;
            CategoryId = categoryId;
            PromoCategoryId = promoCategoryId;
            CalculateDate = calculateDate;
            BonusAmount = bonusAmount;
        }
        public CustCashBackInput(int custId, string bonusCode, int promoCategoryId, string calculateDate)
        {
            CustId = custId;
            BonusCode = bonusCode;
            PromoCategoryId = promoCategoryId;
            CalculateDate = calculateDate;
        }
        public int CustId { get; }
        public string BonusCode { get; }
        public int CategoryId { get; }
        public int PromoCategoryId { get; }
        public string CalculateDate { get; }
        public decimal BonusAmount { get; }
    }
    public class CustCashBackCheckOutput
    {
        public bool Claimed { get; set; }
    }
}