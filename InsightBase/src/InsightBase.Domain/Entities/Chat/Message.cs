using InsightBase.Domain.Enum;

namespace InsightBase.Domain.Entities.Chat
{
    public class Message
    {
        public string MessageId { get; private set; }
        public string ConversationId { get; private set; }
        public MessageRole Role { get; private set; }
        public string Content { get; private set; }
        public int SequenceNumber { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        public DateTime? EditedAt { get; private set; }
        
        // User-specific
        public string UserId { get; private set; }
        public List<string> AttachmentIds { get; private set; } = new();
        
        // Assistant-specific
        public List<CitationInfo> Citations { get; private set; } = new();
        public List<SourceInfo> Sources { get; private set; } = new();
        public QueryContextInfo? QueryContext { get; private set; }
        public int TokensUsed { get; private set; }
        public MessageStatus Status { get; private set; }
        
        // Feedback
        public MessageFeedback? Feedback { get; private set; }
        
        // Metadata
        public Dictionary<string, object> Metadata { get; private set; } = new();


        // BUSINESS METHODS
        public static Message CreateUserMessage(string conversationId, string? userId, string content, int sequenceNumber, List<string>? attachmentIds = null)
        {
            return new Message
            {
                MessageId = $"msg_{Guid.NewGuid():N}",
                ConversationId = conversationId,
                Role = MessageRole.User,
                Content = content,
                SequenceNumber = sequenceNumber,
                UserId = userId,
                AttachmentIds = attachmentIds ?? new List<string>(),
                Status = MessageStatus.Sent,
                CreatedAt = DateTime.UtcNow
            };
        }
        public static Message CreateAssistantMessage(string conversationId, string content, int sequenceNumber, List<CitationInfo> citations, List<SourceInfo> sources, QueryContextInfo queryContext, int tokensUsed)
        {
            return new Message
            {
                MessageId = $"msg_{Guid.NewGuid():N}",
                ConversationId = conversationId,
                Role = MessageRole.Assistant,
                Content = content,
                SequenceNumber = sequenceNumber,
                Citations = citations,
                Sources = sources,
                QueryContext = queryContext,
                TokensUsed = tokensUsed,
                CreatedAt = DateTime.UtcNow,
                Status = MessageStatus.Completed
            };
        }
        public void EditContent(string newContent)
        {
            Content = newContent;
            EditedAt = DateTime.UtcNow;
        }
        public void AddFeedback(bool isHelpful, string? comment = null)
        {
            Feedback = new MessageFeedback
            {
                IsHelpful = isHelpful,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
        }
        public void UpdateStatus(MessageStatus status)
        {
            Status = status;
        }
    }
}