using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepo;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        public RegisterUserCommandHandler(IUserRepository userRepo, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository) => (_userRepo, _tokenService, _refreshTokenRepository) = (userRepo, tokenService, refreshTokenRepository);

        public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken ct)
        {
            var userExists = await _userRepo.GetUserByEmailAsync(request.Request.Email);
            if(userExists != null) return AuthResponse.Fail("Kullanıcı zaten kayıtlı!");

            var userId = await _userRepo.AddUser(request.Request.Email, request.Request.Password);
            if(userId == null) return AuthResponse.Fail("Kullanıcı data base e eklenemedi."); //error içeriği göndermiyor addUser!! --> result.Errors.Select(e => e.Description).ToList()

            var addRole = await _userRepo.AddRoleToUserById(userId);
            if(!addRole) return AuthResponse.Fail("Kullanıcı kaydı sırasında bir hata oluştu");

            var roles = await _userRepo.GetRolesById(userId);

            var accessToken = _tokenService.CreateToken(new TokenUserDto { Id = userId, Email = request.Request.Email }, roles);

            // Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenEntity = RefreshToken.Create(
                userId, 
                _tokenService.HashToken(refreshToken),
                DateTime.UtcNow.AddDays(7));

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return AuthResponse.Success(accessToken, refreshToken);
        }
    }
}