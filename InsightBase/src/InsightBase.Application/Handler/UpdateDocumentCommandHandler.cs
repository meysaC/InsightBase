using InsightBase.Application.Commands;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using MediatR;

namespace InsightBase.Application.Handler
{
    public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand, DocumentDto>
    {
        private readonly IDocumentRepository _documents;
        private readonly IStorageService _storage;
        public UpdateDocumentCommandHandler(IDocumentRepository documents, IStorageService storage) => (_documents, _storage) = (documents, storage);

        public async Task<DocumentDto> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documents.GetByIdAsync(request.Id);
                if (document == null ) return null;

                document.UserFileName = request.FileName ?? document.UserFileName;
                document.LegalArea = request.LegalArea ?? document.LegalArea;
                document.IsPublic = request.IsPublic;
                document.UpdatedAt = DateTime.Now;

                await _documents.UpdateAsync(document);
                await _documents.SaveAsync();

                return new DocumentDto
                {
                    Id = document.Id,
                    FileName = document.UserFileName,
                    DocumentType = document.DocumentType,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"The document update failed document id: {request.Id}", ex);
            }
        }
    }
}