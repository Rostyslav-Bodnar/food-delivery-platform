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
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// 1) Minimal pipeline & basic services
// ------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // your existing OpenAPI helper

// ------------------------------------------------------------
// 2) Database (PostgreSQL)
//    Scoped by default — правильно для EF Core
// ------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Якщо хочете snake_case для всіх таблиць/полів:
    // options.UseNpgsql(...).UseSnakeCaseNamingConvention();
});

// Для Npgsql часом корисно явно ввімкнути legacy timestamp behavior (якщо мігруєте зі старих версій):
// AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // адреса фронтенду
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// ------------------------------------------------------------
// 3) RabbitMQ
//    IConnection — Singleton; IEventBus — Singleton
// ------------------------------------------------------------
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = builder.Configuration.GetSection("RabbitMQ");
    var factory = new ConnectionFactory
    {
        HostName = config["HostName"]!,
        UserName = config["UserName"]!,
        Password = config["Password"]!,
        Port = int.Parse(config["Port"] ?? "5672")
    };
    // Створення асинхронного конекшена (чекаємо до готовності)
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// ВАЖЛИВО: EventBus — Singleton і не тримає scoped-сервіси у конструкторі.
// Він приймає IServiceScopeFactory і створює scope per message всередині підписників.
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    // exchangeName/prefetch/maxRetries можна винести у конфіг
    return new RabbitMQEventBus(
        connection: connection,
        scopeFactory: scopeFactory,
        exchangeName: "df.events",
        prefetchCount: 32,
        maxRetries: 3);
});

// ------------------------------------------------------------
// 4) Domain/Application infrastructure (Scoped)
// ------------------------------------------------------------
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICourierPayoutService, CourierPayoutService>();

// Command handlers (Scoped)
builder.Services.AddScoped<CreatePaymentCommandHandler>();
builder.Services.AddScoped<CollectCashCommandHandler>();
builder.Services.AddScoped<CancelPaymentCommandHandler>();
builder.Services.AddScoped<CreateStripePaymentIntentCommandHandler>();
builder.Services.AddScoped<RefundPaymentCommandHandler>();

// Ідемпотентність (Scoped)
builder.Services.AddScoped<IProcessedMessageStore, ProcessedMessageStore>();
builder.Services.AddScoped<IProcessedWebhookStore, ProcessedWebhookStore>();

// ------------------------------------------------------------
// 5) Stripe
//    IStripeService — Singleton (StripeClient thread-safe, ключ через IOptions)
// ------------------------------------------------------------
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripeService, StripeService>();

// ------------------------------------------------------------
// 6) Background Options (Singleton) + Hosted Services (Singleton)
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

// Consumer слухає події з шини; залежить лише від IEventBus (Singleton) і IServiceScopeFactory
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<OrderCancelledConsumer>();
builder.Services.AddHostedService<OrderDeliveredConsumer>();
builder.Services.AddHostedService<PaymentSucceededConsumer>();

// ------------------------------------------------------------
// 7) Build & pipeline
// ------------------------------------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
