using MediatR;

namespace InsightBase.Application.Commands
{
    public class DeleteDocumentCommand : IRequest<bool>
    {
        public Guid Id { get; }
        public DeleteDocumentCommand(Guid id)
        {
            Id = id;
        }
    }
}