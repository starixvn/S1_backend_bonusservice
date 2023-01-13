using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using BonusService.Service.Model;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace BonusService.Service.DAL;

public class AngPaoEventService
{
    private readonly Common common;
    private readonly MySqlClient _mainDbClient;
    private readonly MySqlClient _reportDbClient;
    public AngPaoEventService(IServiceProvider provider)
    {
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        common = provider.GetRequiredService<Common>();
    }
    public IEnumerable<WorkerPeriodTask> GetEventTask(string workerName)
    {
        return _reportDbClient.Query<WorkerPeriodTask>("Service_WorkerPeriod_Get", new DynamicParameters(new { _WorkerName = workerName }));
    }

    public IEnumerable<WorkerCustInfo> GetCustList(int siteId, int currencyId, string timeZone)
    {
        var p = new
        {
            _SiteId = siteId,
            _CurrencyId = currencyId,
            _CurrencyTimeZone = timeZone
        };
        return _mainDbClient.Query<WorkerCustInfo>("Service_EventDrawCustList_Get", new DynamicParameters(p));
    }

    public DrawEventTpBetRecord GetTpBetRecord(int custId, string timeZone)
    {
        var p = new
        {
            _CustId = custId,
            _CurrencyTimeZone = timeZone
        };

        return _reportDbClient.QueryFirst<DrawEventTpBetRecord>("Service_TpBetRecord_CaculateTurnover_Get", new DynamicParameters(p));
    }

    public bool GenTicket(WorkerCustInfo custInfo, DrawEventTpBetRecord record,int ticketQuantity)
    {
        try
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var tc = DateTime.Now.Ticks;

            for (int i = 1; i <= ticketQuantity; i++)
            {
                conn.Execute("Service_DrawTicket_Ins", new
                {
                    CustId = custInfo.CustId,
                    SettledDate = int.Parse(record.SettledDate.ToString("yyyyMMdd")),
                    TicketNo = i,
                    ValidBet = record.ValidBet
                }, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            trans.Commit();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public int SetTaskDone(int siteId, int currencyId, string workerName)
    {
        return _mainDbClient.Execute("Service_WorkerPeriod_TaskDone_Set",
            new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId, _WorkerName = workerName }));
    }
}
