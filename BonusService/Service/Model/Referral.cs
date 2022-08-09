namespace BonusService.Service.Model
{
    public class ReferralInfo
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserLang { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal ReferralFreeCredit { get; set; }
    }
}
