
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeHubAI.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Token == tokenHash,
                cancellationToken);
    }

    public async Task<bool> RevokeAsync(
    string tokenHash,
    DateTime revokedAtUtc,
    CancellationToken cancellationToken = default)
    {
        var affectedRows = await _context.RefreshTokens
            .Where(x => x.Token == tokenHash && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.RevokedAt,
                    (DateTime?)revokedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }


    public async Task<bool> TryRotateAsync(
        string currentTokenHash,
        DateTime nowUtc,
        RefreshToken newRefreshToken,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var affectedRows = await _context.RefreshTokens
                .Where(x =>
                    x.Token == currentTokenHash &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.RevokedAt,
                        (DateTime?)nowUtc),
                    cancellationToken);

            if (affectedRows != 1)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return false;
            }

            await _context.RefreshTokens.AddAsync(
                newRefreshToken,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

}
