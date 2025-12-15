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
        public GetDocumentByIdQueryHandler(IDocumentRepository documentRepo, ILogger<GetDocumentByIdQueryHandler> logger, IStorageService storage)
            => (_documentRepo, _logger, _storage) = (documentRepo, logger, storage);

        public async Task<DocumentDto> Handle(GetDocumentByIdCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepo.GetByIdAsync(request.Id);
            if (document == null) return null;
            document.FilePath = await _storage.GetPresignedUrlAsync(document.FileName);
            return DocumentMapper.ToDocumentDto(document);
        }
    }
}