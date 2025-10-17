using System.Net.Http.Headers;
using FluentValidation;
using MmsRelay.Client.Application;
using MmsRelay.Client.Application.Validation;
using MmsRelay.Client.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace MmsRelay.Client.Services;

/// <summary>
/// Extension methods for configuring MmsRelay client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MmsRelay client services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action for client options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMmsRelayClient(
        this IServiceCollection services, 
        Action<MmsRelayClientOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        // Configure options
        services.Configure(configure);

        // Add validation
        services.AddScoped<IValidator<Application.Models.SendMmsCommand>, SendMmsCommandValidator>();

        // Add typed HttpClient with Polly policies
        services.AddHttpClient<MmsRelayHttpClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MmsRelayClientOptions>>().Value;
            
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds + 5)); // Buffer for Polly timeout
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MmsRelay.Client", "1.0.0"));
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MmsRelayClientOptions>>().Value;

            // Timeout policy (innermost)
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)),
                TimeoutStrategy.Optimistic);

            // Retry policy with exponential backoff + jitter
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => (int)response.StatusCode is 429 or >= 500)
                .WaitAndRetryAsync(options.Retry.MaxRetries, retryAttempt =>
                {
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200));
                    var exponentialDelay = TimeSpan.FromMilliseconds(options.Retry.BaseDelayMs * Math.Pow(2, retryAttempt - 1));
                    return exponentialDelay + jitter;
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = serviceProvider.GetService<ILogger<MmsRelayHttpClient>>();
                    logger?.LogWarning("Retry {RetryCount}/{MaxRetries} for MmsRelay request after {Delay}ms delay. Reason: {Reason}",
                        retryCount, options.Retry.MaxRetries, timespan.TotalMilliseconds, 
                        outcome.Exception?.Message ?? $"HTTP {outcome.Result?.StatusCode}");
                });

            // Circuit breaker policy (outermost)
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => (int)response.StatusCode is >= 500)
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.CircuitBreaker.FailureRatio,
                    samplingDuration: TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                    minimumThroughput: options.CircuitBreaker.MinThroughput,
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                    onBreak: (delegateResult, duration) =>
                    {
                        var logger = serviceProvider.GetService<ILogger<MmsRelayHttpClient>>();
                        var reason = delegateResult.Exception?.Message ?? 
                                   (delegateResult.Result != null ? $"HTTP {delegateResult.Result.StatusCode}" : "Failure threshold exceeded");
                        logger?.LogWarning("Circuit breaker opened for {Duration}s. Reason: {Reason}",
                            duration.TotalSeconds, reason);
                    },
                    onReset: () =>
                    {
                        var logger = serviceProvider.GetService<ILogger<MmsRelayHttpClient>>();
                        logger?.LogInformation("Circuit breaker reset - service is healthy again");
                    });

            // Wrap policies: timeout → retry → circuit breaker
            return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy, timeoutPolicy);
        });

        // Register the client interface
        services.AddScoped<IMmsRelayClient, MmsRelayHttpClient>();

        return services;
    }

    /// <summary>
    /// Adds MmsRelay client services with configuration from IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="sectionName">The configuration section name (default: "MmsRelayClient")</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMmsRelayClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "MmsRelayClient")
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{sectionName}' not found");

        return services.AddMmsRelayClient(options => section.Bind(options));
    }
}