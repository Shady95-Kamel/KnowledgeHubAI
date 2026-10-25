
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeHubAI.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository
    : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken)
    {
        await _context.PasswordResetTokens.AddAsync(
            token,
            cancellationToken);
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return _context.PasswordResetTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(
                token => token.Token == tokenHash,
                cancellationToken);
    }
}
