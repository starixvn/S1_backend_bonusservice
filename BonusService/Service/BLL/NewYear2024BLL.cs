using BonusService.Service.DAL;
using BonusService.Service.Model;

namespace BonusService.Service.BLL
{
    public class NewYear2024BLL
    {
        private readonly StoreService _storeService;
        private readonly TpService _tpService;

        public NewYear2024BLL(IServiceProvider provider)
        {
            _storeService = provider.GetRequiredService<StoreService>();
            _tpService = provider.GetRequiredService<TpService>();
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
        
        public int AddFreeSpinToPlayer(Customer customer)
        {
            var inputs = new Dictionary<string, string>
            {
                {"bonusCode", "NewYear2024"}
            };
            
            var request = new AddFreeSpin
            {
                // PP
                TpId = 5,
                CustId = customer.CustId,
                SiteId = customer.SiteId,
                inputs = inputs
            };
            var result = _tpService.AddFreeSpinToPlayer(request).Result;

            if (result is not null)
            {
                return result.ErrorCode;
            }
            
            return -1;
        }
    }
}