
using FluentValidation;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPass;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPassس;

public sealed class ResetPasswordCommandValidator
    : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation does not match.");
    }
}
