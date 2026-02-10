using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepo;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public LoginUserCommandHandler(IUserRepository userRepo, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
        {
            _userRepo = userRepo;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepo.GetUserByEmailAsync(request.Request.Email);
                if(user == null) return AuthResponse.Fail("Email  veya şifre hatalı!");

                var passwordCheck = await _userRepo.PasswordCheck(user.Id, request.Request.Password);
                if(!passwordCheck) return AuthResponse.Fail("Email  veya şifre hatalı!");

                var roles = await _userRepo.GetRolesById(user.Id);

                var accessToken = _tokenService.CreateToken(new TokenUserDto { Id = user.Id, Email = user.Email }, roles);
                
                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenEntity = RefreshToken.Create(
                    user.Id,
                    _tokenService.HashToken(refreshToken),
                    DateTime.UtcNow.AddDays(7)
                );
                await _refreshTokenRepository.AddAsync(refreshTokenEntity);
                    
                // // Eski refresh token'ları temizle (opsiyonel - performans için background job olabilir)
                // await _refreshTokenRepository.DeleteExpiredTokensAsync(user.Id);

                return AuthResponse.Success(accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                return AuthResponse.Fail("Giriş işlemi sırasında bir hata oluştu");
            }
        }

    }
}