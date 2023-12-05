using BonusService.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class QueueJobData
    {
        public int SQ { get; set; }
        public int CustId { get; set; }
        public string Data { get; set; }
    }
    
    public class DrawPrizeQueueJob
    {
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
    }
}