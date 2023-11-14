using BonusService.Service.DAL;

namespace BonusService.Service.Workers;

public class BonusSettledWorker : BackgroundService
{
    private Timer _timer;
    private readonly WalletService _walletService;
    private readonly ILogger<BonusSettledWorker> _logger;
    
    public BonusSettledWorker(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<BonusSettledWorker>>();
        _walletService = provider.GetRequiredService<WalletService>();
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoBonusSettledTask, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private void DoBonusSettledTask(object state)
    {
        try
        {
            var bonusFulfillList = _walletService.GetBonusFulfill();
            _logger.LogInformation($"BonusSettledWorker GetBonusFulfill Count : {bonusFulfillList.Count()}");
            foreach (var bonus in bonusFulfillList)
            { 
                _logger.LogInformation($"BonusSettledWorker: {bonus.BonusId} , Name : {bonus.UserName} , DateTime : {DateTime.Now.AddHours(8)}");
                _walletService.BonusSettle(bonus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"BonusSettledWorker: {ex.Message}");
        }
        finally
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}