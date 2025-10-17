# MmsRelay - Architecture and Code Knowledge

## 🏗️ Overview

MmsRelay is a .NET 8 minimal API that provides a resilient MMS (Multimedia Messaging Service) relay service. It follows Clean Architecture principles with proper separation of concerns, comprehensive validation, and robust error handling.

**Key Features:**
- Clean Architecture with Application/Infrastructure separation
- FluentValidation for request validation
- Polly resilience policies (timeout, retry, circuit breaker)
- Structured logging with Serilog
- Twilio integration for MMS delivery
- Comprehensive test coverage

## 📁 Project Structure

```
MmsRelay/
├── src/MmsRelay/
│   ├── Program.cs                          # Application entry point
│   ├── appsettings.json                    # Configuration
│   ├── Api/
│   │   ├── MmsEndpoints.cs                 # HTTP endpoint definitions
│   │   ├── ServiceCollectionExtensions.cs # DI service registration
│   │   └── ProblemDetailsExtensions.cs     # Error handling utilities
│   ├── Application/
│   │   ├── IMmsSender.cs                   # Core business interface
│   │   ├── Models/
│   │   │   ├── SendMmsRequest.cs           # Input model
│   │   │   └── SendMmsResult.cs            # Output model
│   │   └── Validation/
│   │       └── SendMmsRequestValidator.cs  # FluentValidation rules
│   └── Infrastructure/
│       └── Twilio/
│           ├── TwilioMmsSender.cs          # Twilio implementation
│           └── TwilioOptions.cs            # Configuration model
└── tests/MmsRelay.Tests/
    ├── SendMmsRequestValidatorTests.cs     # Validation tests
    └── TwilioMmsSenderTests.cs            # Infrastructure tests
```

## 🚀 Application Flow

### 1. Request Processing Pipeline

```
HTTP POST /mms
    ↓
JSON Deserialization → SendMmsRequest
    ↓
FluentValidation → ValidationResult
    ↓
IMmsSender.SendAsync() → TwilioMmsSender
    ↓
HTTP Client + Polly Policies → Twilio API
    ↓
Response Processing → SendMmsResult
    ↓
HTTP 202 Accepted + Location Header
```

---

## 📋 Detailed Code Walkthrough

### Program.cs - Application Bootstrap

**Purpose:** Entry point that configures services, middleware, and starts the application.

```csharp
// 1. Serilog Configuration (Lines 9-14)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()        // Log level threshold
    .Enrich.FromLogContext()          // Add contextual properties
    .WriteTo.Console(...)             // JSON console output
    .CreateLogger();
```

**Serilog Features:**
- **Structured Logging**: Outputs JSON for easy parsing
- **Log Context Enrichment**: Can add correlation IDs, user info, etc.
- **Console Sink**: Suitable for containerized applications

```csharp
// 2. JSON Serialization (Lines 18-24)
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(...));
});
```

**JSON Configuration:**
- **CamelCase**: Converts C# PascalCase to JavaScript-friendly camelCase
- **Null Handling**: Omits null values from responses (cleaner JSON)
- **Enum Conversion**: Serializes enums as strings instead of numbers

```csharp
// 3. Service Registration (Line 27)
builder.Services.AddMmsRelay(builder.Configuration);
```

**Extension Method Benefits:**
- Encapsulates complex service registration
- Makes Program.cs cleaner and more focused
- Enables easier testing and modularity

```csharp
// 4. Middleware Pipeline (Lines 32-33)
app.UseSerilogRequestLogging();    // Logs every HTTP request/response
app.UseProblemDetails();           // Standardizes error responses (RFC 7807)
```

**Middleware Order Matters:**
- Request logging should be early to capture all requests
- Problem details should be before custom endpoints

```csharp
// 5. Logging Examples (Lines 39-47)
Log.Information("...");                    // Option 1: Serilog static API
app.Logger.LogInformation("...");          // Option 2: ASP.NET Core ILogger
logger.LogInformation("...");              // Option 3: Scoped ILogger<T>
```

