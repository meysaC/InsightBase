namespace InsightBase.Domain.Entities.Chat
{
    public class ConversationSettings
    {
        public bool EnableCitations { get; set; } = true;
        public bool EnableSourceDisplay { get; set; } = true;
        public int MaxSourcesPerMessage { get; set; } = 5;
        public string PreferredLegalArea { get; set; } = string.Empty;

        public static ConversationSettings Default() => new()
        {
            EnableCitations = true,
            EnableSourceDisplay = true,
            MaxSourcesPerMessage = 5
        };
    }
}