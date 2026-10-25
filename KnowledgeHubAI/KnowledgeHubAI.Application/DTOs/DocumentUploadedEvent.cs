namespace KnowledgeHubAI.Application.DTOs;

public sealed record DocumentUploadedEvent(
    Guid DocumentId,
    Guid UserId);