using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;

namespace BonusService.Service.DAL
{
    public class VipService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportBackupDbClient;

        public VipService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportBackupDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportBackupDB);
        }

        public IEnumerable<VipSetting> GetVipSettings(int vipType)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_Status", vipType);
            return _reportBackupDbClient.Query<VipSetting>("Service_VipSettingV3_Get", parameter);
        }

        public IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("SiteId", siteId);
            return _reportBackupDbClient.Query<VipSystem>("Service_VipSystem_GetV2", parameter);
        }

        public bool SetVipSetting(VipSetting vipSetting)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_SiteId", vipSetting.SiteId);
            parameter.Add("_Status", vipSetting.Status);
            parameter.Add("_SettleTime", vipSetting.SettleTime);
            parameter.Add("_LastUpdateTime", vipSetting.LastUpdateTime);
            parameter.Add("_LastReviewedTime", vipSetting.LastReviewedTime);
            parameter.Add("_LastSettledTime", vipSetting.LastSettledTime);
            return _mainDbClient.Execute("Service_VipSettingV3_Set", parameter) >= 0;
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
            return _reportBackupDbClient.Query<CustVip>("Service_CustUpgrade_GetV2", parameter, 600);
        }
        
        public IEnumerable<CustVip> GetCustUpgradeDownGrade(CustomerUpDowngradeInfo customerUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_VipLevel", customerUpgradeInfo.VipLevel);
            parameter.Add("_UpDeposit", customerUpgradeInfo.UpDeposit);
            parameter.Add("_UpTurnover", customerUpgradeInfo.UpTurnover);
            parameter.Add("_SiteId", customerUpgradeInfo.SiteId);
            parameter.Add("_CurrencyId", customerUpgradeInfo.CurrencyId);
            parameter.Add("_StartTime", customerUpgradeInfo.StartTime);
            parameter.Add("_EndTime", customerUpgradeInfo.EndTime);
            parameter.Add("_CalculateMonth", customerUpgradeInfo.CalculateMonth);
            return _reportBackupDbClient.Query<CustVip>("Service_CustUpgrade_Get_V3", parameter, 600);
        }

        public IEnumerable<VipUpgradeReview> GetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_CalculateMonth", vipUpgradeInfo.CalculateMonth);
            parameter.Add("_SiteId", vipUpgradeInfo.SiteId);
            parameter.Add("_Status", vipUpgradeInfo.Status);

            return _reportBackupDbClient.Query<VipUpgradeReview>("Service_VipUpgradeReviewV4_Get", parameter);
        }

        public bool SetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_CustId", vipUpgradeInfo.CustId);
            parameter.Add("_CalculateMonth", vipUpgradeInfo.CalculateMonth);
            parameter.Add("_UserName", vipUpgradeInfo.UserName);
            parameter.Add("_SiteId", vipUpgradeInfo.SiteId);
            parameter.Add("_CurrentVipLevel", vipUpgradeInfo.CurrentVipLevel);
            parameter.Add("_UpgradeVipLevel", vipUpgradeInfo.UpgradeVipLevel);
            parameter.Add("_CurrencyId", vipUpgradeInfo.CurrencyId);
            parameter.Add("_DepositAmount", vipUpgradeInfo.DepositAmount);
            parameter.Add("_TurnoverAmount", vipUpgradeInfo.TurnoverAmount);
            parameter.Add("_UpgradeAmount", vipUpgradeInfo.UpgradeAmount);
            parameter.Add("_Status", vipUpgradeInfo.Status);
            return _mainDbClient.Execute("Service_VipUpgradeReview_Ins_V3", parameter) >= 0;
        }

        public IEnumerable<CustVipNotDefaultLevel> GetCustVipDownToDefaultLevel(int siteId , DateTime startDate, DateTime endTime ,string calculateMonth)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_SiteId", siteId);
            parameter.Add("_StartTime", startDate);
            parameter.Add("_EndTime", endTime);
            parameter.Add("_CalculateMonth", calculateMonth);
            return _reportBackupDbClient.Query<CustVipNotDefaultLevel>("Service_CustVipDownToDefaultLevel_Get", parameter, 600);
        }
        
        public bool SetCustVip(CustomerVipInfo customerVipInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_OldLevel", customerVipInfo.OldLevel);
            parameter.Add("_NewLevel", customerVipInfo.NewLevel);
            parameter.Add("_CustId", customerVipInfo.CustId);
            parameter.Add("_UserName", customerVipInfo.UserName);
            parameter.Add("_SiteId", customerVipInfo.SiteId);
            parameter.Add("_UpgradeAmount", customerVipInfo.UpgradeAmount);
            parameter.Add("_WithdrawalLimitAmount", customerVipInfo.WithdrawalLimitAmount);
            parameter.Add("_FinancialMemo", customerVipInfo.FinancialMemo);
            parameter.Add("_WithdrawalLimitMemo", customerVipInfo.WithdrawalLimitMemo);
            parameter.Add("_CalculateMonth", customerVipInfo.CalculateMonth);
            return _mainDbClient.Execute("Service_CustVip_Set_V4", parameter) >= 0;
        }

        public IEnumerable<CustVipForDowngrade> GetCustDowngrade(CustomerDowngradeInfo customerDowngradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_VipLevel", customerDowngradeInfo.VipLevel);
            parameter.Add("_DownDeposit", customerDowngradeInfo.DownDeposit);
            parameter.Add("_DownTurnover", customerDowngradeInfo.DownTurnover);
            parameter.Add("_SiteId", customerDowngradeInfo.SiteId);
            parameter.Add("_CurrencyId", customerDowngradeInfo.CurrencyId);
            parameter.Add("_StartTime", customerDowngradeInfo.StartTime);
            parameter.Add("_EndTime", customerDowngradeInfo.EndTime);
            parameter.Add("_QualifiedTime", customerDowngradeInfo.QualifiedTime);
            parameter.Add("_CalculateMonth", customerDowngradeInfo.CalculateMonth);
            return _reportBackupDbClient.Query<CustVipForDowngrade>("Service_VIP_CustDowngrade_Get_V4", parameter, commandTimeout: 600);
        }

        public bool SetVipDowngradeReview(VipDowngradeInfo vipDowngradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_CustId", vipDowngradeInfo.CustId);
            parameter.Add("_CalculateMonth", vipDowngradeInfo.CalculateMonth);
            parameter.Add("_SiteId", vipDowngradeInfo.SiteId);
            parameter.Add("_CurrencyId", vipDowngradeInfo.CurrencyId);
            parameter.Add("_CurrentVipLevel", vipDowngradeInfo.CurrentVipLevel);
            parameter.Add("_DowngradeVipLevel", vipDowngradeInfo.DowngradeVipLevel);
            parameter.Add("_DepositAmount", vipDowngradeInfo.DepositAmount);
            parameter.Add("_DowngradeDepositAmount", vipDowngradeInfo.DowngradeDepositAmount);
            parameter.Add("_TurnoverAmount", vipDowngradeInfo.TurnoverAmount);
            parameter.Add("_DowngradeTurnoverAmount", vipDowngradeInfo.DowngradeTurnoverAmount);
            parameter.Add("_Status", vipDowngradeInfo.Status);
            return _mainDbClient.Execute("Service_VipDowngradeReviewV2_Ins", parameter) >= 0;
        }

        public IEnumerable<VipDowngradeReview> GetVipDowngradeReview(VipDowngradeInfo vipDowngradeInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_CalculateMonth", vipDowngradeInfo.CalculateMonth);
            parameter.Add("_SiteId", vipDowngradeInfo.SiteId);
            parameter.Add("_Status", vipDowngradeInfo.Status);

            return _reportBackupDbClient.Query<VipDowngradeReview>("Service_VipDowngradeReviewV2_Get", parameter);
        }

        public bool SetCustVipForDowngrade(CustomerVipInfo customerVipInfo)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_SiteId", customerVipInfo.SiteId);
            parameter.Add("_CustId", customerVipInfo.CustId);
            parameter.Add("_UserName", customerVipInfo.UserName);
            parameter.Add("_OldLevel", customerVipInfo.OldLevel);
            parameter.Add("_NewLevel", customerVipInfo.NewLevel);
            parameter.Add("_CalculateMonth", customerVipInfo.CalculateMonth);
            return _mainDbClient.Execute("Service_CustVip_DowngradeV2_Set", parameter) >= 0;
        }
    }
}