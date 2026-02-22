using InsightBase.Application.Commands.Document;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using MediatR;

namespace InsightBase.Application.Handler.Document
{
    public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand, DocumentDto>
    {
        private readonly IDocumentRepository _documents;
        public UpdateDocumentCommandHandler(IDocumentRepository documents) => _documents = documents;

        public async Task<DocumentDto> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documents.GetByIdAsync(request.Id);
                if (document == null ) return null;

                document.UserFileName = request.FileName ?? document.UserFileName;
                document.DocumentType = request.DocumentType ?? document.DocumentType;
                document.LegalArea = request.LegalArea ?? document.LegalArea;
                document.IsPublic = request.IsPublic;
                document.UpdatedAt = DateTime.UtcNow;

                await _documents.UpdateAsync(document);
                await _documents.SaveAsync();

                return DocumentMapper.ToDocumentDto(document);
            }
            catch (Exception ex)
            {
                throw new Exception($"The document update failed document id: {request.Id}", ex);
            }
        }
    }
}