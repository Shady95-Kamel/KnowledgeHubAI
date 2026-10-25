
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken)
    : IRequest<Result<bool>>;
