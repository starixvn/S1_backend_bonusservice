using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Dapper;

namespace BonusService.Service.DAL
{
    public class QueueJobService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public QueueJobService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<T> GetQueueJob<T>(QueueJob type)
        {
            var p = new
            {
                _Type = (int)type
            };
            return _reportDbClient.Query<T>("Service_QueueJob_Get", new DynamicParameters(p));
        }
        
        public int DeleteQueueJobByCustId(int custId, QueueJob type)
        {
            var p = new
            {
                _CustId = custId,
                _Type = (int)type
            };
            return _mainDbClient.Execute("Service_QueueJobByCustId_Del", new DynamicParameters(p));
        }
    }
}