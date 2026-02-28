namespace InsightBase.Domain.Enum
{
    public enum DocumentSourceType
    {
        System = 0, // InsightBase tarafından sağlanan gömülü belgeler
        UserUpload = 1, // Kullanıcıların yüklediği belgeler
        ChatMemory = 2, // Chat geçmişinden türetilen belgeler
        Generated = 3 // LLM tarafından oluşturulan belgeler ( summary, analysis vb)
    }
}