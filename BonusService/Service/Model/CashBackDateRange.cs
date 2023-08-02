namespace BonusService.Service.Model
{
    public record CashBackDateRange(string TpBetRecordStartDate, string TpBetRecordEndDate, string DepositStartDate, string DepositEndDate);

    public record CashBackInfoDateRange(DateTime StartDate, DateTime EndDate);
}
