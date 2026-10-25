
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Features.Documents.Commands.DeleteDocument;
using KnowledgeHubAI.Application.Features.Documents.Commands.RetryDocument;
using KnowledgeHubAI.Application.Features.Documents.Commands.UploadDocument;
using KnowledgeHubAI.Application.Features.Documents.Queries.GetMyDocuments;
using KnowledgeHubAI.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeHubAI.API.Controllers.Documents;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = "Bff")]
public sealed class DocumentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IBffSessionStore _sessionStore;

    public DocumentsController(
        ISender sender,
        IBffSessionStore sessionStore)
    {
        _sender = sender;
        _sessionStore = sessionStore;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDocuments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true,
        CancellationToken cancellationToken = default)
    {
        // 1. Get the current session ID from the authenticated cookie.
        var sessionId = User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid authentication session."
            });
        }

        // 2. Load the session from the BFF session store.
        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Authentication session has expired."
            });
        }

        // 3. Use the UserId from the trusted session.
        // Never accept the UserId from the query string.
        var query = new GetMyDocumentsQuery(
            UserId: session.UserId,
            PageNumber: pageNumber,
            PageSize: pageSize,
            SearchTerm: searchTerm,
            Status: status,
            SortBy: sortBy,
            Descending: descending);

        // 4. Send the query through MediatR.
        var result = await _sender.Send(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = result.Error.Code,
                    message = result.Error.Message
                }
            });
        }

        // 5. Return the paginated documents.
        return Ok(new
        {
            success = true,
            data = result.Value
        });
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> RetryDocument(
    Guid id,
    CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid authentication session."
            });
        }

        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Authentication session has expired."
            });
        }

        var command = new RetryDocumentCommand(
            DocumentId: id,
            UserId: session.UserId);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Documents.NotFound")
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = result.Error.Code,
                        message = result.Error.Message
                    }
                });
            }

            if (result.Error.Code == "Documents.InvalidRetryStatus")
            {
                return Conflict(new
                {
                    success = false,
                    error = new
                    {
                        code = result.Error.Code,
                        message = result.Error.Message
                    }
                });
            }

            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = result.Error.Code,
                    message = result.Error.Message
                }
            });
        }

        return Accepted(new
        {
            success = true,
            message = "The retry request was published."
        });
    }


    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Please select a PDF file."
            });
        }

        const long maxFileSize = 20 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            return BadRequest(new
            {
                success = false,
                message = "The maximum file size is 20 MB."
            });
        }

        if (!string.Equals(
            Path.GetExtension(file.FileName),
            ".pdf",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "Only PDF files are allowed."
            });
        }

        await using var stream = file.OpenReadStream();

        var header = new byte[5];

        var bytesRead = await stream.ReadAsync(
            header.AsMemory(0, header.Length),
            cancellationToken);

        if (bytesRead != 5 ||
            !header.AsSpan().SequenceEqual("%PDF-"u8))
        {
            return BadRequest(new
            {
                success = false,
                message = "The selected file is not a valid PDF."
            });
        }

        stream.Position = 0;

        var sessionId = User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid authentication session."
            });
        }

        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Authentication session has expired."
            });
        }

        try
        {
            var command = new UploadDocumentCommand(
                UserId: session.UserId,
                FileName: file.FileName,
                ContentType: file.ContentType,
                FileSize: file.Length,
                FileStream: stream);

            var result = await _sender.Send(
                command,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Document uploaded successfully.",
                data = result
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message = "Failed to upload the document."
                });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(
    Guid id,
    CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid authentication session."
            });
        }

        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Authentication session has expired."
            });
        }

        var command = new DeleteDocumentCommand(
            DocumentId: id,
            UserId: session.UserId);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Documents.NotFound")
            {
                return NotFound(new
                {
                    success = false,
                    error = new
                    {
                        code = result.Error.Code,
                        message = result.Error.Message
                    }
                });
            }

            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = result.Error.Code,
                    message = result.Error.Message
                }
            });
        }

        return Ok(new
        {
            success = true,
            message = "Document and its associated resources were deleted."
        });
    }

}
