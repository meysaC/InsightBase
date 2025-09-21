using System.Linq;
using InsightBase.Application.Events;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Domain.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;


namespace InsightBase.Infrastructure.Workers
{
    public class EmbeddingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<EmbeddingWorker> _logger;
        private readonly TokenBucketRateLimiter _rateLimiter; //yani dakikada 60 request hakkın varsa her request ten sonra 1s bekle.
        private readonly SemaphoreSlim _concurrencyLimitter;
        
        public EmbeddingWorker(IServiceScopeFactory scopeFactory, ILogger<EmbeddingWorker> logger, IMessageBus messageBus)
        {
            _messageBus = messageBus;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _concurrencyLimitter = new SemaphoreSlim(2, 2); //aynı anda en fazla 2 iş
            _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 60, // // 1 dakikada max 60 request
                TokensPerPeriod = 60, // 3 token ekle
                ReplenishmentPeriod = TimeSpan.FromMinutes(1), // her saniye
                QueueLimit = 100, // fazladan gelenler kuyruğa alınır, kuyrukta bekleyebilecek max istek sayısı
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,

                //AutoReplenishment = true // otomatik token ekleme
            });
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmbeddingWorker starting and subscribing to queue...");

            _messageBus.Subscribe<EmbeddingJobCreatedEvent>("embedding_jobs", async job =>
            {
                await _concurrencyLimitter.WaitAsync(stoppingToken);
                try
                {
                    await ProcessEmbeddingJobAsync(job, stoppingToken);
                }
                finally
                {
                    _concurrencyLimitter.Release();
                }
            });

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("EmbeddingWorker cancellation requested");
            }

            _logger.LogInformation("EmbeddingWorker stopping.");
        }

        private async Task ProcessEmbeddingJobAsync(EmbeddingJobCreatedEvent job, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var embeddingRepository = scope.ServiceProvider.GetRequiredService<IEmbeddingRepository>();
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            _logger.LogInformation("Processing embedding job for document {DocumentId}", job.DocumentId);
            
            try
            {
                var document = await documentRepository.GetByIdAsync(job.DocumentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found", job.DocumentId);
                    return;
                }

                var pendingChunks = document.Chunks
                    .Where(c => c.Status == ChunkStatus.Pending)
                    .ToList();
                if (!pendingChunks.Any())
                {
                    _logger.LogInformation("No pending chunks found for document {DocumentId} on EmbeddingWorker", job.DocumentId);
                    return;
                }

                _logger.LogInformation("Processing {ChunkCount} chunks for document {DocumentId}",
                    pendingChunks.Count, job.DocumentId);

                // Process in smaller batches with better rate limiting
                const int batchSize = 5; // Reduced batch size for better rate limiting
                var successCount = 0;
                var failureCount = 0;

                for (int i = 0; i < pendingChunks.Count; i += batchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var batch = pendingChunks.Skip(i).Take(batchSize).ToList();
                    var contents = batch
                                .Select(c => c.Content)
                                .Where(content => !string.IsNullOrWhiteSpace(content))
                                .ToList();
                    if (!contents.Any())
                    {
                        _logger.LogWarning("Batch {BatchIndex} contains no valid content", i / batchSize + 1);
                        continue;
                    }

                    try
                    {
                        // Rate limiting (dakikalık limit)
                        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
                        if (!lease.IsAcquired)
                        {
                            _logger.LogWarning("Rate limit exceeded, delaying batch {BatchIndex}", i / batchSize + 1);
                            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                            i -= batchSize; // aynı batch i tekrar dene
                            continue;
                        }

                        var vectors = await embeddingService.GenerateEmbeddingWithRetryAsync(contents);

                        // Create embeddings and update chunk status
                        var embeddings = new List<Embedding>();
                        for (int j = 0; j < Math.Min(batch.Count, vectors.Count); j++)
                        {
                            var chunk = batch[j];
                            if (string.IsNullOrWhiteSpace(chunk.Content))
                                continue;

                            var embedding = new Embedding
                            {
                                Id = Guid.NewGuid(),
                                DocumentChunkId = chunk.Id,
                                Vector = vectors[j],
                                ModelName = "text-embedding-3-small",
                                CreatedAt = DateTime.UtcNow
                            };

                            embeddings.Add(embedding);
                            chunk.Embedding = embedding;
                            chunk.Status = ChunkStatus.Processed;
                        }

                        // Save embeddings in batch
                        await AddEmbeddingsBatchAsync(embeddingRepository, embeddings);

                        successCount += embeddings.Count;
                        _logger.LogDebug("Successfully processed batch {BatchIndex}, {Count} embeddings created",
                            i / batchSize + 1, embeddings.Count);

                        // Rate limiting delay between batches
                        if (i + batchSize < pendingChunks.Count)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process batch {BatchIndex} for document {DocumentId}",
                            i / batchSize + 1, job.DocumentId);

                        // Mark chunks as failed
                        foreach (var chunk in batch)
                        {
                            chunk.Status = ChunkStatus.Failed;
                        }
                        await documentRepository.SaveAsync();

                        failureCount += batch.Count;
                    }
                }

                _logger.LogInformation("Completed processing document {DocumentId}. Success: {SuccessCount}, Failed: {FailureCount}",
                    job.DocumentId, successCount, failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error processing embedding job for document {DocumentId}", job.DocumentId);
            }
        }

        private static async Task AddEmbeddingsBatchAsync(IEmbeddingRepository repository, List<Embedding> embeddings)
        {
            foreach (var embedding in embeddings)
            {
                await repository.AddEmbeddingEntitiesAsync(embedding);
            }
        }

        public override void Dispose()
        {
            _concurrencyLimitter?.Dispose();
            _rateLimiter?.Dispose();
            base.Dispose();
        }
    }

}