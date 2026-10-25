
using KnowledgeHubAI.Infrastructure.Extensions;
using KnowledgeHubAI.Infrastructure.Services;
using KnowledgeHubAI.Workers.DocumentProcessor.Consumers;
using KnowledgeHubAI.Workers.DocumentProcessor.Processing;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDocumentWorkerInfrastructure(
    builder.Configuration);

var rabbitMqHost =
    builder.Configuration["RabbitMQ:Host"] ?? "localhost";

var rabbitMqPort =
    builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);

var username =
    builder.Configuration["RabbitMQ:Username"] ?? "guest";

var password =
    builder.Configuration["RabbitMQ:Password"] ?? "guest";

builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumer<DocumentUploadedConsumer>();
    configuration.AddConsumer<RetryDocumentProcessingConsumer>();

    configuration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            rabbitMqHost,
            (ushort)rabbitMqPort,
            "/",
            host =>
            {
                host.Username(username);
                host.Password(password);
            });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSingleton<TextChunker>();

var host = builder.Build();

await host.RunAsync();
