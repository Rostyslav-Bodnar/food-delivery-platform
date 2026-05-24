using System.Net;
using System.Text;
using DF.Gateway.API.Extensions;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

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
    var allowedOrigin = builder.Configuration["AllowedOrigins:Url"]
                        ?? "http://localhost:5173";

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
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
        // 30s accommodates dev cold-starts and the MenuService → UserService
        // RPC chain on the first request. In production this should drop to
        // ~10s once cold-start is mitigated (warm instances, prefetched RPC
        // clients, downstream caching).
        client.Timeout = TimeSpan.FromSeconds(60);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression =
            DecompressionMethods.GZip |
            DecompressionMethods.Deflate |
            DecompressionMethods.Brotli
    })
    // Retry on transient downstream failures (5xx, 408, network errors). Exponential backoff,
    // up to 3 attempts. Only retry idempotent verbs to avoid double-submits on POST.
    .AddPolicyHandler((sp, req) =>
        HttpMethod.Get.Equals(req.Method) || HttpMethod.Head.Equals(req.Method) || HttpMethod.Options.Equals(req.Method)
            ? HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)))
            : Policy.NoOpAsync<HttpResponseMessage>());

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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// cors BEFORE auth
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();