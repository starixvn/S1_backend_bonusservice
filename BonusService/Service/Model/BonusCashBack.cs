using System;
using System.Collections.Generic;

namespace BonusService.Service.Model
{
    public class RebateTask
    {
        public string BonusCode { set; get; }
        public int SiteId { set; get; }
        public int PromotionType { set; get; }
        public int PeriodType { set; get; }
        public int PeriodDay { set; get; }
        public int CurrencyId { set; get; }
        public int GameType { set; get; }
        public decimal Percentage { set; get; }
    }

    public class RebateInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
    }
    public class RebateTpBetRecord
    {
        public DateTime SettledDate { set; get; }
        public int GameType { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public string TpUserName { set; get; }
        public decimal ValidBet { set; get; }
        public decimal WinlostAmount { set; get; }
    }
    public class RebateRecord
    {
        public int SettledDate { set; get; }
        public int GameType { set; get; }
        public decimal Sum { set; get; }
    }
    public class BonusCashBack
    {
        public int Id { set; get; }
        public int SiteId { set; get; }
        public int Period { set; get; }
        public int DayOfWeek { set; get; }
        public int PayHour { set; get; }
        public int PayMinute { set; get; }
        public int WithdrawCondition { set; get; }
        public decimal DefaultPercentage { set; get; }
        public decimal TimeZone { set; get; }
    }

    public class BonusCashBackGames
    {
        public int CashbackId { set; get; }
        public int GameId { set; get; }
        public int SiteId { set; get; }
        public decimal? Percentage { set; get; }
        public int Status { set; get; }
    }

    public class BonusCashBackMinMax
    {
        public int SiteId { set; get; }
        public int CashbackId { set; get; }
        public int CurrencyId { set; get; }
        public decimal MinPay { set; get; }
        public decimal MaxPay { set; get; }
        public decimal MinBet { set; get; }
        public decimal MaxBet { set; get; }
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
        public int WithdrawCondition {set;get;}
        public decimal WinlostAmountCountCalculationTotal { set; get; }
        public long CalculationDay { set; get; }
        public List<BonusCashBackInfoListDetail> BonusCashBackInfoListDetails { get; set; }
    }
}
