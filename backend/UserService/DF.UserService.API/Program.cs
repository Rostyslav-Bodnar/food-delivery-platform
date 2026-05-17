using System.Text.Json.Serialization;
using DF.UserService.API.Extensions;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.BackgroundJobs;
using DF.UserService.Application.Factories;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Messaging;
using DF.UserService.Application.Messaging.Clients;
using DF.UserService.Application.Messaging.Consumers;
using DF.UserService.Application.Repositories;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using RabbitMQ.Client;
using Serilog;
using Serilog.Formatting.Compact;

const string ServiceName = "UserService";

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

// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5229")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =======================
// DATABASE
// =======================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserServiceDatabase")));

// =======================
// IDENTITY (USER MANAGEMENT ONLY)
// =======================
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// =======================
// AUTHORIZATION (NO JWT HERE)
// =======================
builder.Services.AddAuthorization();

// =======================
// REPOSITORIES
// =======================
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();

// =======================
// SERVICES
// =======================
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITokenService, TokenService>(); // issuing tokens only
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IProcessedWebhookStore, ProcessedWebhookStore>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

// =======================
// STRIPE
// =======================
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe")
);
builder.Services.AddSingleton<IStripeConnectService, StripeConnectService>();

// =======================
// FACTORIES
// =======================
builder.Services.AddScoped<IAccountFactory, AccountFactory>();

// =======================
// RABBITMQ
// =======================
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
        ClientProvidedName = "UserService"
    };

    var logger = sp.GetRequiredService<ILogger<Program>>();
    var retry = new Polly.ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 8,
            BackoffType = Polly.DelayBackoffType.Exponential,
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

// =======================
// CONSUMERS
// =======================
builder.Services.AddSingleton<IConsumer, GetAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetBusinessAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetCustomerAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetCourierAccountConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

// =======================
// BACKGROUND WORKERS
// =======================
builder.Services.AddHostedService<StripeAccountProvisioningWorker>();

// =======================
// RPC CLIENTS
// =======================
builder.Services.AddSingleton(sp =>
    TrackingServiceRpcClient.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());

// =======================
// CONTROLLERS & JSON
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.IncludeFields = true;
        o.JsonSerializerOptions.UnknownTypeHandling =
            JsonUnknownTypeHandling.JsonElement;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: sp => builder.Configuration.GetConnectionString("UserServiceDatabase")!,
        name: "postgres", tags: ["ready"])
    .AddRabbitMQ(
        sp => sp.GetRequiredService<IConnection>(),
        name: "rabbitmq", tags: ["ready"]);

var app = builder.Build();

// Apply database migrations on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCustomExceptionMiddleware();

// =======================
// MIDDLEWARE
// =======================
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

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

app.UseSerilogRequestLogging();

await app.RunAsync();
return 0;

}
catch (Exception ex)
{
    Log.Fatal(ex, "UserService terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}