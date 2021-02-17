using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.DAL
{
    public class VipService
    {
        private readonly IConfiguration configuration;
        public VipService(IConfiguration configuration, ILogger<CustInfoService> logger)
        {
            this.configuration = configuration;
        }
        public IEnumerable<VipSystem> GetVipSystem()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<VipSystem>("Service_VipSystem_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<CustVip> GetCustUpgrade(int vipLevel, decimal upDeposit, int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CustVip>("Service_CustUpgrade_Get", new { VipLevel = vipLevel, UpDeposit = upDeposit, SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public int SetCustVip(int oldLevel, int newLevel, int custId, string userName, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_CustVip_Set", new { OldLevel = oldLevel, NewLevel = newLevel, CustId = custId, UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
