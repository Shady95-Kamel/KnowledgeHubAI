
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;

namespace KnowledgeHubAI.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task AddRefreshTokenAsync(
    RefreshToken refreshToken,
    CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(
            refreshToken,
            cancellationToken);
    }
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
