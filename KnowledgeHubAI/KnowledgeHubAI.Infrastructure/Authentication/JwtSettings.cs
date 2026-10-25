
using KnowledgeHubAI.Application.Abstractions;

namespace KnowledgeHubAI.Infrastructure.Authentication;

public sealed class JwtSettings : ITokenSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
