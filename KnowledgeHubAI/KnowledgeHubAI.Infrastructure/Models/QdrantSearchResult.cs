namespace KnowledgeHubAI.Infrastructure.Models;

public sealed record QdrantSearchResult(
    Guid DocumentId,
    Guid UserId,
    int PageNumber,
    int ChunkIndex,
    string Text,
    double Score);