using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;

namespace InsightBase.Infrastructure.Services
{
    public class EmbeddingService : Application.Interfaces.IEmbeddingService
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly SemaphoreSlim _rateLimitSmaphore = new SemaphoreSlim(3, 3);//tek seferde enfazla 3 istek
        public EmbeddingService(IOpenAIService openAIService, ILogger<EmbeddingService> logger) => (_openAIService, _logger) = (openAIService, logger);
        public async Task<List<float[]>> GenerateEmbeddingAsync(List<string> texts)
        {
            if (texts == null || !texts.Any()) throw new ArgumentException("Input texts cannot be null or empty.");

            if (texts.Count > 100) throw new ArgumentException("Input texts exceed the maximum limit of 2048.");

            await _rateLimitSmaphore.WaitAsync();
            try
            {
                var request = new EmbeddingCreateRequest
                {
                    InputAsList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(), //tek seferde birden fazla text için
                    //OpenAI .NET SDK’da Input parametresi aslında IEnumerable<object> veya List<string> alabiliyor (ama bazı versiyonlarda string gibi görünebilir)
                    Model = "text-embedding-3-small"
                };

                _logger.LogInformation($"Generating embeddings for {request.InputAsList.Count} texts using model {request.Model}.");
                var response = await _openAIService.Embeddings.CreateEmbedding(request);

                if (response?.Data == null || !response.Data.Any()) throw new Exception("Failed to generate embedding. Embedding API failed or returned empty data. Check quota and API key.");

                var embeddings = response.Data
                                    .OrderBy(d => d.Index)
                                    .Select(d => d.Embedding.Select(x => (float)x).ToArray())
                                    .ToList();
                _logger.LogInformation($"Generated {embeddings.Count} embeddings successfully.");

                return embeddings;
            }
            finally
            {
                _rateLimitSmaphore.Release();
            }
        }

        public async Task<List<float[]>> GenerateEmbeddingWithRetryAsync(List<string> texts)
        {
            const int maxRetries = 5;
            TimeSpan baseDelay = TimeSpan.FromSeconds(1);
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return await GenerateEmbeddingAsync(texts);
                }
                catch (HttpRequestException ex) when (IsTransient(ex) && attempt < maxRetries - 1)
                {
                    // Calculate exponential backoff with jitter
                    var delay = TimeSpan.FromSeconds(
                        baseDelay.TotalMilliseconds * Math.Pow(2, attempt) +
                        Random.Shared.Next(100, 500) // add some jitter
                    );
                    _logger.LogWarning($"Transient error occurred while generating embeddings (attempt {attempt}/{maxRetries}): {ex.Message}. Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay); // API rate limit → bekle ve tekrar dene
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to generate embeddings: {ex.Message}");
                    throw; // Diğer hatalar için retry yapma, hatayı yukarı fırlat
                }
            }
            throw new Exception($"Max retry attempts ({maxRetries}) reached while generating embedding.");
        }

        //OpenAI'nin API'sinde rate limit ve kısa süreli hatalar olabilir; düzgün retry stratejisi ile işler daha stabil olur.
        private bool IsTransient(Exception ex)
        {
            var msg = ex.Message?.ToLower() ?? string.Empty;
            return msg.Contains("429") || // Rate limit
                msg.Contains("503") || // Service Unavailable
                msg.Contains("504") || // Gateway Timeout
                msg.Contains("500") ||   // Internal Server Error
                msg.Contains("rate limit") ||
                msg.Contains("quota exceeded") ||
                msg.Contains("timeout") ||
                ex is HttpRequestException ||
                ex is TaskCanceledException;

        }
    }
}