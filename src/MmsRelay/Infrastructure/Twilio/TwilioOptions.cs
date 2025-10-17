using System;

namespace MmsRelay.Infrastructure.Twilio;

public sealed class TwilioOptions
{
    public required string AccountSid { get; init; }
    public required string AuthToken { get; init; }
    public string? FromPhoneNumber { get; init; }
    public string? MessagingServiceSid { get; init; }
    public string BaseUrl { get; init; } = "https://api.twilio.com/2010-04-01";
    public int RequestTimeoutSeconds { get; init; } = 10;

    public RetryOptions Retry { get; init; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; init; } = new();

    public sealed class RetryOptions
    {
        public int MaxRetries { get; init; } = 3;
        public int BaseDelayMs { get; init; } = 200;
    }

    public sealed class CircuitBreakerOptions
    {
        public int SamplingDurationSeconds { get; init; } = 60;
        public double FailureRatio { get; init; } = 0.5;
        public int MinThroughput { get; init; } = 20;
        public int BreakDurationSeconds { get; init; } = 30;
    }
}
