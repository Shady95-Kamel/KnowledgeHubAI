
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ForgotPass;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.ForgotPass;

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEventPublisher eventPublisher,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        // Always return the same response to avoid revealing registered emails.
        if (user is null)
            return Result<bool>.Success(true);

        // OAuth-only accounts may not have a local password to reset.
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return Result<bool>.Success(true);

        var rawToken = _tokenService.GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.HashToken(rawToken),
            ExpiresAt = expiresAt,
            UsedAt = null
        };

        await _resetTokenRepository.AddAsync(
            resetToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = new PasswordResetRequested(
            user.Id,
            user.Email,
            user.FirstName,
            rawToken);

        await _eventPublisher.PublishAsync(
            message,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
