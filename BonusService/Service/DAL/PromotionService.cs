using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;

namespace BonusService.Service.DAL
{
    public class PromotionService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _apiUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly IEnumerable<AllowIp> _allowIp;

        public PromotionService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _apiUrl = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _courierAdapter = serviceProvider.GetRequiredService<CourierAdapter>();
            _allowIp = serviceProvider.GetRequiredService<IEnumerable<AllowIp>>();
        }
        public T GetPromotion<T>(int custId, int currencyId, int siteId, string bonusCode, int deviceType, bool getDetail)
        {
            return _reportDbClient.QueryFirst<T>("Member_Promotion_Get_V4", new DynamicParameters(new
            {
                CustId = custId,
                CurrencyId = currencyId,
                SiteId = siteId,
                BonusCode = bonusCode,
                DeviceType = deviceType,
                GetDetail = getDetail
            }));
        }
            
        public bool IsBlockIP(ClaimPromotion claimPromotion, string bonusCode, int siteId, string custIP)
        {
            if ((claimPromotion.PromotionType == PromotionType.FreeBet || (claimPromotion.IsSpecial && claimPromotion.PromotionType == PromotionType.FstDeposit))
                && !string.IsNullOrEmpty(custIP)
                && !_allowIp.Any(item => item.Type == AllowIpType.Bonus && item.IP == custIP))
            {
                var p1 = new DynamicParameters(new { BonusCode = bonusCode, SiteId = siteId, IP = custIP });
                p1.Add("IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
                _reportDbClient.Execute("Service_BlockFreeBet_Get", p1);
                if (p1.Get<int>("IsBlocked") == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, string bonusCode, int deviceType)
        {
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode, DeviceType = deviceType });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_PromotionCanJoin_Get_V8", p);
            
            var custCondition = new CustCondition
            {
                Result = p.Get<int>("Result"),
                CanJoin = p.Get<byte?>("CanJoin") ?? 0,
                Amount = p.Get<decimal?>("Amount") ?? 0,
                RefList = p.Get<string>("RefList"),
                RefId = p.Get<string>("RefId"),
                reference = new List<Ref>()
            };
            if (custCondition.RefList != null && custCondition.RefList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                custCondition.reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(custCondition.RefList);
            }
           
            return custCondition;
        }

        public int AfterReward(ClaimPromotion claimPromotion, int siteId, string bonusCode, int currencyId, string custIp, MySqlTransaction trans)
        {
            var result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set", trans,
                new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId });
            if (result > 0)
            {
                if ((claimPromotion.PromotionType == PromotionType.FreeBet || (claimPromotion.IsSpecial && claimPromotion.PromotionType == PromotionType.FstDeposit)) 
                    && !string.IsNullOrEmpty(custIp)
                    && !_allowIp.Any(item => item.Type == AllowIpType.Bonus && item.IP == custIp))
                    _mainDbClient.Execute("Service_BlockFreeBet_Ins", trans, new { BonusCode = bonusCode, SiteId = siteId, IP = custIp });
            }
            return result;
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId, bool useMainDb)
        {
            var p = new { CustId = custId };
            return useMainDb
                ? _mainDbClient.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get", p)
                : _reportDbClient.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get", p);
        }

        public IEnumerable<CashBackDetails> GetCustCashBack(int custId, int siteId, int currencyId, CustPromotion promotion, string calculateDate)
        {
            return _reportDbClient.Query<CashBackDetails>("Member_CustCashBack_Get",
                new DynamicParameters(new
                {
                    CustId = custId,
                    promotion.BonusCode,
                    PromoCategoryId = promotion.CategoryId,
                    CalculateDate = calculateDate,
                    CurrencyId = currencyId,
                    SiteId = siteId
                }));
        }

        public IEnumerable<CustCashBackCheckOutput> GetCustCashBackCheck(int custId, CustPromotion promotion, string calculateDate)
        {
            return _reportDbClient.Query<CustCashBackCheckOutput>("Member_CustCashBackCheck_Get",
                new DynamicParameters(new
                {
                    CustId = custId,
                    promotion.BonusCode,
                    PromoCategoryId = promotion.CategoryId,
                    CalculateDate = calculateDate
                }));
        }

        public IEnumerable<CashBackDetail> GetCashBackInfo(int custId, CustPromotion promotion, DateTime lastMonday, DateTime lastSunday)
        {
            return _reportDbClient.Query<CashBackDetail>("Member_CashBackInfo_Get",
                new DynamicParameters(new
                {
                    CustId = custId,
                    promotion.BonusCode,
                    PeriodFrom = lastMonday.ToString("yyyy-MM-dd"),
                    PeriodTo = lastSunday.ToString("yyyy-MM-dd"),
                    PromoCategoryId = promotion.CategoryId
                }));
        }

        public async Task<ApiResult<TransferResult>> TransferTpBonus(TpBonusTransfer tpBonusTransfer)
        {
            var myUrl = $"{_apiUrl}BonusTransfer";
            var Data = await _courierAdapter.MyPostAsync<TransferResult, TpBonusTransfer>(myUrl, tpBonusTransfer);
            return Data;
        }

        public IEnumerable<CustRunningBonus> GetCustRunningBonus(int custId)
        {
            return _mainDbClient.Query<CustRunningBonus>("Service_CustRunningBonus_Get", new DynamicParameters(new { CustId = custId }));

        }

        public IEnumerable<CustPromotion> GetPromotionList(CustPromotionRequest request)
        {
            return _reportDbClient.Query<CustPromotion>("Member_PromotionList_Get_V7",
                new { request.CustId, request.SiteId, request.CurrencyId, request.DeviceType, request.LangId });
        }
        
        public IEnumerable<CustPromotion> GetDepositPromotionList(CustPromotionRequest request)
        {
            return _reportDbClient.Query<CustPromotion>("Member_DepositPromotion_Get_V4", 
                new { request.CustId, request.DeviceType });
        }
        
        public IEnumerable<CustPromotion> GetSpecialPromotionList(CustPromotionRequest request)
        {
            return _reportDbClient.Query<CustPromotion>("Member_SPCodePromotionList_Get_V4",
                new { request.CustId, request.DeviceType });
        }

        public BonusWallet GetBonusInfoByCust(int custId, CustPromotion promo)
        {
           return _reportDbClient.QueryFirst<BonusWallet>("Member_BonusInfo_GetByCust",
                new DynamicParameters(new { promo.BonusCode, PromoCategoryId = promo.CategoryId, promo.TpId, CustId = custId }));
        }
        
        public int SetScratchCardCode(SpecialCodeRequest request)
        {
            var p = new DynamicParameters(new { request.CustId, ScratchCardCode = request.SpecialCode, request.DeviceType });
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_CustScratchCardCodeV3_Ins", p);
            return p.Get<int>("Result");
        }
        
        public bool SetSpecialCode(SpecialCodeRequest request)
        {
            var result = _mainDbClient.Execute("Member_CustSpecialCodeV3_Ins", new { request.CustId, request.SpecialCode, request.DeviceType });
            return result > 0;
        }
        
        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteid,string specialCode)
        {
            return _reportDbClient.QueryFirst<PromotionBySpecialCode>("Member_PromotionBySpecialCode_Get", new DynamicParameters(new { SpecialCode = specialCode, SiteId = siteid }));
        }
        
        public PromotionBySpecialScratchCard GetPromotionBySpecialScratchCard(int siteid, string specialCode)
        {
            return _reportDbClient.QueryFirst<PromotionBySpecialScratchCard>("Member_PromotionBySpecialScratchCard_Get", new DynamicParameters(new { ScratchCardCode = specialCode, SiteId = siteid }));
        }
        
        public BonusInfoByBonusCode GetBonusInfoByBonusCode(int CustId, string BonusCode)
        {
            return _reportDbClient.QueryFirst<BonusInfoByBonusCode>("Member_BonusinfoByBonusCode_Get", new DynamicParameters(new { CustId, BonusCode }));
        }
    }
}
