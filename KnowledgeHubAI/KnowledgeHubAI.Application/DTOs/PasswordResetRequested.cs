
namespace KnowledgeHubAI.Application.DTOs;

public sealed record PasswordResetRequested(
    Guid UserId,
    string Email,
    string FirstName,
    string ResetToken);
