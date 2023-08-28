using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;

namespace BonusService.Service.DAL
{
    public class VipCashBackService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public VipCashBackService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<CashBackSettingOutput> GetCashBackSettings(VipPeriodType periodType)
        {
            return _reportDbClient.Query<CashBackSettingOutput>("Service_CashBackSetting_Get_V2", new { _PeriodType = (byte)periodType });
        }
        
        public bool SetCashBackSettings(CashBackSettingInput cashBackSettingInput)
        {
            var parameter = new DynamicParameters(new
            {
                _SiteId = cashBackSettingInput.SiteId,
                _SettleTime = cashBackSettingInput.SettleTime,
                _PeriodType = (int)cashBackSettingInput.PeriodType,
                _CurrencyId = cashBackSettingInput.CurrencyId,
            });
            return _mainDbClient.Execute("Service_CashBackSetting_Set_V2", parameter) >= 0;
        }
        
        public IEnumerable<CashBackPromotionOutput> GetVipCashBackPromotion(CashBackPromotionInput cashBackPromotionInput)
        {
            var parameter = new DynamicParameters(new
            {
                _SiteId =  cashBackPromotionInput.SiteId,
                _PromotionType = cashBackPromotionInput.PromotionType,
                _PeriodType = (int)cashBackPromotionInput.PeriodType,
                _CurrencyId = cashBackPromotionInput.CurrencyId,

            });
            return _reportDbClient.Query<CashBackPromotionOutput>("Service_VipCashBackPromotion_Get_V2", parameter);
        }

        public IEnumerable<PromotionCashBackSettingOutput> GetPromotionCashBackSetting(PromotionCashBackSettingInput promotionCashBackSettingInput)
        {
            var parameter = new DynamicParameters();
            parameter.Add("BonusCode", promotionCashBackSettingInput.BonusCode);
            parameter.Add("SiteId", promotionCashBackSettingInput.SiteId);
            parameter.Add("CurrencyId", promotionCashBackSettingInput.CurrencyId);
            return _reportDbClient.Query<PromotionCashBackSettingOutput>("Service_PromotionCashBackSetting_Get", parameter);
        }
        
        public IEnumerable<CashBackTpBetRecordOutput> GetVipCashBackTpBetRecord(CashBackTpBetRecordInput cashBackTpBetRecordInput)
        {
            var parameter = new DynamicParameters(new
            {
                StartDate = cashBackTpBetRecordInput.StartDate,
                EndDate = cashBackTpBetRecordInput.EndDate,
                TpIds = cashBackTpBetRecordInput.TpIds,
                CategoryIds = cashBackTpBetRecordInput.CategoryIds,
                SiteId = cashBackTpBetRecordInput.SiteId,
                CurrencyId = cashBackTpBetRecordInput.CurrencyId,
            });

            var spName = cashBackTpBetRecordInput.PeriodType == VipPeriodType.Monthly
                ? "Service_VipCashBackTpBetRecord_Get_V2"
                : "Service_VipCashBackTpBetRecordByDaily_Get";

            return _reportDbClient.Query<CashBackTpBetRecordOutput>(spName, parameter, 180);
        }
        
        public IEnumerable<CustomerVIPInfoOutput> GetCustomerVipInfo(string custIds)
        {
            var parameter = new DynamicParameters();
            parameter.Add("CustIds", custIds);
            return _reportDbClient.Query<CustomerVIPInfoOutput>("Service_CustomerVIPInfo_Get", parameter);
        }

        public bool SetCashBackInfo(CashBackInfoInput cashBackInfoInput)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_BonusCode", cashBackInfoInput.BonusCode);
            parameter.Add("_PromoCategoryId", cashBackInfoInput.PromoCategoryId);
            parameter.Add("_CategoryId", cashBackInfoInput.CategoryId);
            parameter.Add("_TpId", cashBackInfoInput.TpId);
            parameter.Add("_CustId", cashBackInfoInput.CustId);
            parameter.Add("_PeriodFrom", cashBackInfoInput.PeriodFrom);
            parameter.Add("_PeriodTo", cashBackInfoInput.PeriodTo);
            parameter.Add("_VipLevel", cashBackInfoInput.VipLevel);
            parameter.Add("_CurrencyId", cashBackInfoInput.CurrencyId);
            parameter.Add("_SiteId", cashBackInfoInput.SiteId);
            parameter.Add("_Percentage", cashBackInfoInput.Percentage);
            parameter.Add("_WinlostAmount", cashBackInfoInput.WinlostAmount);
            parameter.Add("_WinlostAmountWithBonus", cashBackInfoInput.WinlostAmountWithBonus);
            parameter.Add("_BonusAmount", cashBackInfoInput.BonusAmount);
            return _mainDbClient.Execute("Service_CashBackInfo_Ins_V2", parameter) >= 0;
        }

        public bool SetCustCashBack(CustCashBackInput custCashBackInput)
        {
            var parameter = new DynamicParameters(new
            {
                custCashBackInput.CustId,
                custCashBackInput.BonusCode,
                custCashBackInput.CategoryId,
                custCashBackInput.PromoCategoryId,
                custCashBackInput.CalculateDate,
                custCashBackInput.BonusAmount
            });
            return _mainDbClient.Execute("Service_CustCashBack_Set_V2", parameter) >= 0;
        }

        public IEnumerable<DepositNotUseBonusDto> GetDepositNotUseBonusList(int currencyId, int custId,
            string startDate, string endDate)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_CurrencyId", currencyId);
            parameter.Add("_CustId", custId);
            parameter.Add("_StartDate", startDate);
            parameter.Add("_EndDate", endDate);

            return _reportDbClient.Query<DepositNotUseBonusDto>("Service_DepositNotUseBonus_Get", parameter);
        }
    }
}