**Three Logging Approaches:**
1. **Static API**: Direct Serilog access, simple but couples to Serilog
2. **Framework Logger**: Uses ASP.NET Core abstractions, routes through Serilog
3. **Typed Logger**: Best for class-specific logging, enables filtering

---

### ServiceCollectionExtensions.cs - Dependency Injection

**Purpose:** Configures all MMS-related services including HttpClient, Polly policies, and validation.

```csharp
// 1. Configuration Binding (Line 17)
services.Configure<TwilioOptions>(configuration.GetSection("twilio"));
```

**Configuration Pattern:**
- Binds `appsettings.json` "twilio" section to strongly-typed `TwilioOptions`
- Enables configuration validation and IntelliSense
- Supports configuration providers (environment variables, Azure Key Vault, etc.)

```csharp
// 2. Validation Registration (Line 20)
services.AddValidatorsFromAssemblyContaining<SendMmsRequestValidator>();
```

**FluentValidation Benefits:**
- Automatic discovery of validators in assembly
- Rich validation rule DSL
- Async validation support
- Easy testing

```csharp
// 3. HttpClient Configuration (Lines 23-29)
services.AddHttpClient<TwilioMmsSender>((serviceProvider, httpClient) => {
    var options = serviceProvider.GetRequiredService<IOptions<TwilioOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
    httpClient.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
```

**Typed HttpClient Pattern:**
- Creates HttpClient specifically for `TwilioMmsSender`
- Manages HttpClient lifetime (prevents socket exhaustion)
- Enables configuration from appsettings
- Supports dependency injection of configuration

```csharp
// 4. Polly Resilience Policies (Lines 30-57)
.AddPolicyHandler((serviceProvider, request) => {
    // Timeout Policy (innermost)
    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(Math.Max(2, options.RequestTimeoutSeconds)),
        TimeoutStrategy.Optimistic);

    // Retry Policy with Exponential Backoff + Jitter
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => (int)response.StatusCode is 429 or >= 500)
        .WaitAndRetryAsync(options.Retry.MaxRetries, retryAttempt => {
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 150));
            var exponentialDelay = TimeSpan.FromMilliseconds(options.Retry.BaseDelayMs * Math.Pow(2, retryAttempt - 1));
            return exponentialDelay + jitter;
        });

    // Circuit Breaker Policy (outermost)
    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .AdvancedCircuitBreakerAsync(
            failureThreshold: options.CircuitBreaker.FailureRatio,
            samplingDuration: TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
            minimumThroughput: options.CircuitBreaker.MinThroughput,
            durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds));

    // Policy Wrapping: timeout → retry → circuit breaker
    return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
});
```

**Polly Resilience Strategy:**

**Layer 1 - Timeout Policy (Innermost):**
- Cancels individual requests that take too long
- Prevents hanging requests from consuming resources
- Uses `OptimisticTimeout` (cooperative cancellation)

**Layer 2 - Retry Policy:**
- **Handles**: Network errors, timeouts, HTTP 429 (rate limiting), 5xx server errors
- **Exponential Backoff**: 200ms → 400ms → 800ms (configurable base delay)
- **Jitter**: Adds 0-150ms randomness to prevent thundering herd effect
- **Formula**: `baseDelay * 2^(attempt-1) + random(0-150ms)`

**Layer 3 - Circuit Breaker (Outermost):**
- **Advanced Circuit Breaker**: More sophisticated than simple failure count
- **Failure Threshold**: Opens circuit when failure rate exceeds percentage
- **Sampling Duration**: Time window for measuring failure rate
- **Minimum Throughput**: Minimum requests needed before considering failure rate
- **Break Duration**: How long circuit stays open before trying again

