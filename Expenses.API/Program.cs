using Expenses.API.framework.Data;
using Expenses.API.Framework.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// configurre entity
//string conn = "Data Source=DESKTOP-R25ASQT;Initial Catalog=asp_db_transactions;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";
string? conn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(conn))
    throw new InvalidOperationException("La cha�ne de connexion 'Default' est manquante ou vide dans la configuration.");

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

// Configure Redis
string? redisConnection = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redisConnection))
    throw new InvalidOperationException("Redis connection string is missing or empty in configuration.");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnection));

// add dependency injection for services
builder.Services.AddApplicationServices();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
// add support for swagger #1
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    //app.MapOpenApi();
    // add support for swagger#2
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS - must be before UseHttpsRedirection
app.UseCors("AllowAngularApp");

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
