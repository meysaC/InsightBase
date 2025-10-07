namespace InsightBase.Application.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T> GetCacheAsync<T>(string key);
        Task<bool> SetCacheAsync<T>(string key, T value);
        Task<object> RemoveCacheAsync(string key);
    }
}