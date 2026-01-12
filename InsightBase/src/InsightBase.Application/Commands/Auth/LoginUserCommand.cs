using InsightBase.Application.DTOs.Auth;

namespace InsightBase.Application.Commands.Auth
{
    public class LoginUserCommand(
        LoginDto Request
    ) : MediatR.IRequest<AuthResponse>;
}