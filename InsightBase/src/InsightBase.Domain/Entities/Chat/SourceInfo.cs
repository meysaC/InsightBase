namespace InsightBase.Domain.Entities.Chat
{
    public class SourceInfo
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string LegalArea { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public string? Court { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Url { get; set; }
        public List<string> LawReferences { get; set; } = new();
    }
}