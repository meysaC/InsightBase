namespace InsightBase.Infrastructure.Repositories
{
    using System.Threading.Tasks;
    using InsightBase.Application.Interfaces;
    using InsightBase.Domain.Entities;
    using InsightBase.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;
        public RefreshTokenRepository(AppDbContext context) => _context = context;
        
        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }
        public async Task<RefreshToken?> GetByTokenAsync(string tokenHash)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }
        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }        
        public async Task DeleteAsync(Guid id)
        {
            var refreshToken = await _context.RefreshTokens.FindAsync(id);
            if (refreshToken != null)
            {
                _context.RefreshTokens.Remove(refreshToken);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteExpiredTokensAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var expiredTokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && x.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var activeTokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.Revoke();
            }

            if (activeTokens.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task CleanupRevokedTokensAsync(DateTime olderThan) // Background job ile çalıştırılmalı !!1
        {
            var oldRevokedTokens = await _context.RefreshTokens
                .Where(x => x.IsRevoked && x.CreatedAt < olderThan)
                .ToListAsync();

            if (oldRevokedTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(oldRevokedTokens);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetActiveTokenCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            return await _context.RefreshTokens
                .CountAsync(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow);
        }
    }
}