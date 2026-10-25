
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPass;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPass;

public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository resetTokenRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher)
    {
        _resetTokenRepository = resetTokenRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.InvalidResetToken",
                    "The password reset link is invalid or has expired."));
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.PasswordMismatch",
                    "The password and confirmation password do not match."));
        }

        var tokenHash = _tokenService.HashToken(request.Token);

        var resetToken = await _resetTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (resetToken is null ||
            resetToken.UsedAt.HasValue ||
            resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.InvalidResetToken",
                    "The password reset link is invalid or has expired."));
        }

        var user = resetToken.User;

        if (user is null)
        {
            return Result<bool>.Failure(
                new Error(
                    "Authentication.UserNotFound",
                    "The user associated with this reset link was not found."));
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.NewPassword);

        resetToken.UsedAt = DateTime.UtcNow;

        // Both tracked changes are saved in the same DbContext operation.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
