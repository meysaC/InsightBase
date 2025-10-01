using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Commands;
using InsightBase.Application.DTOs;
using InsightBase.Application.Events;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler
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
        public async Task<DocumentDto?> Handle(UploadDocumentCommand request, CancellationToken cancellationToken) //Guid
        {
            //minio ya yükle
            var fileUrl = await _storage.UploadAsync(request.FileName, request.FileType, request.Content);
            
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = request.FileName,
                FilePath = fileUrl,
                //DocumentType = request.FileType,
                UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
                CreatedAt = DateTime.UtcNow,
                Chunks = new List<DocumentChunk>(),
                // Checksum = ComputeChecksum(request.Content) // İsteğe bağlı: Dosya bütünlüğü için
                FileType = request.FileType
            };

            //dosyayı text e çevvir 
            var text = await _textExtraction.ExtractTextAsync(request.Content, request.FileType);

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
                    Status = Domain.Enum.ChunkStatus.Pending //başlangıçta pending !!
                };

                document.Chunks.Add(chunk);
                
                //Embedding üretimi EmbeddingWorker da backgroundService ile
            }
            await _documents.AddAsync(document);
            await _documents.SaveAsync();

            //Kuyruğa embedding job oluşturma mesajı at
            var job = new EmbeddingJobCreatedEvent(document.Id);
            await _messageBus.PublishAsync("embedding_jobs", job);

            // return document.Id;
            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentType = document.DocumentType,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}