using System.Data;

using BonusService.Service.Library;
using BonusService.Service.Model.enums;

using Dapper;

using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class WalletService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public WalletService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo,
            string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlTransaction trans)
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
            _mainDbClient.Execute("Wallet_Set_V2", trans, p);
            is1StDeposit = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
            return afterAmount >= 0 ? 1 : 0;
        }

        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
    }

}
