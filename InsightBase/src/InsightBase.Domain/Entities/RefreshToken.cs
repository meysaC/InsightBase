namespace InsightBase.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string UserId { get; private set; }
        public string TokenHash { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        private RefreshToken() { }

        public static RefreshToken Create(string userId, string tokenHash, DateTime expiresAt) 
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            };
        }
        public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
        public bool IsValid() => !IsRevoked && !IsExpired() ;
        public void Revoke()
        {
            if(IsRevoked) throw new InvalidOperationException("Token zaten iptal edildi.");
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
        public void Validate()
        {
            if(IsRevoked) throw new InvalidOperationException("Token iptal edilmiş.");
            if(IsExpired()) throw new InvalidOperationException("Token süresi dolmuş.");
        }
    }
}