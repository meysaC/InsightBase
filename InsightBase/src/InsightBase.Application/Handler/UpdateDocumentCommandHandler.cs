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

                //file name ile file path tutulduğu için dosya ismi değiştirilirse file path de değişmeli
                if (request.FileName != null && request.FileName != document.Title)
                {
                    var successDeleteFile = await _storage.RemoveObjectAsync(request.FileName, null);
                    if (!successDeleteFile) return null;
                    var newFileUrl = await _storage.UploadAsync(request.FileName, document.FileType, null);
                    document.FilePath = newFileUrl;
                }

                document.Title = request.FileName ?? document.Title;
                document.LegalArea = request.LegalArea ?? document.LegalArea;
                document.IsPublic = request.IsPublic;
                document.UpdatedAt = DateTime.Now;

                await _documents.UpdateAsync(document);
                await _documents.SaveAsync();

                return new DocumentDto
                {
                    Id = document.Id,
                    Title = document.Title,
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