using BonusService.Service.BLL;
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
            foreach (var bonus in _walletService.GetBonusFulfill())
            { 
                _walletService.BonusSettle(bonus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        //WriteToFile("WriteToFileHostedService: Process Stopped");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}