using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL.Interface
{
    public interface IPromotionService
    {
        CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, int deviceType, string bonusCode, int periodType, int promotionType, decimal requiredAmount, CurrencyUTC currencyUTC, bool InPromotionUploadCustList, int categoryId);
    }
}
