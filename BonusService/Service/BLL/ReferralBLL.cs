using BonusService.Service.DAL;
using BonusService.Service.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BonusService.Service.BLL
{
    public class ReferralBLL
    {
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;
        private readonly CustInfoService custInfoService;
        private readonly ReferralReward _referralReward;

        public ReferralBLL(SelLangBLL selLangBLL, MessageService messageService, CustInfoService custInfoService, ReferralReward referralReward)
        {
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;
            this.custInfoService = custInfoService;
            _referralReward = referralReward;
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return _referralReward.GetReferralSettlement();
        }

        public int PayoutReferral(int agentId, IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            var result = _referralReward.Payout(referralInfos, out payAmount, out paidReferralList);
            if (result > 0 && payAmount > 0)
            {
                var custInfo = custInfoService.GetCustInfo(agentId);

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
