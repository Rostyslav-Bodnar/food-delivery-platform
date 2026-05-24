using DF.TrackingService.API.Hubs;
using DF.TrackingService.Application.Messaging.Consumers;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using DF.TrackingService.API.Extensions;
using DF.TrackingService.API.Hubs.Filters;
using DF.TrackingService.API.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

const string ServiceName = "TrackingService";

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

// 🔹 Swagger
// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:4173",
                "http://localhost:5173",
                "http://localhost:5229")
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// Database connection (PostgreSQL)
builder.Services.AddDbContext<SqlDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TrackingServiceDatabase"),
        o => o.UseNetTopologySuite()
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
        ClientProvidedName = "TrackingService"
    };

    var rabbitLogger = sp.GetRequiredService<ILogger<Program>>();
    var retry = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 8,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
            OnRetry = args =>
            {
                rabbitLogger.LogWarning(args.Outcome.Exception,
                    "RabbitMQ connect attempt {Attempt} failed; retrying in {Delay}",
                    args.AttemptNumber + 1, args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    return retry.ExecuteAsync(async ct => await factory.CreateConnectionAsync(ct))
        .AsTask().GetAwaiter().GetResult();
});

// Typed HttpClient for GeolocationService. The HttpClient + API key are both
// resolved through this single registration; the API key now lives in config
// (Geolocation:ApiKey), not source.
builder.Services.AddHttpClient<GeolocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p =>
    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));



//Repositories
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IBusinessLocationRepository, BusinessLocationRepository>();

//Services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBusinessLocationService, BusinessLocationService>();
builder.Services.AddHttpClient<IRoutingService, RoutingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p =>
    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// Shared Redis snapshot store + hub-context notifier so non-hub code
// (event consumers, jobs) can mutate and broadcast tracking snapshots.
builder.Services.AddSingleton<IOrderTrackingSnapshotStore, OrderTrackingSnapshotStore>();
builder.Services.AddSingleton<ITrackingNotifier, TrackingNotifier>();

//EventPublishers
builder.Services.AddSingleton<IEventPublisher>(sp =>
    TrackingEventPublisher.CreateAsync(sp.GetRequiredService<IConnection>()).GetAwaiter().GetResult());

//Consumers
builder.Services.AddSingleton<IConsumer, OrderCreatedConsumer>();
builder.Services.AddSingleton<IConsumer, OrderPickedUpConsumer>();
builder.Services.AddSingleton<IConsumer, OrderDeliveredConsumer>();
builder.Services.AddSingleton<IConsumer, OrderCancelledConsumer>();
builder.Services.AddSingleton<IConsumer, OrderStatusChangedConsumer>();
builder.Services.AddSingleton<IConsumer, OrderCourierPaidConsumer>();
builder.Services.AddSingleton<IConsumer, GetLocationsConsumer>();
builder.Services.AddSingleton<IConsumer, GetBusinessLocationConsumer>();
builder.Services.AddSingleton<IConsumer, GetBusinessLocationBatchConsumer>();
builder.Services.AddSingleton<IConsumer, GetLocationsBatchConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

// =======================
// AUTHENTICATION
// =======================
// Two JwtBearer schemes:
//   Default      — issuer df.auth / audience df.client (regular user tokens
//                  forwarded by the Gateway for REST endpoints).
//   TrackingHub  — issuer df.orderservice / audience df.tracking (short-lived
//                  per-order tokens for SignalR; access_token query param
//                  required for WebSocket upgrades).
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
var defaultIssuer = jwtSection["Issuer"];
var defaultAudience = jwtSection["Audience"];

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = defaultIssuer,

            ValidateAudience = true,
            ValidAudience = defaultAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // SignalR WebSocket upgrades can't carry the Authorization header, so
        // fall back to the access_token query string for hub paths. This lets a
        // user-scoped subscriber connect with their main JWT.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;

                if (path.StartsWithSegments("/hubs/courier-tracking"))
                {
                    var token =
                        context.Request.Query["access_token"].FirstOrDefault()
                        ?? context.Request.Headers["Authorization"]
                            .FirstOrDefault()?
                            .Replace("Bearer ", "");

                    if (!string.IsNullOrEmpty(token))
                    {
                        context.Token = token;
                    }
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("TrackingHub", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "df.orderservice",

            ValidateAudience = true,
            ValidAudience = "df.tracking",

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;

                if (path.StartsWithSegments("/hubs/courier-tracking"))
                {
                    var token =
                        context.Request.Query["access_token"].FirstOrDefault()
                        ?? context.Request.Headers["Authorization"]
                            .FirstOrDefault()?
                            .Replace("Bearer ", "");

                    if (!string.IsNullOrEmpty(token))
                    {
                        context.Token = token;
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Per-order tokens (TrackingHub) or the main user JWT (Bearer) both grant
    // access to the hub; the individual methods enforce their own auth (caller
    // must have order_id claim for SubscribeToOrder, or account_id+account_type
    // for SubscribeToUserOrders).
    options.AddPolicy("TrackingHubPolicy", policy =>
    {
        policy.AddAuthenticationSchemes("TrackingHub", "Bearer");
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddMemoryCache();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
})
.AddHubOptions<CourierTrackingHub>(options =>
{
    options.AddFilter<RateLimitHubFilter>();
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(
        builder.Configuration["Redis:ConnectionString"]!
    );

    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 3;
    configuration.ReconnectRetryPolicy = new ExponentialRetry(5000);

    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

// =======================
// HEALTH CHECKS
// =======================
// Health checks — DB + RabbitMQ + Redis. /health/live is process-only, /health/ready verifies deps.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: sp => builder.Configuration.GetConnectionString("TrackingServiceDatabase")!,
        name: "postgres", tags: ["ready"])
    .AddRabbitMQ(
        sp => sp.GetRequiredService<IConnection>(),
        name: "rabbitmq", tags: ["ready"])
    .AddRedis(
        sp => sp.GetRequiredService<IConnectionMultiplexer>(),
        name: "redis", tags: ["ready"]);

// =======================
// RATE LIMITING
// =======================
// Baseline per-IP fixed-window limiter on REST endpoints. The hub has its
// own RateLimitHubFilter for SendLocation, so the global limiter only
// covers /api/. Health and the hub negotiate are exempted.
// Behind a reverse proxy, configure UseForwardedHeaders so RemoteIpAddress
// reflects the real client.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Exempt health probes and SignalR transport
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("__unlimited");
        }

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

// Apply database migrations on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCustomExceptionMiddleware();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseMiddleware<InternalAuthMiddleware>();
app.UseMiddleware<UserContextMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<CourierTrackingHub>("/hubs/courier-tracking")
    .RequireAuthorization("TrackingHubPolicy");

// /health      = what Render probes by default; aliased to liveness.
// /health/live = process aliveness; /health/ready = DB + RabbitMQ + Redis checks.
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
    Log.Fatal(ex, "TrackingService terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
