namespace InsightBase.Application.Models
{
    public class RegexExtractionResult // Regex tabanlı bilgi çıkarımı sonuçları
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public List<string> LawReferences { get; set; } = new(); // tck, tbk, cmk, hmk ... gibi
        public List<string> Courts { get; set; } = new(); // ... Dairesi gibi
        public List<string> FileNumbers { get; set; } = new(); // E. 2021/1234 gibi dosya numaraları
        public List<string> LegalAreas { get; set; } = new(); // ceza, medeni, iş, ticaret... hukuku gibi
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}