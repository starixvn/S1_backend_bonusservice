using BonusService.Service.Model.enums;

namespace BonusService.Service.Model
{
    public class ExpiredBonus
    {
        public int BonusId { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public string UserLang { set; get; }
        public int CurrencyId { set; get; }
        public decimal Refund { set; get; }
        public int PromoCategoryId { get; set; }
        public PromotionType PromotionType { get; set; }
        public decimal Bonus { set; get; }
        public decimal BonusAmount { set; get; }
        public decimal LockedAmount { set; get; }
        public int TpId { set; get; }
    }

    public class TpBonusCancelTransfer
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
        public int PromoCategoryId { set; get; }
        public int BonusId { set; get; }
        public decimal RemainingBonus { set; get; }
        public decimal RefundAmount { set; get; }
    }
}
