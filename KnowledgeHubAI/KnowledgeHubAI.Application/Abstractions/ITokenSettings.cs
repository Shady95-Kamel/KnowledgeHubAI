namespace KnowledgeHubAI.Application.Abstractions;

public interface ITokenSettings
{
    int AccessTokenExpirationMinutes { get; }

    int RefreshTokenExpirationDays { get; }
}
