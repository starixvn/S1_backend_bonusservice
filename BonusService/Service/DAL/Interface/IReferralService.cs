using System.Collections.Generic;
using BonusService.Service.Model;

namespace BonusService.Service.DAL.Interface
{
    public interface IReferralService
    {
        IEnumerable<ReferralInfo> GetReferralSettlement();
        int GetRemainingCount(int agentId, int siteId, int currencyId);

        int PayoutReferral(IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList);
    }
}