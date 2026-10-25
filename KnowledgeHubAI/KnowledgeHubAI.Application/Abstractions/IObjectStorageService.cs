namespace KnowledgeHubAI.Application.Abstractions;

public interface IObjectStorageService
{
    Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DownloadToStreamAsync(
        string objectKey,
        Stream destination,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}