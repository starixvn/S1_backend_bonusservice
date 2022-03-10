using System;
using System.Collections.Generic;
using BonusService.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.BLL
{
    public class RewardBLL
    {
        private readonly RewardService _rewardService;
        
        public RewardBLL(IServiceProvider provider)
        {
            _rewardService = provider.GetRequiredService<RewardService>();
        }

        public IEnumerable<ClaimVerificationRewardData> GetVerificationRewardData()
        {
            return _rewardService.GetVerificationRewardData();
        }
    }
}