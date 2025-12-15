using InsightBase.Application.Models.Enum;

namespace InsightBase.Application.Models
{
    public class RAGStreamChunk
    {
        public StreamChunkType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public object? Metadata { get; set; }
    }
}