
using KnowledgeHubAI.Domain.Entities;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken);

    Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);
}
