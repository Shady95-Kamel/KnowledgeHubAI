
namespace KnowledgeHubAI.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(
        T message,
        CancellationToken cancellationToken = default)
        where T : class;
}