**Policy Wrapping Order:**
```
Circuit Breaker (outer)
    ↓
Retry Policy (middle)
    ↓
Timeout Policy (inner)
    ↓
HTTP Request
```

---

### MmsEndpoints.cs - HTTP API Layer

**Purpose:** Defines the HTTP endpoints and request/response handling logic.

```csharp
// Minimal API Endpoint Definition (Lines 12-16)
app.MapPost("/mms", async (
    SendMmsRequest request,           // JSON body → model binding
    IValidator<SendMmsRequest> validator, // DI: FluentValidation
    IMmsSender sender,               // DI: Business logic interface
    CancellationToken cancellationToken  // Framework: cancellation support
) => {
    // Endpoint logic...
});
```

**Parameter Binding Magic:**
- **`SendMmsRequest`**: ASP.NET Core automatically deserializes JSON body
- **`IValidator<T>`**: Dependency injection provides the registered validator
- **`IMmsSender`**: DI provides implementation (TwilioMmsSender)
- **`CancellationToken`**: Framework provides for request cancellation

```csharp
// Validation Flow (Lines 18-22)
ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
if (!validationResult.IsValid) {
    return Results.ValidationProblem(validationResult.ToDictionary());
}
```

**Validation Pattern:**
- **Async Validation**: Supports database lookups, external service calls
- **Detailed Errors**: Returns specific field-level validation messages
- **Standard Format**: `ValidationProblem` follows RFC 7807 Problem Details
- **HTTP 400**: Correct status code for validation failures

```csharp
// Business Logic Execution (Lines 24-25)
var result = await sender.SendAsync(request, cancellationToken);
return Results.Accepted($"/mms/{result.Provider}/{result.ProviderMessageId}", result);
```

**Response Strategy:**
- **HTTP 202 Accepted**: Correct for async operations (MMS sending is async)
- **Location Header**: Points to where client could check status later
- **Response Body**: Includes immediate result with provider details

```csharp
// OpenAPI Documentation (Lines 27-34)
.WithName("RelayMms")
.WithSummary("Send MMS via configured provider")
.WithDescription("...")
.Produces<SendMmsResult>(202)
.ProducesProblem(400)
.ProducesProblem(500)
.WithTags("MMS");
```

**API Documentation Benefits:**
- **Swagger/OpenAPI**: Auto-generates interactive documentation
- **Client Generation**: Enables auto-generated client SDKs
- **Contract First**: Documents expected behavior clearly

---

### Application Models - Domain Layer

#### SendMmsRequest.cs - Input Model

```csharp
public sealed class SendMmsRequest
{
    public required string To { get; init; }
    public string? Body { get; init; }
    public IReadOnlyList<Uri>? MediaUrls { get; init; }
}
```

**Design Principles:**
- **`sealed`**: Prevents inheritance (performance optimization)
- **`required`**: C# 11 feature ensuring property must be set
- **`init`**: Properties can only be set during object initialization (immutability)
- **`IReadOnlyList<Uri>`**: Strongly typed, immutable collection

**Business Rules:**
- **Phone Number**: Must be provided, validated as E.164 format
- **Content**: Either Body OR MediaUrls must be provided (enforced by validator)
- **Media URLs**: Must be HTTP/HTTPS (enforced by validator)

#### SendMmsResult.cs - Output Model

```csharp
public sealed class SendMmsResult
{
    public required string Provider { get; init; }
    public required string ProviderMessageId { get; init; }
    public required string Status { get; init; }
    public Uri? ProviderMessageUri { get; init; }
}
```

**Provider Abstraction:**
- **Provider**: Which service handled the request ("twilio", "sendgrid", etc.)
- **ProviderMessageId**: External tracking ID (Twilio SID, SendGrid message ID)
- **Status**: Current state in provider's system ("queued", "sent", "delivered")
- **ProviderMessageUri**: Link to check status in provider's dashboard

---

### IMmsSender.cs - Business Interface

