
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeHubAI.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository
    : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationTokenRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken)
    {
        await _context.EmailVerificationTokens.AddAsync(
            token,
            cancellationToken);
    }

    public Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return _context.EmailVerificationTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(
                token => token.Token == tokenHash,
                cancellationToken);
    }
}
