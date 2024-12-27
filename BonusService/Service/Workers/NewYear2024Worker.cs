using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;
using Newtonsoft.Json;
using Serilog.Context;

namespace BonusService.Service.Workers
{
    public class NewYear2024Worker : BackgroundService
    {
        private readonly ILogger<NewYear2024Worker> _logger;
        private readonly QueueJobBLL _queueJobBll;
        private readonly NewYear2024BLL _newYear2024Bll;
        private readonly CustInfoBLL _custInfoBll;
        private Timer _timerClaimDrawPrize;

        public NewYear2024Worker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<NewYear2024Worker>>();
            _queueJobBll = provider.GetRequiredService<QueueJobBLL>();
            _newYear2024Bll = provider.GetRequiredService<NewYear2024BLL>();
            _custInfoBll = provider.GetService<CustInfoBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerClaimDrawPrize = new Timer(DoClaimDrawPrizeTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoClaimDrawPrizeTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            
            try
            {
                _logger.LogInformation("Claim Draw Prize Task Start");

                var list = _queueJobBll.GetQueueJob<QueueJobData>(QueueJob.DrawPrize);

                foreach (var listData in list)
                {
                    _logger.LogInformation($"Do Claim Draw Prize, Data: {JsonConvert.SerializeObject(listData)}");

                    var result = -1;
                    var jobData = JsonConvert.DeserializeObject<DrawPrizeQueueJob>(listData.Data);
                    var custInfo = _custInfoBll.GetCustInfo(listData.CustId);
                    
                    //轉AP的Name
                    if(jobData.PrizeName.Contains("Advant Play Free Bet"))
                    {
                        jobData.PrizeName = "Advant Play Free Bet";
                    }
                    
                    switch (jobData.PrizeName)
                    {
                        case "Pragmatic Play FreeSpin x 38":
                            result = _newYear2024Bll.AddPrizeToPlayer(custInfo,5);
                            break;

                        // case "88Reward x 200pts":
                        //     result = _newYear2024Bll.DrawPrizeRewardPoints(custInfo);
                        //     break;
    
                        case "Advant Play Free Bet":
                            result = _newYear2024Bll.AddPrizeToPlayer(custInfo,39);
                            break;
                    }

                    if (result == 0 )
                    {
                        _queueJobBll.DeleteQueueJobBySq(listData.SQ);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                _timerClaimDrawPrize.Change(TimeSpan.FromMinutes(1), TimeSpan.Zero);
                _logger.LogInformation("Claim Draw Prize Task End");
            }
        }
    }
}