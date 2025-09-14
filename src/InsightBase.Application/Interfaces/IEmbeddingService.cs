using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<List<float[]>> GenerateEmbeddingAsync(List<string> texts);
        Task<List<float[]>> GenerateEmbeddingWithRetryAsync(List<string> texts);
    }
}