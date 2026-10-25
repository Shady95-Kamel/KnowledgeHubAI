using KnowledgeHubAI.Application.DTOs.Documents;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.UploadDocument;

public sealed record UploadDocumentCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSize,
    Stream FileStream)
    : IRequest<DocumentUploadDto>;