using System.Linq;
using InsightBase.Application.Events;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Domain.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace InsightBase.Infrastructure.Workers
{
    public class EmbeddingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessageBus _messageBus;
        // private readonly IEmbeddingService _embeddingService;
        // private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<EmbeddingWorker> _logger;
        public EmbeddingWorker(IServiceScopeFactory scopeFactory ,ILogger<EmbeddingWorker> logger, IMessageBus messageBus) //IMessageBus messageBus,IEmbeddingService embeddingService, IDocumentRepository documentRepository,
        {
            _messageBus = messageBus;
            // _embeddingService = embeddingService;
            // _documentRepository = documentRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmbeddingWorker starting and subscribing to queue...");

            //her mesaj için ayrı scope — repository, dbcontext gibi scoped servislerin doğru yaşam süresini garantiler. 
            // Task.Delay(Timeout.Infinite, stoppingToken) worker'ın host up olduğu sürece canlı kalmasını sağlar.
            _messageBus.Subscribe<EmbeddingJobCreatedEvent>("embedding_jobs", async job =>
            {
                using var scope = _scopeFactory.CreateScope();
                //var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                var embeddingRepository = scope.ServiceProvider.GetRequiredService<IEmbeddingRepository>();
                var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmbeddingWorker>>();

                _logger.LogInformation("Embedding job receiver for document {DocumentId}", job.DocumentId);

                try
                {
                    var document = await documentRepository.GetByIdAsync(job.DocumentId);
                    if (document == null)
                    {
                        logger.LogWarning("Document {DocumentId} not found", job.DocumentId);
                        return;
                    }

                    var pendingChunks = document.Chunks.Where(c => c.Status == ChunkStatus.Pending).ToList();
                    for (int i = 0; i < pendingChunks.Count; i += 10) // var batch in pendingChunks.Chunk(10)
                    {
                        var batch = pendingChunks.Skip(i).Take(10).ToList(); // 10 luk batch ler (rate limit ve performans yönetimi)
                        var contents = batch.Select(c => c.Content).ToList();

                        var vectors = await embeddingService.GenerateEmbeddingWithRetryAsync(contents);

                        for (int j = 0; j < batch.Count; j++)
                        {
                            var chunk = batch[j];
                            var embedding = new Embedding
                            {
                                Id = Guid.NewGuid(),
                                DocumentChunkId = chunk.Id,
                                Vector = vectors[j],
                                ModelName = "text-embedding-3-small",
                                CreatedAt = DateTime.UtcNow
                            };
                            chunk.Embedding = embedding;
                            chunk.Status = ChunkStatus.Processed;

                            await embeddingRepository.SaveEmbeddingEntitiesAsync(embedding); 
                        }
                        await documentRepository.SaveAsync(); //DocumentChunk status update!!!!!!!!!!!!!!!!!!!!!!!! için DocumentRepository 
                        await Task.Delay(1000, stoppingToken); // rate limit koruması
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing embedding for document {DocumentId}", job.DocumentId);
                }
            });
            // keep the worker alive until the host shuts down
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException) { }
            //await Task.CompletedTask;
            _logger.LogInformation("EmbeddingWorker stopping.");
        }
        // protected override Task ExecuteAsync(CancellationToken stoppingToken)
        // {
        //     _messageBus.Subscribe<EmbeddingJobCreatedEvent>("embedding_jobs", async job =>
        //     {
        //         _logger.LogInformation("Embedding job receiver for document {DocumentId}", job.DocumentId);

        //         var document = await _documentRepository.GetByIdAsync(job.DocumentId);
        //         if (document == null) return;

        //         var pendingChunks = document.Chunks.Where(c => c.Status == ChunkStatus.Pending).ToList();
        //         for (int i = 0; i < pendingChunks.Count; i += 10) // var batch in pendingChunks.Chunk(10)
        //         {
        //             try
        //             {
        //                 var batch = pendingChunks.Skip(i).Take(10).ToList(); // 10 luk batch ler (rate limit ve performans yönetimi)

        //                 var contents = batch.Select(c => c.Content).ToList();
        //                 var vectors = await _embeddingService.GenerateEmbeddingWithRetryAsync(contents);
        //                 for (int j = 0; j < batch.Count; j++)
        //                 {
        //                     var chunk = batch[j];
        //                     chunk.Embedding = new Embedding
        //                     {
        //                         Id = Guid.NewGuid(),
        //                         DocumentChunkId = chunk.Id,
        //                         Vector = vectors[i],
        //                         ModelName = "text-embedding-3-small",
        //                         CreatedAt = DateTime.UtcNow
        //                     };
        //                     chunk.Status = ChunkStatus.Processed;
        //                 }
        //                 await _documentRepository.SaveAsync();
        //                 await Task.Delay(1000, stoppingToken); // rate limit koruması
        //             }
        //             catch (Exception ex)
        //             {
        //                 _logger.LogError(ex, "Error processing embedding for document {DocumentId}", job.DocumentId);
        //             }
        //         }
        //     });
        //     return Task.CompletedTask;
        // }
    }
}