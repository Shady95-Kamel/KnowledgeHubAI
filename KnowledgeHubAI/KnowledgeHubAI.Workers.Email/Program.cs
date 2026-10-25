using KnowledgeHubAI.Workers.Email.Configuration;
using KnowledgeHubAI.Workers.Email.Consumers;
using KnowledgeHubAI.Workers.Email.Services;
using KnowledgeHubAI.Workers.Email.Services.Abstractions;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

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
    configuration.AddConsumer<UserRegisteredConsumer>();
    configuration.AddConsumer<PasswordResetRequestedConsumer>();

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

builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Host) &&
        !string.IsNullOrWhiteSpace(options.SenderEmail) &&
        !string.IsNullOrWhiteSpace(options.Username) &&
        !string.IsNullOrWhiteSpace(options.Password),
        "Email configuration is incomplete.")
    .ValidateOnStart();

builder.Services.AddScoped<IEmailService, EmailService>();

var host = builder.Build();


await host.RunAsync();
