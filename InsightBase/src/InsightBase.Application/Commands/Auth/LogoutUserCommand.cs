using InsightBase.Application.DTOs.Common;
using MediatR;

namespace InsightBase.Application.Commands.Auth
{
    public record LogoutUserCommand(string RefreshToken) : IRequest<Result>; 
}