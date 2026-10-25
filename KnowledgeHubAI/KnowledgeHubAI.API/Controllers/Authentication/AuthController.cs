using FluentValidation;
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.DTOs;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ExtLogin;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ForgotPass;
using KnowledgeHubAI.Application.Features.Authentication.Commands.Login;
using KnowledgeHubAI.Application.Features.Authentication.Commands.Logout;
using KnowledgeHubAI.Application.Features.Authentication.Commands.RefToken;
using KnowledgeHubAI.Application.Features.Authentication.Commands.Register;
using KnowledgeHubAI.Application.Features.Authentication.Commands.ResetPass;
using KnowledgeHubAI.Application.Features.Authentication.Commands.VerifyEmail;
using KnowledgeHubAI.Infrastructure.Authentication;
using MassTransit.Mediator;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeHubAI.API.Controllers.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IBffSessionStore _sessionStore;
    private readonly IConfiguration _configuration;
    private readonly IAntiforgery _antiforgery;

    public AuthController(
    ISender sender,
    IBffSessionStore sessionStore,
    IConfiguration configuration,
    IAntiforgery antiforgery)
    {
        _sender = sender;
        _sessionStore = sessionStore;
        _configuration = configuration;
        _antiforgery = antiforgery;
    }

    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new
        {
            success = true,
            data = new
            {
                requestToken = tokens.RequestToken
            }
        });
    }


    private async Task CreateBffSessionAsync(
        LoginResponse loginResponse,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var session = new BffSessionData(
            loginResponse.UserId,
            loginResponse.Email,
            loginResponse.Role,
            loginResponse.AccessToken,
            loginResponse.RefreshToken,
            loginResponse.AccessTokenExpiresAt,
            loginResponse.RefreshTokenExpiresAt,
            now);

        var sessionId = await _sessionStore.CreateAsync(
            session,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException(
                "BFF session store returned an empty session ID.");
        }

        var identity = new ClaimsIdentity(
            new[]
            {
            new Claim("sid", sessionId)
            },
            "Bff");

        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = false,
            ExpiresUtc = new DateTimeOffset(
                loginResponse.RefreshTokenExpiresAt)
        };

        try
        {
            await HttpContext.SignInAsync(
                "Bff",
                principal,
                properties);

            Console.WriteLine(
                "[BFF] Session stored and sign-in completed.");
            Console.WriteLine(
                $"[BFF] New session created for UserId: {loginResponse.UserId}, Email: {loginResponse.Email}, SessionId: {sessionId}");
        }
        catch
        {
            await _sessionStore.DeleteAsync(
                sessionId,
                cancellationToken);

            throw;
        }
    }


    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        try
        {
            var result = await _sender.Send(
                command,
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code ==
                    "Authentication.EmailAlreadyExists")
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Email already registered",
                        Detail = result.Error.Message
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Registration failed",
                    Detail = result.Error.Message
                });
            }

            var response = new RegisterResponseDto(
                result.Value!,
                "Registration successful. Please verify your email.");

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more validation errors occurred."
            };

            return BadRequest(problemDetails);
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Token);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new
        {
            success = true,
            message = "Email verified successfully."
        });
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(result.Error);

        var loginResponse = result.Value!;

        await CreateBffSessionAsync(
            loginResponse,
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Login successful.",
            data = new
            {
                userId = loginResponse.UserId,
                email = loginResponse.Email,
                role = loginResponse.Role
            }
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
    [FromHeader(Name = "X-CSRF-TOKEN")] string? csrfToken,
    CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("sid");

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);

            if (session is not null)
            {
                await _sender.Send(
                    new LogoutCommand(session.RefreshToken),
                    cancellationToken);

                await _sessionStore.DeleteAsync(
                    sessionId,
                    cancellationToken);
            }
        }

        await HttpContext.SignOutAsync("Bff");

        return Ok(new
        {
            success = true,
            message = "Logged out successfully."
        });
    }



    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("sid");

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            await HttpContext.SignOutAsync("Bff");
            return Unauthorized();
        }

        var session = await _sessionStore.GetAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            await _sessionStore.DeleteAsync(
                sessionId,
                cancellationToken);

            await HttpContext.SignOutAsync("Bff");

            return Unauthorized();
        }

        var result = await _sender.Send(
            new RefreshTokenCommand(session.RefreshToken),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            await _sessionStore.DeleteAsync(
                sessionId,
                cancellationToken);

            await HttpContext.SignOutAsync("Bff");

            return Unauthorized();
        }

        var refreshed = result.Value;

        var updatedSession = new BffSessionData(
            refreshed.UserId,
            refreshed.Email,
            refreshed.Role,
            refreshed.AccessToken,
            refreshed.RefreshToken,
            refreshed.AccessTokenExpiresAt,
            refreshed.RefreshTokenExpiresAt,
            session.CreatedAtUtc);

        var updated = await _sessionStore.UpdateAsync(
            sessionId,
            updatedSession,
            cancellationToken);

        if (!updated)
        {
            await _sessionStore.DeleteAsync(
                sessionId,
                cancellationToken);

            await HttpContext.SignOutAsync("Bff");

            return Unauthorized();
        }

        try
        {
            await HttpContext.SignInAsync(
                "Bff",
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                        new Claim("sid", sessionId)
                        },
                        "Bff")),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = false,
                    ExpiresUtc = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            refreshed.RefreshTokenExpiresAt,
                            DateTimeKind.Utc))
                });
        }
        catch
        {
            // The refresh token has already been rotated.
            // Remove the session so the client must log in again.
            await _sessionStore.DeleteAsync(
                sessionId,
                CancellationToken.None);

            await HttpContext.SignOutAsync("Bff");

            throw;
        }

        return Ok(new
        {
            success = true,
            message = "Session refreshed."
        });
    }



    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var callbackUrl = Url.Action(
            nameof(GoogleCallback),
            "Auth",
            values: null,
            protocol: Request.Scheme);

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl
        };

        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        CancellationToken cancellationToken)
    {
        var authenticationResult =
            await HttpContext.AuthenticateAsync("External");

        if (!authenticationResult.Succeeded ||
            authenticationResult.Principal is null)
        {
            await HttpContext.SignOutAsync("External");
            return Unauthorized(new
            {
                message = "Google authentication failed."
            });
        }

        var principal = authenticationResult.Principal;

        var providerUserId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        var email =
            principal.FindFirstValue(ClaimTypes.Email);

        var firstName =
            principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;

        var lastName =
            principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;


        await HttpContext.SignOutAsync("External");

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            return Unauthorized(new
            {
                message = "Google did not provide a verified email and valid user identifier."
            });
        }

        var command = new ExternalLoginCommand(
            "Google",
            providerUserId,
            email,
            firstName,
            lastName,
            true);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new
            {
                result.Error.Code,
                result.Error.Message
            });
        }

        var loginResponse = result.Value!;

        await CreateBffSessionAsync(
            loginResponse,
            cancellationToken);

        var frontendUrl =
            _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Frontend base URL is missing.");

        return Redirect(
            $"{frontendUrl.TrimEnd('/')}/auth/callback");

    }


    [HttpGet("me")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        // Read only the session ID from the authenticated cookie.
        var sessionId = User.FindFirstValue("sid");
        Console.WriteLine(
        $"[BFF /me] SessionId from cookie: {sessionId}");

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid authentication session."
            });
        }

        // Always load the current session data from Redis.
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

        // Return user information from Redis, not cookie claims.
        return Ok(new
        {
            success = true,
            data = new
            {
                userId = session.UserId,
                email = session.Email,
                role = session.Role,
                refreshTokenExpiresAtUtc =
                    session.RefreshTokenExpiresAt
            }
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new
        {
            message = "If the email is registered, a password reset link will be sent."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new
        {
            message = "Password has been reset successfully."
        });
    }

}
