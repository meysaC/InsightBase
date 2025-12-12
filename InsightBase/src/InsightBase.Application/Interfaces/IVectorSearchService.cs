using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<SearchResult>> SearchAsync(string query, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default);
    }
}