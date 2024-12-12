using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class SpecialBonusService : IPromotionService
    {
        private readonly PromotionBll _promotionBll;
        public SpecialBonusService(IServiceProvider provider)
        {
            _promotionBll = provider.GetRequiredService<PromotionBll>();
        }
        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, int deviceType, string bonusCode, int periodType, int promotionType, decimal requiredAmount, CurrencyUTC currencyUTC, bool InPromotionUploadCustList)
        {
            return new CustCondition()
            {
                RefList = "[]",
                CanJoin = InPromotionUploadCustList,
                Result = (int)BonusErrorCode.Success
            };
        }
    }
}
