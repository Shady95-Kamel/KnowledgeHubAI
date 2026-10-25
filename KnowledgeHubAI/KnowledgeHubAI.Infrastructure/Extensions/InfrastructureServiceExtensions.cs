
using Amazon.Runtime;
using Amazon.S3;
using KnowledgeHubAI.Application.Abstractions;
using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Infrastructure.Authentication;
using KnowledgeHubAI.Infrastructure.Authentication.Sessions;
using KnowledgeHubAI.Infrastructure.Options;
using KnowledgeHubAI.Infrastructure.Persistence;
using KnowledgeHubAI.Infrastructure.Repositories;
using KnowledgeHubAI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace KnowledgeHubAI.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    // Used by the API.
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDocumentWorkerInfrastructure(services, configuration);

        // API-specific services.
        var redisConnectionString =
            configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Redis connection string is missing.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IBffSessionStore, BffSessionStore>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<
            IEmailVerificationTokenRepository,
            EmailVerificationTokenRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();

        services.AddScoped<
            IPasswordResetTokenRepository,
            PasswordResetTokenRepository>();

        services.Configure<OllamaOptions>(
      configuration.GetSection(OllamaOptions.SectionName));

        services.AddHttpClient<OllamaChatService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<OllamaOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        return services;
    }

    // Used by DocumentProcessorWorker.
    public static IServiceCollection AddDocumentWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL Server.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Cloudflare R2 configuration.
        services.AddOptions<CloudflareR2Options>()
            .Bind(configuration.GetSection(
                CloudflareR2Options.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.AccountId) &&
                    !string.IsNullOrWhiteSpace(options.AccessKeyId) &&
                    !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
                    !string.IsNullOrWhiteSpace(options.BucketName) &&
                    !string.IsNullOrWhiteSpace(options.ServiceUrl),
                "Cloudflare R2 configuration is incomplete.")
            .ValidateOnStart();

        // Cloudflare R2 S3 client.
        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<CloudflareR2Options>>()
                .Value;

            var credentials = new BasicAWSCredentials(
                options.AccessKeyId,
                options.SecretAccessKey);

            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                AuthenticationRegion = "auto",
                ForcePathStyle = true
            };

            return new AmazonS3Client(credentials, config);
        });

        services.AddScoped<
            IObjectStorageService,
            CloudflareR2StorageService>();

        // Qdrant configuration.
        services.AddOptions<QdrantOptions>()
            .Bind(configuration.GetSection(
                QdrantOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.Endpoint,
                        UriKind.Absolute,
                        out var endpointUri) &&
                    (endpointUri.Scheme == Uri.UriSchemeHttps ||
                     endpointUri.Scheme == Uri.UriSchemeHttp) &&
                    !string.IsNullOrWhiteSpace(options.ApiKey),
                "Qdrant configuration is incomplete or invalid.")
            .ValidateOnStart();

        // Qdrant HTTP client.
        services.AddHttpClient<QdrantConnectionService>();

        services.AddTransient<IVectorStoreService>(serviceProvider =>
    serviceProvider.GetRequiredService<QdrantConnectionService>());


        return services;
    }
}