```csharp
public interface IMmsSender
{
    Task<SendMmsResult> SendAsync(SendMmsRequest request, CancellationToken ct);
}
```

**Interface Design Benefits:**
- **Single Responsibility**: Only concerned with sending MMS
- **Async Pattern**: Non-blocking operations
- **Cancellation Support**: Respects request timeouts
- **Provider Abstraction**: Easy to swap implementations

**Possible Implementations:**
- `TwilioMmsSender` (current)
- `SendGridMmsSender`
- `AwsSnsMmsSender`
- `MockMmsSender` (for testing)
- `CompositeMultiProviderSender` (fallback logic)

---

### SendMmsRequestValidator.cs - Validation Rules

```csharp
public sealed class SendMmsRequestValidator : AbstractValidator<SendMmsRequest>
{
    public SendMmsRequestValidator()
    {
        // E.164 Phone Number Validation
        RuleFor(x => x.To)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{1,14}$")
            .WithMessage("To must be E.164, e.g., +15551234567.");

        // Body Length Validation
        RuleFor(x => x.Body)
            .MaximumLength(1600);

        // Media URL Validation
        RuleForEach(x => x.MediaUrls ?? Array.Empty<Uri>())
            .Must(uri => uri.Scheme is "http" or "https")
            .WithMessage("MediaUrls must be HTTP/HTTPS.");

        // Business Rule: Body OR Media Required
        RuleFor(x => x)
            .Must(x => !(string.IsNullOrWhiteSpace(x.Body) && (x.MediaUrls is null || x.MediaUrls.Count == 0)))
            .WithMessage("Either Body or at least one MediaUrl must be provided.");
    }
}
```

**Validation Rules Explained:**

