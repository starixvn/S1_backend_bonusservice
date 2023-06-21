using BonusService.Service.Model;

namespace BonusService.Service.DAL.Interface
{
    public interface IReferralService
    {
        IEnumerable<ReferralInfo> GetReferralSettlement();
        int GetRemainingCount(int agentId, int siteId, int currencyId);

        IEnumerable<DepositReferralCustInfo> GetDepositReferralCustList(int siteId, int currencyId, DateTime specificDate, string currencyTimeZone);
    }
}