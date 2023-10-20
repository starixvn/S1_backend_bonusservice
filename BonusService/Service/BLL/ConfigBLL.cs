using BonusService.Service.Model;

using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;
        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;
        private readonly IWebHostEnvironment _environment;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;

            _siteIdConfigs = siteConfigList.ToDictionary(i => i.SiteId, i => i);
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
        }

        public string GetConfigValue(string configKey, int siteId = 0)
        {
            var defaultResult = _defaultSiteConfig.TryGetValue(configKey, out var result) ? result : string.Empty;

            if (siteId == 0)
            {
                return defaultResult;
            }

            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultResult;
            }

            return defaultResult;
        }

        public TItem GetConfigValue<TItem>(string configKey, int siteId = 0)
        {
            var json = GetConfigValue(configKey, siteId);
            return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<TItem>(json) : default;
        }

        public TItem GetConfigValueByEnvironment<TItem>(string configKey, int siteId = 0)
        {
            var json = GetConfigValue($"{configKey}.{_environment.EnvironmentName}.json", siteId);
            return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<TItem>(json) : default;
        }        
    }
}
