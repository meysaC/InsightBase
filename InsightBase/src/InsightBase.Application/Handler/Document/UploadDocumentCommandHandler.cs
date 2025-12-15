using InsightBase.Application.Commands.Document;
using InsightBase.Application.DTOs;
using InsightBase.Application.Events;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Mapper;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler.Document
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto> //Guid
    {
        private readonly IStorageService _storage;
        private readonly IChunkingService _chunking;
        private readonly IDocumentRepository _documents;
        private readonly IMessageBus _messageBus;
        private readonly ITextExtractionService _textExtraction;

        public UploadDocumentCommandHandler(
            IStorageService storage,
            IChunkingService chunking,
            IDocumentRepository documents,
            IMessageBus messageBus,
            ITextExtractionService textExtraction
        )
        {
            _storage = storage;
            _chunking = chunking;
            //_embedding = embedding;
            _documents = documents;
            _messageBus = messageBus;
            _textExtraction = textExtraction;
        }
        public async Task<DocumentDto?> Handle(UploadDocumentCommand command, CancellationToken cancellationToken) //Guid
        {
            var document = new Domain.Entities.Document
            {
                Id = Guid.NewGuid(),
                UserFileName = command.UserFileName,
                FileName = command.FileName,
                //FilePath = fileUrl,
                //FileType = command.FileType,
                UserId = string.IsNullOrEmpty(command.UserId) ? null : command.UserId,
                CreatedAt = DateTime.UtcNow,
                Chunks = new List<DocumentChunk>(),
                DocumentType = command.DocumentType, //kanun, yönetmelik, karar...
                LegalArea = command.LegalArea,
                IsPublic = command.IsPublic,
                FileSize = command.Content.LongLength,
                // Checksum = ComputeChecksum(command.Content) // İsteğe bağlı: Dosya bütünlüğü için
            };

            //pdf, docx mi?
            var ext = Path.GetExtension(document.FileName).ToLowerInvariant();

            //minio ya yükle
            var fileUrl = await _storage.UploadAsync(command.FileName, document.UserFileName, ext, command.Content);

            document.FileType = ext;
            document.FilePath = fileUrl;
            
            //dosyayı text e çevvir 
            var text = await _textExtraction.ExtractTextAsync(command.Content, command.FileName);

            //text i chunk lara böl
            var chunks = _chunking.ChunkText(text, maxTokens: 200); //350 tokenlik chunklar

            int index = 0;
            foreach (var (content, start, end) in chunks)
            {
                var chunk = new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Content = content,
                    ChunkIndex = index++,
                    StartToken = start,
                    EndToken = end,
                    Length = content.Length,
                    CreatedAt = DateTime.UtcNow,
                    Status = Domain.Enum.ChunkStatus.Pending // başlangıçta pending !!
                };

                document.Chunks.Add(chunk);
            }
            await _documents.AddAsync(document);
            await _documents.SaveAsync();

            //Kuyruğa embedding job oluşturma mesajı at (Embedding üretimi EmbeddingWorker da backgroundService ile)
            var job = new EmbeddingJobCreatedEvent(document.Id);
            await _messageBus.PublishAsync("embedding_jobs", job);

            return DocumentMapper.ToDocumentDto(document);
            // return new DocumentDto
            // {
            //     Id = document.Id,
            //     FileName = document.FileName,
            //     FilePath = document.FilePath,
            //     DocumentType = document.DocumentType,
            //     CreatedAt = document.CreatedAt,
            //     UpdatedAt = document.UpdatedAt
            // };
        }
    }
}