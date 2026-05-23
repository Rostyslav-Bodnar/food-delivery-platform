using DF.MenuService.API.Extensions;
using DF.MenuService.API.Middlewares;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Messaging.Consumers;
using DF.MenuService.Application.Repositories;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Application.Validation;
using DF.MenuService.Infrastructure.Data;
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

const string ServiceName = "MenuService";

// Serilog — structured JSON to stdout. Bootstrap logger covers errors during host startup.
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

// OpenTelemetry — traces + metrics with OTLP exporter; respects OTEL_EXPORTER_OTLP_ENDPOINT.
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

// Add services to the container
builder.Services.AddControllers();

// FluentValidation — discover all validators in the Application assembly, run them automatically.
builder.Services.AddValidatorsFromAssemblyContaining<CreateDishRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5229") // адреса фронтенду
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// Database connection (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MenuServiceDatabase")));

// Upstash-friendly: a full StackExchange.Redis connection string
// (`host:port,password=...,ssl=true,abortConnect=false`) carries auth + TLS,
// which the old Host/Port split couldn't. In production this is overridden
// by the `Redis__ConnectionString` env var on Render.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

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
        RequestedHeartbeat = TimeSpan.FromSeconds(60),
        ClientProvidedName = "MenuService"
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

// Consumers
builder.Services.AddSingleton<IConsumer, GetDishesConsumer>();
builder.Services.AddSingleton<IConsumer, GetDishConsumer>();
builder.Services.AddSingleton<IConsumer, GetDishesBatchConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

builder.Services.AddAuthorization();

// RPC client
builder.Services.AddSingleton(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    return UserServiceRpcClient.CreateAsync(connection).GetAwaiter().GetResult();
});

//Services
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

//Repositories
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();

// Build the app
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

// Request logging — populates Serilog with status code, elapsed ms, traceId.
app.UseSerilogRequestLogging();

await app.RunAsync();
return 0;

}
catch (Exception ex)
{
    Log.Fatal(ex, "MenuService terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}