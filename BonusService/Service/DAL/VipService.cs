using System.Collections.Generic;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BonusService.Service.DAL
{
    public class VipService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public VipService(IConfiguration configuration, ILogger<MySqlClient> logger)
        {
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }

        public IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("SiteId", siteId);
            return _reportDbClient.Query<VipSystem>("Service_VipSystem_GetV2", parameter);
        }

        public IEnumerable<VipSetting> GetVipSettings()
        {
            return _reportDbClient.Query<VipSetting>("Service_VipSetting_Get", null);
        }

        public bool SetVipSetting(VipSetting vipSetting)
        {
            var parameter = new DynamicParameters();
            parameter.Add("SiteId", vipSetting.SiteId);
            parameter.Add("SettleTime", vipSetting.SettleTime);
            parameter.Add("LastUpdateTime", vipSetting.LastUpdateTime);
            return _mainDbClient.Execute("Service_VipSetting_Set", parameter);
        }


        public IEnumerable<CustVip> GetCustUpgrade(CustomerUpgradeInfo customerUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("VipLevel", customerUpgradeInfo.VipLevel);
            parameter.Add("UpDeposit", customerUpgradeInfo.UpDeposit);
            parameter.Add("SiteId", customerUpgradeInfo.SiteId);
            parameter.Add("CurrencyId", customerUpgradeInfo.CurrencyId);
            parameter.Add("StartTime", customerUpgradeInfo.StartTime);
            parameter.Add("EndTime", customerUpgradeInfo.EndTime);
            parameter.Add("CalculateMonth", customerUpgradeInfo.CalculateMonth);
            return _reportDbClient.Query<CustVip>("Service_CustUpgrade_GetV2", parameter);
        }

        public IEnumerable<VipUpgradeReview> GetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("CalculateMonth", vipUpgradeInfo.CalculateMonth);
            parameter.Add("SiteId", vipUpgradeInfo.SiteId);
            parameter.Add("UserName", vipUpgradeInfo.UserName);
            parameter.Add("Status", vipUpgradeInfo.Status);
            parameter.Add("UpgradeTime", vipUpgradeInfo.UpgradeTime);
            return _reportDbClient.Query<VipUpgradeReview>("Service_VipUpgradeReview_Get", parameter);
        }

        public bool SetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("CustId", vipUpgradeInfo.CustId);
            parameter.Add("CalculateMonth", vipUpgradeInfo.CalculateMonth);
            parameter.Add("UserName", vipUpgradeInfo.UserName);
            parameter.Add("SiteId", vipUpgradeInfo.SiteId);
            parameter.Add("CurrentVipLevel", vipUpgradeInfo.CurrentVipLevel);
            parameter.Add("UpgradeVipLevel", vipUpgradeInfo.UpgradeVipLevel);
            parameter.Add("CurrencyId", vipUpgradeInfo.CurrencyId);
            parameter.Add("DepositAmount", vipUpgradeInfo.DepositAmount);
            parameter.Add("UpgradeAmount", vipUpgradeInfo.UpgradeAmount);
            parameter.Add("Status", vipUpgradeInfo.Status);
            parameter.Add("SettleTime", vipUpgradeInfo.SettleTime);
            return _mainDbClient.Execute("Service_VipUpgradeReview_Ins", parameter);
        }

        public bool SetCustVip(CustomerVipInfo customerVipInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("OldLevel", customerVipInfo.OldLevel);
            parameter.Add("NewLevel", customerVipInfo.NewLevel);
            parameter.Add("CustId", customerVipInfo.CustId);
            parameter.Add("UserName", customerVipInfo.UserName);
            parameter.Add("SiteId", customerVipInfo.SiteId);
            parameter.Add("UpgradeAmount", customerVipInfo.UpgradeAmount);
            parameter.Add("WithdrawalLimitAmount", customerVipInfo.WithdrawalLimitAmount);
            parameter.Add("FinancialMemo", customerVipInfo.FinancialMemo);
            parameter.Add("WithdrawalLimitMemo", customerVipInfo.WithdrawalLimitMemo);
            parameter.Add("CalculateMonth", customerVipInfo.CalculateMonth);
            parameter.Add("SettleTime", customerVipInfo.SettleTime);
            return _mainDbClient.Execute("Service_CustVip_SetV2", parameter);
        }
    }
}