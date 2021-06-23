using BonusService.Service.DAL;
using BonusService.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.Logging;
using BonusService.Service.Library;

namespace BonusService.Service.BLL
{
    public class VipBLL
    {
        private readonly VipService _vipService;
        private readonly MessageService _messageService;
        private readonly ILogger<VipBLL> _logger;
        private readonly SelLangBLL _selLangBLL;
        private readonly Currency _currency;

        public VipBLL(VipService vipService,
                      MessageService messageService,
                      ILogger<VipBLL> logger,
                      SelLangBLL selLangBLL,
                      Currency currency)
        {
            _logger = logger;
            _vipService = vipService;
            _messageService = messageService;
            _selLangBLL = selLangBLL;
            _currency = currency;
        }

        private IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            return _vipService.GetVipSystem(siteId);
        }

        public IEnumerable<VipSetting> GetVipSettings(VipTypeEnum vipType)
        {
            return _vipService.GetVipSettings((int)vipType);
        }

        private bool SetVipSetting(VipSetting vipSetting)
        {
            return _vipService.SetVipSetting(vipSetting);
        }


        private IEnumerable<CustVip> GetCustUpgrade(CustomerUpgradeInfo customerUpgradeInfo)
        {
            return _vipService.GetCustUpgrade(customerUpgradeInfo);
        }

