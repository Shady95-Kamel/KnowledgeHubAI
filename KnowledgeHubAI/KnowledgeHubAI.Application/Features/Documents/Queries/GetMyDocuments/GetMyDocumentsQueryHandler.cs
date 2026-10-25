using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs.Documents;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Pagination;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Queries.GetMyDocuments;

public sealed class GetMyDocumentsQueryHandler
    : IRequestHandler<
        GetMyDocumentsQuery,
        Result<PagedResult<DocumentDto>>>
{
    private readonly IDocumentRepository _documentRepository;

    public GetMyDocumentsQueryHandler(
        IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<Result<PagedResult<DocumentDto>>> Handle(
        GetMyDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result<PagedResult<DocumentDto>>.Failure(
                new Error(
                    "Documents.InvalidUserId",
                    "The user ID is invalid."));
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        DocumentStatus? status = null;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<DocumentStatus>(
                    request.Status,
                    true,
                    out var parsedStatus)
                || !Enum.IsDefined(parsedStatus)
                || parsedStatus == DocumentStatus.Deleted)
            {
                return Result<PagedResult<DocumentDto>>.Failure(
                    new Error(
                        "Documents.InvalidStatus",
                        "The specified document status is invalid."));
            }

            status = parsedStatus;
        }

        var pagination = new PaginationRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _documentRepository.GetByUserIdAsync(
            request.UserId,
            pagination,
            request.SearchTerm?.Trim(),
            status,
            request.SortBy,
            request.Descending,
            cancellationToken);

        var dtoResult = new PagedResult<DocumentDto>
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(document =>
                new DocumentDto(
                    document.Id,
                    document.Name,
                    document.OriginalFileName,
                    document.ContentType,
                    document.FileSize,
                    document.Status,
                    document.PageCount,
                    document.CreatedAt))
        };

        return Result<PagedResult<DocumentDto>>.Success(dtoResult);
    }
}