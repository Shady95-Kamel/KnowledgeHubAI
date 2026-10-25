
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeHubAI.Infrastructure.Repositories;

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly ApplicationDbContext _context;

    public ExternalLoginRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ExternalLogin?> GetByProviderAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<ExternalLogin>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Provider == provider &&
                     x.ProviderUserId == providerUserId,
                cancellationToken);
    }

    public async Task AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<ExternalLogin>()
            .AddAsync(externalLogin, cancellationToken);
    }
}
