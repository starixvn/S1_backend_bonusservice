using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.Model
{
    public class VipSystem
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int Level { get; set; }
        public decimal UpDeposit { get; set; }
        public decimal? DownDeposit { get; set; }
        public decimal? DownTurnover { get; set; }
    }

    public class CustVip
    {
        public int CustId { get; set; }
        public int VipLevel { get; set; }
        public string UserName { get; set; }
    }
}
