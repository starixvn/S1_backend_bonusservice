using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BonusService.Service.Model;

namespace BonusService.Service.DAL
{
    public class WalletService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public WalletService(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo,
            string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlConnection conn,
            MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                Amount = amount,
                Type = type,
                Memo = memo,
                RefTransId = refTransId
            });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Wallet_Set", p, conn, trans);
            is1StDeposit = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
            return afterAmount >= 0 ? 1 : 0;
        }
        public decimal GetBalance(int custId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId }, commandType: CommandType.StoredProcedure);
        }
        public WalletInfo GetWalletInfo(int custId, int siteId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            conn.Open();
            var walletInfo = conn.QueryFirstOrDefault<WalletInfo>("Service_CustInfoLog_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            walletInfo.Balance = GetBalance(custId);

            return walletInfo;
        }
        public int GetWalletTransferStatus(int custId, out int autoTransfer)
        {
            autoTransfer = 0;
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            conn.Open();
            var p = new DynamicParameters(new { CustId = custId });
            p.Add("AutoTransfer", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute("Member_CustWalletTransferStatus_Get", p, commandType: CommandType.StoredProcedure);
            autoTransfer = p.Get<int?>("AutoTransfer") ?? 0;
            return result >= 0 ? 1 : 0;

        }
        public int SetWalletTransferStatus(int custId, int autoTransfer)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            return conn.Execute("Member_CustWalletTransferStatus_Set", new { CustId = custId, AutoTransfer = autoTransfer }, commandType: CommandType.StoredProcedure) > 0 ? 1 : 0;
        }
    }

}
