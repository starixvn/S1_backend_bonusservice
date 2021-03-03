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
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            var isError = false;
                    
            try
            {
                // TODO 排程設定資料暫存到 Memory
                var vipSettingList = _vipBll.GetVipSetting().ToList();
                
                foreach (var vipSetting in vipSettingList)
                {
                    // TODO 判斷啟動頻率是否符合設定表
                    // TODO 改成從設定表抓上個月的區間
                    var currentTime = DateTime.UtcNow.AddHours(8);
                    var startTime = new DateTime(currentTime.Year, currentTime.Month, 1).Date;
                    var endTime = startTime.AddMonths(1).AddTicks(-1);

                    var vipList = _vipBll.GetVipSystem().ToList();
                    foreach (var vipSystem in vipList.OrderByDescending(x => x.Level))
                    {

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
                                // Error Handle
                            }
                        }
                    }
                }

                // TODO Error Handle Retry & Log
            }
            catch (Exception ex)
            {
                isError = true;
                _log2File.Error(ex.ToString(), "Vip");
            }
            finally
            {
                // TODO 需要修改觸發時間
                var dueTimeSpan = TimeSpan.FromMinutes(30);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
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
