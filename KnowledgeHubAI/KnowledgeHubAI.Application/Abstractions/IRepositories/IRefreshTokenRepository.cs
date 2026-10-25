
using KnowledgeHubAI.Domain.Entities;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
    string tokenHash,
    DateTime revokedAtUtc,
    CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
    string currentTokenHash,
    DateTime nowUtc,
    RefreshToken newRefreshToken,
    CancellationToken cancellationToken = default);
}
