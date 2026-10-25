using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token)
    : IRequest<Result<bool>>;
