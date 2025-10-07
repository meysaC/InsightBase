using InsightBase.Application.Commands;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InsightBase.Application.Handler
{
    public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdCommand, DocumentDto?> //TRequest, TResponse
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly ILogger<GetDocumentByIdQueryHandler> _logger;
        public GetDocumentByIdQueryHandler(IDocumentRepository documentRepo, ILogger<GetDocumentByIdQueryHandler> logger)
            => (_documentRepo, _logger) = (documentRepo, logger);

        public async Task<DocumentDto> Handle(GetDocumentByIdCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepo.GetByIdAsync(request.Id);
            if (document == null) return null;
            return DocumentMapper.ToDocumentDto(document);
        }
    }
}