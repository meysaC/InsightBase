using InsightBase.Application.Commands.Document;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InsightBase.Application.Handler.Document
{
    public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdCommand, DocumentDto?> //TRequest, TResponse
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly ILogger<GetDocumentByIdQueryHandler> _logger;
        private readonly IStorageService _storage;
        private readonly ICurrentUserService _currentUser;
        public GetDocumentByIdQueryHandler(IDocumentRepository documentRepo, ILogger<GetDocumentByIdQueryHandler> logger, IStorageService storage, ICurrentUserService currentUser)
            => (_documentRepo, _logger, _storage, _currentUser) = (documentRepo, logger, storage, currentUser);

        public async Task<DocumentDto> Handle(GetDocumentByIdCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (userId == null) throw new UnauthorizedAccessException("User is not authenticated.");
            var document = await _documentRepo.GetUserDocumentByIdAsync(request.Id, userId);
            if (document == null) return null;
            document.FilePath = await _storage.GetPresignedUrlAsync(document.FileName);
            return DocumentMapper.ToDocumentDto(document);
        }
    }
}