using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Workers.Email.Services.Abstractions;
using MassTransit;

namespace KnowledgeHubAI.Workers.Email.Consumers;

public sealed class UserRegisteredConsumer
    : IConsumer<UserRegistered>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IEmailService emailService,
        ILogger<UserRegisteredConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<UserRegistered> context)
    {
        var message = context.Message;

        await _emailService.SendVerificationEmailAsync(
            message.Email,
            message.FirstName,
            message.VerificationToken,
            context.CancellationToken);

        _logger.LogInformation(
            "UserRegistered event processed for UserId {UserId}",
            message.UserId);
    }
}
