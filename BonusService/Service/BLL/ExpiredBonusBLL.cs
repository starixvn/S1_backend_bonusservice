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
        private readonly CustInfoService custInfoService;
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;

        public ExpiredBonusBLL(ExpiredBonusService expiredBonusService, CustInfoService custInfoService, SelLangBLL selLangBLL, MessageService messageService)
        {
            this.expiredBonusService = expiredBonusService;
            this.custInfoService = custInfoService;
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;

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
                if (custInfoService.IsConvertRequired(expiredBonus.CurrencyId))
                {
                    Refund = $"{expiredBonus.Refund / 1000:#,##0.##}";
                }

                var title = string.Format($"{expiredBonus.UserName} {selLangBLL.GetResourceValue("lbl_CancelBonus", expiredBonus.UserLang)}", Refund);
                msg.AppendLine(title);

                messageService.SendMessage(expiredBonus.SiteId, "Bonus Service", title, msg.ToString(), expiredBonus.CustId, expiredBonus.UserName, expiredBonus.UserLang);
            }
            return rtn;

        }
    }
}
