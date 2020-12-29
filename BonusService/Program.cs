using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Workers;

namespace BonusService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();
                    services.AddSingleton<Common>();
                    services.AddSingleton<Log2File>();

                    services.AddSingleton<MessageService>();
                    services.AddSingleton<CustInfoService>();
                    services.AddSingleton<WalletService>();
                    services.AddSingleton<WithdrawalLimitService>();
                    services.AddSingleton<RebateService>();
                    services.AddSingleton<ReferralService>();

                    services.AddSingleton<RebateBLL>();
                    services.AddSingleton<ReferralBLL>();
                    services.AddSingleton<SelLangBLL>();

                    services.AddHostedService<RebateWorker>();
                    services.AddHostedService<ReferralWorker>();
                });
    }
}
