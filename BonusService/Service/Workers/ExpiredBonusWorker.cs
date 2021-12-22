using BonusService.Service.BLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonusService.Service.Extensions;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.Workers
{
    public class ExpiredBonusWorker : BackgroundService
    {
        private readonly ILogger<ExpiredBonusWorker> _logger;
        private readonly ExpiredBonusBLL _expiredBonusBll;
        private Timer _timerExpiredBonus;
        private Timer _timerExpiredTransferBonus;
        private const int Limit = 1000;

        public ExpiredBonusWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ExpiredBonusWorker>>();
            _expiredBonusBll = provider.GetRequiredService<ExpiredBonusBLL>();
            
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _timerExpiredBonus = new Timer(DoExpiredBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerExpiredTransferBonus = new Timer(DoExpiredTransferBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private void DoExpiredTransferBonusTaskAsync(object state)
        {
            try
            {
                _logger.LogInformation("Expired Transfer Bonus Task Start");
                int count;
                var allTasks = new List<Task>();
                do
                {
                    var data = _expiredBonusBll.GetTransferBonusExpired(Limit);
                    foreach (var item in data)
                    {
                        allTasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                var winLossAmount = _expiredBonusBll.GetBonusWinLoss(item.BonusId)?.FirstOrDefault() ?? 0m;
                                //計算當初領取 Bonus Amount 總額 
                                var transferBonusAmount = item.BonusAmount + item.LockedAmount;
                                var remainingBonus = transferBonusAmount + winLossAmount;

                                switch (item.PromotionType)
                                {
                                    case PromotionType.FstDeposit:
                                    case PromotionType.ReDeposit:
                                    case PromotionType.FreeBet:
                                    case PromotionType.WalletBonus:
                                    case PromotionType.Rebate:
                                    case PromotionType.Cashback:
                                    case PromotionType.ManualBonus:
                                    case PromotionType.RetentionBonus:

                                        //確認剩餘本金
                                        var refundAmount = remainingBonus - item.BonusAmount;
                                        if (refundAmount < 0)
                                        {
                                            refundAmount = 0;
                                        }
                                        else if (refundAmount > item.LockedAmount)
                                        {
                                            refundAmount = item.LockedAmount;
                                        }

                                        //呼叫轉帳 API 扣款
                                        if (remainingBonus > 0)
                                        {
                                            var tpBonusCancelTransfer = new TpBonusCancelTransfer
                                            {
                                                TpId = item.TpId,
                                                CustId = item.CustId,
                                                UserName = item.UserName,
                                                SiteId = item.SiteId,
                                                PromoCategoryId = item.PromoCategoryId,
                                                BonusId = item.BonusId,
                                                RemainingBonus = remainingBonus,
                                                RefundAmount = refundAmount
                                            };
                                            _expiredBonusBll.BonusCancelTransfer(tpBonusCancelTransfer);
                                        }
                                        else
                                        {
                                            _expiredBonusBll.RefundExpiredBonus(item,
                                                $"CancelBonus: BonusId={item.BonusId}, Remaining Bonus={remainingBonus:#,##0.##}, Refund Amount={0:#,##0.##}");
                                        }

                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                            }
                        }));

                        if (allTasks.Count >= 4)
                        {
                            Task.WhenAll(allTasks).Wait();
                            allTasks.Clear();
                        }
                    }

                    if (allTasks.Count > 0)
                    {
                        Task.WhenAll(allTasks).Wait();
                        allTasks.Clear();
                    }

                    count = data.Count();
                } while (count >= Limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                _timerExpiredTransferBonus.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }
        }

        private void DoExpiredBonusTaskAsync(object state)
        {
            try
            {
                _logger.LogInformation("Expired Bonus Task Async Start");
                var allTasks = new List<Task>();
                int nCount;
                do
                {
                    var data = _expiredBonusBll.GetBonusExpired(Limit).ToList();
                    foreach (var item in data)
                    {
                        allTasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                _expiredBonusBll.RefundExpiredBonus(item,
                                    $"CancelBonus: BonusId={item.BonusId}, Remaining Bonus={item.Bonus:#,##0.##}, Refund Amount={item.Refund:#,##0.##}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                            }
                        }));

                        if (allTasks.Count >= 4)
                        {
                            Task.WhenAll(allTasks).Wait();
                            allTasks.Clear();
                        }
                    }

                    if (allTasks.Count > 0)
                    {
                        Task.WhenAll(allTasks).Wait();
                        allTasks.Clear();
                    }

                    nCount = data.Count;
                } while (nCount >= Limit);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                _timerExpiredBonus.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
            }

        }
        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerExpiredBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
