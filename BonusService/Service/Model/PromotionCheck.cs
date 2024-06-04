namespace BonusService.Service.Model
{
    public class PromotionCheck
    {
        public string BonusCode { get; set; }
        public int SiteId { get; set; }
        public long PeriodTo { get; set; }
        public long DisplayFrom { get; set; }
        public long DisplayTo { get; set; }
        public bool AutoExtend { get; set; }
    }
}
