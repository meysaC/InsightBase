namespace InsightBase.Application.Models
{
    public class RAGResponse
    {
        public bool Success { get; set; }
        public string Query { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public QueryContext? QueryContext { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? EnhancedAnswer { get; set; }
        public List<CitationMapping> Citations { get; set; } = new();
        public string? CitationSummary { get; set; }
        public List<SearchResult> Sources { get; set; } = new();
        public int SourceCount { get; set; }
        public ValidationResult? ValidationResult { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}