using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
    {
        private readonly IAuthService _authService;
        public RegisterUserCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken ct)
        {
            var result = await _authService.RegisterUserAsync(request.Request);
            return result;
        }
    }
}