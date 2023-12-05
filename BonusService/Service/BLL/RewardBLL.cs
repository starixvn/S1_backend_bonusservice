using BonusService.Service.DAL;

using MemberSite.Service.Model;

namespace BonusService.Service.BLL
{
    public class RewardBLL
    {
        private readonly RewardService _rewardService;

        public RewardBLL(IServiceProvider provider)
        {
            _rewardService = provider.GetRequiredService<RewardService>();
        }

        public IEnumerable<QueueJobData> GetVerificationRewardData()
        {
            return _rewardService.GetVerificationRewardData();
        }
    }
}