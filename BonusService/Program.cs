using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BonusService.Service.BLL;
using BonusService.Service.DAL;

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
                    services.AddHostedService<Worker>();

                    services.AddSingleton<WalletService>();
                    services.AddSingleton<BonusCashBackService>();

                    services.AddSingleton<BonusCashBackBLL>();
                });
    }
}
