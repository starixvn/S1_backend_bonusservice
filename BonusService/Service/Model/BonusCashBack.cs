namespace BonusService.Service.Model
{
    public class RebateTask
    {
        public int SiteId { set; get; }
        public int CurrencyId { set; get; }
        public int? PeriodType { set; get; }
        public int? PeriodDay { set; get; }
        public int? RedeemHours { set; get; }
        public DateTime? SettleTime { set; get; }
        public DateTime SpecificDate { set; get; }
        public string TimeZone { set; get; }
        public DateTime CurrencyNowTime { set; get; }
        public DateTime LastCurrencySettledTime { get;set; }

        //===================已下未使用，可能是舊版rebate=======================
        public string BonusCode { set; get; }
        public int PromotionType { set; get; }
        public int GameType { set; get; }
        public decimal Percentage { set; get; }
        public decimal MaxBonus { set; get; }
        public decimal RollOver { set; get; }
        public decimal MinPayout { set; get; }
    }

    public class RebateExcludeGame
    {              
        public int GameType { set; get; }
        public string GameId { set; get; }
        public string AppId { set; get; }
    }

    public class RebateInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
    }
    public class VipRebateInfo : RebateInfo
    {
        public string UserName { set; get; }
        public string TpUserName { set; get; }
    }
    public class VipRebateTpBetRecord
    {
        public DateTime SettledDate { set; get; }
        public int CategoryId { set; get; }
        public int GameType { set; get; }
        public string GameID { set; get; }
        public decimal Percentage { set; get; }
        public decimal RedeemAmount { set; get; }
        public decimal ValidBet { set; get; }
        public decimal ValidTurnover { set; get; }
    }
    public class RebatePayoutLimit
    {
        public int SiteId { set; get; }
        public int CurrencyId { set; get; }
        public int CategoryId { set; get; }
        public decimal MaxPayout { set; get; }
        public decimal MinPayout { set; get; }
    }

    public class RebateRecord
    {
        public int SettledDate { set; get; }
        public int GameType { set; get; }
        public decimal Sum { set; get; }
    }

    public class TpCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BonusCashBackGames
    {
        public int CashbackId { set; get; }
        public int GameId { set; get; }
        public int SiteId { set; get; }
        public decimal? Percentage { set; get; }
        public int Status { set; get; }
    }

    public class TpBetRecord
    {
        public int TpId { set; get; }
        public string TransId { set; get; }
        public int GameType { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public int SiteId { set; get; }
        public string TpUserName { set; get; }
        public DateTime TransTime { set; get; }
        public DateTime SettledTime { set; get; }
        public decimal Stake { set; get; }
        public decimal WinlostAmount { set; get; }
        public string Betdetails { set; get; }
        public int SettlementType { set; get; }
        public int TicketStatus { set; get; }
        public string OriginalState { set; get; }
        public long TicketKey { set; get; }
        public long StartDate { set; get; }
        public long EndDate { set; get; }
        public decimal StakeCount { set; get; }
        public decimal WinlostAmountCount { set; get; }
    }
    public class BonusCashBackInfoListDetail
    {
        public int GameType { set; get; }
        public decimal StakeCount { set; get; }
        public decimal WinlostAmountCount { set; get; }
        public decimal WinlostAmountCountCalculation { set; get; }
        public decimal Percentage { set; get; }
    }

    public class BonusCashBackInfoList
    {
        public int CustId { get; set; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public int SiteId { set; get; }
        public string TpUserName { set; get; }
        public int WithdrawCondition { set; get; }
        public decimal WinlostAmountCountCalculationTotal { set; get; }
        public long CalculationDay { set; get; }
        public List<BonusCashBackInfoListDetail> BonusCashBackInfoListDetails { get; set; }
    }
}
