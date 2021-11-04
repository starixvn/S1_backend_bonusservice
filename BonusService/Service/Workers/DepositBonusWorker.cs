using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Workers
{
    public class DepositBonusWorker : BackgroundService
    {
        private readonly ILogger<DepositBonusWorker> logger;
        private readonly Log2File log2File;
        private readonly DepositBonusBLL depositBonusBLL;
        private Timer timerDepositBonus;
        private DateTime LastChickDate;

        public DepositBonusWorker(ILogger<DepositBonusWorker> logger, Log2File log2File, DepositBonusBLL depositBonusBLL)
        {
            this.logger = logger;
            this.log2File = log2File;
            this.depositBonusBLL = depositBonusBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerDepositBonus = new Timer(DoDepositBonusTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoDepositBonusTaskAsync(object state)
        {
            try
            {
                //logger.LogInformation("DoDepositBonusTaskAsync start");
                if (LastChickDate != GetUTC8Time().Date)
                {
                    LastChickDate = GetUTC8Time().Date;
                    depositBonusBLL.CheckDepositBonusExpired();
                }

                var depositBonusQueueCustList = depositBonusBLL.GetDepositBonusQueueCustList();
                foreach (var CudepositBonusQueueCuststList in depositBonusQueueCustList)
                {
                    var depositBonusList = depositBonusBLL.GetDepositBonusQueueByCustId(CudepositBonusQueueCuststList.CustId);
                    foreach (var depositBonus in depositBonusList)
                    {
                        //table "DepositInfo" Status 0. Draft, 1. Pending, 2. Paid, 3. Approved, 4. Rejected, 5. Canceled, 6. Expired, 7. OnHold, 99. Error.
                        if (depositBonus.DepositStatus == 3)
                        {
                            var rtn = await depositBonusBLL.SetClaimPromotionAsync(depositBonus);
                            if (rtn)
                            {
                                logger.LogInformation($"DoDepositBonus  success TransId: {depositBonus.TransId}");
                                depositBonusBLL.SetDepositBonusFinish(depositBonus.TransId, CudepositBonusQueueCuststList.CustId);
                                break;
                            }
                            else
                            {
                                depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_not_eligible);
                            }
                        }
                        //table "DepositInfo" Status 0. Draft, 1. Pending, 2. Paid, 3. Approved, 4. Rejected, 5. Canceled, 6. Expired, 7. OnHold, 99. Error.
                        else if (depositBonus.DepositStatus == 4 || depositBonus.DepositStatus == 5 || depositBonus.DepositStatus == 6 ||depositBonus.DepositStatus == 99 )
                        {
                            depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_Rejected);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), "DepositBonus");
            }
            finally
            {
                timerDepositBonus.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
            }

        }
        //private async void DoDepositBonusTaskAsync(object state)
        //{
        //    try
        //    {
        //        //logger.LogInformation("DoDepositBonusTaskAsync start");
        //        if (LastChickDate != GetUTC8Time().Date)
        //        {
        //            LastChickDate = GetUTC8Time().Date;
        //            //logger.LogInformation("DoDepositBonus CheckExpired");
        //            depositBonusBLL.CheckDepositBonusExpired();
        //        }

        //        var depositBonusList = depositBonusBLL.GetDepositBonusQueue();
        //        //logger.LogInformation($"DoDepositBonus  count: {depositBonusList.Count()}");
        //        foreach (var depositBonus in depositBonusList)
        //        {
        //            //table "DepositInfo" Status 0. Draft, 1. Pending, 2. Paid, 3. Approved, 4. Rejected, 5. Canceled, 6. Expired, 7. OnHold, 99. Error.
        //            if (depositBonus.DepositStatus == 3)
        //            {
        //                var rtn = await depositBonusBLL.SetClaimPromotionAsync(depositBonus);
        //                if (rtn)
        //                {
        //                    logger.LogInformation($"DoDepositBonus  success TransId: {depositBonus.TransId}");
        //                    depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, 2);//2. Finish
        //                }
        //                else
        //                {
        //                    depositBonusBLL.SetDepositBonusStatus(depositBonus.TransId, 5); //5.Cancel(user not eligible)
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex.Message);
        //        log2File.Error(ex.ToString(), "DepositBonus");
        //    }
        //    finally
        //    {
        //        timerDepositBonus.Change(TimeSpan.FromMinutes(3), TimeSpan.Zero);
        //    }

        //}
        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerDepositBonus?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
