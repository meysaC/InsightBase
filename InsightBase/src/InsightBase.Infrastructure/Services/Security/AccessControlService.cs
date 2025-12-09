using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;

namespace InsightBase.Infrastructure.Services.Security
{
    public class AccessControlService : IAccessControlService // global mevzuat + firmal özel döküman erişim kontrolü
    {
        // private readonly string _
        public Task<AccessDomain> BuildAccessDomainAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanAccessDocumentAsync(string userId, string documentId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<SearchResult>> FilterResultByAccessAsync(List<SearchResult> results, AccessDomain accessDomain, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

    }
}