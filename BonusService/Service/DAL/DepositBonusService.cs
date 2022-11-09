using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class DepositBonusService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public DepositBonusService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList(int limit)
        {
            return _reportDbClient.Query<DepositBonusCustList>("Service_DepositBonusQueueCustList_Get_V2", new { Limit = limit });
        }

        public void CheckDepositBonusExpired()
        {
            _mainDbClient.Execute("Service_DepositBonusRemoveExpired_Set");
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            return _mainDbClient.Execute("Service_DepositBonusStatus_Set", new { TransId = transId, Status = status });
        }
    }
}
