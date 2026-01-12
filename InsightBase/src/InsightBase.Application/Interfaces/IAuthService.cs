using InsightBase.Application.DTOs.Auth;

namespace InsightBase.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterUserAsync(RegisterDto request);
        Task<AuthResponse> LoginUserAsync(LoginDto request);
        Task<AuthResponse> RefreshTokenAsync(string token);
    }
}