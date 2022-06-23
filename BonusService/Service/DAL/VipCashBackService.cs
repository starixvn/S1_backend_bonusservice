using System;
using System.Collections.Generic;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

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
        
        public IEnumerable<CashBackSettingOutput> GetCashBackSettings()
        {
            return _reportDbClient.Query<CashBackSettingOutput>("Service_CashBackSetting_Get", null);
        }
        
        public bool SetCashBackSettings(CashBackSettingInput cashBackSettingInput)
        {
            var parameter = new DynamicParameters(new
            {
                cashBackSettingInput.SiteId,
                cashBackSettingInput.SettleTime
            });
            return _mainDbClient.Execute("Service_CashBackSetting_Set", parameter) >= 0;
        }
        
        public IEnumerable<CashBackPromotionOutput> GetVipCashBackPromotion(CashBackPromotionInput cashBackPromotionInput)
        {
            var parameter = new DynamicParameters(new
            {
                cashBackPromotionInput.SiteId,
                cashBackPromotionInput.PromotionType
            });
            return _reportDbClient.Query<CashBackPromotionOutput>("Service_VipCashBackPromotion_Get", parameter);
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
                cashBackTpBetRecordInput.StartDate,
                cashBackTpBetRecordInput.EndDate,
                cashBackTpBetRecordInput.TpIds,
                cashBackTpBetRecordInput.CategoryIds,
                cashBackTpBetRecordInput.SiteId,
                cashBackTpBetRecordInput.CurrencyId
            });
            return _reportDbClient.Query<CashBackTpBetRecordOutput>("Service_VipCashBackTpBetRecord_Get", parameter, 180);
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
            parameter.Add("BonusCode", cashBackInfoInput.BonusCode);
            parameter.Add("PromoCategoryId", cashBackInfoInput.PromoCategoryId);
            parameter.Add("CategoryId", cashBackInfoInput.CategoryId);
            parameter.Add("TpId", cashBackInfoInput.TpId);
            parameter.Add("CustId", cashBackInfoInput.CustId);
            parameter.Add("PeriodFrom", cashBackInfoInput.PeriodFrom);
            parameter.Add("PeriodTo", cashBackInfoInput.PeriodTo);
            parameter.Add("VipLevel", cashBackInfoInput.VipLevel);
            parameter.Add("CurrencyId", cashBackInfoInput.CurrencyId);
            parameter.Add("SiteId", cashBackInfoInput.SiteId);
            parameter.Add("Percentage", cashBackInfoInput.Percentage);
            parameter.Add("WinlostAmount", cashBackInfoInput.WinlostAmount);
            parameter.Add("BonusAmount", cashBackInfoInput.BonusAmount);
            return _mainDbClient.Execute("Service_CashBackInfo_Ins", parameter) >= 0;
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
            return _mainDbClient.Execute("Service_CustCashBack_Set", parameter) >= 0;
        }
    }
}