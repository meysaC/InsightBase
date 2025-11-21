namespace InsightBase.Application.Models
{
    public class RegexExtractionResult // Regex tabanlı bilgi çıkarımı sonuçları
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public List<string> LawReferences { get; set; } = new();
        public List<string> Courts { get; set; } = new();
        public List<string> FileNumbers { get; set; } = new();
        public List<string> LegalAreas { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}