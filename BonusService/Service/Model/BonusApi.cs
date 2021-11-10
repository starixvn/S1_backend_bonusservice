using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.Model
{
    public class BonusApi
    {
    }
    public enum AdjustStatus
    {
        Draft = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        OnHold = 4,
        Error = 99
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
}
