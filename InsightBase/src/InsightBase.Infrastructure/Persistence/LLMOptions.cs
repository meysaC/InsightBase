namespace InsightBase.Infrastructure.Persistence
{
    public class LLMOptions
    {
        public string Provider { get; set; } = "OpenAI";
        public string Model { get; set; } = "gpt-4o-mini";
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.1;
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
    }
}