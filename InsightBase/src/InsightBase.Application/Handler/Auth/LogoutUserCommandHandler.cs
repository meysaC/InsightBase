using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs.Common;
using InsightBase.Application.Interfaces;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Application.DTOs.Common.Result>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        public LogoutUserCommandHandler(IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
        }
        public async Task<Application.DTOs.Common.Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            if(token is not null) 
            {
                token.Revoke();
                await _refreshTokenRepository.UpdateAsync(token); 
            }
            return Application.DTOs.Common.Result.Success();
        }

    }
}