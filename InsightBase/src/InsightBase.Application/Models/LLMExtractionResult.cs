namespace InsightBase.Application.Models
{
    public class LLMExtractionResult // LLM tabanlı bilgi çıkarımı sonuçları
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public string RawJson { get; set; } = string.Empty;
        public List<string> Intents { get; set; } = new();
        public List<string> LegalAreas { get; set; } = new();
        public List<string> LawReferences { get; set; } = new();
        public List<string> Courts { get; set; } = new();
        public List<string> DateExpressions { get; set; } = new();
        public List<string> LegalConcepts { get; set; } = new();
        public List<string> Parties { get; set; } = new(); //hangi taraf olunduğu
        public List<string> Keywords { get; set; } = new();
        public QueryType QueryType { get; set; }
        public bool RequiresCaseLaw { get; set; }
        public bool RequiresLegislation { get; set; }
        public double ConfidenceScore { get; set; }
        public bool ExtractionFailed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}