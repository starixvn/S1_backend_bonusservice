using BonusService.Service.Model.enums;

namespace BonusService.Service.Model
{
    public class CreateManualRewardPointInfoRequest
    {
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public decimal Point { get; set; }
        public int CategoryId { get; set; }
        public string Memo { get; set; }
        public string Operator { get; set; }
    }
    
    public class TransIdModel
    {
        public string TransId { get; set; }
    }
}