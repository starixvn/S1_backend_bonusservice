namespace BonusService.Service.Model
{
    public class SiteConfig
    {
        public int SiteId { get; set; }
        public Dictionary<string, string> Config { get; set; }

        public SiteConfig(ServerInfo serverInfo, Dictionary<string, string> config)
        {
            SiteId = serverInfo.SiteId;
            Config = config;
        }
    }
}
