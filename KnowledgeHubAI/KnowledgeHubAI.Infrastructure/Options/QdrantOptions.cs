
namespace KnowledgeHubAI.Infrastructure.Options;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
