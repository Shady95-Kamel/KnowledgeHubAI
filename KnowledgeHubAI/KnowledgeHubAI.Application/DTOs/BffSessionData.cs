using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.DTOs
{
    public sealed record BffSessionData(
     Guid UserId,
     string Email,
     string Role,
     string AccessToken,
     string RefreshToken,
     DateTime AccessTokenExpiresAt,
     DateTime RefreshTokenExpiresAt,
     DateTime CreatedAtUtc);
}
