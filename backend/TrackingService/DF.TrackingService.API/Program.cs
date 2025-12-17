using DF.TrackingService.Application.Messaging.Consumers;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Infrastructure.Data;
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
        policy.WithOrigins("http://localhost:5173") // адреса фронтенду
            .AllowAnyHeader()                     // дозволяємо всі заголовки
            .AllowAnyMethod()                   // дозволяємо всі HTTP методи
            .AllowCredentials();               // розкоментуй, якщо потрібні куки або авторизація
    });
});

// Database connection (PostgreSQL)
builder.Services.AddDbContext<SqlDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()
    ));

builder.Services.AddDbContext<MongoDbContext>(options =>
    options.UseMongoDB(builder.Configuration.GetConnectionString("MongoDdConnection"), "TrackingDb"));



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

builder.Services.AddHttpClient<GeolocationService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddSingleton(sp => new GeolocationService(
    sp.GetRequiredService<HttpClient>(),
    "693f09a9c9da2935478707hjvd2ab0f" // твій API key
));



//Repositories
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IBusinessLocationRepository, BusinessLocationRepository>();
builder.Services.AddScoped<ICourierLocationRepository, CourierLocationRepository>();

//Services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBusinessLocationService, BusinessLocationService>();
builder.Services.AddHttpClient<IRoutingService, RoutingService>();

//EventPublishers
builder.Services.AddSingleton<IEventPublisher, TrackingEventPublisher>();

//Consumers
builder.Services.AddSingleton<IConsumer, OrderCreatedConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
    db.Database.Migrate(); 
}

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();