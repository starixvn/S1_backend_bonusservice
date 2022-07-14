using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BonusService.Service.Library
{
    public class RedisCacheHelper : ICacheHelper
    {
        private readonly ILogger<ICacheHelper> _logger;
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly string[] _charsToRemove = { "[", "]", "/", " ", "," };
        
        public RedisCacheHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ICacheHelper>>();
            _multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        }

        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = factory();
                    if (result != null)
                    {
                        db.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return factory();
            }
        }

        public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = await factory();
                    if (result != null)
                    {
                        await db.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return await factory();
            }
        }

        public async Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return await db.StringSetAsync(key, value, timeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }
        
        public bool StringSet(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.StringSet(key, value, timeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.KeyExists(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }
        
        public async Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var json = await db.StringGetAsync(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        public List<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {            
            try
            {
                var taskList = new List<Task<RedisValue>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();
                foreach (var key in keys)
                {
                    var rediskey = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                    var task = db.StringGetAsync(rediskey);
                    taskList.Add(task);
                }

                trans.Execute();

                var reslstList = new List<TItem>();
                foreach (var task in taskList)
                {
                    if (task.Result.HasValue)
                    {
                        var data = JsonConvert.DeserializeObject<TItem>(task.Result);
                        reslstList.Add(data);
                    }
                }
                return reslstList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{keys} | {ex.Message}");
                return default;
            }
        }

        public bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            try
            {
                var taskList = new List<Task<bool>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();
                foreach (var data in dataList)
                {
                    var key = $"{redisDataBase}:{RemoveSpecialChars(data.Key)}";
                    var task = db.StringSetAsync(key, JsonConvert.SerializeObject(data.Value), timeSpan);
                    taskList.Add(task);
                }

                return trans.Execute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{dataList.Keys} | {ex.Message}");
                return default;
            }
        }

        public bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            try
            {
                if (list == null)
                {
                    return false;
                }

                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var taskList = new List<Task<long>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();

                foreach (var item in list)
                {
                    var task = db.ListRightPushAsync(key, JsonConvert.SerializeObject(item));
                    taskList.Add(task);
                }

                db.KeyExpire(key, timeSpan);
                return trans.Execute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex,
            RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            try
            {
                var result = new List<TItem>();
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                if (!db.KeyExists(key))
                {
                    return result;
                }

                var list = db.ListRange(key, startIndex, endIndex);
                foreach (var item in list)
                {
                    if (item.HasValue)
                    {
                        var data = JsonConvert.DeserializeObject<TItem>(item);
                        result.Add(data);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        public long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            try
            {           
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.ListLength(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        private string RemoveSpecialChars(string key)
        {
            foreach (var c in _charsToRemove)
            {
                key = key.Replace(c, string.Empty);
            }

            return key;
        }
    }
}