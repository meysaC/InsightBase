using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IAccessControlService
    {
        Task<AccessDomain> BuildAccessDomainAsync(string userId, CancellationToken cancellationToken=default);
        Task<bool> CanAccessDocumentAsync(string userId, string documentId, CancellationToken cancellationToken = default);
        Task<List<SearchResult>> FilterResultByAccessAsync(List<SearchResult> results, AccessDomain accessDomain, CancellationToken cancellationToken = default);
        string BuildAccessControlWhereClause(AccessDomain accessDomain);
    }
}