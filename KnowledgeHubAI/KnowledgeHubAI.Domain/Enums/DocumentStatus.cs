using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Enums
{
    public enum DocumentStatus
    {
        Uploading = 1,
        Processing = 2,
        Ready = 3,
        Failed = 4,
        Deleted = 5
    }
}
