using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class NewYear2024BLL
    {
        private readonly StoreService _storeService;
        private readonly TpService _tpService;
        private readonly MemberSiteService _memberService;
        private readonly ICacheHelper _redisHelper;
        private readonly ILogger<NewYear2024BLL> _logger;

        public NewYear2024BLL(IServiceProvider provider)
        {
            _storeService = provider.GetRequiredService<StoreService>();
            _tpService = provider.GetRequiredService<TpService>();
            _memberService = provider.GetRequiredService<MemberSiteService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _logger = provider.GetRequiredService<ILogger<NewYear2024BLL>>();
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
        
        public int AddPrizeToPlayer(Customer customer,int tpId)
        {
            var request = new AddPrize();
            var inputs = new Dictionary<string, string>();

            switch (tpId)
            {
                case 5: 
                    //TODO 上PRD要改
                   inputs.Add("bonusCode", "NewYear2024Test0104");
                    request = new AddPrize
                    {
                        // PP
                        TpId = 5,
                        CustId = customer.CustId,
                        SiteId = customer.SiteId,
                        inputs = inputs
                    };
                    break;
                case 39:
                    inputs.Add("CampaignId", CheckAPCampaignId(customer.SiteId,customer.CustId,(CurrencyEnum)customer.CurrencyId));
                    inputs.Add("operatorUser", "NewYear2024Job");
                    request = new AddPrize
                    {
                        // AP
                        TpId = 39,
                        CustId = customer.CustId,
                        SiteId = customer.SiteId,
                        inputs = inputs
                    };
                    break;
            }
            
            var result = _tpService.AddPrizeToPlayer(request).Result;

            if (result is not null)
            {
                return result.ErrorCode;
            }
            
            return -1;
        }

        private string CheckAPCampaignId(int siteId,int custId,CurrencyEnum currencyId)
        {
            try
            {
                var getCustPrizeList = _memberService.GetCustDrawPrizeList(siteId,custId,0);
                var custAPCount = getCustPrizeList.Where(x => x.PrizeName == "Advant Play Free Bet");
                var thisAPCount = custAPCount.Count() + 1;
                if (thisAPCount > 30)
                {
                    //超過30次了 88
                    return "-1";
                }
                //TODO 上PRD要更新Campaign
                var champaignList = _redisHelper.StringGetAsync<APCampaign>("NewYear2024:AP").Result;
                var campaignList = champaignList.Sites
                    .FirstOrDefault(s => s.SiteId == siteId)?
                    .Currency.FirstOrDefault(c => c.CurrencyId == currencyId.ToString())?
                    .CampaignList;
                
                var result = campaignList != null ? campaignList[thisAPCount] : "-1";
                _logger.LogInformation($"CustId : {custId} , This AP Prize Count : {thisAPCount} , This CampaignId : {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"CheckAPCampaignId Error : {ex.Message}");
                throw;
            }
        }


        #region Model
        
        private class APCampaign
        {
            public List<Site> Sites { get; set; }
        }
        
        public class Site
        {
            public int SiteId { get; set; }
            public List<Currency> Currency { get; set; }
        }
        
        public class Currency
        {
            private string _currencyId;
            public string CurrencyId
            {
                get => _currencyId;
                set
                {
                    _currencyId = value;
                    CurrencyEnumValue = ConvertToCurrencyEnum(value);
                }
                
            }
            public List<string> CampaignList { get; set; }
            
            [JsonIgnore]
            public int CurrencyEnumValue { get; private set; }

            private int ConvertToCurrencyEnum(string currencyId)
            {
                if (Enum.TryParse(currencyId, out CurrencyEnum enumValue))
                {
                    return (int)enumValue;
                }
                return -1;
            }
        }
        
        

        #endregion
    }
}