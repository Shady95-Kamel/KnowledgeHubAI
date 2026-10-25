
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.Features.Authentication.Commands.Logout;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<bool>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<bool>.Success(true);
        }

        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        await _refreshTokenRepository.RevokeAsync(
            tokenHash,
            DateTime.UtcNow,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
