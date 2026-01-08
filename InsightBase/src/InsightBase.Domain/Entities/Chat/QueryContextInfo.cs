namespace InsightBase.Domain.Entities.Chat
{
    public class QueryContextInfo
    {
        public List<string> Intents { get; set; } = new();
        public List<string> LegalAreas { get; set; } = new();
        public List<string> LawReferences { get; set; } = new();
        public List<string> Courts { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }
}