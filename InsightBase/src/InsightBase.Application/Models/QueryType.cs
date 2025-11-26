namespace InsightBase.Application.Models
{
    public enum QueryType //QueryAnalyzer tarafından belirlenen sorgu türüdür.
    {
        Unknown = 0, 
        Semantic = 1, //Kullanıcı tam metin arıyor
        Keyword = 2, //Kullanıcı belirli anahtar kelimelerle arama yapıyor
        Structured = 3, //Kullanıcı belirli alanlarda arama yapıyor
        Hybrid = 4, //Kullanıcı hem tam metin hem de yapılandırılmış alanlarda arama yapıyor
        complex = 5, //LLMExtractor prompt'taki gibi
        simple = 6,
        multi_part = 7
    }
}