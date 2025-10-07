using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class GetDocumentsQueryCommand : IRequest<PagedResult<DocumentDto?>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public GetDocumentsQueryCommand(int page, int pageSize)
        {
            Page = page;
            PageSize = pageSize;
        }

    }
}