using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class GetDocumentByIdCommand : IRequest<DocumentDto?> // TResponse
    {
        public Guid Id { get; }
        public GetDocumentByIdCommand(Guid id) => Id = id;
    }
}