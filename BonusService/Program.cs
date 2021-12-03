using BonusService.Service.BLL;
using BonusService.Service.DAL;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.IO;
using System.Linq;

namespace BonusService
{
    public class Program
    {
        private static IConfigurationRoot Configuration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json")
            .Build();

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var excludesMessage = Configuration.GetSection("ExcludesMessage").Get<string[]>();
                    var excludesApiEndpoints = Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
                    
                    webBuilder.UseSerilog((hostContext, loggerConfiguration) =>
                    {
                        loggerConfiguration
                            .ReadFrom.Configuration(Configuration)
                            .Enrich.FromLogContext()
                            .Filter.ByExcluding(c =>
                                c.Properties.Any(p =>
                                    excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                                    || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
                            .Enrich.WithExceptionDetails();
                    })
                        .UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();
                    services.AddDatabaseConnection(Configuration);
                    services.AddSingleton<Common>();
                    services.AddSingleton<Currency>();
                    services.AddSingleton<CourierAdapter>();
                    services.AddSingleton<CacheHelper>();

                    services.AddSingleton<MessageService>();
                    services.AddSingleton<CustInfoService>();
                    services.AddSingleton<WalletService>();
                    services.AddSingleton<WithdrawalLimitService>();
                    services.AddSingleton<RebateService>();
                    services.AddSingleton<VipRebateService>();
                    services.AddSingleton<ExpiredBonusService>();
                    services.AddSingleton<ReferralService>();
                    services.AddSingleton<VipService>();
                    services.AddSingleton<VipCashBackService>();
                    services.AddSingleton<PromotionAutoExtendService>();
                    services.AddSingleton<DepositBonusService>();
                    services.AddSingleton<ConfigService>();

                    services.AddSingleton<RebateBLL>();
                    services.AddSingleton<ReferralBLL>();
                    services.AddSingleton<SelLangBLL>();
                    services.AddSingleton<ExpiredBonusBLL>();
                    services.AddSingleton<VipBLL>();
                    services.AddSingleton<VipCashBackBLL>();
                    services.AddSingleton<PromotionAutoExtendBLL>();
                    services.AddSingleton<DepositBonusBLL>();

                    services.AddSingleton<BonusAPIBLL>();
                    services.AddSingleton<BonusAPIService>();
                    services.AddSingleton<BonusWalletService>();
                    services.AddSingleton<BonusAPIServiceV2>();
                    services.AddSingleton<BonusAPIBLLV2>();
                    
                    services.AddHostedService<ExpiredBonusWorker>();
                    services.AddHostedService<ReferralWorker>();
                    services.AddHostedService<VipUpgradeWorker>();
                    services.AddHostedService<VipRebateWorker>();
                    services.AddHostedService<VipCashBackWorker>();
                    services.AddHostedService<PromotionAutoExtendWorker>();
                    services.AddHostedService<DepositBonusWorker>();

                    if (Configuration.GetSection("RunVipDowngradeWorker").Get<bool>())
                    {
                        services.AddHostedService<VipDowngradeWorker>();
                    }
                    services.AddControllers();
                });
    }
}
