using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface ILLMClient
    {
        Task<LLMJsonResponse> GenerateJsonResponseAsync(string instruction, string input, CancellationToken cancellationToken = default);
    }
}