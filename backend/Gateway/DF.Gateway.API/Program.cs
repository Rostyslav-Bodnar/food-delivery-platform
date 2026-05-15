using System.Text;
using DF.Gateway.API.Extensions;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =======================
// CONFIG
// =======================
var configuration = builder.Configuration;

// =======================
// CORS (frontend gateway access)
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =======================
// CONTROLLERS
// =======================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// =======================
// CORE HTTP INFRASTRUCTURE
// =======================

builder.Services.AddSingleton<ServiceResolver>();
builder.Services.AddHttpContextAccessor();

// =======================
// INTERNAL SECURITY LAYER
// =======================
builder.Services.AddSingleton<InternalAuthSigner>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new InternalAuthSigner(config);
});

// =======================
// GATEWAY CONTEXT (user context abstraction)
// =======================
builder.Services.AddScoped<InternalGatewayContext>();

// =======================
// HTTP CLIENT FOR MICROSERVICES
// =======================
builder.Services.AddHttpClient<GatewayProxy>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);

    // IMPORTANT: disable auto redirect for security in gateway
    client.DefaultRequestHeaders.ConnectionClose = false;
});

// =======================
// JWT AUTH (CLIENT → GATEWAY)
// =======================
var jwtSection = configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"]!;
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
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

    // IMPORTANT for gateway scenarios (cookies + browser)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // support both Authorization header and cookie
            var token = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(token))
                token = context.Request.Cookies["accessToken"];

            if (!string.IsNullOrEmpty(token))
                context.Token = token.Replace("Bearer ", "");

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// =======================
// APP BUILD
// =======================
var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================

// global exception handling FIRST (important)
app.UseCustomExceptionMiddleware();

// cors BEFORE auth
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();