using DF.PaymentService.API.Helpers;
using DF.PaymentService.API.Middlewares;
using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Contracts;
using DF.PaymentService.Infrastructure.BackgroundJobs;
using DF.PaymentService.Infrastructure.Data;
using DF.PaymentService.Infrastructure.Messaging;
using DF.PaymentService.Infrastructure.Messaging.Consumers;
using DF.PaymentService.Infrastructure.Repositories;
using DF.PaymentService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// 1) Controllers / OpenAPI
// ------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ------------------------------------------------------------
// 2) CORS — origins come from config (AllowedOrigins) so prod doesn't ship localhost
// ------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ------------------------------------------------------------
// 3) Database (PostgreSQL)
// ------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentServiceDatabase"));
});

// ------------------------------------------------------------
// 4) RabbitMQ — resilient connection with exponential retry (broker may not be up at boot)
// ------------------------------------------------------------
builder.Services.AddSingleton<IConnection>(sp =>
{
    var rabbitUrl = builder.Configuration["RabbitMQ:Url"];
    if (string.IsNullOrWhiteSpace(rabbitUrl))
        throw new InvalidOperationException("RabbitMQ:Url is missing or empty in configuration");

    var factory = new ConnectionFactory
    {
        Uri = new Uri(rabbitUrl),
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        RequestedHeartbeat = TimeSpan.FromSeconds(30),
        ClientProvidedName = "PaymentService"
    };

    var logger = sp.GetRequiredService<ILogger<Program>>();
    var retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 8,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
            OnRetry = args =>
            {
                logger.LogWarning(args.Outcome.Exception,
                    "RabbitMQ connect attempt {Attempt} failed; retrying in {Delay}",
                    args.AttemptNumber + 1, args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    return retry.ExecuteAsync(async ct => await factory.CreateConnectionAsync(ct))
        .AsTask().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IEventBus>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    return new RabbitMQEventBus(
        connection: connection,
        scopeFactory: scopeFactory,
        exchangeName: "df.events",
        prefetchCount: 32,
        maxRetries: 3);
});

// ------------------------------------------------------------
// 5) Domain / Application
// ------------------------------------------------------------
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentTaskRepository, PaymentTaskRepository>();
builder.Services.AddScoped<ICourierPayoutService, CourierPayoutService>();

builder.Services.AddScoped<CreatePaymentCommandHandler>();
builder.Services.AddScoped<CollectCashCommandHandler>();
builder.Services.AddScoped<CancelPaymentCommandHandler>();
builder.Services.AddScoped<CreateStripePaymentIntentCommandHandler>();
builder.Services.AddScoped<RefundPaymentCommandHandler>();

builder.Services.AddScoped<IProcessedMessageStore, ProcessedMessageStore>();
builder.Services.AddScoped<IProcessedWebhookStore, ProcessedWebhookStore>();

// ------------------------------------------------------------
// 6) Stripe
// ------------------------------------------------------------
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripeService, StripeService>();

// ------------------------------------------------------------
// 7) Background workers
// ------------------------------------------------------------
builder.Services.AddSingleton(new OutboxOptions
{
    BatchSize = 50,
    PollingInterval = TimeSpan.FromSeconds(2),
    MaxRetries = 5,
    BaseDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromSeconds(60),
    MoveToDeadLetter = true
});
builder.Services.AddHostedService<OutboxPublisher>();

builder.Services.AddSingleton(new PaymentTimeoutOptions
{
    BatchSize = 100,
    PollingInterval = TimeSpan.FromMinutes(1),
    SafetyWindow = TimeSpan.FromSeconds(10),
    Enable = true,
    CancelStripePiOnTimeout = false
});
builder.Services.AddHostedService<PaymentTimeoutWorker>();

builder.Services.AddSingleton(new StripeTaskProcessorOptions
{
    BatchSize = 50,
    PollingInterval = TimeSpan.FromSeconds(5),
    MaxRetries = 5,
    BaseDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromMinutes(2),
    PaymentExpiration = TimeSpan.FromMinutes(15),
    Enable = true
});
builder.Services.AddHostedService<StripeTaskProcessor>();

var courierPayoutSettings = builder.Configuration.GetSection("CourierPayouts").Get<CourierPayoutOptions>()
                           ?? new CourierPayoutOptions();
builder.Services.AddSingleton(courierPayoutSettings);
builder.Services.AddHostedService<CourierPayoutWorker>();

builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<OrderCancelledConsumer>();
builder.Services.AddHostedService<OrderDeliveredConsumer>();
builder.Services.AddHostedService<PaymentSucceededConsumer>();

// ------------------------------------------------------------
// 8) Auth / user context (gate is the Gateway via HMAC headers)
// ------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddAuthorization();

// ------------------------------------------------------------
// 9) Health checks — Postgres + RabbitMQ; /health/live is process-only
// ------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: _ => builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres", tags: ["ready"])
    .AddRabbitMQ(
        sp => sp.GetRequiredService<IConnection>(),
        name: "rabbitmq", tags: ["ready"]);

// ------------------------------------------------------------
// 10) Build + apply migrations + pipeline
// ------------------------------------------------------------
var app = builder.Build();

// Global exception handler — maps domain/validation exceptions to proper HTTP codes
// instead of bubbling 500s. Must be first so it wraps every downstream middleware.
app.UseCustomExceptionMiddleware();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("Migrations applied successfully");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseMiddleware<InternalAuthMiddleware>();
app.UseMiddleware<UserContextMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
