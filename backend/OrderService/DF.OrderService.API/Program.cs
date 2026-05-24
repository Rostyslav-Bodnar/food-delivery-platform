using DF.OrderService.API.Extensions;
using DF.OrderService.API.Middlewares;
using DF.OrderService.Application.BackgroundJobs;
using DF.OrderService.Application.Messaging.Clients;
using DF.OrderService.Application.Messaging.Consumers;
using DF.OrderService.Application.Messaging.Publishers;
using DF.OrderService.Application.Options;
using DF.OrderService.Application.Repositories;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Application.Validation;
using DF.OrderService.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using Serilog;
using Serilog.Formatting.Compact;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

const string ServiceName = "OrderService";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", ServiceName)
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", ServiceName)
    .WriteTo.Console(new CompactJsonFormatter()));

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                   ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

// Add services to the container.

builder.Services.AddControllers();

// FluentValidation — register all validators from the Application assembly, run automatically.
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// 🔹 Swagger
// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Policy
builder.Services.AddCors(options =>
{
    // Origin comes from config (AllowedOrigins:Url) so prod (Render) and
    // dev (appsettings.Development.json) can each set the appropriate
    // Gateway URL. Override on Render via env var `AllowedOrigins__Url`.
    var allowedOrigin = builder.Configuration["AllowedOrigins:Url"]
                        ?? "http://localhost:5229";

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// Database connection (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("OrderServiceDatabase")
    ));

// RabbitMQ connection — auto-recovering, retried on startup
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
        ClientProvidedName = "OrderService"
    };

    var logger = sp.GetRequiredService<ILogger<Program>>();
    var retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 8,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(60),
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

// RPC clients — async factories so the channel + reply queue are set up without blocking the constructor
builder.Services.AddSingleton(sp =>
    UserServiceRpcClient.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());
builder.Services.AddSingleton(sp =>
    MenuServiceRpcClient.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());
builder.Services.AddSingleton(sp =>
    TrackingServiceRpcClient.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());
builder.Services.Configure<CourierCompensationOptions>(
    builder.Configuration.GetSection("CourierCompensation"));

//EventPublishers — async factory so the exchange is declared before the first publish
builder.Services.AddSingleton<IEventPublisher>(sp =>
    OrderEventPublisher.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());

// Outbox — writer is scoped (shares AppDbContext with the request); publisher polls in the background.
builder.Services.AddScoped<OutboxWriter>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

//Consumers
builder.Services.AddSingleton<IConsumer, LocationsCreatedConsumer>();
builder.Services.AddSingleton<IConsumer, CourierPayoutCompletedConsumer>();
builder.Services.AddSingleton<IConsumer, PaymentSucceededConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

// Auto-cancel Online orders that sit unpaid past the deadline (default 15 min).
// Configurable via the "OrderPaymentTimeout" config section so ops can tune
// the deadline / disable it without redeploying.
var paymentTimeoutOptions =
    builder.Configuration.GetSection("OrderPaymentTimeout").Get<OrderPaymentTimeoutOptions>()
    ?? new OrderPaymentTimeoutOptions();
builder.Services.AddSingleton(paymentTimeoutOptions);
builder.Services.AddHostedService<OrderPaymentTimeoutWorker>();

builder.Services.AddAuthorization();

// Health checks — DB + RabbitMQ. /health/live is process-only, /health/ready also verifies dependencies.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: sp => builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres", tags: ["ready"])
    .AddRabbitMQ(
        sp => sp.GetRequiredService<IConnection>(),
        name: "rabbitmq", tags: ["ready"]);

//Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDishRepository, OrderDishRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

//Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<ITrackingTokenService, TrackingTokenService>();
builder.Services.AddHttpClient<IDistanceService, OsrmDistanceService>();


var app = builder.Build();

// Apply database migrations on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCustomExceptionMiddleware();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseMiddleware<InternalAuthMiddleware>();
app.UseMiddleware<UserContextMiddleware>();

app.UseAuthorization();

app.MapControllers();

// /health      = what Render probes by default; aliased to liveness.
// /health/live = process aliveness; /health/ready = DB + RabbitMQ checks.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.UseSerilogRequestLogging();

await app.RunAsync();
return 0;

}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
