using System.Net.Http.Headers;
using FluentValidation;
using MmsRelay.Application;
using MmsRelay.Application.Validation;
using MmsRelay.Infrastructure.Twilio;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace MmsRelay.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMmsRelay(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<TwilioOptions>(configuration.GetSection("twilio"));

        // Validation
        services.AddValidatorsFromAssemblyContaining<SendMmsRequestValidator>();

        // Typed HttpClient with Polly policies
        services.AddHttpClient<TwilioMmsSender>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwilioOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwilioOptions>>().Value;

            // Timeout policy
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(Math.Max(2, options.RequestTimeoutSeconds)),
                TimeoutStrategy.Optimistic);

            // Retry policy with exponential backoff + jitter
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => (int)response.StatusCode is 429 or >= 500)
                .WaitAndRetryAsync(options.Retry.MaxRetries, retryAttempt =>
                {
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 150));
                    var exponentialDelay = TimeSpan.FromMilliseconds(options.Retry.BaseDelayMs * Math.Pow(2, retryAttempt - 1));
                    return exponentialDelay + jitter;
                });

            // Circuit breaker policy
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.CircuitBreaker.FailureRatio,
                    samplingDuration: TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                    minimumThroughput: options.CircuitBreaker.MinThroughput,
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds));

            // Wrap policies: timeout (innermost), retry, circuit breaker (outermost)
            return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
        });

        // MMS sender implementation
        services.AddScoped<IMmsSender, TwilioMmsSender>();

        return services;
    }
}