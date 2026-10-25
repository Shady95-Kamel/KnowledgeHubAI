using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class Conversation : AuditableEntity
    {
        public Guid UserId { get; set; }

        public string Title { get; set; } = null!;

        public User User { get; set; } = null!;

        public ICollection<Message> Messages { get; set; }
            = new List<Message>();

        public ICollection<ConversationDocument> ConversationDocuments { get; set; }
            = new List<ConversationDocument>();
    }
}
