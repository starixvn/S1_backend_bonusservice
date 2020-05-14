namespace BonusService.Service.Model
{
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
        public int TpId { set; get; }
        public int SiteId { set; get; }
        public decimal Percentage { set; get; }
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
}
