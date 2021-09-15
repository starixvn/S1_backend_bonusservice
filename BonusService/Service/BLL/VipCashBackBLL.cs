using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BonusService.Service.DAL;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace BonusService.Service.BLL
{
    public class VipCashBackBLL
    {
        private readonly VipCashBackService _vipCashBackCashBackService;
        private readonly ILogger<VipCashBackBLL> _logger;
        private readonly SelLangBLL _selLangBLL;
        private readonly Currency _currency;

        public VipCashBackBLL(VipCashBackService vipCashBackService, ILogger<VipCashBackBLL> logger,
            SelLangBLL selLangBLL, Currency currency)
        {
            _logger = logger;
            _vipCashBackCashBackService = vipCashBackService;
            _selLangBLL = selLangBLL;
            _currency = currency;
        }
        
        private IEnumerable<CashBackSettingOutput> GetCashBackSettings()
        {
            return _vipCashBackCashBackService.GetCashBackSettings();
        }
        
        private bool SetCashBackSettings(CashBackSettingInput cashBackSettingInput)
        {
            return _vipCashBackCashBackService.SetCashBackSettings(cashBackSettingInput);
        }

        private IEnumerable<TpGameTypeOutput> GetTpGameType(string categoryIds)
        {
            return _vipCashBackCashBackService.GetTpGameType(categoryIds);
        }

        private bool SetPromotionCashBackSetting(PromotionCashBackSettingInputInsert promotionCashBackSettingInputInsert)
        {
            return _vipCashBackCashBackService.SetPromotionCashBackSetting(promotionCashBackSettingInputInsert);
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

        public bool CalculateCashBack()
        {
            try
            {
                var currentTime = DateTime.UtcNow.AddHours(8);
                var startTime = currentTime.AddDays(-(int) currentTime.DayOfWeek - 6).Date;
                var startDate = startTime.ToString("yyyy-MM-dd");
                var endDate = startTime.AddDays(7).AddTicks(-1).ToString("yyyy-MM-dd");
                var monday = currentTime.AddDays(-(int) currentTime.DayOfWeek + (int) DayOfWeek.Monday)
                    .ToString("yyyy-MM-dd");

                var cashBackSettings = GetCashBackSettings();
                foreach (var cashBackSetting in cashBackSettings)
                {
                    if (!VerifyTask(cashBackSetting))
                    {
                        continue;
                    }

                    var promotions = GetVipCashBackPromotion(new CashBackPromotionInput
                    {
                        SiteId = cashBackSetting.SiteId,
                        PromotionType = (int) PromotionType.Cashback
                    });

                    foreach (var promotion in promotions)
                    {
                        var settings = GetPromotionCashBackSetting(new PromotionCashBackSettingInput
                        {
                            BonusCode = promotion.BonusCode,
                            SiteId = promotion.SiteId,
                            CurrencyId = promotion.CurrencyId
                        }).ToList();

                        if (!settings.Any())
                        {
                            continue;
                        }

                        var cashBackTpBetRecordInput = new CashBackTpBetRecordInput
                        {
                            StartDate = startDate,
                            EndDate = endDate,
                            CurrencyId = promotion.CurrencyId,
                            SiteId = promotion.SiteId,
                            TpIds = $"[{string.Join(",", settings.Select(s => s.TpId).Distinct())}]",
                            CategoryIds = $"[{string.Join(",", settings.Select(s => s.CategoryId).Distinct())}]"
                        };

                        var customers = GetVipCashBackTpBetRecord(cashBackTpBetRecordInput).ToList();
                        var custIds = customers.Select(c => c.CustId).Distinct();
                        var vipInfos = GetCustomerVipInfo($"[{string.Join(",", custIds)}]").ToList();

                        foreach (var customerGroupByCustId in customers.GroupBy(output => output.CustId))
                        {
                            var vipLevel = vipInfos
                                .FirstOrDefault(item => item.CustId == customerGroupByCustId.First().CustId)
                                ?.VipLevel ?? 1;

                            var percentage = settings.Where(item =>
                                    (item.Level == 0 || item.Level == vipLevel) &&
                                    item.TpId == customerGroupByCustId.First().TpId &&
                                    item.CategoryId == customerGroupByCustId.First().CategoryId &&
                                    Math.Abs(customerGroupByCustId.Sum(i => i.WinlostAmount)) >= item.LossAmount)
                                .OrderByDescending(i => i.LossAmount).FirstOrDefault()?.Percentage ?? 0;

                            if (percentage == 0)
                            {
                                continue;
                            }

                            foreach (var customer in customerGroupByCustId)
                            {
                                SetCashBackInfo(new CashBackInfoInput
                                {
                                    BonusCode = promotion.BonusCode,
                                    PromoCategoryId = promotion.CategoryId,
                                    CategoryId = customer.CategoryId,
                                    TpId = customer.TpId,
                                    CustId = customer.CustId,
                                    PeriodFrom = startDate,
                                    PeriodTo = endDate,
                                    VipLevel = vipLevel,
                                    CurrencyId = promotion.CurrencyId,
                                    SiteId = promotion.SiteId,
                                    Percentage = percentage,
                                    WinlostAmount = customer.WinlostAmount,
                                    BonusAmount = percentage / 100 * Math.Abs(customer.WinlostAmount)
                                });
                            }

                            SetCustCashBack(new CustCashBackInput(customerGroupByCustId.First().CustId,
                                promotion.BonusCode,
                                customerGroupByCustId.First().CategoryId, promotion.CategoryId, monday,
                                (percentage / 100 *
                                 Math.Abs(customerGroupByCustId.Sum(output => output.WinlostAmount)))));
                        }
                    }

                    SetCashBackSettings(new CashBackSettingInput
                    {
                        SiteId = cashBackSetting.SiteId,
                        SettleTime = DateTime.UtcNow.AddHours(8)
                    });
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, MethodExtension.GetCurrentMethod());
                return false;
            }
        }

        private static bool VerifyTask(CashBackSettingOutput cashBackSettingOutput)
        {
            var currentTime = DateTime.UtcNow.AddHours(8);
            switch (cashBackSettingOutput.PeriodType)
            {
                case (int) VipPeriodType.Weekly:
                    var monday = currentTime.AddDays(-(int) currentTime.DayOfWeek + cashBackSettingOutput.PeriodInterval);
                    return cashBackSettingOutput.SettleTime < monday;

                case (int) VipPeriodType.Monthly:
                case (int) VipPeriodType.Daily:
                default:
                    return false;
            }
        }
    }
}