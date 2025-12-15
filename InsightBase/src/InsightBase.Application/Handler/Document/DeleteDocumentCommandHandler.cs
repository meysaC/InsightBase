using MediatR;
using InsightBase.Application.Commands.Document;
using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Logging;
using InsightBase.Application.DTOs;
using System.Data.SqlTypes;

namespace InsightBase.Application.Handler.Document
{
    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand ,RemoveResult>
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IStorageService _storage;
        private readonly ILogger<DeleteDocumentCommandHandler> _logger;
        public DeleteDocumentCommandHandler(IDocumentRepository documentRepo, IStorageService storage, ILogger<DeleteDocumentCommandHandler> logger) => (_documentRepo, _storage, _logger) = (documentRepo, storage, logger);
        public async Task<RemoveResult> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var result = new RemoveResult();
            try
            {
                var document = await _documentRepo.GetByIdAsync(request.Id);
                if (document == null)
                {
                    _logger.LogWarning("Document with Id {DocumentId} not found.", request.Id);
                    result.Failed.Add(document.FileName);
                    return result;
                }

                bool successDeleteFromDb = await _documentRepo.DeleteAsync(document); //.OnDelete(DeleteBehavior.Cascade) old için db context de chunk falan silmemize gerek yok

                if (!successDeleteFromDb)
                {
                    _logger.LogInformation($"Failed to removed document for id: {request.Id}");
                    result.Failed.Add(document.FileName);
                    return result;
                }

                await _documentRepo.SaveAsync();
                _logger.LogInformation($"Document deleted succesfully from db for id: {request.Id}");

                var deleteFromStorage = await _storage.RemoveAsync(document.FileName);
                if (deleteFromStorage.Failed.Any())
                {
                    _logger.LogWarning($"Some files failed to delete from storage document id: {request.Id}");
                    result.Failed.AddRange(deleteFromStorage.Failed);
                }
                result.Successful.AddRange(deleteFromStorage.Successful);
                return result;

                // if (successDelete)
                // {
                //     await _documentRepo.SaveAsync();
                //     await _storage.RemoveAsync(document.FileName);//RemoveObjectAsync(document.FileName, null);
                //     return true;
                // }
                // return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting document Id: {DocumentId}", request.Id);
                result.Failed.Add($"Unexpected error for document {request.Id}: {ex.Message}");
                return result;
            }
        }
    }
}