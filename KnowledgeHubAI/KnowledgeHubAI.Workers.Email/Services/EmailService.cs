
using KnowledgeHubAI.Workers.Email.Configuration;
using KnowledgeHubAI.Workers.Email.Services.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KnowledgeHubAI.Workers.Email.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl =
          $"{_options.FrontendBaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(verificationToken)}";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.SenderName,
                _options.SenderEmail));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject = "Verify your KnowledgeHubAI account";

        var body = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Hello {System.Net.WebUtility.HtmlEncode(firstName)},</h2>
                <p>Thank you for registering with KnowledgeHubAI.</p>
                <p>Please click the link below to verify your email address:</p>
                <p><a href="{System.Net.WebUtility.HtmlEncode(verificationUrl)}">Verify Email</a></p>
                <p>This link expires in 24 hours.</p>
                """
        };

        message.Body = body.ToMessageBody();

        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "Verification email sent to {RecipientEmail}",
                recipientEmail);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    true,
                    cancellationToken);
            }
        }
    }

    public async Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetUrl =
            $"{_options.FrontendBaseUrl.TrimEnd('/')}/resetpassword?token={Uri.EscapeDataString(resetToken)}";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.SenderName,
                _options.SenderEmail));

        message.To.Add(MailboxAddress.Parse(recipientEmail));

        message.Subject = "Reset your KnowledgeHubAI password";

        var safeFirstName =
            System.Net.WebUtility.HtmlEncode(firstName);

        var safeResetUrl =
            System.Net.WebUtility.HtmlEncode(resetUrl);

        var body = new BodyBuilder
        {
            HtmlBody = $"""
            <h2>Hello {safeFirstName},</h2>
            <p>We received a request to reset your password.</p>
            <p>
                <a href="{safeResetUrl}">
                    Reset Password
                </a>
            </p>
            <p>This link expires in 30 minutes and can only be used once.</p>
            <p>If you did not request this, you can ignore this email.</p>
            """
        };

        message.Body = body.ToMessageBody();

        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            _logger.LogInformation(
                "Password reset email sent to {RecipientEmail}",
                recipientEmail);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    true,
                    cancellationToken);
            }
        }
    }

}
