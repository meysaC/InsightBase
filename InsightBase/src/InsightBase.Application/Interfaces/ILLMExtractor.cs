using System.Text.RegularExpressions;
using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface ILLMExtractor
    {
        Task<LLMExtractionResult> ExtractAsync(string query, CancellationToken cancellationToken = default);
    }
}