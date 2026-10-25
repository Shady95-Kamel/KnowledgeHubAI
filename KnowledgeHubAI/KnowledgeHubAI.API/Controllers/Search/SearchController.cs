
using KnowledgeHubAI.API.Contracts.Search;
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Infrastructure.Authentication.Sessions;
using KnowledgeHubAI.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeHubAI.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize(AuthenticationSchemes = "Bff")]
public sealed class SearchController : ControllerBase
{
    private readonly IBffSessionStore _sessionStore;
    private readonly QdrantConnectionService _qdrantService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IBffSessionStore sessionStore,
        QdrantConnectionService qdrantService,
        ILogger<SearchController> logger)
    {
        _sessionStore = sessionStore;
        _qdrantService = qdrantService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Search(
        [FromBody] DocumentSearchRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("sid");

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized();
        }

        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            var results = await _qdrantService.SearchAsync(
                question: request.Question.Trim(),
                userId: session.UserId,
                documentId: request.DocumentId,
                limit: request.Limit,
                cancellationToken: cancellationToken);

            return Ok(new
            {
                success = true,
                data = results,
                count = results.Count
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Qdrant search failed. HTTP status: {StatusCode}",
                ex.StatusCode);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    success = false,
                    message = "The document search service is unavailable."
                });
        }

    }
}
