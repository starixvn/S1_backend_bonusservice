using System.Collections.Generic;
using System.Data;
using BonusService.Service.BLL;
using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class VipService
    {
        private readonly IConfiguration _configuration;

        public VipService(IConfiguration configuration, ILogger<CustInfoService> logger)
        {
            this._configuration = configuration;
        }

        public IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Query<VipSystem>("Service_VipSystem_GetV2", new {SiteId = siteId},
                commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<VipSetting> GetVipSetting()
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Query<VipSetting>("Service_VipSetting_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetVipSetting(VipSetting vipSetting)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_VipSetting_Set",
                new {vipSetting.SiteId, vipSetting.SettleTime}, commandType: CommandType.StoredProcedure);
        }


        public IEnumerable<CustVip> GetCustUpgrade(CustomerUpgradeInfo customerUpgradeInfo)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CustVip>("Service_CustUpgrade_GetV2",
                new
                {
                    customerUpgradeInfo.VipLevel,
                    customerUpgradeInfo.UpDeposit,
                    customerUpgradeInfo.SiteId,
                    customerUpgradeInfo.CurrencyId,
                    customerUpgradeInfo.StartTime,
                    customerUpgradeInfo.EndTime
                },
                commandType: CommandType.StoredProcedure);
        }

        public int SetCustVip(CustomerVipInfo customerVipInfo)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_CustVip_SetV2",
                new
                {
                    customerVipInfo.OldLevel,
                    customerVipInfo.NewLevel,
                    customerVipInfo.CustId,
                    customerVipInfo.UserName,
                    customerVipInfo.SiteId,
                    customerVipInfo.UpgradeAmount,
                    customerVipInfo.WithdrawalLimitAmount,
                    customerVipInfo.FinancialMemo,
                    customerVipInfo.WithdrawalLimitMemo
                }, commandType: CommandType.StoredProcedure);
        }
        
    }
}
