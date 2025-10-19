using DF.MenuService.Application.Repositories;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database connection (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//Services
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IMenuService, MenuService>();

//Repositories
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();


// Build the app
var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();