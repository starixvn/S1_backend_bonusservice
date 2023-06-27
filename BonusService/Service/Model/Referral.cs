using Google.Protobuf.WellKnownTypes;

namespace BonusService.Service.Model
{
    public class ReferralInfo
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserLang { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal ReferralFreeCredit { get; set; }
    }

    public class DepositReferralCustInfo
    {
        public int SettledDate { set; get; }
        public int Referrer_CustId { set; get; }
        public string Referrer_UserName { private set; get; }
        public int Referrer_ActStatus { set; get; }
        public int Referrer_Status { set; get; }
        public int Referred_CustId { set; get; }
        public string Referred_UserName { set; get; }
        public int Referred_Status { set; get; }
        public int DepositId { set; get; }
        public string TransId { set; get; }
        public decimal ActualDeposit { set; get; }
        public decimal DepositRewardPercentage { set; get; }
        public decimal MinPayout { set; get; }
        public decimal MaxPayout { set; get; }
    }

    public class DepositReferralReferrerCaculateResult
    {
        public int SettledDate { private set; get; }
        public int Referrer_CustId { private set; get; }
        public string Referrer_UserName { private set; get; }
        public decimal MinPayout { private set; get; }
        public decimal MaxPayout { private set; get; }
        public decimal CaculateDepositReferral { private set; get; }
        public decimal RewardDepositReferral { private set; get; }
        public IEnumerable<DepositReferralReferredInfo> ReferredDetail { private set; get; }

        public DepositReferralReferrerCaculateResult(int settledDate,
            int referrerCustId,
            string userName,
            decimal minPayout,
            decimal maxPayout,
            decimal caculateDepositReferral,
            IEnumerable<DepositReferralReferredInfo> referredDetail)
        {
            SettledDate = settledDate;
            Referrer_CustId = referrerCustId;
            Referrer_UserName = userName;
            MinPayout = minPayout;
            MaxPayout = maxPayout;
            CaculateDepositReferral = caculateDepositReferral;
            RewardDepositReferral = minPayout > caculateDepositReferral ? 0
                : maxPayout < caculateDepositReferral ? maxPayout : caculateDepositReferral;
            ReferredDetail = referredDetail;
        }
    }

    public class DepositReferralReferredInfo
    {
        public int Referred_CustId { set; get; }
        public string Referred_UserName { set; get; }
        public decimal TotalDeposit { set; get; }
        public decimal DepositRewardPercentage { set; get; }
        public decimal CaculateDepositReferral => TotalDeposit * DepositRewardPercentage / 100;
        public Dictionary<string, decimal> DepositInfo { set; get; }
    }
}
