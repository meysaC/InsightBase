using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands.Document
{
    public class DeleteDocumentCommand : IRequest<RemoveResult>
    {
        public Guid Id { get; }
        public DeleteDocumentCommand(Guid id)
        {
            Id = id;
        }
    }
}