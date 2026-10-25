
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.RefToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenSettings _tokenSettings;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IJwtTokenService jwtTokenService,
        ITokenSettings tokenSettings)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _jwtTokenService = jwtTokenService;
        _tokenSettings = tokenSettings;
    }

    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return InvalidRefreshToken();

        var tokenHash = _tokenService.HashToken(
            request.RefreshToken);

        var storedToken =
            await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        var nowUtc = DateTime.UtcNow;

        if (storedToken is null ||
            storedToken.RevokedAt.HasValue ||
            storedToken.ExpiresAt <= nowUtc)
        {
            return InvalidRefreshToken();
        }

        var user = storedToken.User;

        if (user is null ||
            !user.EmailConfirmed ||
            user.Status != UserStatus.Active)
        {
            return InvalidRefreshToken();
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        var accessTokenExpiresAt = nowUtc.AddMinutes(
            _tokenSettings.AccessTokenExpirationMinutes);

        var rawRefreshToken =
            _tokenService.GenerateSecureToken();

        var refreshTokenExpiresAt = nowUtc.AddDays(
            _tokenSettings.RefreshTokenExpirationDays);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.HashToken(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt
        };

        var rotated =
            await _refreshTokenRepository.TryRotateAsync(
                tokenHash,
                nowUtc,
                newRefreshToken,
                cancellationToken);

        if (!rotated)
            return InvalidRefreshToken();

        return Result<LoginResponse>.Success(
            new LoginResponse(
                user.Id,
                user.Email,
                user.Role.ToString(),
                accessToken,
                rawRefreshToken,
                accessTokenExpiresAt,
                refreshTokenExpiresAt));
    }

    private static Result<LoginResponse> InvalidRefreshToken() =>
        Result<LoginResponse>.Failure(
            new Error(
                "Authentication.InvalidRefreshToken",
                "The refresh token is invalid or expired. Please log in again."));
}
