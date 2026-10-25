
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler
    : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<Result<bool>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Hash the raw token received from the client.
        var tokenHash = _tokenService.HashToken(request.Token);

        // 2. Find the token and its associated user.
        var verificationToken =
            await _tokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (verificationToken is null)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.InvalidVerificationToken",
                    "The verification token is invalid."));
        }

        // 3. Prevent token reuse.
        if (verificationToken.UsedAt.HasValue)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.VerificationTokenAlreadyUsed",
                    "This verification token has already been used."));
        }

        // 4. Check expiration.
        if (verificationToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.VerificationTokenExpired",
                    "The verification token has expired."));
        }

        var user = verificationToken.User;

        // 5. Check account state.
        if (user.EmailConfirmed ||
            user.Status != UserStatus.PendingVerification)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.AccountNotEligibleForVerification",
                    "This account cannot be verified."));
        }

        // 6. Activate the account.
        user.EmailConfirmed = true;
        user.Status = UserStatus.Active;

        // 7. Mark the token as used.
        verificationToken.UsedAt = DateTime.UtcNow;

        // 8. Save the changes.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
