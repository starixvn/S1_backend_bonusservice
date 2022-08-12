using BonusService.Service.Model;

namespace BonusService.Service.DAL.Interface
{
    public interface IReferralService
    {
        IEnumerable<ReferralInfo> GetReferralSettlement();
        int GetRemainingCount(int agentId, int siteId, int currencyId);
    }
}