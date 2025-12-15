using InsightBase.Application.Models.Enum;

namespace InsightBase.Application.Models
{
    public class ValidationWarning
    {
        public WarningType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public Severity Severity { get; set; }
        public string? Details { get; set; }   
    }
}