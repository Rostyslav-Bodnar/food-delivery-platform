using System.Text.Json.Serialization;
using DF.UserService.API.Extensions;
using DF.UserService.API.Middlewares;
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
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

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
    var config = builder.Configuration.GetSection("RabbitMQ");
    var factory = new ConnectionFactory
    {
        Uri = new Uri(config["RabbitMQ:Url"])
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
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
// RPC CLIENTS
// =======================
builder.Services.AddSingleton<TrackingServiceRpcClient>();

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

var app = builder.Build();

app.UseCustomExceptionMiddleware();

// =======================
// MIGRATIONS
// =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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
app.MapGet("/health", () => "OK");
app.Run();