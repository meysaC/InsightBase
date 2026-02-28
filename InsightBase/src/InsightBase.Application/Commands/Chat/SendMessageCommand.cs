using InsightBase.Application.Models;
using MediatR;

namespace InsightBase.Application.Commands.Chat
{
    public record SendMessageCommand(string Query) : IRequest<RAGResponse> ;
}