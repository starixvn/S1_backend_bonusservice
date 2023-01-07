using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace BonusService.Service.BLL;

public class AngPaoEventBLL
{
    private readonly AngPaoEventService _angPaoEventService;
    private readonly ILogger<RebateBLL> _logger;
    private readonly Dictionary<string, decimal> CurrencyExchange = new Dictionary<string, decimal>()
    { { "1_2", 350 }, { "1_4", 2800 }, { "2_51", 2000000 },{ "3_51", 2000000 } }; //Key=SiteId_CurrencyId

    public AngPaoEventBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<RebateBLL>>();
        _angPaoEventService = provider.GetRequiredService<AngPaoEventService>();
    }

    public IEnumerable<WorkerPeriodTask> VerifyTask(string workerName)
    {
        //檢查參數
        //0.檢查參數是否正確
        //2.檢查期間型別需檢查項目是否符合

        var TaskList = _angPaoEventService.GetEventTask(workerName);

        var doTaskList = new List<WorkerPeriodTask>();

        foreach (var task in TaskList)
        {
            //Step1：檢查小時是否符合
            //幣別時區小時 < 指定小時，不執行排程
            if (task.CurrencyNowTime.Hour < task.RedeemHours)
            {
                _logger.LogWarning($"task.RedeemsHours Invaild. task value={JsonConvert.SerializeObject(task)}");
                continue;
            }

            //Step2：檢查最近一次執行時間是否符合
            //LastCurrencySettledTime 是 Null，代表第一次執行排程
            //LastCurrencySettledTime.Date = CurrencyNowTime.Date，上次執行時間日期等於幣別時區時間日期，不可再執行(原邏輯)
            if (task.LastCurrencySettledTime != null)
            {
                if (task.LastCurrencySettledTime.Date == task.CurrencyNowTime.Date)
                {
                    _logger.LogWarning($"task.LastCurrencySettledTime Invaild. task value={JsonConvert.SerializeObject(task)}");
                    continue;
                }
            }
            doTaskList.Add(task);
        }

        return doTaskList;
    }

    public int SetTaskDone(int siteId, int currencyId, string workerName)
    {
        return _angPaoEventService.SetTaskDone(siteId, currencyId, workerName);
    }

    public IEnumerable<WorkerCustInfo> GetCustList(int siteId, int currencyId, string currencyTimeZone, int pageNumber, int pageSize)
    {
        return _angPaoEventService.GetCustList(siteId, currencyId, currencyTimeZone, pageNumber, pageSize);
    }

    public DrawEventTpBetRecord GetTpBetRecord(int custId, string timeZone)
    {
        return _angPaoEventService.GetTpBetRecord(custId, timeZone);
    }

    public void GenTicket(int siteId, int currencyId, WorkerCustInfo custInfo, DrawEventTpBetRecord record)
    {
        var ticketQuantity = CaculateTicketQuantity(siteId, currencyId, record.ValidBet);
        if (ticketQuantity > 0 && !_angPaoEventService.GenTicket(custInfo, record, ticketQuantity))
        {
            _logger.LogError($"GenTicket Fail. task value={JsonConvert.SerializeObject(custInfo)}");
        }
    }

    public int CaculateTicketQuantity(int siteId, int currencyId, decimal vaildBet)
    {
        if (!CurrencyExchange.ContainsKey($"{siteId}_{currencyId}"))
            return 0;
        var rate = CurrencyExchange[$"{siteId}_{currencyId}"];
        return Math.Floor(vaildBet / rate) > 3 ? 3 : (int)Math.Floor(vaildBet / rate);
    }
}
