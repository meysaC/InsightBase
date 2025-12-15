using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IMetadataFilterService
    {
        Task<List<SearchResult>> FilterAsync(List<SearchResult> results, QueryContext queryContext, CancellationToken cancellationToken = default);
    }
}