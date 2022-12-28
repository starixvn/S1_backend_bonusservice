using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class ExpiredBonusWorker : BackgroundService
    {
        private readonly ILogger<ExpiredBonusWorker> _logger;
        private readonly ExpiredBonusBLL _expiredBonusBll;
        private Timer _timerExpiredBonus;
        private Timer _timerExpiredTransferBonus;
        private readonly Random _random;

        public ExpiredBonusWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ExpiredBonusWorker>>();
            _expiredBonusBll = provider.GetRequiredService<ExpiredBonusBLL>();
            _random = new Random();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerExpiredBonus = new Timer(DoExpiredBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerExpiredTransferBonus = new Timer(DoExpiredTransferBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private void DoExpiredTransferBonusTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Expired Transfer Bonus Task Start");
                var data = _expiredBonusBll.GetTransferBonusExpired(_random.Next(950, 1000));// 避免 ORM 或 db execution plan cache
                _logger.LogInformation($"GetTransferBonusExpired Data Count：{data.Count()}");
                foreach (var item in data)
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
                            case PromotionType.Cashback:
                            case PromotionType.SpecialBonus:
                            case PromotionType.RetentionFreeBet:

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
                            case PromotionType.Rebate:
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                _timerExpiredTransferBonus?.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }
        }


        private void DoExpiredBonusTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            try
            {
                _logger.LogInformation("Expired Bonus Task Async Start");
                var data = _expiredBonusBll.GetBonusExpired(_random.Next(950, 1000)); // 避免 ORM 或 db execution plan cache
                _logger.LogInformation($"GetBonusExpired Data Count：{data.Count()}");
                foreach (var item in data)
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

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                _timerExpiredBonus?.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerExpiredBonus?.Change(Timeout.Infinite, 0);
            _timerExpiredTransferBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
