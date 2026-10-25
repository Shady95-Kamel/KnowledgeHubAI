using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class PasswordResetToken : TokenEntity
    {
        public DateTime? UsedAt { get; set; }
    }
}
