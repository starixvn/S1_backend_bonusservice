using System.Text;

using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;


namespace BonusService.Service.BLL
{
    public class ExpiredBonusBLL
    {
        private readonly ExpiredBonusService _expiredBonusService;
        private readonly CustInfoService _custInfoService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;

        public ExpiredBonusBLL(IServiceProvider provider)
        {
            _expiredBonusService = provider.GetRequiredService<ExpiredBonusService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
        }

        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
            return _expiredBonusService.GetBonusExpired(limit);
        }

        public int RefundExpiredBonus(ExpiredBonus expiredBonus, string memo)
        {
            var result = _expiredBonusService.RefundExpiredBonus(expiredBonus, memo);
            if (result > 0)
            {
                var msg = new StringBuilder();
                var refund = expiredBonus.Refund.ToString("#,##0.##");
                if (_custInfoService.IsConvertRequired(expiredBonus.CurrencyId))
                {
                    refund = $"{expiredBonus.Refund / 1000:#,##0.##}";
                }

                var title = _selLangBll.GetResourceValue("lbl_CancelBonusTitle", expiredBonus.UserLang);
                if (expiredBonus.Refund == 0)
                {
                    msg.AppendLine(_selLangBll.GetResourceValue("lbl_CancelBonus1", expiredBonus.UserLang));
                }
                else
                {
                    msg.AppendLine(string.Format(_selLangBll.GetResourceValue("lbl_CancelBonus2", expiredBonus.UserLang), refund));
                }

                _messageService.SendMessage(new Message("System", expiredBonus.CustId, expiredBonus.UserLang, title, msg.ToString()));
            }
            return result;

        }


        public IEnumerable<PromotionExpiredCheck> GetPromotionExpired(int limit)
        {
            return _expiredBonusService.GetPromotionExpired(limit);
        }

        public int SetPromotionStatus(int siteId, string bonusCode)
        {
            return _expiredBonusService.SetPromotionStatus(siteId, bonusCode);
        }
    }
}
