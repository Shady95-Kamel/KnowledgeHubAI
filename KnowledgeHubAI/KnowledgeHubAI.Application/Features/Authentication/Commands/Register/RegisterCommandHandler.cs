using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeHubAI.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _userRepository.EmailExistsAsync(
            email,
            cancellationToken);

        if (emailExists)
        {
            return Result<Guid>.Failure(
                new Error(
                    "Authentication.EmailAlreadyExists",
                    "This email is already registered."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            EmailConfirmed = false,
            Role = UserRole.User,
            Status = UserStatus.PendingVerification
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        var rawToken = _tokenService.GenerateSecureToken();
        var tokenExpiresAt = DateTime.UtcNow.AddHours(24);

        var verificationToken = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.HashToken(rawToken),
            ExpiresAt = tokenExpiresAt
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _tokenRepository.AddAsync(
            verificationToken,
            cancellationToken);

        // Save the user and verification token together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish only after the database save succeeds.
        var registeredEvent = new UserRegistered(
            user.Id,
            user.Email,
            user.FirstName,
            rawToken,
            tokenExpiresAt);

        await _eventPublisher.PublishAsync(
            registeredEvent,
            cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
