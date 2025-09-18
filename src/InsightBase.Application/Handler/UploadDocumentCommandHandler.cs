using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Commands;
using InsightBase.Application.Events;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
    {
        private readonly IStorageService _storage;
        private readonly IChunkingService _chunking;
        //private readonly IEmbeddingService _embedding;
        private readonly IDocumentRepository _documents;
        private readonly IMessageBus _messageBus;
        private readonly ITextExtractionService _textExtraction;

        public UploadDocumentCommandHandler(
            IStorageService storage,
            IChunkingService chunking,
            IDocumentRepository documents,
            IMessageBus messageBus,
            ITextExtractionService textExtraction
        ) //IEmbeddingService embedding,
        {
            _storage = storage;
            _chunking = chunking;
            //_embedding = embedding;
            _documents = documents;
            _messageBus = messageBus;
            _textExtraction = textExtraction;
        }
        public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            //minio ya yükle
            var fileUrl = await _storage.UploadAsync(request.FileName, request.Content); //
            
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = request.FileName,
                FilePath = fileUrl,
                UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
                CreatedAt = DateTime.UtcNow,
                Chunks = new List<DocumentChunk>()
                // Checksum = ComputeChecksum(request.Content) // İsteğe bağlı: Dosya bütünlüğü için
            };

            //dosyayı text e çevvir 
            var text = await _textExtraction.ExtractTextAsync(request.Content, request.FileName);

            //text i chunk lara böl
            var chunks = _chunking.ChunkText(text, maxTokens: 350); //350 tokenlik chunklar

            int index = 0;
            // int batchSize = 10; //her seferinde 20 chunk işleniyor, 
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

                // //var vector = await _embedding.GenerateEmbeddingAsync(content);

                // // her chunk için embedding oluştur
                // // Retry + exponential backoff ile embedding üret, 
                // // Dosya büyükse onlarca / yüzlerce embedding isteği oluyor.
                // // OpenAI API’nin rate limitleri var (ör. dakikada X request, saniyede Y token). Limit aşılırsa TooManyRequests
                // float[] vector = await _embedding.GenerateEmbeddingWithRetryAsync(content);

                // var embedding = new Embedding
                // {
                //     Id = Guid.NewGuid(),
                //     DocumentChunkId = chunk.Id,
                //     Vector = vector,
                //     ModelName = "text-embedding-3-small",
                //     CreatedAt = DateTime.UtcNow
                // };

                // chunk.Embedding = embedding;
                // document.Chunks.Add(chunk);
            }
            await _documents.AddAsync(document);
            await _documents.SaveAsync();

            //Kuyruğa embedding job oluşturma mesajı at
            var job = new EmbeddingJobCreatedEvent(document.Id);
            await _messageBus.PublishAsync("embedding_jobs", job);

            return document.Id;
        }
    }
}