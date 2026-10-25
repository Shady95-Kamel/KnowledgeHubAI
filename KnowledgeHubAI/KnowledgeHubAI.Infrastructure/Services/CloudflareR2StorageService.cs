using Amazon.S3;
using Amazon.S3.Model;
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeHubAI.Infrastructure.Services;

public sealed class CloudflareR2StorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly CloudflareR2Options _options;

    public CloudflareR2StorageService(
        IAmazonS3 s3Client,
        IOptions<CloudflareR2Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,

            AutoCloseStream = false,
            AutoResetStreamPosition = false,

            // Required for Cloudflare R2 compatibility
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DownloadToStreamAsync(
        string objectKey,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(destination);

        var request = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        };

        using var response = await _s3Client.GetObjectAsync(
            request,
            cancellationToken);

        await response.ResponseStream.CopyToAsync(
            destination,
            cancellationToken);
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(
            request,
            cancellationToken);
    }
}