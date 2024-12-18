using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class FirstDepositService : IPromotionService
    {
        private readonly PromotionBll _promotionBll;
        public FirstDepositService(IServiceProvider provider)
        {
            _promotionBll = provider.GetRequiredService<PromotionBll>();
        }
        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, int deviceType, string bonusCode, int periodType, int promotionType, decimal requiredAmount, CurrencyUTC currencyUTC, bool InPromotionUploadCustList, int categoryId)
        {
            var custCondition = new CustCondition()
            {
                RefList = "[]"
            };

            if (_promotionBll.CheckPromotionRuleBonusInfo(custId, currencyUTC.TodayUtcStart, currencyUTC.TodayUtcEnd))
            {
                custCondition.Result = (int)BonusErrorCode.SpCustomerFirstDepositRuleDontMatch;
                return custCondition;
            }
                
            if (_promotionBll.CheckPromotionRuleWithdrawalInfo(custId))
            {
                custCondition.Result = (int)BonusErrorCode.SpCustomerWithdrawalBefore;
                return custCondition;
            }
               
            var promotionRuleDepositInfo = _promotionBll.GetPromotionRuleDepositInfo(custId);

            if(promotionRuleDepositInfo == null)
                custCondition.Result = (int)BonusErrorCode.SpCustomerDontHaveFirstDeposit;
            else
                custCondition.Result = (int)BonusErrorCode.Success;
                custCondition.CanJoin = true;
                custCondition.Amount = promotionRuleDepositInfo.FirstOrDefault().Amount;
                custCondition.RefList = promotionRuleDepositInfo.FirstOrDefault().RefList;
                custCondition.RefId = promotionRuleDepositInfo.FirstOrDefault().RefId;
                custCondition.reference = new List<Ref>();

            if (custCondition.RefList != null && custCondition.RefList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                custCondition.reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(custCondition.RefList);
            }

            return custCondition;
        }
    }
}
