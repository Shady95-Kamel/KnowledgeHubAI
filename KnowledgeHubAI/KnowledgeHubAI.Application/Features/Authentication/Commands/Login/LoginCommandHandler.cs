
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenService _tokenService;
    private readonly ITokenSettings _tokenSettings;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        ITokenService tokenService,
        ITokenSettings tokenSettings)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tokenService = tokenService;
        _tokenSettings = tokenSettings;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
        {
            return InvalidCredentials();
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return InvalidCredentials();
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (!user.EmailConfirmed ||
            user.Status != UserStatus.Active)
        {
            return Result<LoginResponse>.Failure(
                new Error(
                    "Authentication.AccountNotActive",
                    "Please verify your email and make sure your account is active."));
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

        var response = new LoginResponse(
        user.Id,
        user.Email,
        user.Role.ToString(),
        accessToken,
        rawRefreshToken,
        accessTokenExpiresAt,
        refreshTokenExpiresAt);

        return Result<LoginResponse>.Success(response);
    }

    private static Result<LoginResponse> InvalidCredentials()
    {
        return Result<LoginResponse>.Failure(
            new Error(
                "Authentication.InvalidCredentials",
                "Invalid email or password."));
    }
}
