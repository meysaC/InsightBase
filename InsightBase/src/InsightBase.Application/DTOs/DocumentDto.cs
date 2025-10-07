namespace InsightBase.Application.DTOs
{
    public class DocumentDto //ortak kullanılcak
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string UserFileName { get; set; } = string.Empty;
        //public string FilePath { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? LegalArea { get; set; }
        public bool IsPublic { get; set; }

    }
}