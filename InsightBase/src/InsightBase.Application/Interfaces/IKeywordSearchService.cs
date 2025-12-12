using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IKeywordSearchService
    {
        Task<List<SearchResult>> SearchAsync(List<string> terms, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> ExactMatchLawReferencesAsync(List<string> lawReferences, AccessDomain accessDomain, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> ExactMatchFileNumbersAsync(List<string> fileNumbers, AccessDomain accessDomain, CancellationToken cancellationToken = default);
    }
}