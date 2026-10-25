
using KnowledgeHubAI.Infrastructure.Models;
using KnowledgeHubAI.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KnowledgeHubAI.Infrastructure.Services;

public sealed class QdrantConnectionService : IVectorStoreService
{
    private readonly HttpClient _httpClient;

    public const string CollectionName = "knowledgehubai_chunks";

    public const string EmbeddingModel =
        "intfloat/multilingual-e5-small";

    public const int VectorSize = 384;

    public QdrantConnectionService(
        HttpClient httpClient,
        IOptions<QdrantOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                "Qdrant endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Qdrant API key is not configured.");
        }

        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(
            settings.Endpoint.TrimEnd('/') + "/");

        _httpClient.DefaultRequestHeaders.Remove("api-key");
        _httpClient.DefaultRequestHeaders.Add(
            "api-key",
            settings.ApiKey);
    }

    public async Task<string> GetCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "collections",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(
            cancellationToken);
    }

    public async Task EnsureCollectionAsync(
        CancellationToken cancellationToken = default)
    {
        using var checkResponse = await _httpClient.GetAsync(
            $"collections/{CollectionName}",
            cancellationToken);

        if (checkResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (checkResponse.StatusCode != HttpStatusCode.NotFound)
        {
            checkResponse.EnsureSuccessStatusCode();
        }

        var collectionConfiguration = new
        {
            vectors = new
            {
                size = VectorSize,
                distance = "Cosine"
            }
        };

        using var createResponse = await _httpClient.PutAsJsonAsync(
            $"collections/{CollectionName}",
            collectionConfiguration,
            cancellationToken);

        createResponse.EnsureSuccessStatusCode();
    }

    public async Task UpsertChunksAsync(
        IReadOnlyCollection<(
            Guid DocumentId,
            Guid UserId,
            int ChunkIndex,
            int PageNumber,
            string Content)> chunks,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 64;

        if (chunks.Count == 0)
        {
            return;
        }

        var allChunks = chunks.ToArray();

        foreach (var batch in allChunks.Chunk(batchSize))
        {
            var points = batch.Select(chunk => new
            {
                id = CreatePointId(
                    chunk.DocumentId,
                    chunk.PageNumber,
                    chunk.ChunkIndex),

                vector = new
                {
                    text = chunk.Content,
                    model = EmbeddingModel
                },

                payload = new
                {
                    documentId = chunk.DocumentId,
                    userId = chunk.UserId,
                    chunkIndex = chunk.ChunkIndex,
                    pageNumber = chunk.PageNumber,
                    text = chunk.Content
                }
            }).ToArray();

            using var response = await _httpClient.PutAsJsonAsync(
                $"collections/{CollectionName}/points?wait=true",
                new { points },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }

    private static Guid CreatePointId(
        Guid documentId,
        int pageNumber,
        int chunkIndex)
    {
        var input = $"{documentId:N}:{pageNumber}:{chunkIndex}";

        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));

        return new Guid(hash.AsSpan(0, 16));
    }


    public async Task<IReadOnlyList<QdrantSearchResult>> SearchAsync(
        string question,
        Guid userId,
        Guid? documentId = null,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question cannot be empty.",
                nameof(question));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (limit is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be between 1 and 20.");
        }

        var conditions = new List<object>
    {
        new
        {
            key = "userId",
            match = new
            {
                value = userId.ToString()
            }
        }
    };

        if (documentId.HasValue)
        {
            conditions.Add(new
            {
                key = "documentId",
                match = new
                {
                    value = documentId.Value.ToString()
                }
            });
        }

        var request = new
        {
            query = new
            {
                text = question,
                model = EmbeddingModel
            },
            filter = new
            {
                must = conditions
            },
            limit,
            with_payload = true,
            with_vector = false
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"collections/{CollectionName}/points/query",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Qdrant query failed. " +
                $"Status: {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Response: {errorBody}",
                null,
                response.StatusCode);
        }


        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var json = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);

        var results = new List<QdrantSearchResult>();

        if (!json.RootElement.TryGetProperty(
                "result",
                out var resultElement) ||
            !resultElement.TryGetProperty(
                "points",
                out var pointsElement))
        {
            return results;
        }

        foreach (var point in pointsElement.EnumerateArray())
        {
            if (!point.TryGetProperty("payload", out var payload))
            {
                continue;
            }

            if (!payload.TryGetProperty("documentId", out var docId) ||
                !payload.TryGetProperty("userId", out var userIdElement) ||
                !payload.TryGetProperty("pageNumber", out var pageNumber) ||
                !payload.TryGetProperty("chunkIndex", out var chunkIndex) ||
                !payload.TryGetProperty("text", out var text) ||
                !point.TryGetProperty("score", out var score))
            {
                continue;
            }

            results.Add(new QdrantSearchResult(
                Guid.Parse(docId.GetString()!),
                Guid.Parse(userIdElement.GetString()!),
                pageNumber.GetInt32(),
                chunkIndex.GetInt32(),
                text.GetString() ?? string.Empty,
                score.GetDouble()));
        }

        return results;
    }
    public async Task EnsurePayloadIndexAsync(
     string fieldName,
     CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"collections/{CollectionName}/index",
            new
            {
                field_name = fieldName,
                field_schema = "keyword"
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Failed to create index for {fieldName}. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {errorBody}");
        }
    }

    public async Task DeleteDocumentChunksAsync(
    Guid documentId,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document ID is required.",
                nameof(documentId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        var filter = new
        {
            filter = new
            {
                must = new object[]
                {
                new
                {
                    key = "documentId",
                    match = new
                    {
                        value = documentId.ToString()
                    }
                },
                new
                {
                    key = "userId",
                    match = new
                    {
                        value = userId.ToString()
                    }
                }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"collections/{CollectionName}/points/delete?wait=true",
            filter,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Failed to delete document vectors from Qdrant. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {errorBody}",
                null,
                response.StatusCode);
        }
    }

}
