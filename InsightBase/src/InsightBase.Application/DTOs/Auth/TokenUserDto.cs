namespace InsightBase.Application.DTOs.Auth
{
    public class TokenUserDto
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        // public string? AccessToken { get; set; }
        // public string? RefreshToken { get; set; }
        // public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}