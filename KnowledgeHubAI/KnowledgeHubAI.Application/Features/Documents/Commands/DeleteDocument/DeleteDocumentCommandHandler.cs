using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.DeleteDocument;

public sealed class DeleteDocumentCommandHandler
    : IRequestHandler<DeleteDocumentCommand, Result<bool>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IVectorStoreService _vectorStoreService;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IObjectStorageService objectStorageService,
        IVectorStoreService vectorStoreService)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _objectStorageService = objectStorageService;
        _vectorStoreService = vectorStoreService;
    }

    public async Task<Result<bool>> Handle(
        DeleteDocumentCommand request,
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

        // 1. Remove vectors belonging to this document and user.
        await _vectorStoreService.DeleteDocumentChunksAsync(
            document.Id,
            document.UserId,
            cancellationToken);

        // 2. Remove the physical PDF from Cloudflare R2.
        await _objectStorageService.DeleteAsync(
            document.BlobName,
            cancellationToken);

        // 3. Remove the database entity.
        await _documentRepository.DeleteAsync(
            document,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<bool>.Success(true);
    }
}