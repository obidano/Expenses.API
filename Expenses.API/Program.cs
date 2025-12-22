using Expenses.API.framework.Data;
using Expenses.API.Framework.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Azure.StackExchangeRedis;
using Azure.Identity;
using Azure.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configure entity framework
// Connection string is automatically resolved from:
// - appsettings.json (base configuration)
// - appsettings.{Environment}.json (environment-specific, e.g., appsettings.Production.json)
// - Environment variables (highest priority)
//string conn = "Data Source=DESKTOP-R25ASQT;Initial Catalog=asp_db_transactions;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";
string? conn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(conn))
    throw new InvalidOperationException("La cha�ne de connexion 'Default' est manquante ou vide dans la configuration.");

// Replace placeholders with environment variables for production secrets
conn = conn.Replace("{SQL_PASSWORD}", Environment.GetEnvironmentVariable("SQL_PASSWORD") ?? "");

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn, options =>
{
    // Enable retry logic for transient failures (recommended for Azure SQL Database)
    options.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);
}));

// Configure Redis
string? redisConnection = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redisConnection))
    throw new InvalidOperationException("Redis connection string is missing or empty in configuration.");

// Replace placeholders with environment variables for production secrets
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
if (!string.IsNullOrEmpty(redisPassword))
{
    redisConnection = redisConnection.Replace("{REDIS_PASSWORD}", redisPassword);
}

bool useEntraId = builder.Configuration.GetValue<bool>("Redis:UseEntraId", false);

if (useEntraId)
{
    // Use Microsoft Entra ID authentication for Azure Cache for Redis
    // Extract hostname from connection string (format: hostname:port or hostname:port,password=...,ssl=True)
    var hostname = redisConnection.Split(',')[0];
    
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        // Use DefaultAzureCredential which works with:
        // - Managed Identity (when deployed to Azure)
        // - Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
        // - Azure CLI credentials (for local development)
        // - Visual Studio credentials
        var credential = new DefaultAzureCredential();
        
        // Get access token for Azure Redis
        var tokenRequestContext = new TokenRequestContext(new[] { "https://redis.azure.com/.default" });
        var token = credential.GetToken(tokenRequestContext, default);
        
        // Parse configuration options
        var configOptions = ConfigurationOptions.Parse(hostname);
        
        // Configure for Azure Entra ID authentication
        configOptions.Password = token.Token;
        configOptions.Ssl = true;
        configOptions.AbortOnConnectFail = false;
        
        // Connect synchronously
        // Note: Tokens expire after ~1 hour. For long-running apps, implement token refresh
        // or use a connection factory that refreshes tokens on reconnection
        return ConnectionMultiplexer.Connect(configOptions);
    });
}
else
{
    // Use standard password authentication (for development/local Redis)
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect(redisConnection));
}

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
