using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
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

namespace BonusService.Service.DAL
{
    public class BonusAPIService
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
        private readonly Log2File _log2File;

        public BonusAPIService(IServiceProvider serviceProvider, ILogger<BonusAPIService> logger)
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
            _log2File = serviceProvider.GetRequiredService<Log2File>();
            this._logger = logger;


        }

        public int SetAdjustmentBonus(DepositBonus depositBonus, string Operator, string memo, out string Adjustment_transId, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion, out int errorCode, out string errorMessage)
        {
            Adjustment_transId = null;
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;
            errorCode = 9000;
            errorMessage = "";
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
                errorCode = 201;
                errorMessage = $"BonusCode:{depositBonus.BonusCode} not found";
                _logger.LogError($"BonusCode:{depositBonus.BonusCode} not found  parameter:{ Newtonsoft.Json.JsonConvert.SerializeObject(parameter)}");
                return 0;
            }else if(promotion.PromotionType != PromotionType.RetentionBonus)
            {
                errorCode = 202;
                errorMessage = $"BonusCode:{depositBonus.BonusCode} is not Retention Bonus";
                _logger.LogError($"BonusCode:{depositBonus.BonusCode} is not Retention Bonus  parameter:{ Newtonsoft.Json.JsonConvert.SerializeObject(parameter)}");
                return 0;
            }
            var pCustRunningBonus = new DynamicParameters(new
            {
                CustId = depositBonus.CustId
            });
            
            var runningBonusList = _mainDbClient.Query<CustRunningBonus>("Service_CustRunningBonus_Get", new DynamicParameters(new { CustId = depositBonus.CustId }));
            if (runningBonusList.Any())
            {
                errorCode = 203;
                errorMessage = $"CustId {depositBonus.CustId} has other running bonus";
                _logger.LogError($"CustId {depositBonus.CustId} has other running bonus  BonusList:{ Newtonsoft.Json.JsonConvert.SerializeObject(runningBonusList)}");
                return 0;
            }


            var dLockedAmount = 0;
            IEnumerable<Ref> reference = new List<Ref>();

            decimal dBonus = promotion.Bonus;
            decimal dRollOver = dBonus * promotion.RollOver;
            var sBonusInfo = JsonConvert.SerializeObject(promotion,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            bonusAmount = dBonus;
            lockedAmount = 0;
            rollOver = dRollOver;
            expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();


            var result = 0;

            var p = new DynamicParameters(new
            {
                PromotionType = (int)promotion.PromotionType,
                BonusCode = depositBonus.BonusCode,
                CustId = depositBonus.CustId,
                UserName = depositBonus.UserName,
                CurrencyId = depositBonus.CurrencyId,
                SiteId = depositBonus.SiteId,
                BonusInfo = sBonusInfo,
                LockedAmount = dLockedAmount,
                BonusAmount = dBonus,
                promotion.ExpiryDays,
                Memo = (string)null,
                Prefix = $"B{_common.GetEnv()}",
                RefList = reference.Any() ? JsonConvert.SerializeObject(reference.FirstOrDefault()) : null
            });
            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Member_BonusInfo_InsV2", p, conn, trans);
            var transId = p.Get<string>("TransId");

            if (result > 0 && dLockedAmount > 0)
            {
                var type = 5 /*Bonus*/;
                result = _walletService.SetBalance(depositBonus.CustId, depositBonus.SiteId, depositBonus.UserName, dLockedAmount * -1, type,
                    $"Claim Retention Bonus BonusCode: {depositBonus.BonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
                    out _, out _, conn, trans);

                if (result > 0)
                {
                    _withdrawalLimitService.SetWithdrawalLimit(depositBonus.CustId, depositBonus.UserName, depositBonus.SiteId, dLockedAmount * -1,
                        null, transId, out _, conn, trans);
                }
                else
                {
                    return -2;
                }
            }

            if (result > 0)
            {
                var m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out var bonusId);

                result = _bonusWalletService.RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, depositBonus.CustId,
                    depositBonus.SiteId, depositBonus.UserName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                    $"Payment Claim Retention BonusCode: {depositBonus.BonusCode} ", transId, conn, trans)
                    ? 1
                    : 0;
            }

            if (result > 0)
            {
                p = new DynamicParameters(new
                {
                    SiteId = depositBonus.SiteId,
                    BonusCode = depositBonus.BonusCode,
                    CurrencyId = depositBonus.CurrencyId
                });

                result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set", p, conn, trans);
            }
            if (result > 0)
            { 
                result = PaymentAdjustment(depositBonus, bonusAmount, sBonusInfo, Operator, memo, conn, trans, out Adjustment_transId);
            }
            if (result > 0)
            {
                trans.Commit();
                errorCode = 0;
                errorMessage = "";
            }

            return result;

        }
        
        public int PaymentAdjustment(DepositBonus depositBonus, decimal bonusAmount, string sBonusInfo, string Operator, string memo, MySqlConnection conn, MySqlTransaction trans, out string transId)
        {

            var Adjustment_p = new DynamicParameters(
                new {
                    AdjustType = 5,//Retention Bonus
                    CustId = depositBonus.CustId,
                    UserName = depositBonus.UserName,
                    CurrencyId = depositBonus.CurrencyId,
                    SiteId = depositBonus.SiteId,
                    AdjustIdInfo = sBonusInfo,
                    Amount = bonusAmount,
                    Prefix = $"A{_common.GetEnv()}",
                    Memo = memo
                }); 
            Adjustment_p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = conn.Execute("Admin_Adjustment_Ins", Adjustment_p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = Adjustment_p.Get<string>("TransId");
            if (result > 0)
            {
                var forStatus = new List<int>();
                forStatus.Add((int)AdjustStatus.Pending);
                if (SetAdjustmentInfo(transId, depositBonus.SiteId, $"Payment Claim Retention BonusCode: {depositBonus.BonusCode}  BonusAmount:{bonusAmount}", Operator, forStatus, AdjustStatus.Approved, conn, trans) > 0)
                {
                    transId = Adjustment_p.Get<string>("TransId");
                }
                else
                {
                    result = 0;
                }
            }

            return result;
        }

        public int SetAdjustmentInfo(string transId, int siteId, string comment, string Operator, IEnumerable<int> forStatus, AdjustStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            int adjustId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out adjustId);

            var result = 0;
            try
            {
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(
                    new { 
                        AdjustId = adjustId, 
                        SiteId = siteId, 
                        RefId = (string)null, 
                        Comment = comment, 
                        ForStatus = sForStatus, 
                        Status = (int)status, 
                        ThirdId = (string)null 
                    });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                result = conn.Execute("Adjustment_Set", p, trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    SetAdjustmentLog(adjustId, siteId, comment, (int)status, Operator);
                    if (result > 0)
                    {
                        result = p.Get<int?>("CustId") ?? result;
                    }
                }
            }
            catch (Exception ex)
            {
                SetAdjustmentLog(adjustId, siteId, ex.Message, (int)AdjustStatus.Error, Operator);
            }

            return result;
        }
        private int SetAdjustmentLog(int adjustId, int siteId, string comment, int status, string Operator)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("AdjustmentLog_Ins", new { AdjustId = adjustId, SiteId = siteId, Comment = comment, Action = status, Operator = Operator }, commandType: CommandType.StoredProcedure);
        }
        public int SetClaimPromotion(int custId, string userName, int siteId, string bonusCode, int currencyId, string custIP, int? deviceType, out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            promotion = _reportDbClient.QueryFirst<Promotion>("Member_PromotionV3_Get", new DynamicParameters(new
            {
                CustId = custId,
                CurrencyId = currencyId,
                SiteId = siteId,
                BonusCode = bonusCode,
                DeviceType = deviceType
            }));
            if (promotion == null)
            {
                return 0;
            }

            if (promotion.PromotionType == PromotionType.FreeBet)
            {
                var p1 = new DynamicParameters(new { BonusCode = bonusCode, SiteId = siteId, IP = custIP });
                p1.Add("IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
                _reportDbClient.Execute("Service_BlockFreeBet_Get", p1);
                if (p1.Get<int>("IsBlocked") == 1)
                {
                    return -3;
                }
            }

            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode, DeviceType = deviceType });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_PromotionCanJoinV6_Get", p, conn, trans);

            var jResult = p.Get<int>("Result");
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            var amount = p.Get<decimal?>("Amount") ?? 0;
            var refList = p.Get<string>("RefList");
            IEnumerable<Ref> reference = new List<Ref>();
            if (refList != null && refList.Contains("DepositId", StringComparison.OrdinalIgnoreCase))
            {
                reference = JsonConvert.DeserializeObject<IEnumerable<Ref>>(refList);
            }
            {
                string tmpstring = $"PromotionType:{promotion.PromotionType} jResult:{jResult} canJoin:{canJoin} amount:{amount} refList:{refList} reference:{reference}";
                _logger.LogError(tmpstring);
                //_log2File.Error(tmpstring, "BonusAPIServiceV2_test");
            }
            if (canJoin == 1)
            {
                decimal dBonus = 0;
                if (promotion.PromotionType == PromotionType.ReDeposit ||
                    (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit))
                {
                    amount = promotion.BonusType == BonusType.Amount ? promotion.RequiredAmount : lockedAmount;

                    if (amount <= 0)
                    {
                        return 0;
                    }
                }

                var dLockedAmount = amount;
                decimal dRollOver = 0;
                var sBonusInfo = "";

                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                        {
                            if (amount < promotion.RequiredAmount)
                            {
                                return -4;
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
                            sBonusInfo = JsonConvert.SerializeObject(promotion,
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                            bonusAmount = dBonus;
                            lockedAmount = dLockedAmount;
                            rollOver = dRollOver;
                            expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
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
                            return 0;
                        }

                        dBonus = custPromotion.CashBackDetails?.TotalBonus ?? 0;
                        if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                        {
                            dBonus = promotion.MaxBonus;
                        }

                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        bonusAmount = dBonus;
                        lockedAmount = 0;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        break;

                    case PromotionType.FreeBet:
                        dBonus = promotion.Bonus;
                        dRollOver = dBonus * promotion.RollOver;
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        bonusAmount = dBonus;
                        lockedAmount = 0;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        _mainDbClient.Execute("Service_BlockFreeBet_Ins",
                            new DynamicParameters(new { BonusCode = bonusCode, SiteId = siteId, IP = custIP }), conn,
                            trans);
                        break;

                    case PromotionType.WalletBonus when promotion.RequiredAmount == 0:
                        throw new Exception("Required Amount can't be 0");

                    case PromotionType.WalletBonus:
                        {
                            amount = _walletService.GetBalance(custId);

                            if (amount < promotion.RequiredAmount)
                            {
                                lockedAmount = promotion.RequiredAmount;
                                return -2;
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

                            bonusAmount = dBonus;
                            lockedAmount = dLockedAmount;
                            dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                            sBonusInfo = JsonConvert.SerializeObject(promotion,
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                            rollOver = dRollOver;
                            expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                            break;
                        }

                    case PromotionType.Rebate:
                        break;

                    default:
                        return 0;
                }
               var pdebug = new
                {
                    PromotionType = (int)promotion.PromotionType,
                    BonusCode = bonusCode,
                    CustId = custId,
                    UserName = userName,
                    CurrencyId = currencyId,
                    SiteId = siteId,
                    BonusInfo = sBonusInfo,
                    LockedAmount = dLockedAmount,
                    BonusAmount = dBonus,
                    promotion.ExpiryDays,
                    Memo = (string)null,
                    Prefix = $"B{_common.GetEnv()}",
                    //RefList = reference.Any() ? JsonConvert.SerializeObject(reference) : null
                }; 
                {
                    string tmpstring = $"param;{Newtonsoft.Json.JsonConvert.SerializeObject(pdebug)}";
                    _logger.LogError(tmpstring);
                   // _log2File.Error(tmpstring, "BonusAPIServiceV2_test");
                }
                return 0;
                var result = 0;
                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                    case PromotionType.FreeBet:
                    case PromotionType.WalletBonus:
                    case PromotionType.Cashback:
                        {
                            p = new DynamicParameters(new
                            {
                                PromotionType = (int)promotion.PromotionType,
                                BonusCode = bonusCode,
                                CustId = custId,
                                UserName = userName,
                                CurrencyId = currencyId,
                                SiteId = siteId,
                                BonusInfo = sBonusInfo,
                                LockedAmount = dLockedAmount,
                                BonusAmount = dBonus,
                                promotion.ExpiryDays,
                                Memo = (string)null,
                                Prefix = $"B{_common.GetEnv()}",
                                RefList = reference.Any() ? JsonConvert.SerializeObject(reference) : null
                            });
                            p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                            result = _mainDbClient.Execute("Member_BonusInfo_InsV2", p, conn, trans);
                            var transId = p.Get<string>("TransId");

                            if (result > 0 && dLockedAmount > 0)
                            {
                                var type = 5 /*Bonus*/;
                                result = _walletService.SetBalance(custId, siteId, userName, dLockedAmount * -1, type,
                                    $"Claim Promotion BonusCode: {bonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
                                    out _, out _, conn, trans);

                                if (result > 0)
                                {
                                    _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, dLockedAmount * -1,
                                        null, transId, out _, conn, trans);
                                }
                                else
                                {
                                    return -2;
                                }
                            }

                            if (result > 0)
                            {
                                var m = Regex.Match(transId, @"\d+");
                                int.TryParse(m.Value, out var bonusId);

                                result = _bonusWalletService.RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, custId,
                                    siteId, userName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                                    $"Claim Promotion BonusCode: {bonusCode}", transId, conn, trans)
                                    ? 1
                                    : 0;
                            }

                            if (result > 0 && promotion.PromotionType == PromotionType.Cashback)
                            {
                                var currentTime = DateTime.UtcNow.AddHours(8);
                                var calculateDate = currentTime
                                    .AddDays(-(int)DateTime.UtcNow.AddHours(8).DayOfWeek + (int)DayOfWeek.Monday)
                                    .ToString("yyyy-MM-dd");

                                result = _bonusWalletService.RewardCashBack(
                                    new CustCashBackInput(custId, promotion.BonusCode, promotion.CategoryId, calculateDate),
                                    conn, trans);
                            }

                            break;
                        }

                    case PromotionType.Rebate:
                        result = _bonusWalletService.RewardRebate(promotion.BonusCode, (int)promotion.PromotionType - 5,
                            custId, userName, siteId, currencyId, conn, trans)
                            ? 1
                            : 0;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (result > 0)
                {
                    result = _mainDbClient.Execute("Member_PromotionBeenUsedV2_Set",
                        new DynamicParameters(new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }), conn,
                        trans);
                }

                if (result > 0)
                {
                    CancelAllWaitingBonus(custId, conn, trans);
                    trans.Commit();
                }

                return result;
            }

            if (jResult == 3)
            {
                return -5;
            }

            return 0;

        }
        public void CancelAllWaitingBonus(int custId, MySqlConnection conn, MySqlTransaction trans)
        {//當領取BONUS成功   將客戶DepositBonusQueue中的BONUS CANCEL掉

            var BonusList = _mainDbClient.Query<DepositBonus>("Service_DepositBonusQueueByCustId_Get", new DynamicParameters(new { CustId = custId }));//改變狀態用mainDb取得資料，避免資料不同步

            foreach (var depositBonus in BonusList)
            {
                {
                    var p = new DynamicParameters(new { TransId = depositBonus.TransId, Status = (int)DepositBonusQueueStatus.Cancel_other_Bonus });
                    _mainDbClient.Execute("Service_DepositBonusStatus_Set", p, conn, trans);
                }
            }
        }
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
    }
}
