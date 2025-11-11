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
        private readonly IStorageService _storage;
        public GetDocumentsQueryHandler(IDocumentRepository documentRepo, IStorageService storage) => (_documentRepo, _storage) = (documentRepo, storage);
        public async Task<PagedResult<DocumentDto?>> Handle(GetDocumentsQueryCommand request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _documentRepo.GetAllAsync(request.Page, request.PageSize);

            var dtoList = items.Select(DocumentMapper.ToDocumentDto).ToList();

            foreach (var dto in dtoList) dto.FilePath = await _storage.GetPresignedUrlAsync(dto.FileName);
            
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