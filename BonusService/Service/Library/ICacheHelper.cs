using BonusService.Service.Model.enums;
using StackExchange.Redis;

namespace BonusService.Service.Library
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        bool StringSet(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus);

        Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus);
        List<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Bonus);

        bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex,
            RedisDataBase redisDataBase = RedisDataBase.Bonus);

        long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus);
    }
}