using InsightBase.Domain.Entities;

namespace InsightBase.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenAsync(string tokenHash);
        Task UpdateAsync(RefreshToken refreshToken);
        Task DeleteAsync(Guid id);
        Task DeleteExpiredTokensAsync(string userId);
        Task RevokeAllUserTokensAsync(string userId);
        Task CleanupRevokedTokensAsync(DateTime olderThan);
        Task<int> GetActiveTokenCountAsync(string userId);
        
    }
}