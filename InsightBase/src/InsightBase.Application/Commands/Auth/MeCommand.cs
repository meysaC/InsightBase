using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands.Auth
{
    public record MeCommand : IRequest<Result<DTOs.Auth.MeResponseDto>>;
}