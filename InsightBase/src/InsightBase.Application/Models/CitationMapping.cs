using InsightBase.Application.Models.Enum;

namespace InsightBase.Application.Models
{
    public class CitationMapping
    {
        public string CitationText { get; set; } = string.Empty;
        public int CitationIndex { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
        public string ChunkId { get; set; } = string.Empty;
        public string? Court { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? FileNumber { get; set; }
        public List<string> LawReferences { get; set; } = new();
        public string? Url { get; set; }
        public int Position { get; set; } // metin içindeki yeri
    }
}