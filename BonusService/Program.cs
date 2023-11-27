using BonusService;
using BonusService.Extensions;
using BonusService.Service.Workers;

using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails();
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var serviceType = Environment.GetEnvironmentVariable("ServiceType");
if (!string.IsNullOrEmpty(serviceType) && serviceType.Equals("worker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddHostedService<ExpiredBonusWorker>()
        .AddHostedService<ReferralWorker>()
        .AddHostedService<VipUpgradeWorker>()
        .AddHostedService<VipDowngradeWorker>()
        .AddHostedService<VipRebateWorker>()
        .AddHostedService<VipCashBackWorker>()
        .AddHostedService<PromotionAutoExtendWorker>()
        .AddHostedService<DepositBonusWorker>()
        .AddHostedService<DepositReferralWorker>()
        .AddHostedService<BonusCancelResetWorker>()
        .AddHostedService<BonusSettledWorker>()
        .AddHostedService<RewardWorker>();
}

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();