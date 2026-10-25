using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<Result<LoginResponse>>;
