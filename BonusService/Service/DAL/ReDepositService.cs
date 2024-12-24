using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class ReDepositService : IPromotionService
    {
        private readonly PromotionBll _promotionBll;
        public ReDepositService(IServiceProvider provider)
        {
            _promotionBll = provider.GetRequiredService<PromotionBll>();
        }
        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, int deviceType, string bonusCode , int periodType, int promotionType, decimal requiredAmount, CurrencyUTC currencyUTC, bool InPromotionUploadCustList, int categoryId, PromotionUploadCustList promotionUploadCustList)
        {
            var custCondition = new CustCondition()
            {
                RefList = "[]"
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

            //periodType的檢核先拿掉 打開前再檢查一下邏輯
            //if (periodType == (int)VipPeriodType.EachTime)
            //{
            //    if (_promotionBll.GetPromotionRuleBonusInfoByBonusCode(bonusCode))
            //    {
            //        custCondition.Result = (int)BonusErrorCode.SpCustomerJoinThisPromotionBefore;
            //        return custCondition;
            //    }
            //}
            //else
            //{
            //    _promotionBll.CalculatePeriodRange(periodType, currencyUTC);
            //    if (_promotionBll.CheckPromotionRuleBonusInfoByPeriodType((int)promotionType, periodType, currencyUTC.TodayUtcStart, currencyUTC.TodayUtcEnd, currencyUTC.UTC_StartTime, currencyUTC.UTC_EndTime, categoryId, custId))
            //    {
            //        custCondition.Result = (int)BonusErrorCode.SpCustomerAddOtherBonusInPeriodTime;
            //        return custCondition;
            //    }
            //}

            var promotionRuleDepositInfo = _promotionBll.GetPromotionRuleDepositInfoByTime(currencyUTC.CurrentTime, currencyUTC.TimeZone, custId);

            if (promotionRuleDepositInfo != null && promotionRuleDepositInfo.Amount >= requiredAmount)
            {
                custCondition.CanJoin = true;
                custCondition.Amount = promotionRuleDepositInfo.Amount;
                custCondition.RefList = promotionRuleDepositInfo.RefList;
                custCondition.reference = new List<Ref>();
            }
            else
            {
                custCondition.Result = (int)BonusErrorCode.SpCustomerDepositNotExceedRequiredAmount;
            }

            if (custCondition.RefList != null && custCondition.RefList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                custCondition.reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(custCondition.RefList);
            }

            return custCondition;
        }
    }
}
