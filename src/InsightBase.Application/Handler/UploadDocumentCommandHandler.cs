using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Commands;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
    {
        private readonly IStorageService _storage;
        private readonly IChunkingService _chunking;
        private readonly IEmbeddingService _embedding;
        private readonly IDocumentRepository _documents;

        public UploadDocumentCommandHandler(
            IStorageService storage,
            IChunkingService chunking,
            IEmbeddingService embedding,
            IDocumentRepository documents
        )
        {
            _storage = storage;
            _chunking = chunking;
            _embedding = embedding;
            _documents = documents;
        }
        public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            //minio ya yükle
            var fileUrl = await _storage.UploadAsync(request.FileName, request.Content);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = request.FileName,
                FilePath = fileUrl,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Chunks = new List<DocumentChunk>()
                // Checksum = ComputeChecksum(request.Content) // İsteğe bağlı: Dosya bütünlüğü için
            };

            //dosyayı text e çevvir 
            var text = System.Text.Encoding.UTF8.GetString(request.Content); //!!!!!! basit bir örnek, gerçek dünyada farklı formatlar için farklı işlemler gerekebilir

            //text i chunk lara böl
            var chunks = _chunking.ChunkText(text, maxTokens: 350); //350 tokenlik chunklar
            int index = 0;
            int batchSize = 20; //her seferinde 20 chunk işleniyor 
            foreach (var (content, start, end) in chunks)
            // for (int i = 0; i < chunks.Count; i++)
            {
                // var batch = chunks.Skip(i).Take(batchSize).ToList();

                // var vectors = await _embedding.GenerateEmbeddingWithRetryAsync(batch.Select(c => c.Content)).ToList();

                // foreach (var (content, start, end) in batch)
                // {
                //     var vector = await _embedding.GenerateEmbeddingWithRetryAsync(content);
                //     var chunk = new DocumentChunk
                //     {
                //         Id = Guid.NewGuid(),
                //         DocumentId = document.Id,
                //         Content = content,
                //         ChunkIndex = index++,
                //         StartToken = start,
                //         EndToken = end,
                //         Length = content.Length,
                //         CreatedAt = DateTime.UtcNow
                //     };

                //     var embedding = new Embedding
                //     {
                //         Id = Guid.NewGuid(),
                //         DocumentChunkId = chunk.Id,
                //         Vector = vector,
                //         ModelName = "text-embedding-3-small",
                //         CreatedAt = DateTime.UtcNow
                //     };

                //     chunk.Embedding = embedding;
                //     document.Chunks.Add(chunk);
                // }






                var chunk = new DocumentChunk
                {
                    Id = Guid.NewGuid(), 
                    DocumentId = document.Id,
                    Content = content,
                    ChunkIndex = index++,
                    StartToken = start,
                    EndToken = end,
                    Length = content.Length,
                    CreatedAt = DateTime.UtcNow
                };

                //var vector = await _embedding.GenerateEmbeddingAsync(content);

                // her chunk için embedding oluştur
                // Retry + exponential backoff ile embedding üret, 
                // Dosya büyükse onlarca / yüzlerce embedding isteği oluyor.
                // OpenAI API’nin rate limitleri var (ör. dakikada X request, saniyede Y token). Limit aşılırsa TooManyRequests
                float[] vector = await _embedding.GenerateEmbeddingWithRetryAsync(content);

                var embedding = new Embedding
                {
                    Id = Guid.NewGuid(),
                    DocumentChunkId = chunk.Id,
                    Vector = vector,
                    ModelName = "text-embedding-3-small",
                    CreatedAt = DateTime.UtcNow
                };

                chunk.Embedding = embedding;
                document.Chunks.Add(chunk);
            }
            await _documents.AddAsync(document);
            await _documents.SaveAsync();

            return document.Id;
        }
    }
}