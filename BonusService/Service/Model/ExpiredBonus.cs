using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.Model
{
    public class ExpiredBonus
    {
        public int BonusId { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public string UserLang { set; get; }
        public int CurrencyId { set; get; }
        public int Refund { set; get; }
        public int PromoCategoryId { get; set; }
        public decimal Bonus { set; get; }

    }
}
