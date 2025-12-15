using InsightBase.Application.Models.Enum;

namespace InsightBase.Application.Models
{
    public class SearchResult
    {
        // Identification
        public string DocumentId { get; set; } = string.Empty;
        public string ChunkId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Title { get; set; } = string.Empty;

        // Content
        public string Content { get; set; } = string.Empty;
        public string? MergedContent { get; set; }
        public bool IsMergedWithNext { get; set; }

        // Scores
        public double VectorScore { get; set; }
        public double BM25Score { get; set; }
        public double MetadataScore { get; set; }
        public double ExactMatchScore { get; set; }
        public double FinalScore { get; set; }
        public double Relevance { get; set; }

        // Metadata
        public DocumentType DocumentType { get; set; }
        public string LegalArea { get; set; } = string.Empty;
        public string? Court { get; set; }
        public string? FileNumber { get; set; }
        public DateTime? PublishDate { get; set; }
        public List<string> LawReferences { get; set; } = new();
        public string? Url { get; set; }

        // Access control
        public bool IsGlobal { get; set; }
        public string? OrganizationId { get; set; }

        // Amendment tracking
        public bool IsAmended { get; set; }
        public DateTime? AmendmentDate { get; set; }
    }
}