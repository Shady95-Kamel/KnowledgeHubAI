using FluentValidation;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ForgotPass;

public sealed class ForgotPasswordCommandValidator
    : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Please enter a valid email address.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.");
    }
}
