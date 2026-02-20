using InsightBase.Application.DTOs.Auth;
using InsightBase.Domain.Entities;

namespace InsightBase.Application.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(TokenUserDto user, IList<string> roles);
        string GenerateRefreshToken(); //Task<AuthResponse> TokenUserDto user
        string HashToken(string refreshToken);
    }
}