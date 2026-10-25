public interface IVectorStoreService
{
    Task DeleteDocumentChunksAsync(
        Guid documentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}