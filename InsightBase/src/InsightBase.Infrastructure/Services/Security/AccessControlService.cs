using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InsightBase.Infrastructure.Services.Security
{
    public class AccessControlService : IAccessControlService // global mevzuat + firmal özel döküman erişim kontrolü
    {
        private readonly string _connectionString;
        private readonly ILogger<AccessControlService> _logger;
        private readonly IConfiguration _configuration;
        public AccessControlService( ILogger<AccessControlService> logger, IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<AccessDomain> BuildAccessDomainAsync(string userId, CancellationToken cancellationToken = default) // kullanıcıya özel erişim domaini oluştur
        {
            _logger.LogInformation("Building access domain for user {UserId}", userId);
            var domain = new AccessDomain
            {
                UserId = userId,
                IncludeGlobalData = true, // herkes mevzuata erişebilir
                CreatedAt = DateTime.UtcNow
            };

            using var conn = new NpgsqlConnection(_connectionString);  
            await conn.OpenAsync(cancellationToken);

            // 1. kullancınn organizasyonlarını al
            domain.UserOrganizationIds = await GetUserOrganizationsAsync(conn, userId, cancellationToken);

            // 2. kullanıcının rollerini al
            domain.UserRoles = await GetUserRolesAsync(conn, userId, cancellationToken);

            // 3. kullanıcının doğrudan erişim yetkisi olan dökümanları al
            domain.AllowedDocumentIds = await GetDirectDocumentAccessAsync(conn, userId, cancellationToken);

            // 4. organizasyon bazlı erişim kurallarını al
            domain.OrganizationAccessRules = await GetOrganizationAccessRulesAsync(conn, domain.UserOrganizationIds, cancellationToken);

            _logger.LogInformation("Access domain built. Orgs: {OrgCount}, Roles: {RoleCount}, Direct docs: {DocCount}", domain.UserOrganizationIds.Count, domain.UserRoles.Count, domain.AllowedDocumentIds.Count);
        
            return domain;
        }
        public async Task<bool> CanAccessDocumentAsync(string userId, string documentId, CancellationToken cancellationToken = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // 1. global mevzuat var mı (herkes erişebilir
            var isGlobal = await IsGlobalDocumentAsync(conn, documentId, cancellationToken);
            if (isGlobal) return true;

            // 2. doğrudan erişim yetkisi var mı
            var hasDirectAccess = await HasDirectAccessAsync(conn, userId, documentId, cancellationToken);
            if (hasDirectAccess) return true;

            // 3. organizasyon bazlı erişim kontrolü
            var hasOrgAccess = await HasOrganizationAccessAsync(conn, userId, documentId, cancellationToken);
            if (hasOrgAccess) return true;

            // 4. roller bazlı erişim kontrolü 
            var hasRoleAccess = await HasRoleAccessAsync(conn, userId, documentId, cancellationToken);
            if (hasRoleAccess) return true;

            return false;
        }


        public async Task<List<SearchResult>> FilterResultByAccessAsync(List<SearchResult> results, AccessDomain accessDomain, CancellationToken cancellationToken = default) // arama sonuçlarını access domaine göre filtrele
        {
            if(!results.Any()) return results;

            var filtered = new List<SearchResult>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            foreach (var result in results)
            {
                if(result.IsGlobal)
                {
                    filtered.Add(result);
                    continue;
                }

                if(await CanAccessDocumentInternalAsync(conn, accessDomain, result.DocumentId, cancellationToken))
                {
                    filtered.Add(result);
                }
                else
                {
                    _logger.LogWarning("User {UserId} denied access to document {DocumentId} in search results", accessDomain.UserId, result.DocumentId);
                }
            }

            _logger.LogInformation("Filtered search results. Original: {OriginalCount}, Filtered: {FilteredCount}", results.Count, filtered.Count);
            return filtered;
        }
        public string BuildAccessControlWhereClause(AccessDomain accessDomain) // sql where clause (performance için)
        {
            var clauses = new List<string>();
            
            //global dökümanlara herkes erişebilir
            clauses.Add("is_global = true");

            if(accessDomain.UserOrganizationIds.Any())
            {
                var orgIds = string.Join(",", accessDomain.UserOrganizationIds.Select(id => $"'{id}'"));
                clauses.Add($"organization_id IN ({orgIds})");
            }

            // doğrudan erişim
            if(accessDomain.AllowedDocumentIds.Any())
            {
                var docIds = string.Join(",", accessDomain.AllowedDocumentIds.Select(id => $"'{id}'"));
                clauses.Add($"id IN ({docIds})");
            }
            return string.Join(" OR ", clauses);
        }
        

        private async Task<List<string>> GetUserOrganizationsAsync(NpgsqlConnection conn, string userId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT organization_id 
                FROM user_organizations 
                WHERE user_id = @userId AND is_active = true";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            var orgs = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken); // Çok satır / çok kolon
            while (await reader.ReadAsync(cancellationToken))
            {
                orgs.Add(reader.GetString(0));
            }

            return orgs;
        }
        private async Task<List<string>> GetUserRolesAsync(NpgsqlConnection conn, string userId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT role_name
                FROM user_roles
                WHERE user_id = @userId AND is_active = true";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            var roles = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while(await reader.ReadAsync(cancellationToken))
            {
                roles.Add(reader.GetString(0));
            }
            return roles;
        }
        private async Task<List<string>> GetDirectDocumentAccessAsync(NpgsqlConnection conn, string userId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT document_id
                FROM document_access
                WHERE user_id = @userId AND is_active = true
                AND (expires_at IS NULL OR expires_at > NOW())";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            var docs = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while(await reader.ReadAsync(cancellationToken))
            {
                docs.Add(reader.GetString(0));
            }
            return docs;
        }
        private async Task<Dictionary<string, AccessRule>> GetOrganizationAccessRulesAsync(NpgsqlConnection conn, List<string> userOrganizationIds, CancellationToken cancellationToken)
        {
            if(!userOrganizationIds.Any()) return new Dictionary<string, AccessRule>();

            var sql = @"
                SELECT organization_id, rule_type, rule_value
                FROM organization_access_rules
                WHERE organization_id = ANY(@userOrganizationIds) AND is_active = true";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userOrganizationIds", userOrganizationIds.ToArray());

            var rules = new Dictionary<string, AccessRule>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while(await reader.ReadAsync(cancellationToken))
            {
                var orgId = reader.GetString(0);
                var ruleType = reader.GetString(1);
                var ruleValue = reader.GetString(2);
                
                rules[orgId] = new AccessRule 
                { 
                    OrganizationId = orgId,
                    RuleType = ruleType, 
                    RuleValue = ruleValue 
                };
            }
            return rules;
        }
        private async Task<bool> IsGlobalDocumentAsync(NpgsqlConnection conn, string documentId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT is_global
                FROM documents
                WHERE id = @documentId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("documentId", documentId);

            var result = await cmd.ExecuteScalarAsync(cancellationToken); // ilk satırın ilk kolonunu döndürür
            return result != null && (bool)result;
        }
        private async Task<bool> HasDirectAccessAsync(NpgsqlConnection conn, string userId, string documentId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM document_access
                WHERE user_id = @userId 
                    AND document_id = @documentId
                    AND is_active = true
                    AND (expires_at IS NULL OR expires_at > NOW())";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("documentId", documentId);

            var count = (long)( await cmd.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }
        private async Task<bool> HasOrganizationAccessAsync(NpgsqlConnection conn, string userId, string documentId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM documents d
                JOIN user_organizations uo ON d.organization_id = uo.organization_id
                WHERE uo.user_id = @userId 
                    AND d.id = @documentId
                    AND uo.is_active = true";
            
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("documentId", documentId);

            var count = (long)( await cmd.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }
        private async Task<bool> HasRoleAccessAsync(NpgsqlConnection conn, string userId, string documentId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM document_role_access dra
                JOIN user_roles ur ON dra.role_name = ur.role_name
                WHERE ur.user_id = @userId 
                    AND dra.document_id = @documentId
                    AND ur.is_active = true";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("documentId", documentId);

            var count = (long)( await cmd.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }
        private async Task<bool> CanAccessDocumentInternalAsync(NpgsqlConnection conn, AccessDomain accessDomain, string documentId, CancellationToken cancellationToken)
        {
            // global dokümanlara herkes erişebilir
            if (await IsGlobalDocumentAsync(conn, documentId, cancellationToken))
                return true;

            // doğrudan erişim
            if (accessDomain.AllowedDocumentIds.Contains(documentId))
                return true;

            // organizasyon erişimi
            if (await HasOrganizationAccessAsync(
                conn, accessDomain.UserId, documentId, cancellationToken))
                return true;

            // rol bazlı erişim
            if (await HasRoleAccessAsync(
                conn, accessDomain.UserId, documentId, cancellationToken))
                return true;

            return false;
        }
    }
}