using KnowledgeHubAI.Domain.Enums;

namespace KnowledgeHubAI.Application.DTOs.Documents;

public sealed record DocumentDto(
    Guid Id,
    string Name,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DocumentStatus Status,
    int? PageCount,
    DateTime CreatedAt
);