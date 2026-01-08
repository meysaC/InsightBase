namespace InsightBase.Domain.Entities.Chat
{
    public class CitationInfo
    {
        public string CitationText { get; set; } = string.Empty;
        public int CitationIndex { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? Court { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Url { get; set; }
    }
}