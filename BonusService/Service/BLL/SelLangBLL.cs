using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;

using BonusService.Service.Model.enums;

namespace BonusService.Service.BLL
{
    public class SelLangBLL
    {
        private readonly ConcurrentDictionary<string, ResourceManager> _rm = new();
        public SelLangBLL()
        { }
        public string GetResourceValue(string key, string langId)
        {
            return GetResourceValue(key, "BonusService_lan", langId);
        }
        private string GetResourceValue(string key, string categoryId, string langId)
        {
            var labelString = string.Empty;
            var sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.{categoryId}_{langId}";
            if (!_rm.ContainsKey(sKey))
            {
                _rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }

            try
            {
                labelString = _rm[sKey].GetString(key);
            }
            catch (MissingManifestResourceException)
            {

            }

            if (string.IsNullOrEmpty(labelString) && langId != "en")
            {
                labelString = GetResourceValue(key, categoryId, "en");
            }

            return labelString;
        }
        public string GetMsg(string key, int siteId, string siteName, string langId)
        {
            var msgString = "";
            if (siteId == (int)SiteId.Nova88)
            {
                msgString = GetResourceValue(key, "msg", langId);
            }
            else
            {
                try
                {
                    msgString = GetResourceValue(key, $"{siteName}_msg", langId);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (string.IsNullOrEmpty(msgString))
                {
                    msgString = GetResourceValue(key, "msg", langId);
                }
            }

            return msgString;
        }
    }
}
