namespace InsightBase.Api.DTOs
{
    public class UpdateDocumentRequest
    {
        public string FileName { get; set; }
        public string? LegalArea { get; set; }
        public bool IsPublic { get; set; }
        
    }
}