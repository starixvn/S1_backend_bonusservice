using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
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
                   inputs.Add("bonusCode", "PPNYFreeSpin16012024");
                    request = new AddPrize
                    {
                        // PP
                        TpId = 5,
                        CustId = customer.CustId,
                        SiteId = customer.SiteId,
                        inputs = inputs
                    };
                    break;
            }

            var result = tpId != 39 ? _tpService.AddPrizeToPlayer(request).Result : APRequest(customer);

            if (result is not null)
            {
                return result.ErrorCode;
            }
            
            return -1;
        }
        
        private ApiResult APRequest(Customer customer)
        {
            try
            {
                var key = $"NewYear2024:AP:CustId:{customer.CustId}";
                var currencyId = (CurrencyEnum)customer.CurrencyId;
                var getCustAPCount = new CustGetCount();
            
                var getCustPrizeList = _memberService.GetCustDrawPrizeList(customer.SiteId,customer.CustId,0);
                var custAPCount = getCustPrizeList.Where(x => x.PrizeName.Contains("Advant Play Free Bet"));
                if (custAPCount.Count() + 1 >= 31)
                {
                    //超過30次了 88
                    return new ApiResult(){ ErrorCode = -1 , Message = "Exceeded the number of times allowed for collection."};
                }

                var checkCustGetAPCount = _redisHelper.KeyExists(key);
                if (checkCustGetAPCount == false)
                {
                    getCustAPCount = new CustGetCount()
                    {
                        count = 1
                    };
                    _redisHelper.StringSet(key,JsonConvert.SerializeObject(getCustAPCount) , TimeSpan.FromDays(30));
                }
                else
                {
                    getCustAPCount = _redisHelper.StringGetAsync<CustGetCount>(key).Result;
                }
            
                var champaignList = _redisHelper.StringGetAsync<APCampaign>("NewYear2024:AP").Result;
                var campaignList = champaignList.Sites
                    .FirstOrDefault(s => s.SiteId == customer.SiteId)?
                    .Currency.FirstOrDefault(c => c.CurrencyId == currencyId.ToString())?
                    .CampaignList;
            
                if(campaignList == null)
                {
                    return new ApiResult(){ ErrorCode = -1 , Message = "ChampaignList is null"}; 
                }

                var cpId = campaignList[getCustAPCount.count - 1];
                var inputs = new Dictionary<string, string>
                {
                    { "CampaignId", cpId },
                    { "operatorUser", "NewYear2024Job" }
                };
                
                var request = new AddPrize
                {
                    // AP
                    TpId = 39,
                    CustId = customer.CustId,
                    SiteId = customer.SiteId,
                    inputs = inputs
                };
                
                var result = _tpService.AddPrizeToPlayer(request).Result;
                
                if (result.ErrorCode == 0)
                {
                    var insert = new CustGetCount()
                    {
                        count = getCustAPCount.count + 1
                    };
                    _redisHelper.StringSet(key,JsonConvert.SerializeObject(insert) , TimeSpan.FromDays(30));
                    
                    return new ApiResult()
                    {
                        ErrorCode = 0
                    };
                    
                }

                if (result.RawMessage != "there is no valid new player in the PlayerList")
                    return new ApiResult()
                    {
                        ErrorCode = -1,
                        Message = result.Message,
                        RawMessage = result.RawMessage
                    };
                {
                    var insert = new CustGetCount()
                    {
                        count = getCustAPCount.count + 1
                    };
                    _redisHelper.StringSet(key,JsonConvert.SerializeObject(insert) , TimeSpan.FromDays(30));
                    
                    return new ApiResult()
                    {
                        ErrorCode = -1
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APRequest Error : {ex.Message}");
                return new ApiResult(){ ErrorCode = -1 , Message = "APRequest Exception"};
            }
        }

        #region Model

        private class CustGetCount
        {
            public int count { get; set; }
        }
        
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