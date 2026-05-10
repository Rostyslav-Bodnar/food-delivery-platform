using DF.MenuService.API.Extensions;
using DF.MenuService.API.Middlewares;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Messaging.Consumers;
using DF.MenuService.Application.Repositories;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{builder.Configuration["Redis:Host"]}:{builder.Configuration["Redis:Port"]}";
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// RabbitMQ connection
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = builder.Configuration.GetSection("RabbitMQ");
    var factory = new ConnectionFactory
    {
        HostName = config["HostName"],
        UserName = config["UserName"],
        Password = config["Password"],
        Port = int.Parse(config["Port"])
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// Consumers
builder.Services.AddSingleton<IConsumer, GetDishesConsumer>();
builder.Services.AddSingleton<IConsumer, GetDishConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

builder.Services.AddAuthorization();

// RPC client
builder.Services.AddSingleton<UserServiceRpcClient>();

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