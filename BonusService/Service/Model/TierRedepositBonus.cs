namespace BonusService.Service.Model
{
    public class CheckTierRedepositBonusReq
    {
        public int CustId { get; set; }
        public string BonusCode { get; set; }
        public int PromotionType { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string DepositId { get; set; }
    }

    public class CheckTierRedepositBonusRep
    {
        public bool IsSuccess { get; set; }
        public decimal ActualDeposit { get; set; }
    }
}
