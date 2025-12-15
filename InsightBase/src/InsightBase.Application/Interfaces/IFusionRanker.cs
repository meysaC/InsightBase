using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IFusionRanker
    {
        Task<List<SearchResult>> RankAsync(List<SearchResult> results, QueryContext queryContext, CancellationToken cancellationToken = default);   
    }
}