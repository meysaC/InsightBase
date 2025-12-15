using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IRAGOrchestrator
    {        
        Task<RAGResponse> GenerateAnswerAsync(string userQuery, string? userId, RAGOptions? options = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<RAGStreamChunk> GenerateAnswerStreamAsync(string userQuery, string? userId, RAGOptions? options = null, CancellationToken cancellationToken = default);
    }
}