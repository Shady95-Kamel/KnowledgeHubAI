using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Application.DTOs.Documents;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.RetryDocument;

public sealed class RetryDocumentCommandHandler
    : IRequestHandler<RetryDocumentCommand, Result<bool>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEventPublisher _eventPublisher;

    public RetryDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IEventPublisher eventPublisher)
    {
        _documentRepository = documentRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<bool>> Handle(
        RetryDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DocumentId == Guid.Empty ||
            request.UserId == Guid.Empty)
        {
            return Result<bool>.Failure(
                new Error(
                    "Documents.InvalidRequest",
                    "The document ID or user ID is invalid."));
        }

        var document =
            await _documentRepository.GetByIdAndUserIdAsync(
                request.DocumentId,
                request.UserId,
                cancellationToken);

        if (document is null)
        {
            return Result<bool>.Failure(
                new Error(
                    "Documents.NotFound",
                    "The document was not found."));
        }

        if (document.Status != DocumentStatus.Failed)
        {
            return Result<bool>.Failure(
                new Error(
                    "Documents.InvalidRetryStatus",
                    "Only failed documents can be retried."));
        }

        var message = new RetryDocumentProcessingRequested(
            DocumentId: document.Id,
            UserId: document.UserId,
            RequestedAtUtc: DateTime.UtcNow);

        await _eventPublisher.PublishAsync(
            message,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}