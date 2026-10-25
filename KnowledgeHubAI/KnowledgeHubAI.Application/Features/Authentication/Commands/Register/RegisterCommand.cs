using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Register
{
    public sealed record RegisterCommand(
     string FirstName,
     string LastName,
     string Email,
     string Password
 ) : IRequest<Result<Guid>>;
}
