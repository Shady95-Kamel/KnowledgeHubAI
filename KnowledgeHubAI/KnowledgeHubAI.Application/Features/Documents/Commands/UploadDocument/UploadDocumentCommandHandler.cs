
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Application.DTOs.Documents;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.UploadDocument;

public sealed class UploadDocumentCommandHandler
    : IRequestHandler<UploadDocumentCommand, DocumentUploadDto>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork,
         IEventPublisher eventPublisher)
    {
        _documentRepository = documentRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<DocumentUploadDto> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var documentId = Guid.NewGuid();

        var originalFileName = Path.GetFileName(request.FileName);
        var objectKey = $"{request.UserId}/{documentId}.pdf";

        var document = new Document
        {
            Id = documentId,
            UserId = request.UserId,
            Name = Path.GetFileNameWithoutExtension(originalFileName),
            OriginalFileName = originalFileName,
            BlobName = objectKey,
            ContentType = "application/pdf",
            FileSize = request.FileSize,
            Status = DocumentStatus.Uploading
        };

        // 1. Upload the PDF to Cloudflare R2.
        await _objectStorageService.UploadAsync(
            objectKey,
            request.FileStream,
            "application/pdf",
            cancellationToken);

        try
        {
            // 2. Save document metadata in SQL Server.
            await _documentRepository.AddAsync(
                document,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            // Remove the uploaded file if saving metadata fails.
            try
            {
                await _objectStorageService.DeleteAsync(
                    objectKey,
                    cancellationToken);
            }
            catch
            {
                // Preserve the original exception.
            }

            throw;
        }

        // 3. Notify the Document Processor Worker.
        await _eventPublisher.PublishAsync(
            new DocumentUploadedEvent(
                document.Id,
                document.UserId),
            cancellationToken);

        // 4. Return the upload result.
        return new DocumentUploadDto(
            document.Id,
            document.Name,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.Status.ToString(),
            document.CreatedAt);
    }
}
