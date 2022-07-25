namespace BonusService.Extensions
{
    public static class ConfigurationManagerExtension
    {
        public static ConfigurationManager AddRemoteJsonConfig(this ConfigurationManager configuration, IEnumerable<string> remoteUrls)
        {
            using var client = new HttpClient();
            Stream result = null;
            foreach (var url in remoteUrls)
            {
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                try
                {
                    result = client.GetAsync(url).Result.Content.ReadAsStreamAsync().Result;
                    break;
                }
                catch { }
            }

            configuration.AddJsonStream(result);
            return configuration;
        }
    }
}
