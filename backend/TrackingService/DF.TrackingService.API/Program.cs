using DF.TrackingService.API.Hubs;
using DF.TrackingService.Application.Messaging.Consumers;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DF.TrackingService.API.Hubs.Filters;
using Microsoft.AspNetCore.SignalR;

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
builder.Services.AddSingleton<IConsumer, GetLocationsConsumer>();
builder.Services.AddSingleton<IConsumer, GetBusinessLocationConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = "df.orderservice",
            ValidAudience = "df.tracking",

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!
                )
            ),

            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // 🔥 КРИТИЧНО ДЛЯ SIGNALR (token через query)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/courier-tracking"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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


app.MapHub<CourierTrackingHub>("/hubs/courier-tracking");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();