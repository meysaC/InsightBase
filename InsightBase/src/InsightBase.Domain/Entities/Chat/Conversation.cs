using InsightBase.Domain.Enum;

namespace InsightBase.Domain.Entities.Chat
{
    public class Conversation
    {
        public string ConversationId { get; private set; }
        public string UserId { get; private set; }
        public string Title { get; private set; }
        public ConversationStatus Status { get; private set; }
        
         // Metadata
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? LastMessageAt { get; private set; }
        
        // Statistics
        public int MessageCount { get; private set; }
        public int TokensUsed { get; private set; }
        
        // Context
        public List<string> LegalAreas { get; private set; } = new();
        public List<string> RelevantLaws { get; private set; } = new();
        
        // Messages collection
        private readonly List<Message> _messages = new();
        public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
        
        // Settings
        public ConversationSettings Settings { get; private set; }
        
        // Metadata as JSON
        public Dictionary<string, object> Metadata { get; private set; } = new();


        // BUSINESS METHODS
        public static Conversation Create(string userId, string? title = null)
        {
            var conversation = new Conversation
            {
                ConversationId = $"conv_{Guid.NewGuid()}",
                UserId = userId,
                Title = title ?? "Yeni Sohbet",
                Status = ConversationStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MessageCount = 0,
                TokensUsed = 0,
                Settings = ConversationSettings.Default()
            };
            return conversation;
        }
        public Message AddUserMessage(string content, List<string>? attechmentIds = null)
        {
            var message = Message.CreateUserMessage(ConversationId, UserId, content, _messages.Count, attechmentIds);

            _messages.Add(message);
            MessageCount++;
            LastMessageAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // ilk messaj ise title güncelle
            if(MessageCount == 1 && Title == "Yeni Sohbet")
            {
                Title = content.Length > 50 
                        ? content.Substring(0, 47) + "..."
                        : content;
            }

            return message;
        }
        public Message AddAssistantMessage(string content, List<CitationInfo> citations, List<SourceInfo> sources, QueryContextInfo queryContext, int tokensUsed)
        {
            var message = Message.CreateAssistantMessage(ConversationId, content, _messages.Count, citations, sources, queryContext, tokensUsed);

            _messages.Add(message);
            MessageCount++;
            TokensUsed += tokensUsed;
            LastMessageAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // legal areas laws güncelle
            UpdateConversationContext(queryContext);

            return message;
        }
        public void UpdateTitle(string newTitle)
        {
            if(string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("Title cannot be empty.", nameof(newTitle));

            Title = newTitle;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Archive()
        {
            Status = ConversationStatus.Archived;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Delete()
        {
            Status = ConversationStatus.Deleted;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Restore()
        {
            if(Status == ConversationStatus.Deleted)
            {
                Status = ConversationStatus.Active;
                UpdatedAt = DateTime.UtcNow;
            }
        }
        private void UpdateConversationContext(QueryContextInfo queryContext)
        {
            // legal areas
            foreach(var area in queryContext.LegalAreas)
            {
                if(!LegalAreas.Contains(area))
                {
                    LegalAreas.Add(area);
                }
            }
            // laws
            foreach(var law in queryContext.LawReferences)
            {
                if(!RelevantLaws.Contains(law))
                {
                    RelevantLaws.Add(law);
                }
            }
        }
        public void UpdateSettings(ConversationSettings settings)
        {
            Settings = settings;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}