        private IEnumerable<VipUpgradeReview> GetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            return _vipService.GetVipUpgradeReview(vipUpgradeInfo);
        }

        private bool SetVipUpgradeReview(VipUpgradeInfo vipUpgradeInfo)
        {
            return _vipService.SetVipUpgradeReview(vipUpgradeInfo);
        }

        private bool SetCustVip(CustomerVipInfo customerVipInfo)
        {
            return _vipService.SetCustVip(customerVipInfo);
        }

        private IEnumerable<CustVipForDowngrade> GetCustDowngrade(CustomerDowngradeInfo customerDowngradeInfo)
        {
            return _vipService.GetCustDowngrade(customerDowngradeInfo);
        }

        private bool SetVipDowngradeReview(VipDowngradeInfo vipDowngradeInfo)
        {
            return _vipService.SetVipDowngradeReview(vipDowngradeInfo);
        }

        private IEnumerable<VipDowngradeReview> GetVipDowngradeReview(VipDowngradeInfo vipDowngradeInfo)
        {
            return _vipService.GetVipDowngradeReview(vipDowngradeInfo);
        }

        private bool SetCustVipForDowngrade(CustomerVipInfo customerVipInfo)
        {
            return _vipService.SetCustVipForDowngrade(customerVipInfo);
        }

        public void UpgradeReview(IEnumerable<VipSetting> vipSettings, DateTime currentTime, out bool success)
        {
            success = true;
            try
            {
                foreach (var vipSetting in vipSettings)
                {
                    if (!VerifyTask(vipSetting, currentTime, TaskType.UpgradeReview))
                    {
                        continue;
                    }

                    if (!CalculateTime(vipSetting, currentTime, out var startTime, out var endTime, out var upgradeTime,
                        out var settleTime, out var calculateMonth))
                    {
                        continue;
                    }

                    var vipList = GetVipSystem(vipSetting.SiteId).ToList();
                    foreach (var vipSystem in vipList.OrderByDescending(x => x.Level))
                    {
                        _logger.LogInformation(
                            $@"GetCustUpgrade Start - Level:{vipSystem.Level} CurrencyId:{vipSystem.CurrencyId} UpDeposit:{vipSystem.UpDeposit} StartTime:{startTime} EndTime:{endTime}");
                        foreach (var custVip in GetCustUpgrade(new CustomerUpgradeInfo(vipSystem.Level,
                            vipSystem.UpDeposit, vipSystem.SiteId, vipSystem.CurrencyId, startTime, endTime,
                            calculateMonth)))
                        {
                            var upgradeAmount = vipSystem.UpBonus;

                            success = SetVipUpgradeReview(new VipUpgradeInfo(custVip.CustId,
                                calculateMonth, custVip.UserName, vipSystem.SiteId, custVip.VipLevel, vipSystem.Level,
                                vipSystem.CurrencyId, custVip.DepositAmount, upgradeAmount,
                                (int)VipReviewStatus.Pending, settleTime));
                        }
                    }

                    SetVipSetting(new VipSetting
                    {
                        SiteId = vipSetting.SiteId,
                        Status = vipSetting.Status,
                        LastUpdateTime = DateTime.UtcNow.AddHours(8),
                        SettleTime = null
                    });
                }
            }
            catch (Exception e)
            {
                success = false;
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }
        }

        public void UpgradeCustomer(IEnumerable<VipSetting> vipSettings, DateTime currentTime, out bool success)
        {
            success = true;
            try
            {
                foreach (var vipSetting in vipSettings)
                {
                    if (!VerifyTask(vipSetting, currentTime, TaskType.UpgradeCustomer))
                    {
                        continue;
                    }

                    if (!CalculateTime(vipSetting, currentTime, out var startTime, out var endTime, out var upgradeTime,
                        out var settleTime, out var calculateMonth))
                    {
                        continue;
                    }

                    var customers =
                        GetVipUpgradeReview(new VipUpgradeInfo(calculateMonth, vipSetting.SiteId, string.Empty,
                            (int)VipReviewStatus.Approved, upgradeTime));

                    foreach (var customer in customers)
                    {
                        var withdrawalLimitAmount =
                            CalculateWithdrawalLimitAmount(customer.UpgradeBonus, vipSetting.WithdrawalLimitRatio);

                        var upgradeResult = SetCustVip(new CustomerVipInfo(customer.CurrentVipLevel,
                            customer.UpgradeVipLevel,
                            customer.CustId, customer.UserName, customer.SiteId, customer.UpgradeBonus,
                            withdrawalLimitAmount, $"VIP Upgrade Reward: {customer.UpgradeBonus:#,##0.##}",
                            $"VIP Upgrade Withdrawal Limit: {withdrawalLimitAmount:#,##0.##}", calculateMonth,
                            DateTime.UtcNow.AddHours(8)));

                        if (!upgradeResult)
                        {
                            success = false;
                            continue;
                        }
                        SendMessage(customer, vipSetting);
                    }

                    if (!success) continue;
                    success = SetVipSetting(new VipSetting
                    {
                        SiteId = vipSetting.SiteId,
                        LastUpdateTime = null,
                        SettleTime = DateTime.UtcNow.AddHours(8)
                    });
                }
            }
            catch (Exception e)
            {
                success = false;
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }
        }

        public void DowngradeReview(IEnumerable<VipSetting> vipSettings,
                                    DateTime currentTime,
                                    out bool success)
        {
            success = true;
            try
            {
                foreach (var vipSetting in vipSettings)
                {
                    if (!VerifyDowngradeTask(vipSetting,
                                             currentTime,
                                             TaskType.DowngradeReview))
                    {
                        continue;
                    }

                    if (!CalculateTime(vipSetting,
                                       currentTime,
                                       out var startTime,
                                       out var endTime,
                                       out var downgradeTime,
                                       out var settleTime,
                                       out var calculateMonth))
                    {
                        continue;
                    }

                    var vipList = GetVipSystem(vipSetting.SiteId).ToList();
                    foreach (var vipSystem in vipList.OrderByDescending(x => x.Level))
                    {
                        _logger.LogInformation($"GetCustDowngrade Start - SiteId={vipSystem.SiteId} Level:{vipSystem.Level} CurrencyId:{vipSystem.CurrencyId} DownDeposit:{vipSystem.DownDeposit} DownTurnover:{vipSystem.DownTurnover}  StartTime:{startTime} EndTime:{endTime}.");

                        if (vipSystem.DownDeposit == null
                            && vipSystem.DownTurnover == null)
                        {
                            continue;
                        }

                        var custDowngradeList = GetCustDowngrade(new CustomerDowngradeInfo(vipSystem.Level,
                                                                 vipSystem.DownDeposit,
                                                                 vipSystem.DownTurnover,
                                                                 vipSystem.SiteId,
                                                                 vipSystem.CurrencyId,
                                                                 startTime,
                                                                 endTime,
                                                                 calculateMonth));

                        var downgradeDepositAmount = vipSystem.DownDeposit == null ? 0 : (decimal)vipSystem.DownDeposit;
                        var downgradeTurnoverAmount = vipSystem.DownTurnover == null ? 0 : (decimal)vipSystem.DownTurnover;

                        foreach (var custVip in custDowngradeList)
                        {
                            success = SetVipDowngradeReview(new VipDowngradeInfo(custVip.CustId,
                                                            calculateMonth,
                                                            vipSystem.SiteId,
                                                            vipSystem.CurrencyId,
                                                            custVip.VipLevel,
                                                            vipSystem.Level - 1,
                                                            custVip.DepositAmount,
                                                            downgradeDepositAmount,
                                                            custVip.TurnoverAmount,
                                                            downgradeTurnoverAmount,
                                                            (int)VipReviewStatus.Approved,
                                                            settleTime));
                        }
                    }

                    SetVipSetting(new VipSetting
                    {
                        SiteId = vipSetting.SiteId,
                        Status = vipSetting.Status,
                        LastUpdateTime = DateTime.UtcNow.AddHours(8),
                        SettleTime = null
                    });
                }
            }
            catch (Exception e)
            {
                success = false;
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }
        }

        public void DowngradeCustomer(IEnumerable<VipSetting> vipSettings,
                                      DateTime currentTime,
                                      out bool success)
        {
            success = true;
            try
            {
                foreach (var vipSetting in vipSettings)
                {
                    if (!VerifyDowngradeTask(vipSetting,
                                             currentTime,
                                             TaskType.UpgradeCustomer))
                    {
                        continue;
                    }

                    if (!CalculateTime(vipSetting,
                                       currentTime,
                                       out var startTime,
                                       out var endTime,
                                       out var downgradeTime,
                                       out var settleTime,
                                       out var calculateMonth))
                    {
                        continue;
                    }

                    var customers = GetVipDowngradeReview(new VipDowngradeInfo(calculateMonth,
                                                          vipSetting.SiteId,
                                                          (int)VipReviewStatus.Approved,
                                                          downgradeTime));

                    foreach (var customer in customers)
                    {

                        var downgradeResult = SetCustVipForDowngrade(new CustomerVipInfo(customer.CurrentVipLevel,
                                                                     customer.DowngradeVipLevel,
                                                                     customer.CustId,
                                                                     customer.UserName,
                                                                     customer.SiteId,
                                                                     calculateMonth,
                                                                     DateTime.UtcNow.AddHours(8)));

                        if (!downgradeResult)
                        {
                            success = false;
                            continue;
                        }

                        SendMessageForDowngrade(customer, vipSetting);
                    }

                    if (!success) continue;
                    success = SetVipSetting(new VipSetting
                    {
                        SiteId = vipSetting.SiteId,
                        LastUpdateTime = null,
                        SettleTime = DateTime.UtcNow.AddHours(8)
                    });
                }
            }
            catch (Exception e)
            {
                success = false;
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
            }
        }

        /// <summary>
        /// 驗證執行緒
        /// </summary>
        /// <param name="vipSetting">VIP設定資料</param>
        /// <param name="currentTime">執行時間(UTC+8)</param>
        /// <param name="taskType">執行類型</param>
        /// <returns></returns>
        private static bool VerifyTask(VipSetting vipSetting,
                                       DateTime currentTime,
                                       TaskType taskType)
        {
            switch (vipSetting.PeriodType)
            {
                case (int)VipPeriodType.Monthly:
                    var taskTime = new DateTime(currentTime.Year, currentTime.Month, vipSetting.PeriodInterval).Date;
                    if (taskType == TaskType.UpgradeReview)
                    {
                        return taskTime > vipSetting.LastUpdateTime;
                    }

                    var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);
                    var sixthDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 6);
                    return vipSetting.LastUpdateTime > firstDayOfMonth && taskTime > vipSetting.SettleTime && currentTime > sixthDayOfMonth;

                case (int)VipPeriodType.Daily:
                case (int)VipPeriodType.Weekly:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 驗證降級執行緒
        /// </summary>
        /// <param name="vipSetting">VIP設定資料</param>
        /// <param name="currentTime">執行時間(UTC+8)</param>
        /// <param name="taskType">執行類型</param>
        /// <returns></returns>
        private static bool VerifyDowngradeTask(VipSetting vipSetting,
                                                DateTime currentTime,
                                                TaskType taskType)
        {
            switch (vipSetting.PeriodType)
            {
                case (int)VipPeriodType.Monthly:
                    //當月指定第N天 YYYY/MM/N
                    var taskTime = new DateTime(currentTime.Year,
                                                currentTime.Month,
                                                vipSetting.PeriodInterval).Date;

                    if (taskType == TaskType.DowngradeReview)
                    {
                        //當月指定執行時間 > 設定最近更新時間(Downgrade Review)
                        return taskTime > vipSetting.LastUpdateTime;
                    }

                    //當月第一天 YYYY/MM/01
                    var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);
                    return vipSetting.LastUpdateTime > firstDayOfMonth //最近更新時間(Downgrade Review) > 當月第一天 
                           && taskTime > vipSetting.SettleTime //當月指定執行時間 > 設定執行時間(執行降級)
                           && currentTime > firstDayOfMonth;

                case (int)VipPeriodType.Daily:
                case (int)VipPeriodType.Weekly:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 計算相關日期時間
        /// </summary>
        /// <param name="vipSetting">VIP 設定資料</param>
        /// <param name="currentTime">執行時間(UTC+8)</param>
        /// <param name="startTime">計算開始時間</param>
        /// <param name="endTime">計算結束時間</param>
        /// <param name="operateTime">實際異動時間</param>
        /// <param name="settleTime">排程結算時間</param>
        /// <param name="calculateMonth">計算年月期間</param>
        /// <returns></returns>
        private static bool CalculateTime(VipSetting vipSetting,
                                          DateTime currentTime,
                                          out DateTime startTime,
                                          out DateTime endTime,
                                          out DateTime operateTime,
                                          out DateTime settleTime,
                                          out string calculateMonth)
        {
            switch (vipSetting.PreviousDateRangeType)
            {
                case (int)VipPreviousDateRangeType.LastMonth:
                    var previousDateTime = currentTime.AddMonths(-1);
                    startTime = new DateTime(previousDateTime.Year, previousDateTime.Month, vipSetting.PeriodInterval)
                        .Date;
                    endTime = startTime.AddMonths(1).AddTicks(-1);
                    operateTime = new DateTime(currentTime.Year, currentTime.Month, 6);
                    settleTime = new DateTime(currentTime.Year, currentTime.Month, 1);
                    calculateMonth = startTime.ToString("yyyyMM");
                    break;

                case (int)VipPreviousDateRangeType.LastThreeMonth:
                    #region Description
                    // previousTMDateTime:當下時間前3個月的時間
                    // startTime:計算資料時間-起
                    // endTime:計算資料時間-迄
                    // operateTime:執行降級時間
                    // settleTime:結算降級時間
                    // caculateMonth:計算年月(計算資料時間-起YYYYMM + 計算資料時間-迄 YYYYMM)
                    #endregion
                    var previousTMDateTime = currentTime.AddMonths(-3);
                    startTime = new DateTime(previousTMDateTime.Year,
                                             previousTMDateTime.Month,
                                             vipSetting.PeriodInterval).Date;
                    endTime = startTime.AddMonths(3).AddTicks(-1);
                    operateTime = new DateTime(currentTime.Year, currentTime.Month, 1);
                    settleTime = new DateTime(currentTime.Year, currentTime.Month, 1);
                    calculateMonth = startTime.ToString("yyyyMM") + endTime.ToString("yyyyMM");
                    break;
                default:
                    startTime = endTime = operateTime = settleTime = new DateTime();
                    calculateMonth = string.Empty;
                    return false;
            }

            return true;
        }

        private static decimal CalculateWithdrawalLimitAmount(decimal upgradeAmount,
                                                              int withdrawalLimitRatio)
        {
            return upgradeAmount * withdrawalLimitRatio;
        }

        /// <summary>
        /// 發送升級訊息
        /// </summary>
        /// <param name="customer">客戶升級資料</param>
        /// <param name="vipSetting">VIP設定資料</param>
        private async void SendMessage(VipUpgradeReview customer,
                                       VipSetting vipSetting)
        {
            _currency.GetCurrency().TryGetValue(customer.CurrencyId, out var currencyName);
            var content = string.Format(_selLangBLL.GetResourceValue("lbl_VipUpgrade", customer.UserLang),
                currencyName, customer.UpgradeBonus.ToString("#,##0.##"));
            var title = _selLangBLL.GetResourceValue("lbl_VipUpgradeTitle", customer.UserLang);

            await _messageService.SendMessageAsync(new Message(vipSetting.SiteId, "System", title, content, customer.CustId,
                customer.UserName, customer.UserLang));
        }

        /// <summary>
        /// 發送降級訊息
        /// </summary>
        /// <param name="customer">客戶降級資料</param>
        /// <param name="vipSetting">VIP設定資料</param>
        private async void SendMessageForDowngrade(VipDowngradeReview customer,
                                       VipSetting vipSetting)
        {
            //_currency.GetCurrency().TryGetValue(customer.CurrencyId, out var currencyName);
            var content = _selLangBLL.GetResourceValue("lbl_VipDowngradeContent", customer.UserLang);
            var title = _selLangBLL.GetResourceValue("lbl_VipDowngradeTitle", customer.UserLang);

            await _messageService.SendMessageAsync(new Message(vipSetting.SiteId,
                                                   "System",
                                                   title,
                                                   content,
                                                   customer.CustId,
                                                   customer.UserName,
                                                   customer.UserLang));
        }
    }
}
