namespace InsightBase.Application.Models
{
    public class CitationMappingResult
    {
        public string OriginalAnswer { get; set; } = string.Empty;
        public List<CitationMapping> Citations { get; set; } = new();
        public int TotalCitations { get; set; }
        public int UniqueSources { get; set; }
    }
}