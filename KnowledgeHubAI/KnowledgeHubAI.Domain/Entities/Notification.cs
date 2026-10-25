using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class Notification : Entity
    {
        public Guid UserId { get; set; }

        public string Title { get; set; } = null!;

        public string Message { get; set; } = null!;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        public User User { get; set; } = null!;
    }
}
