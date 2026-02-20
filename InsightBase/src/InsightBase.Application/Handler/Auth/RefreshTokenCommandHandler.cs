using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepo;
        public RefreshTokenCommandHandler(ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepo) => (_tokenService, _refreshTokenRepository, _userRepo) = (tokenService, refreshTokenRepository, userRepo);
        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // return await _authService.GenerateNewRefreshTokenAsync(request.RefreshToken); 
            try
            {
                // 1. refresh token ı sha256 ile hashle
                var hashedRefreshToken = _tokenService.HashToken(request.RefreshToken); // token

                // 2. hashlenmiş token ile db den refresh tokenı bul
                var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(hashedRefreshToken);
                if (refreshTokenEntity == null)
                    return AuthResponse.Fail("Geçersiz refresh token");

                try
                {
                    refreshTokenEntity.Validate();
                }
                catch (InvalidOperationException ex)
                {
                    return AuthResponse.Fail(ex.Message);
                }

                // 3. eğer valid ise, kullanıcıyı bul ve yeni refresh token oluştur
                var user = await _userRepo.GetUserByIdAsync(refreshTokenEntity.UserId);
                if (user == null)
                    return AuthResponse.Fail("Kullanıcı bulunamadı");

                var roles = await _userRepo.GetRolesById(refreshTokenEntity.UserId);

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

                return AuthResponse.Success(newAccessToken, newRefreshToken);
            }
            catch (Exception ex)
            {
                return AuthResponse.Fail("Refresh token işlemi sırasında bir hata oluştu!");
            }
        }

    }
}