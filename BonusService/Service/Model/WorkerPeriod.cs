namespace BonusService.Service.Model;


public class WorkerPeriodTask
{
    public int SiteId { set; get; }
    public int CurrencyId { set; get; }
    public int PeriodType { get; set; }
    public int PeriodDay { get; set; }
    public DateTime SpecificDate { get; set; }
    public int RedeemHours { set; get; }
    public DateTime SettleTime { set; get; }
    public string TimeZone { set; get; }
    public DateTime CurrencyNowTime { set; get; }
    public DateTime LastCurrencySettledTime { get; set; }
}

public class WorkerCustInfo
{
    public int CustId { set; get; }
    public int CurrencyId { set; get; }
    public string UserName { set; get; }
}

public class DrawEventTpBetRecord
{
    public DateTime SettledDate { set; get; }
    public decimal ValidBet { set; get; }
}
