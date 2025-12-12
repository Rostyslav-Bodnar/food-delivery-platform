using System.Text;
using System.Text.Json.Serialization;
using DF.UserService.Application.Factories;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Messaging;
using DF.UserService.Application.Messaging.Consumers;
using DF.UserService.Application.Repositories;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

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
// MSSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserServiceMSSQLDatabase")));

// Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT 
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key")!;
var issuer = jwtSection.GetValue<string>("Issuer");
var audience = jwtSection.GetValue<string>("Audience");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Для refresh token endpoints не потрібна додаткова логіка тут
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

//Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();


//Builders
builder.Services.AddScoped<IAccountFactory, AccountFactory>();

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

// Consumer
builder.Services.AddSingleton<IConsumer, GetAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetBusinessAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetCustomerAccountConsumer>();
builder.Services.AddSingleton<IConsumer, GetCourierAccountConsumer>();

builder.Services.AddHostedService<ConsumerHostedService>();



builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.IncludeFields = true;
        o.JsonSerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); 
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
