namespace KnowledgeHubAI.Application.DTOs.Documents;

public sealed record DocumentUploadDto(
    Guid Id,
    string Name,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string Status,
    DateTime CreatedAt);