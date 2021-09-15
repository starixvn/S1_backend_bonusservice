using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class ReferralBLL
    {
        private readonly SelLangBLL selLangBLL;
        private readonly ReferralService referralService;
        private readonly MessageService messageService;
        private readonly CustInfoService custInfoService;

        public ReferralBLL(SelLangBLL selLangBLL, ReferralService referralService, MessageService messageService, CustInfoService custInfoService)
        {
            this.selLangBLL = selLangBLL;
            this.referralService = referralService;
            this.messageService = messageService;
            this.custInfoService = custInfoService;
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return referralService.GetReferralSettlement();
        }

        public int PayoutReferral(IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            var result = referralService.PayoutReferral(referralInfos, out payAmount, out paidReferralList);
            if (result > 0)
            {
                var agent = referralInfos.FirstOrDefault();
                var custInfo = custInfoService.GetCustInfo(agent.AgentId);

                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = selLangBLL.GetResourceValue("lbl_ReferralBonusCreditedTitle", custInfo.UserLang);
                var message =
                    string.Format($" {selLangBLL.GetResourceValue("lbl_ReferralBonusCreditedMsg", custInfo.UserLang)}",
                        dpayAmount);
                msg.AppendLine(message);


                foreach (var paidReferral in paidReferralList)
                {
                    var dAmount = paidReferral.BonusAmount.ToString("#,##0.##");
                    if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                    {
                        dAmount = $"{paidReferral.BonusAmount / 1000:#,##0.##}";
                    }

                    msg.AppendLine($"{paidReferral.UserName}: {dAmount}");
                }

                messageService.SendMessage(custInfo.SiteId, "Bonus Service", title, msg.ToString(), custInfo.CustId, custInfo.UserName, custInfo.UserLang);
            }

            return result;
        }
    }
}
