using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;

namespace BonusService.Service.DAL;

public class WorkerPeriodService
{
    private readonly MySqlClient _mainDbClient;
    private readonly MySqlClient _reportDbClient;
    public WorkerPeriodService(IServiceProvider provider)
    {
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
    }

    public IEnumerable<WorkerPeriodTask> GetTask(string workerName)
    {
        return _reportDbClient.Query<WorkerPeriodTask>("Service_WorkerPeriod_Get",
            new DynamicParameters(new { _WorkerName = workerName }));
    }

    public int SetTaskDone(int siteId, int currencyId, string workerName)
    {
        return _mainDbClient.Execute("Service_WorkerPeriod_TaskDone_Set",
            new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId, _WorkerName = workerName }));
    }
}
