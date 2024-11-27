using BonusService;
using BonusService.Extensions;
using BonusService.Service.Workers;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Network;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
var remoteUrls = GetRemoteUrls();
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
    OverrideLoggerWriteToLFile(Configuration);
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var serviceType = builder.Configuration.GetValue<string>("ServiceType");
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
        .AddHostedService<PromotionCheckWorker>()
        .AddHostedService<DepositBonusWorker>()
        .AddHostedService<DepositReferralWorker>()
        .AddHostedService<BonusCancelResetWorker>()
        .AddHostedService<BonusSettledWorker>()
        .AddHostedService<RewardWorker>()
        .AddHostedService<NewYear2024Worker>();
}

var app = builder.Build();
startup.Configure(app, builder.Environment);
SentrySdk.CaptureMessage("Bonus Service started");
app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("ApplicationName", builder.Configuration.GetValue<string>("ServiceName"))
        .Enrich.WithProperty("Env", builder.Configuration.GetValue<string>("Environment"))
        .WriteTo.UDPSink(
            builder.Configuration.GetValue<string>("OpenSearch:Uri"),
            builder.Configuration.GetValue<int>("OpenSearch:Port"),
            new Serilog.Formatting.Json.JsonFormatter())
        .WriteTo.Sentry(o =>
        {
            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
            o.MinimumEventLevel = LogEventLevel.Error;
            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
        });
}

string[] GetRemoteUrls()
{
    // 如果有 Runtime Environment Variable 優先使用
    // 沒有則使用預設 AppSetting
    return Environment.GetEnvironmentVariable("ConfigServiceUrl") is null
        ? builder.Configuration.GetSection("ConfigService").Get<string[]>()
        : new List<string>() { Environment.GetEnvironmentVariable("ConfigServiceUrl") }.ToArray();
}