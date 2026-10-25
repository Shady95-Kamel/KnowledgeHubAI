using System.ComponentModel.DataAnnotations;

namespace KnowledgeHubAI.API.Contracts.Search;

public sealed class DocumentSearchRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 3)]
    public string Question { get; init; } = string.Empty;

    public Guid? DocumentId { get; init; }

    [Range(1, 20)]
    public int Limit { get; init; } = 5;
}