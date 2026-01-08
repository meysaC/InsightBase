namespace InsightBase.Domain.Entities.Chat
{
    public class MessageFeedback
    {
        public bool IsHelpful { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}