using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Extensions;
using KnowledgeHubAI.Infrastructure.Authentication;
using KnowledgeHubAI.Infrastructure.Extensions;
using KnowledgeHubAI.Infrastructure.Persistence;
using KnowledgeHubAI.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            const string FrontendCorsPolicy = "FrontendCorsPolicy";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policy =>
                {
                    policy
                        .WithOrigins("https://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });


            var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    "JWT settings are missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) ||
                Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT SecretKey must be at least 32 bytes.");
            }

            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
                string.IsNullOrWhiteSpace(jwtSettings.Audience))
            {
                throw new InvalidOperationException(
                    "JWT Issuer and Audience must be configured.");
            }

            builder.Services.Configure<JwtSettings>(jwtSection);

            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

            builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Smart";
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
        options.DefaultForbidScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "JWT or BFF Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization =
                context.Request.Headers.Authorization.ToString();

            return authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : "Bff";
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    })
    .AddCookie("Bff", options =>
    {
        options.Cookie.Name = "__Host-KnowledgeHubAI.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        options.SlidingExpiration = false;

        options.Events = new CookieAuthenticationEvents
        {

            OnRedirectToLogin = context =>
            {
                Console.WriteLine(
                    $"[BFF] Login challenge: {context.Request.Path}");

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            },

            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            },

            OnValidatePrincipal = async context =>
            {
                Console.WriteLine(
                    "[BFF] OnValidatePrincipal started.");

                var sessionStore = context.HttpContext
                    .RequestServices
                    .GetRequiredService<IBffSessionStore>();

                var sessionId = context.Principal?
                    .FindFirst("sid")?.Value;

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    Console.WriteLine(
                        "[BFF] Session ID is missing.");

                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync("Bff");

                    return;
                }

                var session = await sessionStore.GetAsync(
                    sessionId,
                    context.HttpContext.RequestAborted);

                if (session is null)
                {
                    Console.WriteLine(
                        "[BFF] Redis session was not found.");

                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync("Bff");

                    return;
                }

                if (session.RefreshTokenExpiresAt <= DateTime.UtcNow)
                {
                    Console.WriteLine(
                        "[BFF] Refresh token has expired.");

                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync("Bff");

                    return;
                }

                var claims = new List<Claim>
                {
                    new(
                        ClaimTypes.NameIdentifier,
                        session.UserId.ToString()),

                    new(
                        ClaimTypes.Email,
                        session.Email),

                    new(
                        ClaimTypes.Role,
                        session.Role),

                    new("sid", sessionId)
                };

                context.ReplacePrincipal(
                    new ClaimsPrincipal(
                        new ClaimsIdentity(claims, "Bff")));

                Console.WriteLine(
                    "[BFF] Principal validated successfully.");
            }
        };
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "KnowledgeHubAI.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.SlidingExpiration = false;
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId =
            builder.Configuration[
                "Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException(
                "Google ClientId is missing.");

        options.ClientSecret =
            builder.Configuration[
                "Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException(
                "Google ClientSecret is missing.");

        options.SignInScheme = "External";
        options.CallbackPath = "/signin-google";

        options.ClaimActions.MapJsonKey(
            "verified_email",
            "verified_email",
            "boolean");
    });


            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";

                options.Cookie.Name = "__Host-KnowledgeHubAI.Antiforgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
            });


            builder.Services.AddSingleton<ITokenSettings>(sp =>
            sp.GetRequiredService<
            IOptions<JwtSettings>>().Value);


            builder.Services.AddAuthorization();


            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            // Add services to the container.

            builder.Services.AddControllers();


            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "KnowledgeHubAI API",
                    Version = "v1",
                    Description = "KnowledgeHubAI API Documentation"
                });

                // JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token. Example: eyJhbGciOi..."
                });
            });


            // ...

            builder.Services.AddMassTransit(configuration =>
            {
                configuration.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqHost =
                        builder.Configuration["RabbitMQ:Host"] ?? "localhost";

                    var rabbitMqPort =
                        builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);

                    var username =
                        builder.Configuration["RabbitMQ:Username"] ?? "guest";

                    var password =
                        builder.Configuration["RabbitMQ:Password"] ?? "guest";

                    cfg.Host(rabbitMqHost, (ushort)rabbitMqPort, "/", host =>
                    {
                        host.Username(username);
                        host.Password(password);
                    });
                });
            });


            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var qdrant = scope.ServiceProvider
                    .GetRequiredService<QdrantConnectionService>();

              await  qdrant.EnsurePayloadIndexAsync("userId");
              await  qdrant.EnsurePayloadIndexAsync("documentId");
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors(FrontendCorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
