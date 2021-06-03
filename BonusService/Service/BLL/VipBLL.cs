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

        public VipBLL(VipService vipService, MessageService messageService, ILogger<VipBLL> logger, SelLangBLL selLangBLL, Currency currency)
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
        
        public IEnumerable<VipSetting> GetVipSettings()
        {
            return _vipService.GetVipSettings();
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
                                (int) VipUpgradeReviewStatus.Pending, settleTime));
                        }
                    }

                    SetVipSetting(new VipSetting
                    {
                        SiteId = vipSetting.SiteId,
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
                            (int) VipUpgradeReviewStatus.Approved, upgradeTime));

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

        private static bool VerifyTask(VipSetting vipSetting, DateTime currentTime, TaskType taskType)
        {
            switch (vipSetting.PeriodType)
            {
                case (int) VipPeriodType.Monthly:
                    var taskTime = new DateTime(currentTime.Year, currentTime.Month, vipSetting.PeriodInterval).Date;
                    if (taskType == TaskType.UpgradeReview)
                    {
                        return taskTime > vipSetting.LastUpdateTime;
                    }

                    var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);
                    var sixthDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 6);
                    return vipSetting.LastUpdateTime > firstDayOfMonth && taskTime > vipSetting.SettleTime && currentTime > sixthDayOfMonth;

                case (int) VipPeriodType.Daily:
                case (int) VipPeriodType.Weekly:
                default:
                    return false;
            }
        }

        private static bool CalculateTime(VipSetting vipSetting, DateTime currentTime, out DateTime startTime,
            out DateTime endTime, out DateTime upgradeTime, out DateTime settleTime, out string calculateMonth)
        {
            switch (vipSetting.PreviousDateRangeType)
            {
                case (int) VipPreviousDateRangeType.LastMonth:
                    var previousDateTime = currentTime.AddMonths(-1);
                    startTime = new DateTime(previousDateTime.Year, previousDateTime.Month, vipSetting.PeriodInterval)
                        .Date;
                    endTime = startTime.AddMonths(1).AddTicks(-1);
                    upgradeTime = new DateTime(currentTime.Year, currentTime.Month, 6);
                    settleTime= new DateTime(currentTime.Year, currentTime.Month, 1);
                    calculateMonth = startTime.ToString("yyyyMM");
                    break;

                case (int) VipPreviousDateRangeType.LastThreeMonth:
                default:
                    startTime = endTime = upgradeTime = settleTime = new DateTime();
                    calculateMonth = string.Empty;
                    return false;
            }

            return true;
        }

        private static decimal CalculateWithdrawalLimitAmount(decimal upgradeAmount, int withdrawalLimitRatio)
        {
            return upgradeAmount * withdrawalLimitRatio;
        }

        private async void SendMessage(VipUpgradeReview customer, VipSetting vipSetting)
        {
            _currency.GetCurrency().TryGetValue(customer.CurrencyId, out var currencyName);
            var content = string.Format(_selLangBLL.GetResourceValue("lbl_VipUpgrade", customer.UserLang),
                currencyName, customer.UpgradeBonus.ToString("#,##0.##"));
            var title = _selLangBLL.GetResourceValue("lbl_VipUpgradeTitle", customer.UserLang);
            
            await _messageService.SendMessageAsync(new Message(vipSetting.SiteId, "System", title, content, customer.CustId,
                customer.UserName, customer.UserLang));
        }
    }
}
