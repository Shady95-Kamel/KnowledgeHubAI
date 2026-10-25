
using KnowledgeHubAI.API.Contracts.Chat;
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Infrastructure.Authentication.Sessions;
using KnowledgeHubAI.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeHubAI.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = "Bff")]
public sealed class ChatController : ControllerBase
{
    private const double MinimumRelevanceScore = 0.70;

    private readonly IBffSessionStore _sessionStore;
    private readonly QdrantConnectionService _qdrantService;
    private readonly OllamaChatService _ollamaChatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IBffSessionStore sessionStore,
        QdrantConnectionService qdrantService,
        OllamaChatService ollamaChatService,
        ILogger<ChatController> logger)
    {
        _sessionStore = sessionStore;
        _qdrantService = qdrantService;
        _ollamaChatService = ollamaChatService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var question = request.Question.Trim();

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
            // 1. Retrieve chunks belonging to the authenticated user.
            var searchResults = await _qdrantService.SearchAsync(
                question: question,
                userId: session.UserId,
                documentId: request.DocumentId,
                limit: request.Limit,
                cancellationToken: cancellationToken);

            // 2. Keep only results above the configured threshold.
            var relevantResults = searchResults
                .Where(result =>
                    result.Score >= MinimumRelevanceScore)
                .ToList();

            // 3. If no relevant context remains, do not call the LLM.
            if (relevantResults.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        question,
                        answer = "The information was not found in the provided documents.",
                        sources = Array.Empty<object>()
                    }
                });
            }

            // 4. Build the context for Ollama.
            var contextChunks = relevantResults
                .Select(result =>
                    $"Document ID: {result.DocumentId}\n" +
                    $"Page: {result.PageNumber}\n" +
                    $"Content:\n{result.Text}")
                .ToArray();

            // 5. Generate an answer using the retrieved context.
            var answer = await _ollamaChatService.GenerateAnswerAsync(
                question,
                contextChunks,
                cancellationToken);

            // 6. Return the answer with its source references.
            var sources = relevantResults
                .Select(result => new
                {
                    documentId = result.DocumentId,
                    pageNumber = result.PageNumber,
                    chunkIndex = result.ChunkIndex,
                    score = result.Score,
                    text = result.Text
                })
                .ToArray();

            return Ok(new
            {
                success = true,
                data = new
                {
                    question,
                    answer,
                    sources
                }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Document chat dependency failed for the authenticated user.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    success = false,
                    message = "The document chat service is temporarily unavailable."
                });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Document chat request timed out.");

            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new
                {
                    success = false,
                    message = "Generating the answer took too long. Please try again."
                });
        }
    }
}
