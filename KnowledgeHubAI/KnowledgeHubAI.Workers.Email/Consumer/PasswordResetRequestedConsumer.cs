
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Workers.Email.Services.Abstractions;
using MassTransit;

namespace KnowledgeHubAI.Workers.Email.Consumers;

public sealed class PasswordResetRequestedConsumer
    : IConsumer<PasswordResetRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetRequestedConsumer> _logger;

    public PasswordResetRequestedConsumer(
        IEmailService emailService,
        ILogger<PasswordResetRequestedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<PasswordResetRequested> context)
    {
        var message = context.Message;

        await _emailService.SendPasswordResetEmailAsync(
            message.Email,
            message.FirstName,
            message.ResetToken,
            context.CancellationToken);

        _logger.LogInformation(
            "Password reset email processed for UserId {UserId}",
            message.UserId);
    }
}
