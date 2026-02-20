using InsightBase.Domain.Entities.Chat;
using Microsoft.AspNetCore.Identity;

namespace InsightBase.Infrastructure.Persistence
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
       
        // public virtual ICollection<Document>? Documents { get; set; } = new List<Document>();
        public virtual ICollection<Conversation>? Conversations { get; set; } = new List<Conversation>();
        public virtual ICollection<Message>? Messages { get; set; } = new List<Message>();
    }
}