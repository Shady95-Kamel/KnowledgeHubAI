using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class ConversationDocument : Entity
    {
        public Guid ConversationId { get; set; }

        public Guid DocumentId { get; set; }

        public Conversation Conversation { get; set; } = null!;

        public Document Document { get; set; } = null!;
    }
}
