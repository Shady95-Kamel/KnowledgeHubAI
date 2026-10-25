using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class ExternalLogin : Entity
    {
        public Guid UserId { get; set; }

        public string Provider { get; set; } = null!;

        public string ProviderUserId { get; set; } = null!;

        public string Email { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}
