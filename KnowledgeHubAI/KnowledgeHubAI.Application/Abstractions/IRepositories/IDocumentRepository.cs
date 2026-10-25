using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Pagination;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories;

public interface IDocumentRepository
{
    Task<PagedResult<Document>> GetByUserIdAsync(
        Guid userId,
        PaginationRequest pagination,
        string? searchTerm,
        DocumentStatus? status,
        string? sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task<Document?> GetByIdAndUserIdAsync(
    Guid documentId,
    Guid userId,
    CancellationToken cancellationToken);

    Task AddAsync(
      Document document,
      CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
     Document document,
     CancellationToken cancellationToken = default);
}