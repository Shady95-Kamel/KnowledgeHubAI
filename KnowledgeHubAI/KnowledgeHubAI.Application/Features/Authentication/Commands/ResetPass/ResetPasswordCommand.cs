
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPass;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Result<bool>>;
