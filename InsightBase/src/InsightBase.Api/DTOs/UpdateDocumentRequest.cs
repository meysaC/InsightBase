namespace InsightBase.Api.DTOs
{
    public class UpdateDocumentRequest
    {
        public string UserFileName { get; set; }
        public string? LegalArea { get; set; }
        public string? DocumentType { get; set; }
        public bool IsPublic { get; set; }
    }
}