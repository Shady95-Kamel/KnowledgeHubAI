using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class Message : Entity
    {
        public Guid ConversationId { get; set; }

        public MessageRole Role { get; set; }

        public string Content { get; set; } = null!;

        public int? PromptTokens { get; set; }

        public int? CompletionTokens { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Conversation Conversation { get; set; } = null!;
    }
}
