using MediatR;
using InsightBase.Application.Commands;
using InsightBase.Application.Interfaces;

namespace InsightBase.Application.Handler
{
    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand ,bool>
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IStorageService _storage;
        public DeleteDocumentCommandHandler(IDocumentRepository documentRepo, IStorageService storage) => (_documentRepo, _storage) = (documentRepo, storage);
        public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documentRepo.GetByIdAsync(request.Id);
                if(document == null) return false;
                await _documentRepo.DeleteAsync(document); //.OnDelete(DeleteBehavior.Cascade) old için db context de chunk falan silmemize gerek yok
                await _documentRepo.SaveAsync();
                await _storage.RemoveObjectAsync(document.Title, null);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"The file could not be removed in handler document id: {request.Id}", ex);
            }
        }

    }
}