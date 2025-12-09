using System.Security.AccessControl;

namespace InsightBase.Application.Models
{
    public class AccessDomain
    {
        public string UserId { get; set; } = string.Empty;
        public bool IncludeGlobalData { get; set; } = true;
        public List<string> UserOrganizationIds { get; set; } = new();
        public List<string> UserRoles { get; set; } = new();
        public List<string> AllowedDocumentIds { get; set; } = new();
        public Dictionary<string, AccessRule> OrganizationAccessRules { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}