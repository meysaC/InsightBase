namespace InsightBase.Application.Models
{
    public class QueryContext // birleştirilmiş ve zenginleştirilmiş sorgu bağlamı
                                // hem LLM hem Regex sonuçlarını içerir
    {
        // Metadata
        public string OriginalQuery { get; set; } = string.Empty;
        public string? UserId { get; set; } = string.Empty;
        public DateTime QueryTimeStamp { get; set; } = DateTime.UtcNow;


        // Intent and Type
        public List<string> Intents { get; set; } = new();
        public string QueryType { get; set; } = "simple"; //QueryType QueryType.Unknown??????????????????????? -> llmextractor prompt şu şekilde!!!-> ""complex"" | ""simple"" | ""multi_part""
        public double ConfidenceScore { get; set; } = 0.0;


        //Legal domain
        public List<string> LegalAreas { get; set; } = new();
        public List<string> LegalConcepts { get; set; } = new();


        // References
        public List<string> LawReferences { get; set; } = new();
        public List<string> Courts { get; set; } = new();
        public List<string> FileNumbers { get; set; } = new();


        // Date Filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> DateExpressions { get; set; } = new();


        // Parties and Keywords
        public List<string> Parties { get; set; } = new();
        public List<string> Keywords { get; set; } = new();


        // Search strategy flags
        public bool RequiresCaseLaw { get; set; } = false;
        public bool RequiresLegislation { get; set; } = false;
        public bool RequiresSemanticSearch { get; set; } = true;
        public bool RequiresExactMatch { get; set; } = false;


        // source info
        public ExtractionSource Source { get; set; } = new();


        // validation
        public bool IsValid => !string.IsNullOrWhiteSpace(OriginalQuery) && Intents.Any();


        // public string OriginalQuery { get; set; } = "";
        // public string? CleanedQuery { get; set; }
        // public QueryType QueryType { get; set; } = QueryType.Unknown;
        // public RegexExtractionResult? Extraction { get; set; }
        // public bool NeedsVectorSearch { get; set; }
        // public bool NeedsKeywordSearch { get; set; }
        // public bool NeedsStructuredSearch { get; set; }
    }
}