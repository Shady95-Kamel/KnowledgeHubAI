using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Workers.Email.Services.Abstractions
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(
            string recipientEmail,
            string firstName,
            string verificationToken,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetEmailAsync(
            string recipientEmail,
            string firstName,
            string resetToken,
            CancellationToken cancellationToken = default);
    }
}
