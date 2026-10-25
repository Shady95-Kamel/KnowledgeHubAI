namespace KnowledgeHubAI.Application.DTOs;

public sealed record RetryDocumentProcessingRequested(
    Guid DocumentId,
    Guid UserId,
    DateTime RequestedAtUtc);