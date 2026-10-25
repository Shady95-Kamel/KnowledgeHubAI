using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Enums
{
    public enum UserStatus
    {
        PendingVerification = 1,
        Active = 2,
        Locked = 3,
        Deleted = 4
    }
}
