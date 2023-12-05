using BonusService.Service.DAL;
using BonusService.Service.Model;

namespace BonusService.Service.BLL
{
    public class NewYear2024BLL
    {
        private readonly StoreService _storeService;

        public NewYear2024BLL(IServiceProvider provider)
        {
            _storeService = provider.GetRequiredService<StoreService>();
        }

        public int DrawPrizeRewardPoints(Customer customer)
        {
            var result = -1;
            var param = new CreateManualRewardPointInfoRequest
            {
                SiteId = customer.SiteId,
                UserName = customer.UserName,
                Point = 200,
                CategoryId = 3,
                Memo = "NewYear2024 88Reward200pts",
                Operator = "System"
            };
        
            var response = _storeService.ManualRewardPointAsync(param).Result;

            if (response != null)
            {
                result = response.ErrorCode;
            }
            return result;
        }
    }
}