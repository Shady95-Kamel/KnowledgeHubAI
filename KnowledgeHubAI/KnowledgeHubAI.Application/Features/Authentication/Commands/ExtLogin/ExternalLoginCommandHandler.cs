
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ExtLogin;

public sealed class ExternalLoginCommandHandler
    : IRequestHandler<ExternalLoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenService _tokenService;
    private readonly ITokenSettings _tokenSettings;

    public ExternalLoginCommandHandler(
        IUserRepository userRepository,
        IExternalLoginRepository externalLoginRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ITokenService tokenService,
        ITokenSettings tokenSettings)
    {
        _userRepository = userRepository;
        _externalLoginRepository = externalLoginRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _tokenService = tokenService;
        _tokenSettings = tokenSettings;
    }

    public async Task<Result<LoginResponse>> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        var provider = request.Provider.Trim();
        var providerUserId = request.ProviderUserId.Trim();
        var email = request.Email.Trim().ToLowerInvariant();


        var externalLogin =
            await _externalLoginRepository.GetByProviderAsync(
                provider,
                providerUserId,
                cancellationToken);

        User? user;

        if (externalLogin is not null)
        {
            user = externalLogin.User;
        }
        else
        {
            user = await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

            if (user is not null)
            {
                return Failure(
                    "Authentication.ExternalAccountLinkRequired",
                    "An account with this email already exists. Sign in to that account before linking an external provider.");
            }

            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = null,
                EmailConfirmed = true,
                Role = UserRole.User,
                Status = UserStatus.Active
            };

            await _userRepository.AddAsync(
                user,
                cancellationToken);

            externalLogin = new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = providerUserId,
                Email = email
            };

            await _externalLoginRepository.AddAsync(
                externalLogin,
                cancellationToken);
        }

        if (!user.EmailConfirmed || user.Status != UserStatus.Active)
        {
            return Failure(
                "Authentication.AccountNotActive",
                "This account is not active.");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
            _tokenSettings.AccessTokenExpirationMinutes);

        var rawRefreshToken = _tokenService.GenerateSecureToken();

        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _tokenSettings.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.HashToken(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt
        };

        await _unitOfWork.AddRefreshTokenAsync(
            refreshToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);


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

    private static Result<LoginResponse> Failure(
        string code,
        string message)
    {
        return Result<LoginResponse>.Failure(
            new Error(code, message));
    }
}
