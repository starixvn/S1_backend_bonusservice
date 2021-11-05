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
}
