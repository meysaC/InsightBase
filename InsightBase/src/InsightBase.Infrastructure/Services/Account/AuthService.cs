using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using InsightBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using InsightBase.Infrastructure.Persistence.Account;
using InsightBase.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services.Account
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        public AuthService(ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, UserManager<ApplicationUser> userManager, ILogger<AuthService> logger)
        {
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<AuthResponse> RegisterUserAsync(RegisterDto request)
        {
            try
            {
                var userExists = await _userManager.FindByEmailAsync(request.Email);
                if (userExists != null)
                {
                    _logger.LogWarning("Kullanıcı zaten kayıtlı: {Email}", request.Email);
                    return AuthResponse.Fail("Kullanıcı zaten kayıtlı!");
                } 

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    EmailConfirmed = false
                };
                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    _logger.LogError("Kullanıcı oluşturulamadı: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    return AuthResponse.Fail(result.Errors.Select(e => e.Description).ToList());
                }
                
                var roleResult = await _userManager.AddToRoleAsync(user, UserRoles.User);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Role ekleme başarısız oldu - UserId: {UserId}", user.Id);

                    // Kullanıcı oluşturuldu ama role eklenemedi - rollback yapılabilir
                    await _userManager.DeleteAsync(user);
                    return AuthResponse.Fail("Kullanıcı kaydı sırasında bir hata oluştu");
                }
                var roles = await _userManager.GetRolesAsync(user);

                // Access Token
                var accessToken = _tokenService.CreateToken(new TokenUserDto { Id = user.Id, Email = user.Email }, roles);

                // Refresh Token
                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenEntity = RefreshToken.Create(
                    user.Id, 
                    _tokenService.HashToken(refreshToken),
                    DateTime.UtcNow.AddDays(7));

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                _logger.LogInformation("Yeni kullanıcı kaydedildi - UserId: {UserId}", user.Id);

                return AuthResponse.Success(accessToken, refreshToken);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sırasında beklenmeyen hata");
                return AuthResponse.Fail("Kayıt işlemi sırasında bir hata oluştu");
            }
        }

        public async Task<AuthResponse> LoginUserAsync(LoginDto request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı: {Email}", request.Email);
                    return AuthResponse.Fail("Email  veya şifre hatalı!");
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordCheck)
                {
                    _logger.LogWarning("Şifre hatalı: {Email}", request.Email);
                    return AuthResponse.Fail("Email  veya şifre hatalı!");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(new TokenUserDto { Id = user.Id, Email = user.Email }, roles);

                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenEntity = RefreshToken.Create(
                    user.Id,
                    _tokenService.HashToken(refreshToken),
                    DateTime.UtcNow.AddDays(7));

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                // // Eski refresh token'ları temizle (opsiyonel - performans için background job olabilir)
                // await _refreshTokenRepository.DeleteExpiredTokensAsync(user.Id);

                _logger.LogInformation("Kullanıcı giriş yaptı - UserId: {UserId}", user.Id);

                return AuthResponse.Success(accessToken, refreshToken);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı girişi sırasında beklenmeyen hata");
                return AuthResponse.Fail("Giriş işlemi sırasında bir hata oluştu");
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token)
        {
            try
            {
                // 1. refresh token ı sha256 ile hashle
                var hashedRefreshToken = _tokenService.HashToken(token);

                // 2. hashlenmiş token ile db den refresh tokenı bul
                var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(hashedRefreshToken);
                if (refreshTokenEntity == null)
                {
                    _logger.LogWarning("Refresh token bulunamadı");
                    return AuthResponse.Fail("Geçersiz refresh token");
                }

                try
                {
                    refreshTokenEntity.Validate();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning("Refresh token validasyon hatası: {Message}", ex.Message);
                    return AuthResponse.Fail(ex.Message);
                }

                // 3. eğer valid ise, kullanıcıyı bul ve yeni refresh token oluştur
                var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId);
                if (user == null)
                {
                    _logger.LogError("Refresh token için kullanıcı bulunamadı - UserId: {UserId}", 
                        refreshTokenEntity.UserId);
                    return AuthResponse.Fail("Kullanıcı bulunamadı");
                }

                var roles = await _userManager.GetRolesAsync(user);

                var newAccessToken = _tokenService.CreateToken(new TokenUserDto { Id = user.Id, Email = user.Email }, roles);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // 4. eski tokenı revoke et
                refreshTokenEntity.Revoke();
                await _refreshTokenRepository.UpdateAsync(refreshTokenEntity);

                // 5. yeni refresh tokenı db ye ekle
                var newRefreshTokenEntity = RefreshToken.Create(
                    user.Id,
                    _tokenService.HashToken(newRefreshToken),
                    DateTime.UtcNow.AddDays(7));
                await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

                _logger.LogInformation("Refresh token yenilendi - UserId: {UserId}", user.Id);
                return AuthResponse.Success(newAccessToken, newRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token işlemi sırasında beklenmeyen hata");
                return AuthResponse.Fail("Refresh token işlemi sırasında bir hata oluştu");
            }
        }

    }
}