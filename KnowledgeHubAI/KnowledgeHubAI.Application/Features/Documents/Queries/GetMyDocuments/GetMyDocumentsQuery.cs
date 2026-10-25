using KnowledgeHubAI.Application.DTOs.Documents;
using KnowledgeHubAI.SharedKernel.Pagination;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Queries.GetMyDocuments;

public sealed record GetMyDocumentsQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? Status = null,
    string? SortBy = null,
    bool Descending = true
) : IRequest<Result<PagedResult<DocumentDto>>>;