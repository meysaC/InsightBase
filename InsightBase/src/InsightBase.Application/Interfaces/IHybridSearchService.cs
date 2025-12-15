using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IHybridSearchService
    {
        Task<List<SearchResult>> SearchAsync(QueryContext queryContext, string? userId, CancellationToken cancellationToken = default);
    }
}