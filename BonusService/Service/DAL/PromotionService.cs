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
        private readonly CustInfoService _custInfoService;


        public PromotionService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _apiUrl = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _courierAdapter = serviceProvider.GetRequiredService<CourierAdapter>();
            _allowIp = serviceProvider.GetRequiredService<IEnumerable<AllowIp>>();
            _custInfoService = serviceProvider.GetRequiredService<CustInfoService>();
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

            // check db 相同 IP 領取紀錄
            var p1 = new DynamicParameters(new { _BonusCode = claimPromotion.BonusCode, _SiteId = siteId, _IP = custIP });
            var sameIpList = _reportDbClient.Query<PromotionBlockIpsDto>("Service_PromotionBlockIp_Get", p1).ToList();

            // 判斷相同 IP 是否超過領取上限
            if (sameIpList.Count >= claimPromotion.SameIpRestrictAvailableCount)
            {
                var userName = sameIpList.OrderByDescending(x => x.CreateTime)
                    .Select(r => r.UserName)
                    .First();

                result.BonusErrorCode = BonusErrorCode.SameIp;
                result.IsBlockIp = true;
                result.ErrorMsg = $"Same IP：{custIP} (Account：{userName})";
                result.UseWhitelist = false;
            }

            return result;
        }

        public CheckPromotionBlockDeviceId CheckBlockDeviceIdFromDb(ClaimPromotion claimPromotion, int siteId, IEnumerable<string> deviceIds)
        {
            var result = new CheckPromotionBlockDeviceId
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockDeviceId = false,
                UseWhitelist = false
            };

            var compareDeviceIdInfo = new List<CompareDeviceIdDto>();

            // 找出領過相同 BonusCode 的 CustId + UserName + DeviceId
            var p1 = new DynamicParameters(new { _BonusCode = claimPromotion.BonusCode, _SiteId = siteId });
            var promotionDeviceIds = _reportDbClient.Query<PromotionDeviceIdsDto>("Service_PromotionBlockDeviceId_Get", p1).ToList();
            AddPromotionDeviceIdToCompareList(compareDeviceIdInfo, promotionDeviceIds);

            var custIds = promotionDeviceIds
                .Select(r => r.CustId)
                .Distinct()
                .ToList();

            // 找出領過相同 BonusCode 的人，曾經有哪些DeviceId
            var custDeviceInfos = _custInfoService.GetCustDeviceId(custIds, siteId).ToList();
            AddPromotionDeviceIdToCompareList(compareDeviceIdInfo, custDeviceInfos);

            var deviceGroupResult = compareDeviceIdInfo
                .DistinctBy(x => new { x.CustId, x.DeviceId })
                .Where(r => deviceIds.Contains(r.DeviceId))
                .GroupBy(x => x.DeviceId)
                .Where(r => r.Count() >= claimPromotion.SameDeviceIdAvailableCount)
                .Select(r => r)
                .FirstOrDefault();

            if (deviceGroupResult == null) return result;

            var userName = deviceGroupResult.First().UserName;
            result.BonusErrorCode = BonusErrorCode.SameDeviceId;
            result.IsBlockDeviceId = true;
            result.ErrorMsg = $"Same Device：{deviceGroupResult.Key} (Account：{userName})";
            result.UseWhitelist = false;

            return result;
        }

        private static void AddPromotionDeviceIdToCompareList(List<CompareDeviceIdDto> compareDeviceIdInfo, List<PromotionDeviceIdsDto> deviceIdInfo)
        {
            compareDeviceIdInfo.AddRange(deviceIdInfo.Select(r => new CompareDeviceIdDto
            {
                CustId = r.CustId,
                UserName = r.UserName,
                DeviceId = r.DeviceId
            }));
        }

        public static void AddPromotionDeviceIdToCompareList(List<CompareDeviceIdDto> compareDeviceIdInfo, List<CustDeviceDto> deviceIdInfo)
        {
            compareDeviceIdInfo.AddRange(deviceIdInfo.Select(r => new CompareDeviceIdDto
            {
                CustId = r.CustId,
                UserName = r.UserName,
                DeviceId = r.DeviceId
            }));
        }


        /// <summary>
        /// 去 PromotionSuccessLog 找出領過相同 BonusCode 的紀錄，把 RealName 跟 custId 抓出來
        /// 再去改名 Log 找出 cusid 有改過的紀錄，最後判斷領取人當下是否跟 bonus log + 改名紀錄有重複名稱
        /// </summary>
        /// <param name="claimPromotion"></param>
        /// <param name="siteId"></param>
        /// <param name="realName"></param>
        /// <returns></returns>
        public CheckPromotionBlockRealName CheckBlockRealNameFromDb(ClaimPromotion claimPromotion, int siteId, string realName)
        {
            var result = new CheckPromotionBlockRealName
            {
                BonusErrorCode = BonusErrorCode.Success,
                IsBlockRealName = false,
                UseWhitelist = false
            };

            var compareNameInfo = new List<CompareRealNameDto>();

            var p1 = new DynamicParameters(new
            {
                _SiteId = siteId,
                _BonusCode = claimPromotion.BonusCode
            });
            var realNameList = _reportDbClient.Query<PromotionCustRealNameDto>("Service_PromotionBlockRealName_Get", p1).ToList();
            AddPromotionRealNameToCompareList(compareNameInfo, realNameList); // 領過相同 BonusCode 的 CustId + RealName 清單


            var p2 = new DynamicParameters(new
            {
                _CustIds = JsonConvert.SerializeObject(realNameList.Select(r => r.CustId).ToList()),
            });
            var setRealNameLog = _reportDbClient.Query<CustSetRealNameLogDto>("Service_SetRealNameLog_Get", p2).ToList();
            AddBeforeNameWithAfterNameToCompareList(compareNameInfo, setRealNameLog); // 領過相同 Bonus "且" 有改過名的清單

            var patterns = new List<string>
            {
                realName,
                realName.Replace(" ", string.Empty)
            };

            // 檢查是否有相同
            var realNameCheckResult = new List<CompareRealNameDto>();
            foreach (var name in patterns)
            {
                realNameCheckResult.AddRange(compareNameInfo.Where(r => r.RealName.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList());
            }

            if (!realNameCheckResult.Any() ||
                realNameCheckResult.Count < claimPromotion.SameRealNameRestrictAvailableCount) return result;

            var custInfo = realNameCheckResult.First();
            result.BonusErrorCode = BonusErrorCode.SameRealName;
            result.IsBlockRealName = true;
            result.ErrorMsg = $"Same Full Name：{custInfo.RealName} (Account：{custInfo.UserName})";
            result.UseWhitelist = false;
            return result;
        }

        /// <summary>
        /// 已經領取相同 Bonus 的玩家清單
        /// </summary>
        /// <param name="compareNameInfo"></param>
        /// <param name="realNameList"></param>
        private static void AddPromotionRealNameToCompareList(List<CompareRealNameDto> compareNameInfo, List<PromotionCustRealNameDto> realNameList)
        {
            compareNameInfo.AddRange(realNameList.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.RealName
            }));
        }

        /// <summary>
        /// 已領取相同 Bonus "且" 有改過名的玩家清單
        /// </summary>
        /// <param name="compareNameInfo"></param>
        /// <param name="setRealNameLog"></param>
        private static void AddBeforeNameWithAfterNameToCompareList(List<CompareRealNameDto> compareNameInfo, List<CustSetRealNameLogDto> setRealNameLog)
        {
            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.BeforeRealName
            }));

            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.AfterRealName
            }));
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

        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId, int limit)
        {
            return _mainDbClient.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get_V2", new { CustId = custId, Limit = limit });
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
