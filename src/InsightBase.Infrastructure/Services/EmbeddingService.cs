using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;

namespace InsightBase.Infrastructure.Services
{
    public class EmbeddingService : Application.Interfaces.IEmbeddingService
    {
        private readonly IOpenAIService _openAIService;
        public EmbeddingService(IOpenAIService openAIService) => _openAIService = openAIService;
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var request = new EmbeddingCreateRequest
                {
                    Input = text,
                    Model = "text-embedding-3-small"
                };
                var response = await _openAIService.Embeddings.CreateEmbedding(request);


                if (response?.Data == null || !response.Data.Any())
                    throw new Exception("Failed to generate embedding. Embedding API failed or returned empty data. Check quota and API key.");
                return response.Data[0].Embedding.Select(x => (float)x).ToArray();
                // return response.Data
                //     .Select(d => d.Embedding.Select(x => (float)x).ToArray())
                //     .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating embedding: {ex.Message}", ex);
            }

        }

        public async Task<float[]> GenerateEmbeddingWithRetryAsync(string text)
        {
            int retries = 3;
            TimeSpan delay = TimeSpan.FromSeconds(2);
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return await GenerateEmbeddingAsync(text);
                }
                catch (Exception ex) when (i < retries - 1)
                {
                    await Task.Delay(delay);
                    delay = delay * 2; // Exponential backoff
                }
            }
            throw new Exception("Max retry attempts reached while generating embedding.");
        }
    }
}