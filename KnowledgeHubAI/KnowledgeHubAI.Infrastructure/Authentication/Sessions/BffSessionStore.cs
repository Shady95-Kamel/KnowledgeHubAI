
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.DTOs;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KnowledgeHubAI.Infrastructure.Authentication.Sessions;

public sealed class BffSessionStore : IBffSessionStore
{
    private const string KeyPrefix = "knowledgehub:bff-session:";
    private readonly IDatabase _database;

    public BffSessionStore(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<string> CreateAsync(
        BffSessionData session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var sessionId = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var ttl = session.RefreshTokenExpiresAt - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
            throw new InvalidOperationException(
                "The refresh token has expired.");

        var saved = await _database.StringSetAsync(
            CreateKey(sessionId),
            JsonSerializer.Serialize(session),
            ttl,
            When.NotExists);

        if (!saved)
            throw new InvalidOperationException(
                "Could not create the session.");

        return sessionId;
    }

    public async Task<BffSessionData?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        var value = await _database.StringGetAsync(CreateKey(sessionId));

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<BffSessionData>(
            value.ToString());
    }

    public async Task<bool> DeleteAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        return await _database.KeyDeleteAsync(CreateKey(sessionId));
    }

    public async Task<bool> UpdateAsync(
    string sessionId,
    BffSessionData session,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var ttl = session.RefreshTokenExpiresAt - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
        {
            await DeleteAsync(sessionId, cancellationToken);
            return false;
        }

        return await _database.StringSetAsync(
            CreateKey(sessionId),
            JsonSerializer.Serialize(session),
            ttl,
            When.Exists);
    }

    private static RedisKey CreateKey(string sessionId)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));

        return $"{KeyPrefix}{hash}";
    }
}
