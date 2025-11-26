namespace InsightBase.Application.Models
{
    public class LLMJsonResponse //LLM API yanıtı 
    {
        public string RawJson { get; set; } = string.Empty;
        public Dictionary<string, string>? Fields { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}