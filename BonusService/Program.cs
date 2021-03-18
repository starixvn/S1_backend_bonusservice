using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Workers;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

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
                .UseSerilog((hostContext, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(Configuration())
                        .Enrich.FromLogContext()
                        .Enrich.WithExceptionDetails()
                        .MinimumLevel.Information().WriteTo.Console(new JsonFormatter());
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();
                    services.AddSingleton<Common>();
                    services.AddSingleton<Log2File>();
                    services.AddSingleton<Currency>();

                    services.AddSingleton<MessageService>();
                    services.AddSingleton<CustInfoService>();
                    services.AddSingleton<WalletService>();
                    services.AddSingleton<WithdrawalLimitService>();
                    services.AddSingleton<RebateService>();
                    services.AddSingleton<VipRebateService>();
                    services.AddSingleton<ExpiredBonusService>();
                    services.AddSingleton<ReferralService>();
                    services.AddSingleton<VipService>();

                    services.AddSingleton<RebateBLL>();
                    services.AddSingleton<ReferralBLL>();
                    services.AddSingleton<SelLangBLL>();
                    services.AddSingleton<ExpiredBonusBLL>();
                    services.AddSingleton<VipBLL>();

                    services.AddHostedService<RebateWorker>();
                    services.AddHostedService<ExpiredBonusWorker>();
                    services.AddHostedService<ReferralWorker>();
                    services.AddHostedService<VipWorker>();
                    services.AddHostedService<VipRebateWorker>();
                });

        private static IConfigurationRoot Configuration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }
    }
}