**E.164 Phone Format:**
- **Pattern**: `^\+[1-9]\d{1,14}$`
- **Breakdown**: `+` followed by 1-9 (country code can't start with 0), then 1-14 digits
- **Examples**: `+1555123456`, `+442071234567`, `+86123456789`

**Body Length:**
- **SMS Limit**: 160 characters per segment
- **MMS Limit**: 1600 characters (10 SMS segments)
- **Twilio Limit**: Aligns with Twilio's limits

**Media URL Security:**
- **HTTPS Preferred**: Secure transmission of media
- **HTTP Allowed**: For development/testing
- **No File URLs**: Prevents server-side request forgery

**Business Logic:**
- **Flexible Content**: Either text, media, or both
- **Not Both Empty**: Prevents pointless messages
- **RuleFor(x => x)**: Validates entire object for complex rules

---

### TwilioOptions.cs - Configuration Model

```csharp
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
}
```

**Configuration Strategy:**
- **Required Fields**: AccountSid, AuthToken must be provided
- **Flexible Sender**: Either phone number OR messaging service
- **Sensible Defaults**: BaseUrl, timeouts have reasonable defaults
- **Nested Configuration**: Retry and circuit breaker settings grouped

**From Configuration vs Messaging Service:**
- **FromPhoneNumber**: Simple, single phone number
- **MessagingServiceSid**: Twilio feature for load balancing across multiple numbers

---

### TwilioMmsSender.cs - Infrastructure Implementation

```csharp
public async Task<SendMmsResult> SendAsync(SendMmsRequest request, CancellationToken ct)
{
    // 1. Configuration Validation
    if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        throw new InvalidOperationException("Twilio AccountSid/AuthToken must be configured.");

    // 2. Build Twilio API Request
    var endpoint = $"{_opts.BaseUrl}/Accounts/{WebUtility.UrlEncode(accountSid)}/Messages.json";
    var fields = new List<KeyValuePair<string, string>>();
    
    // 3. Form Data Construction
    if (!string.IsNullOrWhiteSpace(request.Body))
        fields.Add(new("Body", request.Body!));
    
    if (!string.IsNullOrWhiteSpace(_opts.MessagingServiceSid))
        fields.Add(new("MessagingServiceSid", _opts.MessagingServiceSid!));
    else if (!string.IsNullOrWhiteSpace(_opts.FromPhoneNumber))
        fields.Add(new("From", _opts.FromPhoneNumber!));
    else
        throw new InvalidOperationException("Either MessagingServiceSid or FromPhoneNumber required.");

    fields.Add(new("To", request.To));

    if (request.MediaUrls is { Count: > 0 })
    {
        foreach (var mediaUri in request.MediaUrls)
            fields.Add(new("MediaUrl", mediaUri.ToString()));
    }

    // 4. HTTP Request Construction
    using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
    var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    req.Content = new FormUrlEncodedContent(fields);

    // 5. HTTP Request Execution (with Polly policies)
    using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    var content = await resp.Content.ReadAsStringAsync(ct);

    // 6. Error Handling
    if (!resp.IsSuccessStatusCode)
    {
        _logger.LogWarning("Twilio send failed: {Status} {Content}", (int)resp.StatusCode, Truncate(content, 1024));
        throw new TwilioApiException((int)resp.StatusCode, content);
    }

    // 7. Response Parsing
    using var doc = JsonDocument.Parse(content);
    string sid = doc.RootElement.GetProperty("sid").GetString()!;
    string status = doc.RootElement.TryGetProperty("status", out var statusEl)
        ? statusEl.GetString() ?? "unknown"
        : "queued";

    Uri? messageUri = doc.RootElement.TryGetProperty("uri", out var uriEl)
        ? TryParseUri("https://api.twilio.com" + uriEl.GetString())
        : null;

    return new SendMmsResult
    {
        Provider = "twilio",
        ProviderMessageId = sid,
        Status = status,
        ProviderMessageUri = messageUri
    };
}
```

**Implementation Details:**

**Twilio API Integration:**
- **REST API**: Uses Twilio's REST API for sending messages
- **Form Encoding**: Twilio expects `application/x-www-form-urlencoded`
- **Basic Auth**: Uses AccountSid:AuthToken as Basic authentication
- **Multiple Media**: Supports multiple `MediaUrl` parameters

**Error Handling Strategy:**
- **Configuration Errors**: Fail fast with descriptive messages
- **HTTP Errors**: Log details and throw custom exception
- **Parsing Errors**: Let JSON exceptions bubble up (unexpected format)

**Response Processing:**
- **SID**: Twilio's unique message identifier
- **Status**: Initial status (usually "queued" for async processing)
- **URI**: Link to Twilio's API for status checking
- **Defensive Parsing**: Handles missing optional fields gracefully

---

## 🧪 Testing Strategy

### Unit Tests

**SendMmsRequestValidatorTests.cs:**
- Tests all validation rules
- Covers edge cases (boundary conditions)
- Tests error messages

**TwilioMmsSenderTests.cs:**
- Mock HTTP responses
- Test success and failure scenarios
- Verify request construction

### Integration Tests (Potential)
- Full HTTP pipeline testing
- Real configuration binding
- End-to-end validation

---

## 🔧 Configuration

### appsettings.json Structure

```json
{
  "urls": "http://0.0.0.0:8080",
  "logging": {
    "levelSwitch": "Information"
  },
  "twilio": {
    "accountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "authToken": "************",
    "fromPhoneNumber": "+15551234567",
    "messagingServiceSid": null,
    "baseUrl": "https://api.twilio.com/2010-04-01",
    "requestTimeoutSeconds": 10,
    "retry": {
      "maxRetries": 3,
      "baseDelayMs": 200
    },
    "circuitBreaker": {
      "samplingDurationSeconds": 60,
      "failureRatio": 0.5,
      "minThroughput": 20,
      "breakDurationSeconds": 30
    }
  }
}
```

**Configuration Best Practices:**
- **Secrets Management**: Use User Secrets in development, Azure Key Vault in production
- **Environment Variables**: Override settings per environment
- **Validation**: TwilioOptions validates configuration at startup

---

## 🚀 Deployment Considerations

### Docker Support
- Uses port 8080 (standard for containers)
- Console JSON logging (container-friendly)
- Health checks could be added

### Monitoring
- Structured logging with Serilog
- Polly metrics could be exposed
- Application Insights integration possible

### Scaling
- Stateless design (scales horizontally)
- HttpClient connection pooling
- Circuit breaker prevents cascading failures

---

## 🔮 Future Enhancements

### Multi-Provider Support
```csharp
public interface IMmsProviderFactory
{
    IMmsSender GetProvider(string providerName);
}

public class CompositeSmsSender : IMmsSender
{
    // Fallback logic: try Twilio, then SendGrid, then AWS
}
```

### Message Status Tracking
```csharp
public interface IMessageStatusService
{
    Task<MessageStatus> GetStatusAsync(string provider, string messageId);
}
```

### Rate Limiting
```csharp
services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("mms", limiterOptions => {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 100;
    });
});
```

### Observability
- OpenTelemetry tracing
- Prometheus metrics
- Health checks for Twilio connectivity

---

## 📚 Related Documentation

- **[Developer Setup Guide](DEVELOPER-SETUP.md)** - Complete guide for setting up development environment, debugging, and local testing

---

## 🏭 Production Deployment Guide

> **Developer Setup**: For development environment setup, debugging, and IDE configuration, see [DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)
> 
> **Complete Deployment Guide**: For comprehensive production deployment instructions covering all methods (xcopy, Docker, systemd, IIS), monitoring, and troubleshooting, see [PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)

### Quick Production Setup (xcopy Method - Recommended)

The **xcopy deployment method is prioritized** for most production scenarios due to its simplicity, reliability, and ease of maintenance.

#### Why xcopy Deployment?
- **Simple**: No Docker or complex setup required
- **Fast**: Quick to deploy and update  
- **Reliable**: Proven method with fewer moving parts
- **Portable**: Self-contained executable runs anywhere
- **Scriptable**: Easily automated with batch files

#### Quick Start Steps:

1. **Publish Application:**
   ```bash
   dotnet publish src/MmsRelay/MmsRelay.csproj -c Release -o ./publish --self-contained true --runtime win-x64 -p:PublishSingleFile=true
   ```

2. **Create Deployment Package:**
   ```bash
   mkdir deployment\app deployment\config deployment\scripts
   xcopy publish\* deployment\app\ /E /Y
   ```

3. **Configure Production Secrets:**
   ```bash
   # Create secrets.env on target machine (not in source control)
   set ASPNETCORE_ENVIRONMENT=Production
   set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   set TWILIO__AUTHTOKEN=your_actual_auth_token_here
   set TWILIO__FROMPHONENUMBER=+15551234567
   set URLS=http://localhost:8080
   ```

4. **Deploy and Run:**
   ```bash
   # Copy to target machine and run install script
   xcopy deployment\* C:\MmsRelay\ /E /Y
   cd C:\MmsRelay
   call secrets.env
   MmsRelay.exe
   ```

For complete deployment instructions, advanced configurations, alternative deployment methods, monitoring setup, and troubleshooting guides, see the **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)** file.

