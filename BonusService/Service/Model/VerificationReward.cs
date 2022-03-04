using System;

namespace MemberSite.Service.Model
{
    public class VerificationRewardData
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }        
        public int VerificationType { get; set; }
        public decimal PhoneReward { get; set; }
        public decimal EmailReward { get; set; }
    }
    
    public class CustVerificationRewardData
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public bool PhoneVerified { get; set; }
        public decimal? PhoneReward { get; set; }
        public DateTime? PhoneRewardTime { get; set; }
        public bool EmailVerified { get; set; }
        public decimal? EmailReward { get; set; }
        public DateTime? EmailRewardTime { get; set; }
        public decimal MaxDepositAmount { get; set; }
    }
    
    public class VerificationRewardLimit
    {
        public int MinDeposit { get; set; }
    }
}