using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.RefToken
{
    public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<LoginResponse>>;
}
