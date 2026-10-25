
using KnowledgeHubAI.Domain.Entities;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByProviderAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default);
}
