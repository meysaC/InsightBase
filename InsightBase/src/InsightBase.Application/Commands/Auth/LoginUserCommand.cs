using InsightBase.Application.DTOs.Auth;
using MediatR;

namespace InsightBase.Application.Commands.Auth
{
    //(LoginDto Request) 
    public record LoginUserCommand(LoginDto Request) : IRequest<AuthResponse>;
}