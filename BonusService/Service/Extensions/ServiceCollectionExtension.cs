using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;


namespace BonusService.Service.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = config.GetValue<string>("ServiceType");

            services.AddSingleton(factory => new MySqlClient(config.GetConnectionString($"{serviceType}_MainDB"), logger)
            {
                DataBase = DataBase.Main
            });
            services.AddSingleton(factory => new MySqlClient(config.GetConnectionString($"{serviceType}_ReportDB"), logger)
            {
                DataBase = DataBase.Report
            });
            services.AddSingleton(factory => new MySqlClient(config.GetConnectionString($"{serviceType}_WagerDB"), logger)
            {
                DataBase = DataBase.Wager
            });
            services.AddSingleton(factory => new MySqlClient(config.GetConnectionString($"{serviceType}_LogDB"), logger)
            {
                DataBase = DataBase.Log
            });
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfo>() { new() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<ConfigObject>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId)
                                 ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);
                services.AddSingleton(new SiteConfig(serverInfo, config));
            }

            return services;
        }
    }
}