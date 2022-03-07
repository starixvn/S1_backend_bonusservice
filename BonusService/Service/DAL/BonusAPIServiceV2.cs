using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BonusService.Service.DAL
{
    public class BonusAPIServiceV2
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly DepositBonusService _depositBonusService;
        private readonly string _connectionString;
        private readonly Common _common;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly ILogger<BonusAPIService> _logger;
        private readonly BonusWalletService _bonusWalletService;
        private readonly SelLangBLL _selLangBll;
        private readonly CustInfoService _custInfoService;
        private readonly string _apiUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly IEnumerable<AllowIp> _allowIp;

        public BonusAPIServiceV2(IServiceProvider serviceProvider, ILogger<BonusAPIService> logger)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = _configuration.GetConnectionString("ConnectionString");
            _common = serviceProvider.GetRequiredService<Common>();
            _walletService = serviceProvider.GetRequiredService<WalletService>();
            _withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _depositBonusService = serviceProvider.GetRequiredService<DepositBonusService>();
            _bonusWalletService = serviceProvider.GetRequiredService<BonusWalletService>();
            _selLangBll = serviceProvider.GetRequiredService<SelLangBLL>();
            _custInfoService = serviceProvider.GetRequiredService<CustInfoService>();
            _apiUrl = _configuration.GetValue<string>("TpService");
            _courierAdapter = serviceProvider.GetRequiredService<CourierAdapter>();
            _allowIp = serviceProvider.GetRequiredService<IEnumerable<AllowIp>>();
            this._logger = logger;
        }
        public Promotion GetPromotion(int custId, int currencyId, int siteId, string bonusCode, int? deviceType)
        {
            return _reportDbClient.QueryFirst<Promotion>("Member_PromotionV3_Get", new DynamicParameters(new
            {
                CustId = custId,
                CurrencyId = currencyId,
                SiteId = siteId,
                BonusCode = bonusCode,
                DeviceType = deviceType
            }));
        }
        public bool IsBlockIP(Promotion promotion, string bonusCode, int siteId, string custIP)
        {
            if ((promotion.PromotionType == PromotionType.FreeBet || (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit))
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
        public bool CheckProfileFinish(int custId, int siteId) //Profile Finish & Phone Verified
        {
            var custInfoProfile = _custInfoService.GetCustInfoProfile( custId, siteId);
            if (!custInfoProfile.PhoneVerified
                || string.IsNullOrEmpty(custInfoProfile.AccountName)
                || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                || string.IsNullOrEmpty(custInfoProfile.Birth_d))
            {
                return false;
            }
            return true;
        }
        public CustCondition GetCustConditionInfo(int custId, int currencyId, int siteId, string bonusCode, int? deviceType, MySqlConnection conn,
            MySqlTransaction trans)
        {
            var rtn = new CustCondition();
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode, DeviceType = deviceType });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_PromotionCanJoin_Get_V8", p, conn, trans);

            rtn.Result = p.Get<int>("Result");
            rtn.CanJoin = p.Get<byte?>("CanJoin") ?? 0;
            rtn.Amount = p.Get<decimal?>("Amount") ?? 0;
            rtn.RefList = p.Get<string>("RefList");
            rtn.reference = new List<Ref>();
            if (rtn.RefList != null && rtn.RefList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                rtn.reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(rtn.RefList);
            }
           
            return rtn;
        }
        public int BonusReward(BonusRewardInput bonusRewardInput, MySqlConnection conn, MySqlTransaction trans)
        {

            var p = new DynamicParameters(new
            {
                PromotionType = (int)bonusRewardInput.PromotionType,
                BonusCode = bonusRewardInput.BonusCode,
                CustId = bonusRewardInput.CustId,
                UserName = bonusRewardInput.UserName,
                CurrencyId = bonusRewardInput.CurrencyId,
                SiteId = bonusRewardInput.SiteId,
                BonusInfo = bonusRewardInput.BonusInfo,
                LockedAmount = bonusRewardInput.LockedAmount,
                BonusAmount = bonusRewardInput.BonusAmount,
                ExpiryDays = bonusRewardInput.ExpiryDays,
                Memo = bonusRewardInput.Memo,
                Prefix = bonusRewardInput.Prefix,
                RefList = bonusRewardInput.RefList,
                AtPage = (int)bonusRewardInput.AtPage
            });
            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Member_BonusInfo_Ins_V3", p, conn, trans);
            var transId = p.Get<string>("TransId");

            if (result > 0 && bonusRewardInput.LockedAmount > 0)
            {

                result = _walletService.SetBalance(bonusRewardInput.CustId, bonusRewardInput.SiteId, bonusRewardInput.UserName, bonusRewardInput.LockedAmount * -1, (int)bonusRewardInput.FinancialType,
                    bonusRewardInput.SetBalanceMemo, transId,
                    out _, out _, conn, trans);

                if (result > 0)
                {
                    _withdrawalLimitService.SetWithdrawalLimit(bonusRewardInput.CustId, bonusRewardInput.UserName, bonusRewardInput.SiteId, bonusRewardInput.LockedAmount * -1,
                        null, transId, out _, conn, trans);
                }
                else
                {
                    return (int)BonusErrorCode.MAINWALLET_AMOUNT_NOT_ENOUGH;
                }
            }

            if (result > 0)
            {
                var m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out var bonusId);

                result = _bonusWalletService.RewardBonus(bonusId, bonusRewardInput.CategoryId, bonusRewardInput.TpId, bonusRewardInput.CustId,
                    bonusRewardInput.SiteId, bonusRewardInput.UserName, bonusRewardInput.BonusAmount, bonusRewardInput.LockedAmount, bonusRewardInput.RollOver, bonusRewardInput.ExpiryTime, (int)bonusRewardInput.FinancialType,
                    bonusRewardInput.RewardBonusMemo, transId, conn, trans)
                    ? 1
                    : 0;
            }

            if (result > 0 && bonusRewardInput.PromotionType == PromotionType.Cashback)
            {
                var currentTime = DateTime.UtcNow.AddHours(8);
                var calculateDate = currentTime
                    .AddDays(-(int)DateTime.UtcNow.AddHours(8).DayOfWeek + (int)DayOfWeek.Monday)
                    .ToString("yyyy-MM-dd");

                result = _bonusWalletService.RewardCashBack(
                    new CustCashBackInput(bonusRewardInput.CustId, bonusRewardInput.BonusCode, bonusRewardInput.CategoryId, calculateDate),
                    conn, trans);
            }
            return result;
        }
        public int Reward(BonusRewardInput bonusRewardInput, MySqlConnection conn, MySqlTransaction trans)
        {
            int result = (int)BonusErrorCode.UNKNOWERROR;
            switch (bonusRewardInput.PromotionType)
            {
                case PromotionType.FstDeposit:
                case PromotionType.ReDeposit:
                case PromotionType.FreeBet:
                case PromotionType.WalletBonus:
                case PromotionType.Cashback:
                case PromotionType.ManualBonus:
                case PromotionType.RetentionBonus:
                    {

                        result = BonusReward(bonusRewardInput, conn, trans);
                        break;
                    }

                case PromotionType.Rebate:
                    result = _bonusWalletService.RewardRebate(bonusRewardInput.BonusCode, (int)bonusRewardInput.PromotionType - 5,
                        bonusRewardInput.CustId, bonusRewardInput.UserName, bonusRewardInput.SiteId, bonusRewardInput.CurrencyId, conn, trans)
                        ? 1
                        : 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }
        public CalculateBonusOutput CalculateBonus(Promotion promotion, decimal amount, decimal lockedAmount, int custId, int siteId, int currencyId)
        {
            var output = new CalculateBonusOutput();
            output.result = 1;

            decimal dBonus = 0;
            var dLockedAmount = amount;
            decimal dRollOver = 0;

            switch (promotion.PromotionType)
            {
                case PromotionType.FstDeposit:
                case PromotionType.ReDeposit:
                    {
                        if (promotion.PromotionType == PromotionType.ReDeposit ||
                           (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit))
                        {
                            amount = promotion.BonusType == BonusType.Amount ? promotion.RequiredAmount : lockedAmount;

                            if (amount <= 0)
                            {
                                output.result = (int)BonusErrorCode.CALCULATE_AMOUNT_IS_NEGATIVE;
                                return output;
                            }
                            dLockedAmount = amount;
                        }

                        if (amount < promotion.RequiredAmount)
                        {
                            output.result = (int)BonusErrorCode.DEPOSIT_AMOUNT_NOT_ENOUGH;
                            return output;
                        }

                        if (promotion.BonusType == BonusType.Amount)
                        {
                            if (promotion.RequiredAmount == 0)
                            {
                                throw new Exception("Required Amount can't be 0");
                            }

                            dLockedAmount = promotion.RequiredAmount;
                            dBonus = promotion.Bonus;
                        }
                        else
                        {
                            dBonus = Math.Ceiling(promotion.Bonus * amount / 100);
                            if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                            {
                                dBonus = promotion.MaxBonus;
                                dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                            }
                        }

                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        break;
                    }

                case PromotionType.Cashback:
                    var custPromotion = new CustPromotion
                    {
                        BonusCode = promotion.BonusCode,
                        CategoryId = promotion.CategoryId
                    };
                    GetCashbackInfo(custId, siteId, currencyId, custPromotion, true);
                    if (!(custPromotion.canJoin && custPromotion.CashBackDetails?.TotalBonus > promotion.MinPayout))
                    {
                        output.result = (int)BonusErrorCode.CALCULATE_CASHBACK_TOTALBONUS_LESS_THEN_MINPAY;
                        return output;
                    }

                    dBonus = custPromotion.CashBackDetails?.TotalBonus ?? 0;
                    if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                    {
                        dBonus = promotion.MaxBonus;
                    }

                    dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                    dLockedAmount = 0;
                    break;

                case PromotionType.FreeBet:
                    dBonus = promotion.Bonus;
                    dRollOver = dBonus * promotion.RollOver;

                    dLockedAmount = 0;
                    break;

                case PromotionType.WalletBonus when promotion.RequiredAmount == 0:
                    throw new Exception("Required Amount can't be 0");

                case PromotionType.WalletBonus:
                    {
                        amount = _walletService.GetBalance(custId);

                        if (amount < promotion.RequiredAmount)
                        {
                            output.LockedAmount = promotion.RequiredAmount;
                            output.result = (int)BonusErrorCode.DEPOSIT_AMOUNT_NOT_ENOUGH;
                            return output;
                        }

                        if (promotion.BonusType == BonusType.Amount)
                        {
                            dBonus = promotion.Bonus;
                            dLockedAmount = promotion.RequiredAmount;
                        }
                        else
                        {
                            dBonus = Math.Ceiling(promotion.Bonus * amount / 100);
                            if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                            {
                                dBonus = promotion.MaxBonus;
                                dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                            }
                        }
                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        break;
                    }

                case PromotionType.Rebate:
                    return output;

                default:
                    output.result = (int)BonusErrorCode.CALCULATE_UNKNOW_PROMOTION_TYPE;
                    return output;
            }
            promotion.Details = "";
            output.BonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            output.ExpiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
            output.BonusAmount = dBonus;
            output.LockedAmount = dLockedAmount;
            output.RollOver = dRollOver;

            return output;
        }
        public int AfterReward(Promotion promotion, int custId, int siteId, string bonusCode, int currencyId, string custIP, BonusInfoAtPage atPage, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set",
                       new DynamicParameters(new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }), conn, trans);
            if (result > 0)
            {
                if ((promotion.PromotionType == PromotionType.FreeBet || (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit)) 
                    && !string.IsNullOrEmpty(custIP)
                    && !_allowIp.Any(item => item.Type == AllowIpType.Bonus && item.IP == custIP))
                    _mainDbClient.Execute("Service_BlockFreeBet_Ins",
                               new DynamicParameters(new { BonusCode = bonusCode, SiteId = siteId, IP = custIP }), conn, trans);
                //if(atPage != BonusInfoAtPage.Deposit)
                //    CancelAllWaitingBonus(custId, conn, trans);
            }
            return result;
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId, bool useMainDb = false)
        {
            var ConnectionString = _configuration.GetConnectionString("ConnectionStringReport");
            if (useMainDb)
                ConnectionString = _configuration.GetConnectionString("ConnectionString");//避免時間差造成資料不同步
            using MySqlConnection conn = new MySqlConnection(ConnectionString);

            return conn.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }

        //public void CancelAllWaitingBonus(int custId, MySqlConnection conn, MySqlTransaction trans)
        //{//當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉

        //    var BonusList = GetDepositBonusQueueByCustId(custId,true);//改變狀態用mainDb取得資料，避免資料不同步

        //    foreach (var depositBonus in BonusList)
        //    {
        //        _depositBonusService.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
        //    }
        //}
        private static DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek)
        {
            var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }
        private void GetCashbackInfo(int custId, int siteId, int currencyId, CustPromotion promotion, bool getDetail)
        {
            var custInfo = _custInfoService.GetCustInfo(custId);
            var offset = 8;
            if (siteId == (int)SiteId.Goal123 || siteId == (int)SiteId.Bwing)
            {
                offset = 7;
            }
            var currentTime = DateTime.UtcNow.AddHours(offset);
            var calculateDate = StartOfWeek(currentTime, DayOfWeek.Monday)
                .ToString("yyyy-MM-dd");

            var result =
                _reportDbClient.Query<CustCashBackCheckOutput>("Member_CustCashBackCheck_Get",
                    new DynamicParameters(new
                    {
                        CustId = custId,
                        promotion.BonusCode,
                        PromoCategoryId = promotion.CategoryId,
                        CalculateDate = calculateDate
                    })).ToList();

            promotion.isJoin = result.FirstOrDefault()?.Claimed ?? false;
            promotion.canJoin = result.Any();

            var utcMonday = StartOfWeek(currentTime, DayOfWeek.Monday).AddHours(-offset);
            var utcSunday = utcMonday.AddDays(7).AddTicks(-1);
            promotion.PeriodFrom = ((DateTimeOffset)utcMonday).ToUnixTimeSeconds();
            promotion.PeriodTo = ((DateTimeOffset)utcSunday).ToUnixTimeSeconds();

            if (!getDetail) return;
            var lastMonday = StartOfWeek(currentTime.AddDays(-7), DayOfWeek.Monday).Date;
            var lastSunday = lastMonday.AddDays(6);
            var detail = _reportDbClient.Query<CashBackDetail>("Member_CashBackInfo_Get",
                new DynamicParameters(new
                {
                    CustId = custId,
                    promotion.BonusCode,
                    PeriodFrom = lastMonday.ToString("yyyy-MM-dd"),
                    PeriodTo = lastSunday.ToString("yyyy-MM-dd"),
                    PromoCategoryId = promotion.CategoryId
                })).ToList();

            if (detail.Any() && detail.First().TpId == (int)Tp.Saba)
            {
                foreach (var d in detail)
                {
                    d.TpName = d.CategoryId switch
                    {
                        (int)TpCategoryId.Sports => _selLangBll.GetResourceValue("lbl_Cashback_Sports", custInfo.UserLang),
                        (int)TpCategoryId.Casino => _selLangBll.GetResourceValue("lbl_Cashback_RNG", custInfo.UserLang),
                        (int)TpCategoryId.Lottery => _selLangBll.GetResourceValue("lbl_Cashback_Lottery", custInfo.UserLang),
                        (int)TpCategoryId.VirtualSports => _selLangBll.GetResourceValue("lbl_Cashback_Virtual_Sports", custInfo.UserLang),
                        _ => d.TpName
                    };
                }
            }

            var details = _reportDbClient.Query<CashBackDetails>("Member_CustCashBack_Get",
                new DynamicParameters(new
                {
                    CustId = custId,
                    promotion.BonusCode,
                    PromoCategoryId = promotion.CategoryId,
                    CalculateDate = calculateDate,
                    CurrencyId = currencyId,
                    SiteId = siteId
                })).ToList();

            if (!details.Any()) return;
            details.First().CashBackDetail = detail;
            promotion.CashBackDetails = details.First();
        }
        public int SetClaimPromotionV2(int custId, string userName, int siteId, string bonusCode, int currencyId, string custIP, int? deviceType, BonusInfoAtPage atPage, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion, string DepositTransId = null)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            promotion = GetPromotion(custId, currencyId, siteId, bonusCode, deviceType);

            if (promotion == null)
                return (int)BonusErrorCode.PROMOTION_NOT_FOUND;

            if (IsBlockIP(promotion, bonusCode, siteId, custIP))
                return (int)BonusErrorCode.IP_IS_BLOCKED;
            if (!CheckProfileFinish(custId, siteId))//Profile Finish & Phone Verified
                return (int)BonusErrorCode.PROFILE_OR_PHONE_NOT_FINISH;

            var custConditionInfo = GetCustConditionInfo(custId, currencyId, siteId, bonusCode, deviceType, conn, trans);

            if (custConditionInfo.CanJoin == 1)
            {
                var CalculateBonus = this.CalculateBonus(promotion, custConditionInfo.Amount, lockedAmount, custId, siteId, currencyId);
                if (CalculateBonus.result <= 0)
                    return CalculateBonus.result;

                bonusAmount = CalculateBonus.BonusAmount;
                rollOver = CalculateBonus.RollOver;
                expiryTime = CalculateBonus.ExpiryTime;
                
                if (atPage == BonusInfoAtPage.Deposit) //由Deposit頁面進來的 reference紀錄指定TransId、Amount
                {
                    custConditionInfo.reference = new List<Ref>
                    {
                        new Ref
                        {
                            DepositId = DepositTransId,
                            Amount = lockedAmount
                        }
                    };
                }

                var result = (int)BonusErrorCode.UNKNOWERROR;
                var bonusRewardInput = new BonusRewardInput
                {
                    PromotionType = promotion.PromotionType,
                    BonusCode = bonusCode,
                    CategoryId = promotion.CategoryId,
                    TpId = promotion.TpId,
                    BonusInfo = CalculateBonus.BonusInfo,
                    BonusAmount = CalculateBonus.BonusAmount,
                    LockedAmount = CalculateBonus.LockedAmount,
                    RollOver = CalculateBonus.RollOver,
                    ExpiryTime = expiryTime,
                    ExpiryDays = promotion.ExpiryDays,
                    CustId = custId,
                    UserName = userName,
                    CurrencyId = currencyId,
                    SiteId = siteId,
                    Memo = (string)null,
                    Prefix = $"B{_common.GetEnv()}",
                    RefList = custConditionInfo.reference.Any() ? JsonConvert.SerializeObject(custConditionInfo.reference) : null,
                    SetBalanceMemo = $"Claim {Enum.GetName(typeof(PromotionType), (PromotionType)promotion.PromotionType)} BonusCode: {bonusCode}, Locked Amount: {CalculateBonus.LockedAmount:#,##0.##}",
                    RewardBonusMemo = $"Claim {Enum.GetName(typeof(PromotionType), (PromotionType)promotion.PromotionType)} BonusCode: {bonusCode}",
                    FinancialType = FinancialType.Bonusreward,
                    AtPage = atPage
                };
                result = Reward(bonusRewardInput, conn, trans);

                if (result > 0)
                {
                    if (AfterReward(promotion, custId, siteId, bonusCode, currencyId, custIP, atPage, conn, trans) > 0)
                        trans.Commit();
                }
                return result;
            }else
            {
                return custConditionInfo.Result;
            }

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
        public CalculateBonusOutput CalculateAdjustmentBonus(Promotion promotion, decimal amount, decimal lockedAmount, int custId, int siteId, int currencyId)
        {
            var output = new CalculateBonusOutput();
            output.result = (int)BonusErrorCode.UNKNOWERROR;

            decimal dBonus = 0;
            var dLockedAmount = 0;
            decimal dRollOver = 0;

            switch (promotion.PromotionType)
            {
                case PromotionType.RetentionBonus:
                    {
                        dBonus = promotion.Bonus;
                        dRollOver = dBonus * promotion.RollOver;
                        output.result = (int)BonusErrorCode.SUCCESS;
                    }
                    break;
                case PromotionType.ManualBonus:
                    {
                        dBonus = amount;
                        dRollOver = amount * promotion.RollOver;
                        output.result = (int)BonusErrorCode.SUCCESS;
                    }
                    break;
                default:
                    output.result = (int)BonusErrorCode.CALCULATE_UNKNOW_PROMOTION_TYPE;
                    return output;
            }

            output.BonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            output.ExpiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
            output.BonusAmount = dBonus;
            output.LockedAmount = dLockedAmount;
            output.RollOver = dRollOver;

            return output;
        }
        public int SetAdjustmentBonusV2(DepositBonus depositBonus, decimal amount, int promotionType, string memo, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var parameter = new DynamicParameters(new
            {
                depositBonus.CustId,
                depositBonus.CurrencyId,
                depositBonus.SiteId,
                depositBonus.BonusCode,
                depositBonus.DeviceType
            });

            promotion = _depositBonusService.GetMemberPromotion(parameter);

            if (promotion == null)
            {
                return (int)BonusErrorCode.PROMOTION_NOT_FOUND;
            }
            else if (promotion.PromotionType != (PromotionType)promotionType)
            {
                return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTIONTYPE_NOT_MATCH;
            }
            if (promotion.PromotionType == PromotionType.ManualBonus )
            {
                if (promotion.PromotionAction != 1)
                    return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_NOT_ACTIVE;
                else if(amount > promotion.MaxBonus)
                    return (int)BonusErrorCode.ADJUSTMENT_BONUS_PROMOTION_AMOUNT_OVER_MAXBONUS;
            }

            if (!CheckProfileFinish(depositBonus.CustId, depositBonus.SiteId))//Profile Finish & Phone Verified
                return (int)BonusErrorCode.PROFILE_OR_PHONE_NOT_FINISH;

            var runningBonusList = GetCustRunningBonus(depositBonus.CustId)
;
            if (runningBonusList.Any())
            {
                return (int)BonusErrorCode.ADJUSTMENT_BONUS_CUSTOMER_HAS_RUNNING_BONUS;
            }


            var CalculateBonus = this.CalculateAdjustmentBonus(promotion, amount, lockedAmount, depositBonus.CustId, depositBonus.SiteId, depositBonus.CurrencyId);
            if (CalculateBonus.result <= 0)
                return CalculateBonus.result;

            bonusAmount = CalculateBonus.BonusAmount;
            rollOver = CalculateBonus.RollOver;
            expiryTime = CalculateBonus.ExpiryTime;

            var result = (int)BonusErrorCode.UNKNOWERROR;
            var bonusRewardInput = new BonusRewardInput
            {
                PromotionType = promotion.PromotionType,
                BonusCode = depositBonus.BonusCode,
                CategoryId = promotion.CategoryId,
                TpId = promotion.TpId,
                BonusInfo = CalculateBonus.BonusInfo,
                BonusAmount = CalculateBonus.BonusAmount,
                LockedAmount = CalculateBonus.LockedAmount,
                RollOver = CalculateBonus.RollOver,
                ExpiryTime = expiryTime,
                ExpiryDays = promotion.ExpiryDays,
                CustId = depositBonus.CustId,
                UserName = depositBonus.UserName,
                CurrencyId = depositBonus.CurrencyId,
                SiteId = depositBonus.SiteId,
                Memo = memo,
                Prefix = $"B{_common.GetEnv()}",
                RefList =  null,
                SetBalanceMemo = $"Claim {Enum.GetName(typeof(PromotionType), (PromotionType)promotion.PromotionType)} BonusCode: {depositBonus.BonusCode}, Locked Amount: {CalculateBonus.LockedAmount:#,##0.##}",
                RewardBonusMemo = $"Claim {Enum.GetName(typeof(PromotionType), (PromotionType)promotion.PromotionType)} BonusCode: {depositBonus.BonusCode}",
                FinancialType = FinancialType.Bonusreward,
                AtPage = BonusInfoAtPage.Promotion
            };
            result = Reward(bonusRewardInput, conn, trans);

            if (result > 0)
            {
                if (AfterReward(promotion, depositBonus.CustId, depositBonus.SiteId, depositBonus.BonusCode, depositBonus.CurrencyId, null, 0, conn, trans) > 0)
                    trans.Commit();
            }
           
            return result;

        }
    }
}
