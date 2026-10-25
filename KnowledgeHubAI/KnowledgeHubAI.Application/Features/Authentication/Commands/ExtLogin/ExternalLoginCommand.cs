using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ExtLogin;

public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderUserId,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerified)
    : IRequest<Result<LoginResponse>>;
