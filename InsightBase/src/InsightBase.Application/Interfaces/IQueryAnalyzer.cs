using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IQueryAnalyzer
    {
        Task<QueryContext> AnalyzeAsync(string query, string? userId = null, CancellationToken cancellationToken = default);
    }
}