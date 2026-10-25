using KnowledgeHubAI.SharedKernel.Results;
using MediatR;

namespace KnowledgeHubAI.Application.Features.Documents.Commands.DeleteDocument;

public sealed record DeleteDocumentCommand(
    Guid DocumentId,
    Guid UserId
) : IRequest<Result<bool>>;