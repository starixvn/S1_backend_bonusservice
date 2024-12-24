using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class SpecialBonusService : IPromotionService
    {
        private readonly PromotionBll _promotionBll;
        public SpecialBonusService(IServiceProvider provider)
        {
            _promotionBll = provider.GetRequiredService<PromotionBll>();
        }
        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, int deviceType, string bonusCode, int periodType, int promotionType, decimal requiredAmount, CurrencyUTC currencyUTC, bool InPromotionUploadCustList, int categoryId, PromotionUploadCustList promotionUploadCustList)
        {
            var custCondition = new CustCondition()
            {
                RefList = "[]",
                reference = new List<Ref>()
            };

            if (_promotionBll.CheckPromotionRuleRunningBonus(custId))
            {
                custCondition.Result = (int)BonusErrorCode.SpCustomerBonusStillRunning;
                return custCondition;
            }

            if (promotionUploadCustList == null)
            {
                custCondition.Result = (int)BonusErrorCode.PromotionNotFound;
                return custCondition;
            }

            if (promotionUploadCustList.Status)
            {
                custCondition.Result = (int)BonusErrorCode.DuplicateClaimBonus;
                return custCondition;
            }

            if ((promotionUploadCustList.ExpiryDate + currencyUTC.timeOffset) < currencyUTC.CurrentTime)
            {
                custCondition.Result = (int)BonusErrorCode.PromotionNotFound;
                return custCondition;
            }

            if (custCondition.RefList != null && custCondition.RefList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                custCondition.reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(custCondition.RefList);
            }

            custCondition.CanJoin = true;
            return custCondition;
        }
    }
}
