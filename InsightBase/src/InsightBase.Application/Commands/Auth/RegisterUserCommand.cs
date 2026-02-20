using InsightBase.Application.DTOs.Auth;
using MediatR;

namespace InsightBase.Application.Commands.Auth
{
    public record RegisterUserCommand(
        RegisterDto Request
    ) : IRequest<AuthResponse>; 
}