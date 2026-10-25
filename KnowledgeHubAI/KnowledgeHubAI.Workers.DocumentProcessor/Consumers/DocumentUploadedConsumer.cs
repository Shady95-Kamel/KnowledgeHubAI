
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.Infrastructure.Services;
using KnowledgeHubAI.Workers.DocumentProcessor.Processing;
using MassTransit;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace KnowledgeHubAI.Workers.DocumentProcessor.Consumers;

public sealed class DocumentUploadedConsumer
    : IConsumer<DocumentUploadedEvent>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly QdrantConnectionService _qdrantService;
    private readonly TextChunker _textChunker;
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(
        IDocumentRepository documentRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork,
        QdrantConnectionService qdrantService,
        TextChunker textChunker,
        ILogger<DocumentUploadedConsumer> logger)
    {
        _documentRepository = documentRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
        _qdrantService = qdrantService;
        _textChunker = textChunker;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<DocumentUploadedEvent> context)
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
                "Document {DocumentId} was not found.",
                message.DocumentId);

            return;
        }

        // Ignore duplicate messages for processed or deleted documents.
        if (document.Status == DocumentStatus.Ready ||
            document.Status == DocumentStatus.Deleted)
        {
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await using var pdfStream = new MemoryStream();

            await _objectStorageService.DownloadToStreamAsync(
                document.BlobName,
                pdfStream,
                cancellationToken);

            pdfStream.Position = 0;

            using var pdf = PdfDocument.Open(pdfStream);

            var pages = pdf.GetPages()
                .Select(page => (
                    PageNumber: page.Number,
                    Text: page.Text))
                .ToList();

            if (!pages.Any(page =>
                    !string.IsNullOrWhiteSpace(page.Text)))
            {
                throw new InvalidOperationException(
                    "No extractable text was found in the PDF.");
            }

            var chunks = _textChunker.CreateChunks(
                document.Id,
                document.UserId,
                pages);

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "No text chunks were generated from the PDF.");
            }

            // Ensure the target collection exists.
            await _qdrantService.EnsureCollectionAsync(
                cancellationToken);

            // Convert the Worker chunk model to the Infrastructure input.
            var qdrantChunks = chunks
                .Select(chunk => (
                    chunk.DocumentId,
                    chunk.UserId,
                    chunk.ChunkIndex,
                    chunk.PageNumber,
                    chunk.Content))
                .ToArray();

            await _qdrantService.UpsertChunksAsync(
                qdrantChunks,
                cancellationToken);

            // Mark the document Ready only after indexing succeeds.
            document.PageCount = pdf.NumberOfPages;
            document.Status = DocumentStatus.Ready;

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Document {DocumentId} processed successfully. " +
                "Pages: {PageCount}, Chunks: {ChunkCount}",
                document.Id,
                document.PageCount,
                chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process or index document {DocumentId}.",
                message.DocumentId);

            document.Status = DocumentStatus.Failed;

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            throw;
        }
    }
}
