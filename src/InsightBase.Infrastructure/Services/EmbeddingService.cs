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
        public async Task<List<float[]>> GenerateEmbeddingAsync(List<string> texts)
        {
            try
            {
                var request = new EmbeddingCreateRequest
                {
                    InputAsList = texts, //tek seferde birden fazla text için
                    //OpenAI .NET SDK’da Input parametresi aslında IEnumerable<object> veya List<string> alabiliyor (ama bazı versiyonlarda string gibi görünebilir)
                    Model = "text-embedding-3-small"
                };
                var response = await _openAIService.Embeddings.CreateEmbedding(request);


                if (response?.Data == null || !response.Data.Any())
                    throw new Exception("Failed to generate embedding. Embedding API failed or returned empty data. Check quota and API key.");
                // return response.Data[0].Embedding.Select(x => (float)x).ToArray();
                // return response.Data
                //     .Select(d => d.Embedding.Select(x => (float)x).ToArray())
                //     .ToList();
                return response.Data
                        .OrderBy(d => d.Index)
                        .Select(d => d.Embedding.Select(x => (float)x).ToArray())
                        .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating embedding: {ex.Message}", ex);
            }

        }

        public async Task<List<float[]>> GenerateEmbeddingWithRetryAsync(List<string> text)
        {
            int retries = 3;
            TimeSpan delay = TimeSpan.FromSeconds(2);
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return await GenerateEmbeddingAsync(text);
                }
                // catch (Exception ex) when (i < retries - 1)
                // {
                //     await Task.Delay(delay);
                //     delay = delay * 2; // Exponential backoff
                // }
                catch (HttpRequestException ex) when (ex.Message.Contains("429")) // Rate limit hatası(429) için retry
                {
                    await Task.Delay(delay); // API rate limit → bekle ve tekrar dene
                    delay = delay * 2; // Exponential backoff
                }
            }
            throw new Exception("Max retry attempts reached while generating embedding.");
        }
    }
}