using System;
using System.Threading.Tasks;
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
    }
}