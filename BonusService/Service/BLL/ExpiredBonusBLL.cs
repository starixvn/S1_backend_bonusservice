using BonusService.Service.DAL;
using BonusService.Service.Model;
using System.Text;
using BonusService.Service.Library;
using MemberSite.Service.Model;

namespace BonusService.Service.BLL
{
    public class ExpiredBonusBLL
    {
        private readonly ExpiredBonusService _expiredBonusService;
        private readonly CustInfoService _custInfoService;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _apiUrl;

        public ExpiredBonusBLL(IServiceProvider provider)
        {
            _expiredBonusService = provider.GetRequiredService<ExpiredBonusService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageService = provider.GetRequiredService<MessageService>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _apiUrl = configuration.GetValue<string>("TpService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        }
        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
           return _expiredBonusService.GetBonusExpired(limit);
        }
        public IEnumerable<ExpiredBonus> GetTransferBonusExpired(int limit)
        {
            return _expiredBonusService.GetTransferBonusExpired(limit);
        }

        public IEnumerable<decimal> GetBonusWinLoss(int bonusId)
        {
            return _expiredBonusService.GetBonusWinLoss(bonusId);
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

        public ApiResult<TransferResult> BonusCancelTransfer(TpBonusCancelTransfer tpBonusCancelTransfer)
        {
            var myUrl = $"{_apiUrl}BonusCancelTransfer";
            var data = _courierAdapter.MyPostAsync<TransferResult, TpBonusCancelTransfer>(myUrl, tpBonusCancelTransfer).Result;
            return data;
        }
    }
}
