using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.DTOs
{
    public sealed record UserRegistered(
     Guid UserId,
     string Email,
     string FirstName,
     string VerificationToken,
     DateTime TokenExpiresAt
 );
}