### Production Configuration Changes

#### 1. Security Configuration

**Remove Sensitive Data from appsettings.json:**
```json
{
  "urls": "http://0.0.0.0:8080",
  "logging": {
    "levelSwitch": "Information"
  },
  "twilio": {
    "accountSid": "",
    "authToken": "",
    "fromPhoneNumber": "",
    "messagingServiceSid": "",
    "baseUrl": "https://api.twilio.com/2010-04-01",
    "requestTimeoutSeconds": 30,
    "retry": {
      "maxRetries": 5,
      "baseDelayMs": 500
    },
    "circuitBreaker": {
      "samplingDurationSeconds": 120,
      "failureRatio": 0.3,
      "minThroughput": 50,
      "breakDurationSeconds": 60
    }
  }
}
```

**Create appsettings.Production.json:**
```json
{
  "urls": "http://0.0.0.0:8080",
  "logging": {
    "levelSwitch": "Warning"
  },
  "AllowedHosts": "*",
  "ForwardedHeaders": {
    "ForwardedHeaders": "XForwardedFor,XForwardedProto"
  }
}
```

#### 2. Environment Variables for Secrets

**Required Environment Variables:**
```bash
# Twilio Configuration
TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
TWILIO__AUTHTOKEN=your_actual_auth_token_here
TWILIO__FROMPHONENUMBER=+15551234567
# OR
TWILIO__MESSAGINGSERVICESID=MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Optional Overrides
TWILIO__REQUESTTIMEOUTSECONDS=30
TWILIO__RETRY__MAXRETRIES=5
TWILIO__RETRY__BASEDELAYMS=500

# Logging Level (optional)
LOGGING__LEVELSWITCH=Warning

# Application URLs
URLS=http://0.0.0.0:8080
```

