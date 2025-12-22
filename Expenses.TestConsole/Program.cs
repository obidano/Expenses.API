using Expenses.API.framework.Data;
using Expenses.API.Domain.Ussd.Handlers;
using Expenses.API.Domain.Ussd.Models;
using Expenses.API.Domain.Ussd.Services;
using Expenses.API.Framework.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

Console.WriteLine("===========================================");
Console.WriteLine("   USSD Controller Test Console");
Console.WriteLine("===========================================\n");

// Setup configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup dependency injection
var services = new ServiceCollection();

// Add configuration
services.AddSingleton<IConfiguration>(configuration);

// Add DbContext
string? conn = configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(conn))
{
    Console.WriteLine("ERROR: Database connection string 'Default' is missing!");
    return;
}
services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

// Add Redis
string? redisConnection = configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redisConnection))
{
    Console.WriteLine("ERROR: Redis connection string is missing!");
    return;
}

try
{
    services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnection));
}
catch (Exception ex)
{
    Console.WriteLine($"WARNING: Could not connect to Redis: {ex.Message}");
    Console.WriteLine("Continuing with test...\n");
}

// Add logging services
services.AddLogging();

// Add application services
services.AddApplicationServices();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get required services
var stateService = serviceProvider.GetRequiredService<UssdStateService>();
var mainMenuHandler = serviceProvider.GetRequiredService<MainMenuHandler>();

Console.WriteLine("Services initialized successfully!\n");

// Simulate USSD Controller logic
async Task<UssdResponse> ProcessUssdRequest(UssdRequest request)
{
    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
    {
        Console.WriteLine("ERROR: Phone number is required");
        return UssdResponse.End("Error: Phone number is required");
    }

    // Get or create state
    var state = await stateService.GetStateAsync(request.PhoneNumber);

    if (state == null)
    {
        // New session - initialize state
        state = new UssdState
        {
            PhoneNumber = request.PhoneNumber,
            SessionId = request.SessionId,
            CurrentMenu = "MainMenu",
            CurrentStep = 0
        };
    }
    else
    {
        // Update session ID if provided
        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            state.SessionId = request.SessionId;
        }
    }

    // Route and handle request through MainMenuHandler
    var result = await mainMenuHandler.HandleAsync(request, state);

    // Get the updated state from the result
    var updatedState = result.UpdatedState;
    var response = result.Response;

    // Save updated state (unless session ended)
    if (response.Type == "CON")
    {
        await stateService.SaveStateAsync(request.PhoneNumber, updatedState);
    }
    else
    {
        // Clear state when session ends
        await stateService.ClearStateAsync(request.PhoneNumber);
    }

    return response;
}

// Test session simulator
async Task RunTestSession()
{
    Console.WriteLine("Starting USSD Test Session");
    Console.WriteLine("===========================================\n");

    // Get phone number
    Console.Write("Enter phone number (e.g., +1234567890): ");
    var phoneNumber = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(phoneNumber))
    {
        Console.WriteLine("Invalid phone number. Exiting...");
        return;
    }

    var sessionId = Guid.NewGuid().ToString();
    var sessionActive = true;
    var isFirstRequest = true;

    Console.WriteLine($"\nSession ID: {sessionId}");
    Console.WriteLine($"Phone: {phoneNumber}\n");

    while (sessionActive)
    {
        try
        {
            // Create request
            var request = new UssdRequest
            {
                PhoneNumber = phoneNumber,
                SessionId = sessionId,
                Input = isFirstRequest ? "" : Console.ReadLine()?.Trim() ?? ""
            };

            isFirstRequest = false;

            // Process request
            var response = await ProcessUssdRequest(request);

            // Display response
            Console.WriteLine("\n-------------------------------------------");
            Console.WriteLine($"Response Type: {response.Type}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine(response.Message);
            Console.WriteLine("-------------------------------------------\n");

            // Check if session ended
            if (response.Type == "END")
            {
                sessionActive = false;
                Console.WriteLine("Session ended.\n");
            }
            else
            {
                Console.Write("Your input: ");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}\n");
            sessionActive = false;
        }
    }
}

// Menu
while (true)
{
    Console.WriteLine("\n===========================================");
    Console.WriteLine("Menu:");
    Console.WriteLine("1. Run USSD Test Session");
    Console.WriteLine("2. Clear all states (Redis)");
    Console.WriteLine("3. Exit");
    Console.WriteLine("===========================================");
    Console.Write("Select option: ");

    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await RunTestSession();
            break;

        case "2":
            try
            {
                var redis = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();
                var endpoints = redis.GetEndPoints();
                var server = redis.GetServer(endpoints[0]);
                
                Console.Write("\nAre you sure you want to clear all USSD states? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() == "y")
                {
                    var keys = server.Keys(pattern: "ussd:*").ToArray();
                    if (keys.Length > 0)
                    {
                        foreach (var key in keys)
                        {
                            db.KeyDelete(key);
                        }
                        Console.WriteLine($"Cleared {keys.Length} state(s) from Redis.\n");
                    }
                    else
                    {
                        Console.WriteLine("No states found in Redis.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR clearing states: {ex.Message}\n");
            }
            break;

        case "3":
            Console.WriteLine("\nExiting...");
            return;

        default:
            Console.WriteLine("\nInvalid option. Please try again.\n");
            break;
    }
}
