
using KnowledgeHubAI.Application.Abstractions;
using MassTransit;

namespace KnowledgeHubAI.Infrastructure.Services;

public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<T>(
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return _publishEndpoint.Publish(
            message,
            cancellationToken);
    }
}
