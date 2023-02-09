using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;
using Ubiety.Dns.Core.Records;

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

        public CheckPromotionBlockIp CheckBlockIpFromDb(ClaimPromotion claimPromotion, int siteId, string custIP)
        {
            var result = new CheckPromotionBlockIp
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockIp = false,
                UseWhitelist = false
            };

            // check db 相同 IP 領取是否達到上限
            var p1 = new DynamicParameters(new { _BonusCode = claimPromotion.BonusCode, _SiteId = siteId, _IP = custIP, _LimitCount = claimPromotion.SameIpRestrictAvailableCount });
            p1.Add("_IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Service_PromotionBlockIp_Get", p1);
            if (p1.Get<int>("_IsBlocked") == 1)
            {
                var userName = p1.Get<string>("_UserName");

                result.BonusErrorCode = BonusErrorCode.SameIp;
                result.IsBlockIp = true;
                result.ErrorMsg = $"Same IP：{custIP} (Account：{userName})";
                result.UseWhitelist = false;
            }

            return result;
        }

        public CheckPromotionBlockDeviceId CheckBlockDeviceIdFromDb(ClaimPromotion claimPromotion, int siteId, string deviceId)
        {
            var result = new CheckPromotionBlockDeviceId
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockDeviceId = false,
                UseWhitelist = false
            };

            // check db 相同 DeviceId 領取是否達到上限
            var p1 = new DynamicParameters(new { _BonusCode = claimPromotion.BonusCode, _SiteId = siteId, _DeviceID = deviceId, _LimitCount = claimPromotion.SameIpRestrictAvailableCount });
            p1.Add("_IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Service_PromotionBlockDeviceId_Get", p1);

            if (p1.Get<int>("_IsBlocked") == 1)
            {
                var userName = p1.Get<string>("_UserName");

                result.BonusErrorCode = BonusErrorCode.SameDeviceId;
                result.IsBlockDeviceId = true;
                result.ErrorMsg = $"Same Device：{deviceId} (Account：{userName})";
                result.UseWhitelist = false;
            }

            return result;
        }

        public CheckPromotionBlockRealName CheckBlockRealNameFromDb(ClaimPromotion claimPromotion, int siteId, string realName)
        {
            var result = new CheckPromotionBlockRealName
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockRealName = false,
                UseWhitelist = false
            };

            var patterns = new List<string>
            {
                realName,
                realName.Replace(" ", string.Empty)
            };

            var p = new DynamicParameters(new
            {
                _SiteId = siteId,
                _BonusCode = claimPromotion.BonusCode,
                _AccountNames = JsonConvert.SerializeObject(patterns)
            });

            var realNameList = _reportDbClient.Query<CustRealNameDto>("Service_PromotionBlockRealName_Get", p);

            var realNames = realNameList.Where(x => patterns.Any(y => y.Equals(x.RealName, StringComparison.CurrentCultureIgnoreCase))).ToList();

            if (realNames.Count >= claimPromotion.SameRealNameRestrictAvailableCount)
            {
                result.BonusErrorCode = BonusErrorCode.SameRealName;
                result.IsBlockRealName = true;
                result.ErrorMsg = $"Same Full Name：{realNames.First().RealName} (Account：{realNames.First().UserName})";
                result.UseWhitelist = false;
                return result;
            }

            return result;
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

        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId)
        {
            return _mainDbClient.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get", new { CustId = custId });
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
            return _reportDbClient.Query<CustPromotion>("Member_PromotionList_Get_V8",
                new { request.CustId, request.SiteId, request.CurrencyId, request.DeviceType, request.LangId, request.PromotionType });
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

        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteid, string specialCode)
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

        public TagDto GetTagName(int tagId)
        {
            return _reportDbClient.QueryFirst<TagDto>("Service_TagName_Get ", new DynamicParameters(new { _TagId = tagId }));
        }


        /// <summary>
        /// 寫入 bonus 領取失敗 Log
        /// </summary>
        /// <param name="bonusCode"></param>
        /// <param name="promotionType"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMsg"></param>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        public int InsertPromotionFailLog(string bonusCode, PromotionType promotionType, int errorCode, string errorMsg,
            int siteId, int custId, int currencyId)
        {
            return _mainDbClient.Execute("Service_PromotionReceiveFailLog_Ins",
                new DynamicParameters(new
                {
                    _BonusCode = bonusCode,
                    _PromotionType = (int)promotionType,
                    _ErrorCode = errorCode,
                    _ErrorReason = errorMsg,
                    _SiteId = siteId,
                    _CustId = custId,
                    _CurrencyId = currencyId
                }));
        }


        /// <summary>
        /// 寫入 Bonus 領取成功 log
        /// </summary>
        /// <param name="bonusCode"></param>
        /// <param name="promotionType"></param>
        /// <param name="ip"></param>
        /// <param name="deviceId"></param>
        /// <param name="realName">真實姓名 CustBankInfo.AccountName</param>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="userName">玩家帳號</param>
        /// <param name="currencyId"></param>
        /// <param name="useWhitelist"></param>
        /// <returns></returns>
        public int InsertPromotionSuccessLog(string bonusCode, PromotionType promotionType, string ip, string deviceId,
            string realName, int siteId, int custId, string userName, int currencyId, bool useWhitelist)
        {
            return _mainDbClient.Execute("Service_PromotionReceiveSuccessLog_Ins",
                new DynamicParameters(new
                {
                    _BonusCode = bonusCode,
                    _PromotionType = (int)promotionType,
                    _IP = ip,
                    _DeviceId = string.IsNullOrEmpty(deviceId) ? string.Empty : deviceId,
                    _RealName = realName,
                    _UserName = userName,
                    _SiteId = siteId,
                    _CustId = custId,
                    _CurrencyId = currencyId,
                    _UseWhitelist = useWhitelist
                }));
        }

        public VipLevelWithBlockTagsDto GetVipLevelWithBlockTags(int siteId, string specialCode)
        {
            return _reportDbClient.QueryFirst<VipLevelWithBlockTagsDto>("Service_PromotionSpecialCodeCondition_Get ", new DynamicParameters(new { _SiteId = siteId, _SpecialCode = specialCode }));
        }
    }
}
