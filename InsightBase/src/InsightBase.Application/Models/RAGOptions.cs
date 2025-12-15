namespace InsightBase.Application.Models
{
    public class RAGOptions
    {
        public bool IncludeMetadata { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool UseStreaming { get; set; } = false;
        public int MaxSources { get; set; } = 10;
    }
}