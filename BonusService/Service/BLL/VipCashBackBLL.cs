using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using System.Linq;
using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class VipCashBackBLL
    {
        private readonly VipCashBackService _vipCashBackCashBackService;
        private readonly ICommonService _commonService;
        private readonly ILogger<VipCashBackBLL> _logger;
        private readonly PromotionBll _promotionBll;
        private Dictionary<int, TimeSpan> _currencyTimezoneMap = new();

        public VipCashBackBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<VipCashBackBLL>>();
            _vipCashBackCashBackService = provider.GetRequiredService<VipCashBackService>();
            _commonService = provider.GetService<ICommonService>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            InitCurrencyTimezoneMap();
        }

        private IEnumerable<CashBackSettingOutput> GetCashBackSettings(VipPeriodType periodType)
        {
            return _vipCashBackCashBackService.GetCashBackSettings(periodType);
        }

        private bool SetCashBackSettings(CashBackSettingInput cashBackSettingInput)
        {
            return _vipCashBackCashBackService.SetCashBackSettings(cashBackSettingInput);
        }

        private IEnumerable<CashBackPromotionOutput> GetVipCashBackPromotion(CashBackPromotionInput cashBackPromotionInput)
        {
            return _vipCashBackCashBackService.GetVipCashBackPromotion(cashBackPromotionInput);
        }

        public IEnumerable<PromotionCashBackSettingOutput> GetPromotionCashBackSetting(PromotionCashBackSettingInput promotionCashBackSettingInput)
        {
            return _vipCashBackCashBackService.GetPromotionCashBackSetting(promotionCashBackSettingInput);
        }

        private IEnumerable<CashBackTpBetRecordOutput> GetVipCashBackTpBetRecord(CashBackTpBetRecordInput cashBackTpBetRecordInput)
        {
            return _vipCashBackCashBackService.GetVipCashBackTpBetRecord(cashBackTpBetRecordInput);
        }

        private IEnumerable<CustomerVIPInfoOutput> GetCustomerVipInfo(string custIds)
        {
            return _vipCashBackCashBackService.GetCustomerVipInfo(custIds);
        }

        private bool SetCashBackInfo(CashBackInfoInput cashBackInfoInput)
        {
            return _vipCashBackCashBackService.SetCashBackInfo(cashBackInfoInput);
        }

        private bool SetCustCashBack(CustCashBackInput custCashBackInput)
        {
            return _vipCashBackCashBackService.SetCustCashBack(custCashBackInput);
        }

        private IEnumerable<DepositNotUseBonusDto> GetDepositNotUseBonus(int currencyId, int custId,
            string startDate,
            string endDate)
        {
            return _vipCashBackCashBackService.GetDepositNotUseBonusList(currencyId, custId, startDate, endDate);
        }

        public bool CalculateCashBack(VipPeriodType periodType)
        {
            try
            {
                //使用 logKey 組出來後可以用 logKey 看整個執行過程
                var logKey = string.Empty;

                // 取得時間設定
                var cashBackSettings = GetCashBackSettings(periodType);
                
                foreach (var cashBackSetting in cashBackSettings)
                {
                    // 依據幣別偏移時區，判斷是否要執行計算 CashBack
                    var checkTimeResult = CheckSettleTime(cashBackSetting);
                    if (!checkTimeResult) { continue; }

                    #region log Step1

                    logKey = $"CashBackLogKey_{cashBackSetting.SiteId}_{cashBackSetting.CurrencyId}_{periodType}";
                    _logger.LogInformation(@$"Step1 CashBackLog SiteId：{cashBackSetting.SiteId}, CurrencyId：{cashBackSetting.CurrencyId}, 
                                              PeriodType:{periodType}, SettleTime：{cashBackSetting.SettleTime}, UtcNow：{DateTime.UtcNow} logKey:{logKey}");

                    #endregion

                    // 取得起始日 & 結束日
                    var dateRange = GetDateRangeByPeriodType(periodType, cashBackSetting.CurrencyId);

                    #region log Step2

                    _logger.LogInformation(@$"Step2 TpBetRecordStartDate：{dateRange.TpBetRecordStartDate},
                                                    TpBetRecordEndDate：{dateRange.TpBetRecordEndDate},
                                                    DepositStartDate：{dateRange.DepositStartDate},
                                                    DepositEndDate：{dateRange.DepositEndDate},
                                                    logKey:{logKey}");

                    #endregion

                    // By Site + 幣別 + PeriodType 取得 CashBackPromotion，(日返、周返)
                    var promotions = GetVipCashBackPromotion(new CashBackPromotionInput
                    {
                        SiteId = cashBackSetting.SiteId,
                        PromotionType = (int)PromotionType.Cashback,
                        CurrencyId = cashBackSetting.CurrencyId,
                        PeriodType = cashBackSetting.PeriodType
                    });

                    #region log Step3
                    _logger.LogInformation(@$"Step3 promotions Result：{JsonConvert.SerializeObject(promotions)} ,logKey:{logKey}");
                    #endregion

                    // 判斷是否指定 BonusCode 重跑
                    if (!string.IsNullOrWhiteSpace(cashBackSetting.BonusCode))
                    {
                        promotions = promotions.Where(r => r.BonusCode == cashBackSetting.BonusCode);
                    }

                    foreach (var promotion in promotions)
                    {
                        var settings = GetPromotionCashBackSetting(new PromotionCashBackSettingInput
                        {
                            BonusCode = promotion.BonusCode,
                            SiteId = promotion.SiteId,
                            CurrencyId = promotion.CurrencyId
                        }).ToList();

                        if (!settings.Any()) { continue; }

                        #region log Step4
                        _logger.LogInformation(@$"Step4 settings Result：{JsonConvert.SerializeObject(settings)} ,logKey:{logKey}");
                        #endregion

                        // 取得注單
                        var cashBackTpBetRecordInput = GetCashBackTpBetRecordInput(dateRange, promotion, settings, periodType);
                        var customersTpBetRecord = GetVipCashBackTpBetRecord(cashBackTpBetRecordInput).ToList();

                        #region log Step5

                        _logger.LogInformation(@$"Step5 cashBackTpBetRecordInput Result：{JsonConvert.SerializeObject(cashBackTpBetRecordInput)}
                                                        customersTpBetRecord Result {JsonConvert.SerializeObject(customersTpBetRecord)}
                                                        ,logKey:{logKey}");

                        #endregion

                        // 取得每位玩家Vip等級
                        var custIds = customersTpBetRecord.Select(c => c.CustId).Distinct();
                        var vipInfos = GetCustomerVipInfo($"[{string.Join(",", custIds)}]").ToList();

                        #region log Step6

                        _logger.LogInformation(@$"Step6 custIds Result：{JsonConvert.SerializeObject(custIds)}
                                                        vipInfos Result {JsonConvert.SerializeObject(vipInfos)}
                                                        ,logKey:{logKey}");
                        #endregion

                        foreach (var customerGroupByCustId in customersTpBetRecord.GroupBy(output => output.CustId))
                        {
                            var custVipLevel = GetCustVipLevel(vipInfos, customerGroupByCustId);
                            var setting = FilterPromotionCashBackSetting(settings, custVipLevel, customerGroupByCustId);
                            var percentage = setting?.Percentage ?? 0;

                            if (percentage == 0) { continue; }

                            foreach (var customer in customerGroupByCustId)
                            {
                                SetCashBackInfo(new CashBackInfoInput
                                {
                                    BonusCode = promotion.BonusCode,
                                    PromoCategoryId = promotion.CategoryId,
                                    CategoryId = customer.CategoryId,
                                    TpId = customer.TpId,
                                    CustId = customer.CustId,
                                    PeriodFrom = dateRange.TpBetRecordStartDate,
                                    PeriodTo = dateRange.TpBetRecordEndDate,
                                    VipLevel = custVipLevel,
                                    CurrencyId = promotion.CurrencyId,
                                    SiteId = promotion.SiteId,
                                    Percentage = percentage,
                                    WinlostAmount = customer.WinlostAmount,
                                    WinlostAmountWithBonus = customer.WinlostAmountWithBonus,
                                    BonusAmount = percentage / 100 * Math.Abs(customer.WinlostAmount)
                                });
                            }

                            // 判斷玩家輸贏值
                            var winLostAmount = customerGroupByCustId.Sum(i => i.WinlostAmount);
                            if (winLostAmount >= 0 || Math.Abs(winLostAmount) < setting?.LossAmount)
                            {
                                #region log Step7

                                _logger.LogInformation(@$"Step7 custId: {customerGroupByCustId.First().CustId} winLostAmountNotEnough,
                                                            winLostAmount Result：{winLostAmount},
                                                           ,logKey:{logKey}");
                                #endregion
                                continue;
                            }

                            // 檢查玩家存款金額
                            if (!VerifyCustDepositAmountCondition(customerGroupByCustId.First().CustId, dateRange, promotion, logKey))
                            {
                                continue;
                            }

                            var bonus = percentage / 100 * Math.Abs(winLostAmount);
                            if (promotion.MaxBonus > 0 && bonus > promotion.MaxBonus)
                            {
                                bonus = promotion.MaxBonus;
                            }

                            var calculateDate = GetCalculateDate(periodType, cashBackSetting.CurrencyId);
                            SetCustCashBack(new CustCashBackInput(
                                custId: customerGroupByCustId.First().CustId,
                                bonusCode: promotion.BonusCode,
                                categoryId: customerGroupByCustId.First().CategoryId,
                                promoCategoryId: promotion.CategoryId,
                                calculateDate: calculateDate,
                                bonusAmount: bonus));
                        }
                    }


                    var settleTime = GetNextSettleDate(periodType, cashBackSetting.CurrencyId);
                    SetCashBackSettings(new CashBackSettingInput
                    {
                        SiteId = cashBackSetting.SiteId,
                        PeriodType = periodType,
                        CurrencyId = cashBackSetting.CurrencyId,
                        SettleTime = settleTime // 更新成下一次要跑的時間
                    });

                    #region log

                    _logger.LogInformation(@$"StepEnd，NextSettleTime：{settleTime} logKey:{logKey}");

                    #endregion
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
                return false;
            }
        }

        #region private logic method

        /// <summary>
        /// 過濾 PromotionCashBackSetting
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="custVipLevel"></param>
        /// <param name="customerGroupByCustId"></param>
        /// <returns></returns>
        private static PromotionCashBackSettingOutput FilterPromotionCashBackSetting(List<PromotionCashBackSettingOutput> settings,
            int custVipLevel,
            IGrouping<int, CashBackTpBetRecordOutput> customerGroupByCustId)
        {
            return settings.Where(item =>
                    (item.Level == 0 || item.Level == custVipLevel) &&
                    item.TpId == customerGroupByCustId.First().TpId &&
                    item.CategoryId == customerGroupByCustId.First().CategoryId)
                .MaxBy(i => i.LossAmount);
        }

        /// <summary>
        /// 取得玩家 Vip等級
        /// </summary>
        /// <param name="vipInfos"></param>
        /// <param name="customerGroupByCustId"></param>
        /// <returns></returns>
        private static int GetCustVipLevel(List<CustomerVIPInfoOutput> vipInfos,
            IGrouping<int, CashBackTpBetRecordOutput> customerGroupByCustId)
        {
            return vipInfos
                .FirstOrDefault(item => item.CustId == customerGroupByCustId.First().CustId)
                ?.VipLevel ?? 1;
        }

        /// <summary>
        /// 組合取得 TpBetRecord 查詢條件
        /// </summary>
        /// <param name="dateRange"></param>
        /// <param name="promotion"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static CashBackTpBetRecordInput GetCashBackTpBetRecordInput(CashBackDateRange dateRange,
            CashBackPromotionOutput promotion,
            List<PromotionCashBackSettingOutput> settings,
            VipPeriodType periodType)
        {
            var cashBackTpBetRecordInput = new CashBackTpBetRecordInput
            {
                StartDate = dateRange.TpBetRecordStartDate,
                EndDate = dateRange.TpBetRecordEndDate,
                CurrencyId = promotion.CurrencyId,
                SiteId = promotion.SiteId,
                TpIds = $"[{string.Join(",", settings.Select(s => s.TpId).Distinct())}]",
                CategoryIds = $"[{string.Join(",", settings.Select(s => s.CategoryId).Distinct())}]",
                PeriodType = periodType
            };
            return cashBackTpBetRecordInput;
        }

        /// <summary>
        /// 檢查是否要執行 CashBack 
        /// </summary>
        /// <param name="cashBackSetting"></param>
        /// <returns></returns>
        public bool CheckSettleTime(CashBackSettingOutput cashBackSetting)
        {
            var currentTime = DateTimeOffset.UtcNow.DateTime;
            var settleTime = cashBackSetting.SettleTime;

            return settleTime < currentTime;
        }

        /// <summary>
        /// 初始化從 DB 取得各幣別時區
        /// </summary>
        private void InitCurrencyTimezoneMap()
        {
            var currencyTimeZoneFromDb = _commonService.GetCurrencyTimeZones();

            foreach (var data in currencyTimeZoneFromDb)
            {
                var offSet = TryParseTimezoneOffset(data.TimeZone);
                _currencyTimezoneMap.Add(data.CurrencyId, offSet);
            }
        }

        /// <summary>
        /// 將 DB 取得的時區 Parse 成 TimeSpan offset
        /// </summary>
        /// <param name="offsetString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private TimeSpan TryParseTimezoneOffset(string offsetString)
        {
            try
            {
                if (offsetString.StartsWith("+") || offsetString.StartsWith("-"))
                {
                    var hours = int.Parse(offsetString.Substring(1, 2));
                    var minutes = int.Parse(offsetString.Substring(4, 2));

                    var offset = offsetString.StartsWith("+")
                        ? TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes))
                        : TimeSpan.FromHours(-hours).Subtract(TimeSpan.FromMinutes(minutes));

                    return offset;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TryParseTimezoneOffset ExceptionMessage：{ex.Message}");
            }

            throw new Exception($"offsetString Format Fail，offsetString:{offsetString}");
        }


        /// <summary>
        /// 檢查玩家存款金額是否符合資格
        /// </summary>
        /// <returns></returns>
        private bool VerifyCustDepositAmountCondition(int custId, CashBackDateRange dateRange, CashBackPromotionOutput promotion,string logKey)
        {
            // 玩家符合資格存款單List
            var custDepositList = GetDepositNotUseBonus(promotion.CurrencyId, custId, dateRange.DepositStartDate, dateRange.DepositEndDate).ToList();

            // 玩家存款金額加總
            var custDepositAmountSummary = custDepositList.Sum(r => r.ActualDepositAmount);

            #region log Step8

            _logger.LogInformation(@$"Step8 CashBack VerifyCustDepositAmount CustId：{custId},
                            DepositList：{JsonConvert.SerializeObject(custDepositList)},
                            custDepositAmountSummary：{custDepositAmountSummary}
                            logKey:{logKey}");

            #endregion

            // 存款金額條件等於 0 就不檢查
            if (promotion.RequiredAmount == 0) return true;

            // 如果存款金額不等於 0，且玩家身上沒有符合資格的存款單，就不會符合資格
            if (!custDepositList.Any()) return false;

            return custDepositAmountSummary >= promotion.RequiredAmount;
        }

        #endregion

        #region GetDateRangeByPeriodType

        /// <summary>
        /// 依據週返、日返，取得時間範圍
        /// </summary>
        /// <param name="periodType"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public CashBackDateRange GetDateRangeByPeriodType(VipPeriodType periodType, int currencyId)
        {
            var offset = _currencyTimezoneMap[currencyId];
            var dateRangeFunc = new Dictionary<VipPeriodType, Func<TimeSpan, CashBackDateRange>>
            {
                { VipPeriodType.Weekly, GetDateRangeByWeekly },
                { VipPeriodType.Daily, GetDateRangeByDaily }
            };

            return dateRangeFunc[periodType].Invoke(offset);
        }

        private CashBackDateRange GetDateRangeByWeekly(TimeSpan offset)
        {
            var currentTime = DateTimeOffset.UtcNow.ToOffset(offset);
            var startTime = _promotionBll.StartOfWeek(currentTime.Date.AddDays(-7), DayOfWeek.Monday).Date;

            // 兩個不同 EndDate for SP 日期克制
            var startDate = startTime.ToString("yyyy-MM-dd");
            var tpBetRecordEndDate = startTime.AddDays(7).AddTicks(-1).ToString("yyyy-MM-dd");
            var depositEndEndDate = startTime.AddDays(7).ToString("yyyy-MM-dd");

            return new CashBackDateRange(TpBetRecordStartDate: startDate,
                DepositStartDate: startDate,
                TpBetRecordEndDate: tpBetRecordEndDate,
                DepositEndDate: depositEndEndDate);
        }

        private CashBackDateRange GetDateRangeByDaily(TimeSpan offset)
        {
            var currentTime = DateTimeOffset.UtcNow.ToOffset(offset);
            var startDate = currentTime.AddDays(-2).ToString("yyyy-MM-dd");
            var endDate = currentTime.AddDays(-1).ToString("yyyy-MM-dd");

            return new CashBackDateRange(TpBetRecordStartDate: startDate,
                DepositStartDate: startDate,
                TpBetRecordEndDate: endDate,
                DepositEndDate: endDate);
        }

        #endregion

        #region GetCalculateDate

        private string GetCalculateDate(VipPeriodType period, int currencyId)
        {
            var offset = _currencyTimezoneMap[currencyId];
            var calculateDateFunc = new Dictionary<VipPeriodType, Func<TimeSpan, string>>
            {
                { VipPeriodType.Weekly, CalculateWeeklyDate },
                { VipPeriodType.Daily, CalculateDailyDate }
            };

            return calculateDateFunc[period].Invoke(offset);
        }

        private string CalculateWeeklyDate(TimeSpan offset)
        {
            return _promotionBll.StartOfWeek(DateTimeOffset.UtcNow.ToOffset(offset).DateTime, DayOfWeek.Monday).ToString("yyyy-MM-dd");
        }

        private string CalculateDailyDate(TimeSpan offset)
        {
            return DateTimeOffset.UtcNow.ToOffset(offset).DateTime.ToString("yyyy-MM-dd");
        }

        #endregion

        #region GetNextSettleDate

        private DateTime GetNextSettleDate(VipPeriodType period, int currencyId)
        {
            var offset = _currencyTimezoneMap[currencyId];
            var nextDateFunc = new Dictionary<VipPeriodType, Func<TimeSpan, DateTime>>
            {
                { VipPeriodType.Weekly, GetNextMonday },
                { VipPeriodType.Daily, GetNextDay }
            };

            return nextDateFunc[period].Invoke(offset);
        }

        /// <summary>
        /// 計算下一個禮拜一
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private DateTime GetNextMonday(TimeSpan offset)
        {
            var currentDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);
            var currentDayOfWeek = currentDate.DayOfWeek;
            var daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)currentDayOfWeek + 7) % 7;
            var nextMonday = currentDate.AddDays(daysUntilNextMonday);
            var nextMonDay = new DateTimeOffset(nextMonday, offset);
            return nextMonDay.UtcDateTime;
        }

        /// <summary>
        /// 計算下一個日期
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private DateTime GetNextDay(TimeSpan offset)
        {
            var nextDay = new DateTimeOffset(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day), offset)
                .AddDays(1)
                .ToOffset(offset)
                .ToUniversalTime().DateTime;

            return nextDay;
        }

        #endregion
    }
}