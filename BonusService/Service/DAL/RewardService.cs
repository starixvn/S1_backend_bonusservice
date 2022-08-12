using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;

namespace BonusService.Service.DAL
{
    public class RewardService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public RewardService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<ClaimVerificationRewardData> GetVerificationRewardData()
        {
            return _reportDbClient.Query<ClaimVerificationRewardData>("Service_ClaimVerificationReward_Get");
        }
    }
}