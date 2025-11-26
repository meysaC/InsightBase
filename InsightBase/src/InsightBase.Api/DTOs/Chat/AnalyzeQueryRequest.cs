namespace InsightBase.Api.DTOs.Chat
{
    public class AnalyzeQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }
}