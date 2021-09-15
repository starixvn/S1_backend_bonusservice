using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.Library
{
    public class CacheHelper
    {
        private readonly IMemoryCache _cache;
        public CacheHelper(IServiceProvider provider)
        {
            _cache = provider.GetRequiredService<IMemoryCache>();
        }

        public TItem TryGetOrCreate<TItem>(string key, MemoryCacheEntryOptions options, Func<ICacheEntry, TItem> factory)
        {
            if (!_cache.TryGetValue(key, out var result))
            {
                var entry = _cache.CreateEntry(key);
                result = factory(entry);
                if (options != null)
                {
                    entry.SetOptions(options);
                }
                entry.SetValue(result);
                
                //need to manually call dispose instead of having a using in case the factory passed in throws,
                //in which case we do not want to add the entry to the cache
                entry.Dispose();
            }
            return (TItem)result;
        }
    }
}