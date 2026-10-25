using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ForgotPass
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;
}
