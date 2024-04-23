using BonusService.Service.Model.enums;

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
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : factory();
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
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : await factory();
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

        public bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Bonus)
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

        public List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Bonus)
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
                return new List<TItem>();
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

        public async Task<bool> ListLeftPopAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            try
            {
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                if (!db.KeyExists(key))
                {
                    return false;
                }

                var item = await db.ListLeftPopAsync(key);
                return item.HasValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TryCreateLockAsync(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return await db.StringSetAsync(key, lockValue, expiry, When.NotExists);// 使用When.NotExists選項，確保操作的原子性
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnLockAsync(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Bonus)
        {
            //當鎖的值與預期的值相匹配時才釋放鎖，使用Lua script確保操作的原子性
            var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
            var script = @"if redis.call('GET', KEYS[1]) == ARGV[1] then 
                           return redis.call('DEL', KEYS[1]) 
                       else 
                           return 0 
                       end";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { lockValue });
                return result.ToString() == "1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
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