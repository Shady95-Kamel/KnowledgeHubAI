
namespace KnowledgeHubAI.Workers.DocumentProcessor.Processing;

public sealed record DocumentTextChunk(
    Guid DocumentId,
    Guid UserId,
    int ChunkIndex,
    int PageNumber,
    string Content);
