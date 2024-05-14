namespace BonusService.Service.Model
{
    public class PromotionExpiredCheck
    {
        public string BonusCode { get; set; }
        public int SiteId { get; set; }
        public long PeriodTo { get; set; }
        public long DisplayTo { get; set; }
    }
}
