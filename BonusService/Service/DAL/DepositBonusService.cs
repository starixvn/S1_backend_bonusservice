using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class DepositBonusService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _reportDbClient;

        public DepositBonusService(IServiceProvider serviceProvider)
        {
            configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonus>("Service_DepositBonusQueue_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonusCustList>("Service_DepositBonusQueueCustList_Get", commandType: CommandType.StoredProcedure);
        }

        public Promotion GetMemberPromotion(DynamicParameters parameters)
        {
            return _reportDbClient.QueryFirst<Promotion>("Service_Promotion_Get", parameters);
        }
        public void CheckDepositBonusExpired()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("Service_DepositBonusRemoveExpired_Set", commandType: CommandType.StoredProcedure);
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_DepositBonusStatus_Set", new { TransId = transId, Status = status }, commandType: CommandType.StoredProcedure);
        }
    }
}
