using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.DTOs
{
    public sealed record RegisterResponseDto(
     Guid UserId,
     string Message);
}
