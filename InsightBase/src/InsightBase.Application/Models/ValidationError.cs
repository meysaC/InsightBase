using InsightBase.Application.Models.Enum;

namespace InsightBase.Application.Models
{
    public class ValidationError
    {
        public ErrorType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Location { get; set; }
        public string? Details { get; set; }
    }
}