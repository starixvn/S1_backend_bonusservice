using BonusService.Service.BLL;
using BonusService.Service.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.Workers
{
    public class VipWorker : BackgroundService
    {
        private Timer _timer;
        private readonly IHostEnvironment _environment;
        private readonly Log2File _log2File;
        private readonly SelLangBLL _selLangBll;
        private readonly VipBLL _vipBll;
        private readonly ILogger<VipWorker> _logger;

        public VipWorker(ILogger<VipWorker> logger, IHostEnvironment environment, Common tools, Log2File log2File, SelLangBLL selLangBLL, VipBLL vipBLL)
        {
            _environment = environment;
            _log2File = log2File;
            _selLangBll = selLangBLL;
            _vipBll = vipBLL;
            _logger = logger;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(@"VIP Worker Start");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            var isError = false;
                    
            try
            {
                Console.WriteLine(@"GetVipSetting Start");
                var vipSettingList = _vipBll.GetVipSetting().ToList();
                foreach (var vipSetting in vipSettingList)
                {
                    var currentTime = DateTime.UtcNow.AddHours(8);
                    if (VerifyTask(vipSetting, currentTime))
                    {
                        continue;
                    }

                    if (CalculateTime(vipSetting, currentTime, out var startTime, out var endTime))
                    {
                        continue;
                    }

                    Console.WriteLine($@"GetVipSystem Start - SiteId:{vipSetting.SiteId}");
                    var vipList = _vipBll.GetVipSystem(vipSetting.SiteId).ToList();
                    foreach (var vipSystem in vipList.OrderByDescending(x => x.Level))
                    {
                        Console.WriteLine(
                            $@"GetCustUpgrade Start - Level:{vipSystem.Level} CurrencyId:{vipSystem.CurrencyId} UpDeposit:{vipSystem.UpDeposit} StartTime:{startTime} EndTime:{endTime}");
                        foreach (var custVip in _vipBll.GetCustUpgrade(new CustomerUpgradeInfo(vipSystem.Level,
                            vipSystem.UpDeposit, vipSystem.SiteId, vipSystem.CurrencyId, startTime, endTime)))
                        {
                            var vipAmount = CalculateAmount(vipSystem, custVip, vipList, vipSetting.WithdrawalLimitRatio);
                            var result = _vipBll.SetCustVip(new CustomerVipInfo(custVip.VipLevel, vipSystem.Level,
                                custVip.CustId,
                                custVip.UserName, vipSystem.SiteId, vipAmount.UpgradeAmount,
                                vipAmount.WithdrawalLimitAmount,
                                $"VIP Upgrade Reward: {vipAmount.UpgradeAmount:#,##0.##}",
                                $"VIP Upgrade Withdrawal Limit: {vipAmount.WithdrawalLimitAmount:#,##0.##}"));

                            if (result == 0)
                            {
                                isError = true;
                            }
                        }
                    }

                    Console.WriteLine(@"SetVipSetting Start");
                    vipSetting.SettleTime = DateTime.UtcNow.AddHours(8);
                    _vipBll.SetVipSetting(vipSetting);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"VIPWorker Exception:{ex.Message}");
                isError = true;
                _log2File.Error(ex.ToString(), "Vip");
            }
            finally
            {
                Console.WriteLine(@"VIPWorker is done");
                var dueTimeSpan = TimeSpan.FromHours(4);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        private static bool VerifyTask(VipSetting vipSetting, DateTime currentTime)
        {
            DateTime taskTime;
            switch (vipSetting.PeriodType)
            {
                case (int) VipPeriodType.Monthly:
                    taskTime = new DateTime(currentTime.Year, currentTime.Month, vipSetting.PeriodInterval).Date;
                    break;

                case (int) VipPeriodType.Daily:
                case (int) VipPeriodType.Weekly:
                default:
                    return true;
            }

            return taskTime < vipSetting.SettleTime;
        }

        private static bool CalculateTime(VipSetting vipSetting, DateTime currentTime, out DateTime startTime,
            out DateTime endTime)
        {
            switch (vipSetting.PreviousDateRangeType)
            {
                case (int) VipPreviousDateRangeType.LastMonth:
                    currentTime = currentTime.AddMonths(-1);
                    startTime = new DateTime(currentTime.Year, currentTime.Month, vipSetting.PeriodInterval).Date;
                    endTime = startTime.AddMonths(1).AddTicks(-1);
                    break;

                case (int) VipPreviousDateRangeType.LastThreeMonth:
                default:
                    startTime = endTime = new DateTime();
                    return true;
            }

            return false;
        }

        private static VipAmount CalculateAmount(VipSystem vipSystem, CustVip custVip,
            IReadOnlyCollection<VipSystem> vipList, int withdrawalLimitRatio)
        {
            var vipAmount = new VipAmount
            {
                UpgradeAmount = 0,
                WithdrawalLimitAmount = 0
            };

            var diffLevel = vipSystem.Level - custVip.VipLevel;
            for (var i = 0; i < diffLevel; i++)
            {
                var currentLevel = vipSystem.Level - i;
                var currentVip = vipList.FirstOrDefault(item =>
                    item.Level == currentLevel && item.CurrencyId == vipSystem.CurrencyId &&
                    item.SiteId == vipSystem.SiteId);
                if (currentVip == null) continue;
                var currentBonus = currentVip.UpDeposit;

                vipAmount.UpgradeAmount += currentBonus;
            }

            vipAmount.WithdrawalLimitAmount = withdrawalLimitRatio * vipAmount.UpgradeAmount;
            return vipAmount;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
