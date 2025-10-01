using System.Reflection.Metadata;
using InsightBase.Application.Commands;
using MediatR;

namespace InsightBase.Application.Handler
{
    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQueryCommand, IEnumerable<Document>>
    {
        public Task<IEnumerable<Document>> Handle(GetDocumentsQueryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}