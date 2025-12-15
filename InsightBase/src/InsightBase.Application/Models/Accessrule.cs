namespace InsightBase.Application.Models
{
    public class AccessRule
    {
        public string OrganizationId { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty; // e.g., "Allow", "Deny"
        public string RuleValue { get; set; } = string.Empty; // e.g., specific permissions or conditions
    }
}