using BonusService.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.Model
{
    public class DepositBonus
    {
        public string TransId { set; get; }
        public string BonusCode { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public decimal LockedAmount { set; get; }
        public int DeviceType { set; get; }
        public decimal Amount { set; get; }
    }

    public class Promotion
    {
        public string BonusCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public PromotionType PromotionType { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ExpiryDays { get; set; }
        public int PeriodTo { get; set; }
        public int CurrencyId { get; set; }
        public int RollOver { get; set; }
        public decimal Bonus { get; set; }
        public BonusType BonusType { get; set; }
        public LockedType LockedType { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal LockedAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int BonusInfoStatus { get; set; }
        public int Sort { get; set; }
        public CustRollOver custRollOver { get; set; }
        public string Details { get; set; }
        public int TpId { get; set; }
        public bool IsSpecial { get; set; }
        public string SpecialCode { get; set; }
        public decimal MinPayout { get; set; }
    }
}
