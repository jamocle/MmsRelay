using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MmsRelay.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog console JSON
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = false;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Add MMS relay services
builder.Services.AddMmsRelay(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseProblemDetails();

// Map API endpoints
app.MapMmsEndpoints();

// Log startup messages after the host has fully started
app.Lifetime.ApplicationStarted.Register(() =>
{
    // Option 1: Serilog's static API
    Log.Information("MmsRelay started via Serilog static API 👋");

    // Option 2: ASP.NET Core ILogger (goes through Serilog in this app)
    app.Logger.LogInformation("MmsRelay started via ASP.NET Core ILogger 👋");

    // Option 3: Scoped ILogger<T> for specific class logging
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("MmsRelay started via scoped ILogger<Program> 👋");
});


app.Run();
