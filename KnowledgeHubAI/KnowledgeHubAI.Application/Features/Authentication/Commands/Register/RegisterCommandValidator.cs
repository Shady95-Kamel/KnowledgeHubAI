using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Register
{
    public sealed class RegisterCommandValidator
     : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]")
                .WithMessage(
                    "Password must contain at least one uppercase letter.")
                .Matches("[a-z]")
                .WithMessage(
                    "Password must contain at least one lowercase letter.")
                .Matches("[0-9]")
                .WithMessage(
                    "Password must contain at least one number.");
        }
    }
}
