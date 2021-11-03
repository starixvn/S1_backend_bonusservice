using System;

namespace BonusService.Service.Model
{
    public class PromotionAutoExtend
    {
        public string BonusCode { set; get; }
        public int SiteId { get; }
        public DateTime PeriodToGMT8 { set; get; }
        public DateTime NOWGMT8 { set; get; }
        public DateTime ExtendTimeGMT8 { set; get; }
        public long ExtendTime { get; set; }
    }
}
