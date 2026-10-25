
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace KnowledgeHubAI.Workers.DocumentProcessor.Consumers;

public sealed class RetryDocumentProcessingConsumer
    : IConsumer<RetryDocumentProcessingRequested>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RetryDocumentProcessingConsumer> _logger;

    public RetryDocumentProcessingConsumer(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        ILogger<RetryDocumentProcessingConsumer> logger)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<RetryDocumentProcessingRequested> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var document =
            await _documentRepository.GetByIdAndUserIdAsync(
                message.DocumentId,
                message.UserId,
                cancellationToken);

        if (document is null)
        {
            _logger.LogWarning(
                "Retry requested for missing document {DocumentId}.",
                message.DocumentId);

            return;
        }

        if (document.Status != DocumentStatus.Failed)
        {
            _logger.LogInformation(
                "Skipping retry for document {DocumentId}. Current status: {Status}",
                document.Id,
                document.Status);

            return;
        }

        // Reserve the document for processing before publishing the next event.
        document.Status = DocumentStatus.Processing;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await context.Publish(
                new DocumentUploadedEvent(
                    DocumentId: document.Id,
                    UserId: document.UserId),
                cancellationToken);

            _logger.LogInformation(
                "Retry event forwarded for document {DocumentId}.",
                document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish processing event for document {DocumentId}.",
                document.Id);

            // Restore the failed state so the retry can be attempted again.
            document.Status = DocumentStatus.Failed;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
