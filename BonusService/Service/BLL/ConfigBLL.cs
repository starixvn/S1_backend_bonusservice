using BonusService.Service.Model;
using Newtonsoft.Json;

namespace BonusService.Service.BLL;

public class ConfigBLL
{
    private readonly Dictionary<string, string> defaultSiteConfig;
    private readonly Dictionary<int, SiteConfig> siteIdConfigs;
    private readonly IConfiguration configuration;

    public ConfigBLL(IServiceProvider provider, IConfiguration configuration)
    {
        var siteConfigList = provider.GetServices<SiteConfig>();
        defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;

        siteIdConfigs = siteConfigList.ToDictionary(i => i.SiteId, i => i);
        this.configuration = configuration;
    }

    public string GetConfigValue(string configKey, int siteId = 0)
    {
        var defaultResult = defaultSiteConfig.TryGetValue(configKey, out var result) ? result : string.Empty;

        if (siteId == 0)
        {
            return defaultResult;
        }

        if (siteIdConfigs.TryGetValue(siteId, out var siteConfig))
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

    public ServiceHostConfig GetServiceHostConfig()
    {
        var serviceHostConfig = configuration.GetSection("serviceHosts").Get<ServiceHostConfig>();
        return serviceHostConfig;
    }       
}
