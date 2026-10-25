using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.Infrastructure.Persistence;
using KnowledgeHubAI.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeHubAI.Infrastructure.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
    Document document,
    CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(
            document,
            cancellationToken);
    }

    public Task<bool> DeleteAsync(
      Document document,
      CancellationToken cancellationToken = default)
    {
        _context.Documents.Remove(document);

        return Task.FromResult(true);
    }


    public async Task<PagedResult<Document>> GetByUserIdAsync(
        Guid userId,
        PaginationRequest pagination,
        string? searchTerm,
        DocumentStatus? status,
        string? sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = _context.Documents
            .AsNoTracking()
            .Where(document =>
                document.UserId == userId &&
                !document.IsDeleted &&
                document.Status != DocumentStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(document =>
                document.Name.Contains(searchTerm) ||
                document.OriginalFileName.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(document =>
                document.Status == status.Value);
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, pagination.PageNumber);
        var pageSize = Math.Clamp(pagination.PageSize, 1, 100);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Document>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Document?> GetByIdAndUserIdAsync(
    Guid documentId,
    Guid userId,
    CancellationToken cancellationToken)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(
                document =>
                    document.Id == documentId &&
                    document.UserId == userId &&
                    !document.IsDeleted &&
                    document.Status != DocumentStatus.Deleted,
                cancellationToken);
    }

    private static IQueryable<Document> ApplySorting(
        IQueryable<Document> query,
        string? sortBy,
        bool descending)
    {
        return (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => descending
                ? query.OrderByDescending(document => document.Name)
                : query.OrderBy(document => document.Name),

            "filesize" => descending
                ? query.OrderByDescending(document => document.FileSize)
                : query.OrderBy(document => document.FileSize),

            "status" => descending
                ? query.OrderByDescending(document => document.Status)
                : query.OrderBy(document => document.Status),

            "createdat" or null or "" => descending
                ? query.OrderByDescending(document => document.CreatedAt)
                : query.OrderBy(document => document.CreatedAt),

            _ => descending
                ? query.OrderByDescending(document => document.CreatedAt)
                : query.OrderBy(document => document.CreatedAt)
        };
    }
}