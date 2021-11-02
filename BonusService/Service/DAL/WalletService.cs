using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.DAL
{
    public class WalletService
    {
        private readonly MySqlClient _mainDbClient;

        public WalletService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
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
    }

}
