using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.RetryDocument;

public sealed record RetryDocumentCommand(
    Guid DocumentId,
    Guid UserId
) : IRequest<Result<bool>>;