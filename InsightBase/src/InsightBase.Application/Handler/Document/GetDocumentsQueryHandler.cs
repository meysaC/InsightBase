using System.Reflection.Metadata;
using InsightBase.Application.Commands.Document;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using MediatR;

namespace InsightBase.Application.Handler.Document
{
    public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQueryCommand, PagedResult<DocumentDto?>>
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IStorageService _storage;
        private readonly ICurrentUserService _currentUser;
        public GetDocumentsQueryHandler(IDocumentRepository documentRepo, IStorageService storage, ICurrentUserService currentUser)
            => (_documentRepo, _storage, _currentUser) = (documentRepo, storage, currentUser);
        public async Task<PagedResult<DocumentDto?>> Handle(GetDocumentsQueryCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            var (items, totalCount) = await _documentRepo.GetUserAllDocumentsAsync(request.Page, request.PageSize, userId);

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