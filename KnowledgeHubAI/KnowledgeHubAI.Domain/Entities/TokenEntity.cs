using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public abstract class TokenEntity : Entity
    {
        public Guid UserId { get; set; }

        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public User User { get; set; } = null!;
    }
}
