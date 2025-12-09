namespace InsightBase.Application.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();

    }
}