**Environment Variable Naming Convention:**
- Use double underscores `__` to represent JSON nesting
- `TWILIO__ACCOUNTSID` maps to `twilio:accountSid`
- `TWILIO__RETRY__MAXRETRIES` maps to `twilio:retry:maxRetries`

#### 3. Production Resilience Settings

**Recommended Production Values:**
```json
{
  "twilio": {
    "requestTimeoutSeconds": 30,        // Increased from 10
    "retry": {
      "maxRetries": 5,                  // Increased from 3
      "baseDelayMs": 500                // Increased from 200
    },
    "circuitBreaker": {
      "samplingDurationSeconds": 120,   // Increased from 60
      "failureRatio": 0.3,              // Decreased from 0.5 (more sensitive)
      "minThroughput": 50,              // Increased from 20
      "breakDurationSeconds": 60        // Increased from 30
    }
  }
}
```

**Rationale for Changes:**
- **Longer Timeouts**: Production networks may have higher latency
- **More Retries**: Better resilience against transient failures
- **Longer Delays**: Reduces load on Twilio during issues
- **Sensitive Circuit Breaker**: Fails faster to protect upstream services
- **Higher Throughput Threshold**: More data for accurate failure rate calculation

### Production Security Hardening

#### 1. HTTPS Configuration

**Update Program.cs for HTTPS:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Production HTTPS configuration
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;  // Remove server header
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB limit
    });
}

// ... existing configuration

var app = builder.Build();

// Production middleware
if (app.Environment.IsProduction())
{
    app.UseHsts();                    // HTTP Strict Transport Security
    app.UseHttpsRedirection();        // Redirect HTTP to HTTPS
}

// ... existing middleware
```

#### 2. Security Headers

**Add Security Middleware:**
```csharp
// Add to ServiceCollectionExtensions.cs
public static IServiceCollection AddProductionSecurity(this IServiceCollection services)
{
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    return services;
}

// Usage in Program.cs
if (builder.Environment.IsProduction())
{
    builder.Services.AddProductionSecurity();
}

// In middleware pipeline
if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders();
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}
```

---

## 📚 Documentation Summary

This MmsRelay knowledge base provides:

- **Architecture Overview**: Clean Architecture implementation with separation of concerns
- **Code Walkthrough**: Detailed explanation of all components and patterns
- **Configuration Guide**: Production-ready settings and security considerations
- **Quick Setup**: Prioritized xcopy deployment method for immediate production use

### Related Documentation

- **[DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)**: Complete development environment setup, debugging, and IDE configuration
- **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)**: Comprehensive production deployment guide with all methods (xcopy, Docker, systemd, IIS), monitoring, and troubleshooting

The MmsRelay service demonstrates modern .NET 8 minimal API development with enterprise-grade reliability patterns, comprehensive logging, and flexible deployment options suitable for various production environments.