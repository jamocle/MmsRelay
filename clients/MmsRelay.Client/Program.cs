using System;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MmsRelay.Client.Application;
using MmsRelay.Client.Application.Models;
using MmsRelay.Client.Infrastructure;
using MmsRelay.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MmsRelay.Client;

/// <summary>
/// Main program entry point for the MmsRelay console client
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog early for startup logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        try
        {
            Log.Information("Starting MmsRelay Client");

            var rootCommand = CreateRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("MmsRelay Client - Send MMS messages through the MmsRelay service");

        // Create options that will be shared
        var toOption = new Option<string>(
            aliases: new[] { "--to", "-t" },
            description: "Recipient phone number in E.164 format (e.g., +15551234567)")
        {
            IsRequired = true
        };

        var bodyOption = new Option<string?>(
            aliases: new[] { "--body", "-b" },
            description: "Message body text (required if no media URLs provided)");

        var mediaOption = new Option<string?>(
            aliases: new[] { "--media", "-m" },
            description: "Comma-separated list of media URLs (required if no body provided)");

        var serviceUrlOption = new Option<string>(
            aliases: new[] { "--service-url", "-s" },
            description: "Base URL of the MmsRelay service",
            getDefaultValue: () => "http://localhost:8080");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging output");

        // Create send command
        var sendCommand = new Command("send", "Send an MMS message");
        sendCommand.AddOption(toOption);
        sendCommand.AddOption(bodyOption);
        sendCommand.AddOption(mediaOption);
        sendCommand.AddOption(serviceUrlOption);
        sendCommand.AddOption(verboseOption);

        sendCommand.SetHandler(async (string to, string? body, string? media, string serviceUrl, bool verbose) =>
        {
            var command = new SendMmsCommand
            {
                To = to,
                Body = body,
                MediaUrls = media,
                ServiceUrl = serviceUrl,
                Verbose = verbose
            };

            await HandleSendCommandAsync(command);
        }, toOption, bodyOption, mediaOption, serviceUrlOption, verboseOption);

        // Create health command
        var healthCommand = new Command("health", "Check MmsRelay service health");
        healthCommand.AddOption(serviceUrlOption);
        healthCommand.AddOption(verboseOption);

        healthCommand.SetHandler(async (string serviceUrl, bool verbose) =>
        {
            var command = new SendMmsCommand
            {
                To = "+15551234567", // Dummy value for validation
                Body = "dummy",      // Dummy value for validation
                ServiceUrl = serviceUrl,
                Verbose = verbose
            };

            await HandleHealthCommandAsync(command);
        }, serviceUrlOption, verboseOption);

        rootCommand.AddCommand(sendCommand);
        rootCommand.AddCommand(healthCommand);

        return rootCommand;
    }

    private static async Task<int> HandleSendCommandAsync(SendMmsCommand command)
    {
        return await ExecuteWithServices(command, async (services, ct) =>
        {
            var validator = services.GetRequiredService<IValidator<SendMmsCommand>>();
            var client = services.GetRequiredService<IMmsRelayClient>();
            var logger = services.GetRequiredService<ILogger<MmsRelayHttpClient>>();

            // Validate command
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                logger.LogError("Validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"  • {error.ErrorMessage}");
                }
                return 1;
            }

            // Convert command to request
            var request = new SendMmsRequest
            {
                To = command.To,
                Body = command.Body,
                MediaUrls = ParseMediaUrls(command.MediaUrls)
            };

            try
            {
                logger.LogInformation("Sending MMS to {To}...", request.To);
                
                var result = await client.SendMmsAsync(request, ct);
                
                Console.WriteLine("✓ MMS sent successfully!");
                Console.WriteLine($"  Provider: {result.Provider}");
                Console.WriteLine($"  Message ID: {result.ProviderMessageId}");
                Console.WriteLine($"  Status: {result.Status}");
                
                if (result.ProviderMessageUri is not null)
                {
                    Console.WriteLine($"  Status URL: {result.ProviderMessageUri}");
                }

                return 0;
            }
            catch (MmsRelayClientException ex)
            {
                logger.LogError(ex, "Failed to send MMS");
                Console.WriteLine($"✗ Failed to send MMS: {ex.Message}");
                
                if (ex.StatusCode is not null)
                {
                    Console.WriteLine($"  HTTP Status: {ex.StatusCode}");
                }

                return 1;
            }
        });
    }

    private static async Task<int> HandleHealthCommandAsync(SendMmsCommand command)
    {
        return await ExecuteWithServices(command, async (services, ct) =>
        {
            var client = services.GetRequiredService<IMmsRelayClient>();
            var logger = services.GetRequiredService<ILogger<MmsRelayHttpClient>>();

            try
            {
                logger.LogInformation("Checking MmsRelay service health...");
                
                var isHealthy = await client.CheckHealthAsync(ct);
                
                if (isHealthy)
                {
                    Console.WriteLine("✓ MmsRelay service is healthy");
                    return 0;
                }
                else
                {
                    Console.WriteLine("✗ MmsRelay service is unhealthy");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health check failed");
                Console.WriteLine($"✗ Health check failed: {ex.Message}");
                return 1;
            }
        });
    }

    private static async Task<int> ExecuteWithServices(
        SendMmsCommand command, 
        Func<IServiceProvider, CancellationToken, Task<int>> operation)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => 
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nOperation cancelled by user");
        };

        var host = CreateHost(command);
        await host.StartAsync(cts.Token);

        try
        {
            return await operation(host.Services, cts.Token);
        }
        finally
        {
            await host.StopAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static IHost CreateHost(SendMmsCommand command)
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure Serilog
        var logLevel = command.Verbose ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;
        
        builder.Services.AddSerilog(config => config
            .MinimumLevel.Is(logLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

        // Configure MmsRelay client
        builder.Services.AddMmsRelayClient(options =>
        {
            options.BaseUrl = command.ServiceUrl;
            options.TimeoutSeconds = 30;
            options.Retry.MaxRetries = 3;
            options.Retry.BaseDelayMs = 500;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDurationSeconds = 60;
            options.CircuitBreaker.MinThroughput = 5;
            options.CircuitBreaker.BreakDurationSeconds = 30;
        });

        return builder.Build();
    }

    private static Uri[]? ParseMediaUrls(string? mediaUrls)
    {
        if (string.IsNullOrWhiteSpace(mediaUrls))
            return null;

        return mediaUrls
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(url => new Uri(url, UriKind.Absolute))
            .ToArray();
    }
}