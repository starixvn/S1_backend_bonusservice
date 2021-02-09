using BonusService.Service.DAL;
using BonusService.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class ExpiredBonusBLL
    {
        private readonly ExpiredBonusService expiredBonusService;
        private readonly RebateBLL rebateBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly RebateService rebateService;

        public ExpiredBonusBLL(ExpiredBonusService expiredBonusService, RebateBLL rebateBLL, SelLangBLL selLangBLL, RebateService rebateService)
        {
            this.expiredBonusService = expiredBonusService;
            this.rebateBLL = rebateBLL;
            this.selLangBLL = selLangBLL;
            this.rebateService = rebateService;

        }
        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
           return expiredBonusService.GetBonusExpired(limit);
        }
        public int RefundExpiredBonus(ExpiredBonus expiredBonus)
        {
            var rtn = expiredBonusService.RefundExpiredBonus(expiredBonus);
            if (rtn > 0)
            {
                var msg = new StringBuilder();
                var Refund = expiredBonus.Refund.ToString("#,##0.##");
                if (rebateBLL.IsConvertRequired(expiredBonus.CurrencyId))
                {
                    Refund = $"{expiredBonus.Refund / 1000:#,##0.##}";
                }

                var title = string.Format($"{expiredBonus.UserName} {selLangBLL.GetResourceValue("lbl_CancelBonus", expiredBonus.UserLang)}", Refund);
                msg.AppendLine(title);

                rebateService.SendMessage(expiredBonus.SiteId, "Bonus Service", title, msg.ToString(), expiredBonus.CustId, expiredBonus.UserName, expiredBonus.UserLang);
            }
            return rtn;

        }
    }
}
