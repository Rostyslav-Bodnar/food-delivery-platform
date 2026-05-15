using DF.OrderService.API.Extensions;
using DF.OrderService.API.Middlewares;
using DF.OrderService.Application.Messaging.Clients;
using DF.OrderService.Application.Messaging.Consumers;
using DF.OrderService.Application.Messaging.Publishers;
using DF.OrderService.Application.Options;
using DF.OrderService.Application.Repositories;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins("http://localhost:5229") // адреса фронтенду
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// Database connection (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// RabbitMQ connection
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(builder.Configuration["RabbitMQ:Url"]
                      ?? throw new InvalidOperationException("RabbitMQ Url is missing"))
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// RPC clients
builder.Services.AddSingleton<UserServiceRpcClient>();
builder.Services.AddSingleton<MenuServiceRpcClient>();
builder.Services.AddSingleton<TrackingServiceRpcClient>();
builder.Services.Configure<CourierCompensationOptions>(
    builder.Configuration.GetSection("CourierCompensation"));

//EventPublishers
builder.Services.AddSingleton<IEventPublisher, OrderEventPublisher>();

//Consumers
builder.Services.AddSingleton<IConsumer, LocationsCreatedConsumer>();
builder.Services.AddSingleton<IConsumer, CourierPayoutCompletedConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

builder.Services.AddAuthorization();

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

app.UseCustomExceptionMiddleware();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); 
}

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

app.Run();
