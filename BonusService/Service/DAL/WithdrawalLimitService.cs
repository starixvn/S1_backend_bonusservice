using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class WithdrawalLimitService
    {
        private readonly MySqlClient _mainDbClient;

        public WithdrawalLimitService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit,int reasonId, string memo, string refTransId, int createFrom, string creator, out decimal afterWithdrawalLimit, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId, 
                UserName = userName, 
                SiteId = siteId, 
                WithdrawalLimit = withdrawalLimit, 
                Memo = memo,
                ReasonId = reasonId,
                RefTransId = refTransId,
                CreateFrom = createFrom, 
                Creator = creator
            });
            p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result =_mainDbClient.Execute("Common_WithdrawalLimit_Set_V2", trans, p);
            afterWithdrawalLimit = p.Get<decimal?>("AfterWithdrawalLimit") ?? 0;
            return result;
        }
    }
}
