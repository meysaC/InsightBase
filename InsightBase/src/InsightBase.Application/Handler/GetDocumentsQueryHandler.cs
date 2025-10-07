using System.Reflection.Metadata;
using InsightBase.Application.Commands;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using MediatR;

namespace InsightBase.Application.Handler
{
    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQueryCommand, PagedResult<DocumentDto?>>
    {
        private readonly IDocumentRepository _documentRepo;
        public GetDocumentsQueryHandler(IDocumentRepository documentRepo) => _documentRepo = documentRepo;
        public async Task<PagedResult<DocumentDto?>> Handle(GetDocumentsQueryCommand request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _documentRepo.GetAllAsync(request.Page, request.PageSize);

            var dtoList = items.Select(DocumentMapper.ToDocumentDto).ToList();

            return new PagedResult<DocumentDto?>
            {
                Items = dtoList,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

    }
}