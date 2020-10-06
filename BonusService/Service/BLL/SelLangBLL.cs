using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class SelLangBLL
    {
        private readonly ConcurrentDictionary<string, ResourceManager> rm = new ConcurrentDictionary<string, ResourceManager>();
        public SelLangBLL()
        { }
        public string GetResourceValue(string key, string langId)
        {
            return GetResourceValue(key, "BonusService_lan", langId);
        }
        private string GetResourceValue(string key, string categoryId, string langId)
        {
            //
            string labelString = string.Empty;
            string sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.{categoryId}_{langId}";
            if (!rm.ContainsKey(sKey))
            {
                rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }

            try
            {
                labelString = rm[sKey].GetString(key);
            }
            catch (MissingManifestResourceException mme)
            {

            }

            if (string.IsNullOrEmpty(labelString) && langId != "en")
            {
                labelString = GetResourceValue(key, categoryId, "en");
            }

            return labelString;
        }
    }
}
