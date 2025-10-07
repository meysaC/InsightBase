using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace InsightBase.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _cacheDb;
        private readonly int _defaultExpiryMinutes;
        private readonly IConfiguration _config;

        public RedisCacheService(IConfiguration config)
        {
            _config = config;

            var host = _config["REDIS_HOST"];
            var port = _config["REDIS_PORT"];
            var password = _config["REDIS_PASSWORD"];

            var options = new ConfigurationOptions
            {
                EndPoints = { $"{host}:{port}" },
                Password = password,
                AbortOnConnectFail = false
            };

            var connection = ConnectionMultiplexer.Connect(options);
            _cacheDb = connection.GetDatabase();

            //default expiry .env de yoksa default ekle
            //int.tryParse bool döndürür
            _defaultExpiryMinutes = int.TryParse(
                    _config["REDIS_DEFAULT_EXPIRY_MINUTES"], out var expiry) ? expiry : 180;
        }
        public async Task<T> GetCacheAsync<T>(string key)
        {
            try
            {
                var value = await _cacheDb.StringGetAsync(key);
                return value.IsNullOrEmpty ? default : JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Redis GetCacheAsync Error: {ex.Message}", ex);
            }
        }

        public async Task<object> RemoveCacheAsync(string key)
        {
            try
            {
                return _cacheDb.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                throw new Exception($"Redis RemoveCacheAsync Error: {ex.Message}", ex);
            }
        }

        public async Task<bool> SetCacheAsync<T>(string key, T value)
        {
            try
            {
                var expirationTime = DateTimeOffset.Now.AddMinutes(_defaultExpiryMinutes);
                var expiryTime = TimeSpan.FromMinutes(_defaultExpiryMinutes);
                var isSet = await _cacheDb.StringSetAsync(key, JsonConvert.SerializeObject(value), expiryTime);
                return isSet;
            }
            catch (Exception ex)
            {
                throw new Exception($"Redis SetCacheAsync Error: {ex.Message}", ex);
            }
        }

    }
}