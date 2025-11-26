namespace InsightBase.Application.Models
{
    public class ExtractionSource // Bilgi çıkarımının kaynağı (debugging için)
    {
        public bool RegexUsed { get; set; } = false;
        public bool LLMUsed { get; set; } = false;
        public string? LLMRawJson { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}