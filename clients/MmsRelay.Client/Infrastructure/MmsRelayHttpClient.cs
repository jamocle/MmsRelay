using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MmsRelay.Client.Application;
using MmsRelay.Client.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MmsRelay.Client.Infrastructure;

/// <summary>
/// HTTP client implementation for communicating with the MmsRelay service
/// </summary>
public sealed class MmsRelayHttpClient : IMmsRelayClient
{
    private readonly HttpClient _httpClient;
    private readonly MmsRelayClientOptions _options;
    private readonly ILogger<MmsRelayHttpClient> _logger;

    public MmsRelayHttpClient(
        HttpClient httpClient, 
        IOptions<MmsRelayClientOptions> options, 
        ILogger<MmsRelayHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendMmsResult> SendMmsAsync(SendMmsRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Sending MMS to {To} via MmsRelay service", request.To);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/mms", request, cancellationToken)
                .ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SendMmsResult>(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (result is null)
                    throw new MmsRelayClientException("Failed to deserialize successful response from MmsRelay service");

                _logger.LogInformation("MMS sent successfully: Provider={Provider}, MessageId={MessageId}, Status={Status}", 
                    result.Provider, result.ProviderMessageId, result.Status);

                return result;
            }

            // Handle error responses
            var statusCode = (int)response.StatusCode;
            _logger.LogWarning("MMS send failed: HTTP {StatusCode} - {Content}", 
                statusCode, TruncateContent(content, 1000));

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => $"Invalid request: {ExtractErrorMessage(content)}",
                HttpStatusCode.Unauthorized => "Authentication failed - check MmsRelay service configuration",
                HttpStatusCode.Forbidden => "Access denied - insufficient permissions",
                HttpStatusCode.ServiceUnavailable => "MmsRelay service is temporarily unavailable",
                HttpStatusCode.TooManyRequests => "Rate limit exceeded - please try again later",
                _ => $"MmsRelay service error (HTTP {statusCode})"
            };

            throw new MmsRelayClientException(errorMessage, statusCode, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while communicating with MmsRelay service");
            throw new MmsRelayClientException("Failed to communicate with MmsRelay service - check network connectivity and service URL", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while communicating with MmsRelay service");
            throw new MmsRelayClientException("Request timed out - MmsRelay service may be unresponsive", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("MMS send operation was cancelled");
            throw new OperationCanceledException("MMS send operation was cancelled", ex, cancellationToken);
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking MmsRelay service health");

        try
        {
            using var response = await _httpClient.GetAsync("/health/live", cancellationToken)
                .ConfigureAwait(false);

            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("MmsRelay service health check: {Status} (HTTP {StatusCode})", 
                isHealthy ? "Healthy" : "Unhealthy", (int)response.StatusCode);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed - MmsRelay service may be unreachable");
            return false;
        }
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...";
    }

    private static string ExtractErrorMessage(string content)
    {
        // Simple extraction - in production you might parse JSON problem details
        if (string.IsNullOrWhiteSpace(content))
            return "Unknown error";

        // Try to extract meaningful error from common patterns
        if (content.Contains("validation", StringComparison.OrdinalIgnoreCase))
            return "Validation failed";
        
        if (content.Contains("phone", StringComparison.OrdinalIgnoreCase))
            return "Invalid phone number";

        return TruncateContent(content, 200);
    }
}

/// <summary>
/// Configuration options for the MmsRelay client
/// </summary>
public sealed class MmsRelayClientOptions
{
    /// <summary>
    /// Base URL of the MmsRelay service
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    public sealed class RetryOptions
    {
        public int MaxRetries { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 500;
    }

    public sealed class CircuitBreakerOptions
    {
        public int SamplingDurationSeconds { get; set; } = 60;
        public double FailureRatio { get; set; } = 0.5;
        public int MinThroughput { get; set; } = 10;
        public int BreakDurationSeconds { get; set; } = 30;
    }
}

/// <summary>
/// Exception thrown when the MmsRelay client encounters an error
/// </summary>
public sealed class MmsRelayClientException : Exception
{
    public int? StatusCode { get; }
    public string? ResponseContent { get; }

    public MmsRelayClientException(string message) : base(message)
    {
    }

    public MmsRelayClientException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public MmsRelayClientException(string message, int statusCode, string? responseContent = null) : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}