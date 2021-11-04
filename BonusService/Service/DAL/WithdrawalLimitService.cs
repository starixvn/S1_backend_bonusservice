using System;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.DAL
{
    public class WithdrawalLimitService
    {
        private readonly MySqlClient _mainDbClient;

        public WithdrawalLimitService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo, string refTransId, out decimal afterWithdrawalLimit, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId, 
                UserName = userName, 
                SiteId = siteId, 
                WithdrawalLimit = withdrawalLimit, 
                Memo = memo,
                RefTransId = refTransId
            });
            p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result =_mainDbClient.Execute("Common_WithdrawalLimit_Set", p, conn, trans);
            afterWithdrawalLimit = p.Get<decimal?>("AfterWithdrawalLimit") ?? 0;
            return result;
        }
    }
}
