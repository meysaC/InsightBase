using System.Reflection.Metadata;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class GetDocumentsQueryCommand : IRequest<IEnumerable<Document>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}