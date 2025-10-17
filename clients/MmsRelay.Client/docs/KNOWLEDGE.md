# MmsRelay Console Client - Knowledge Base

## Overview

The MmsRelay Console Client is an enterprise-grade command-line interface for consuming the MmsRelay service. Built with .NET 8 and following Clean Architecture principles, it provides a robust, testable, and maintainable solution for sending MMS messages programmatically.

## Architecture

### Clean Architecture Implementation

```
MmsRelay.Client/
├── Application/           # Core business logic and contracts
│   ├── Models/           # Request/Response DTOs
│   ├── Services/         # Business services and interfaces
│   └── Validation/       # FluentValidation rules
├── Infrastructure/       # External concerns
│   ├── Http/            # HTTP client implementations
│   └── Configuration/   # Configuration management
├── Commands/            # CLI command handlers
└── Program.cs           # Application entry point
```

### Key Design Principles

1. **Separation of Concerns**: Clear boundaries between CLI, business logic, and infrastructure
2. **Dependency Inversion**: All dependencies flow inward toward the Application layer
3. **Single Responsibility**: Each class has one reason to change
4. **Open/Closed**: Extensible without modification through interfaces
5. **Interface Segregation**: Small, focused interfaces

## Core Components

### Command Line Interface (System.CommandLine)

```csharp
// Root command with subcommands
var rootCommand = new RootCommand("MmsRelay Client - Send MMS messages through the MmsRelay service");

// Send command with validation
var sendCommand = new Command("send", "Send an MMS message");
sendCommand.AddOption(toOption);
sendCommand.AddOption(bodyOption);
sendCommand.AddOption(mediaOption);
```

**Key Features:**
- Type-safe command parsing
- Built-in help generation
- Option validation and binding
- Hierarchical command structure

### HTTP Client with Polly Resilience

```csharp
// Three-layer resilience policy
var policyWrap = Policy.WrapAsync(
    timeoutPolicy,
    retryPolicy,
    circuitBreakerPolicy
);
```

**Resilience Patterns:**
1. **Timeout Policy**: 30-second request timeout
2. **Retry Policy**: Exponential backoff with jitter (3 retries)
3. **Circuit Breaker**: 5 consecutive failures trigger open state

### Validation (FluentValidation)

```csharp
public class SendMmsCommandValidator : AbstractValidator<SendMmsCommand>
{
    public SendMmsCommandValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g., +15551234567)");
    }
}
```

**Validation Rules:**
- E.164 phone number format validation
- URL format validation for media
- Business rule: Either body or media URLs required
- Cross-field validation logic

### Structured Logging (Serilog)

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

**Logging Features:**
- JSON structured output for production
- Configurable log levels (verbose mode)
- Request/response correlation
- Performance metrics logging

## Configuration Management

### appsettings.json Structure

```json
{
  "MmsRelay": {
    "BaseUrl": "http://localhost:8080",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Environment-Specific Configuration

- **Development**: `appsettings.Development.json` with localhost defaults
- **Production**: Environment variables override file settings
- **User Secrets**: Sensitive configuration in development
- **Command Line**: Service URL override per execution

## Error Handling Strategy

### Exception Hierarchy

```csharp
public class MmsRelayClientException : Exception
{
    public int? StatusCode { get; }
    public string? ErrorCode { get; }
    
    // Specific constructors for different error scenarios
}
```

### Error Categories

1. **Validation Errors**: FluentValidation with detailed messages
2. **HTTP Errors**: Status code-specific error handling
3. **Network Errors**: Timeout, connectivity, DNS issues
4. **Service Errors**: Circuit breaker, rate limiting
5. **Configuration Errors**: Missing/invalid settings

### Resilience Patterns

- **Retry**: Transient HTTP errors (429, 502, 503, 504)
- **Circuit Breaker**: Prevent cascade failures
- **Timeout**: Prevent hanging requests
- **Graceful Degradation**: Meaningful error messages

## Testing Strategy

### Test Architecture

```
MmsRelay.Client.Tests/
├── Unit Tests/
│   ├── ValidationTests/      # FluentValidation rules
│   ├── ServiceTests/         # Business logic
│   └── CommandTests/         # CLI command handling
├── Integration Tests/
│   ├── HttpClientTests/      # HTTP client with mocking
│   └── EndToEndTests/        # Full command execution
└── Test Utilities/
    ├── TestDataBuilders/     # Object mother pattern
    └── MockHelpers/          # Test doubles
```

### Testing Frameworks

- **MSTest**: Test framework with lifecycle management
- **FluentAssertions**: Readable assertion syntax
- **NSubstitute**: Mocking framework
- **MockHttp**: HTTP client testing

### Test Categories

1. **Unit Tests**: Isolated component testing
2. **Integration Tests**: HTTP client behavior
3. **Command Tests**: CLI argument parsing
4. **Validation Tests**: Business rule enforcement
5. **Error Handling Tests**: Exception scenarios

## Performance Considerations

### HTTP Client Optimization

- **Connection Pooling**: Reuse HTTP connections
- **Keep-Alive**: Persistent connections
- **Compression**: Accept gzip/deflate encoding
- **Timeout Configuration**: Appropriate timeout values

### Memory Management

- **IDisposable Pattern**: Proper resource cleanup
- **Async/Await**: Non-blocking operations
- **Stream Processing**: Large payload handling
- **Object Pooling**: Reduce GC pressure

### CLI Responsiveness

- **Fast Startup**: Minimal dependency loading
- **Progress Indication**: User feedback for long operations
- **Cancellation Support**: Ctrl+C handling
- **Efficient Parsing**: Minimal command parsing overhead

## Security Considerations

### Authentication & Authorization

- **API Key Management**: Secure credential storage
- **Token Refresh**: Automatic token renewal
- **Certificate Validation**: SSL/TLS verification
- **Credential Rotation**: Support for key rotation

### Input Validation

- **Phone Number Sanitization**: E.164 format enforcement
- **URL Validation**: Prevent SSRF attacks
- **File Path Validation**: Secure file operations
- **Command Injection Prevention**: Safe argument handling

### Data Protection

- **Sensitive Data Logging**: Avoid logging credentials
- **Memory Clearing**: Clear sensitive data from memory
- **Secure Configuration**: User Secrets for development
- **Transport Security**: HTTPS enforcement

## Monitoring & Observability

### Structured Logging

```csharp
logger.LogInformation("Sending MMS to {PhoneNumber} with {MediaCount} attachments", 
    request.To, request.MediaUrls?.Count ?? 0);
```

### Key Metrics

- **Request Duration**: API call performance
- **Success Rate**: Request success percentage
- **Error Rate**: Failure analysis
- **Retry Attempts**: Resilience effectiveness

### Health Monitoring

- **Service Availability**: Health check command
- **Dependency Health**: External service status
- **Circuit Breaker State**: Resilience pattern status
- **Connection Pool Status**: Resource utilization

## Troubleshooting Guide

### Common Issues

1. **Connection Refused**
   - Check service URL configuration
   - Verify service is running
   - Check firewall settings

2. **Validation Errors**
   - Verify phone number format (E.164)
   - Check required field combinations
   - Validate URL formats

3. **Timeout Errors**
   - Increase timeout configuration
   - Check network latency
   - Verify service performance

4. **Authentication Failures**
   - Verify API credentials
   - Check token expiration
   - Validate service configuration

### Debug Commands

```bash
# Verbose logging
dotnet run -- send --to +15551234567 --body "test" --verbose

# Health check with custom URL
dotnet run -- health --service-url "https://api.example.com" --verbose

# Configuration validation
dotnet run -- send --help
```

### Log Analysis

```json
{
  "@t": "2025-10-17T12:30:42.123Z",
  "@l": "Information",
  "@mt": "HTTP Request completed in {Duration}ms with status {StatusCode}",
  "Duration": 245,
  "StatusCode": 200,
  "RequestId": "abc123"
}
```

## Best Practices

### Command Design

1. **Clear Command Names**: Descriptive, action-oriented
2. **Consistent Options**: Standard naming conventions
3. **Helpful Descriptions**: Clear help text
4. **Validation Messages**: Actionable error messages

### Error Handling

1. **Graceful Failures**: Meaningful error messages
2. **Exit Codes**: Standard Unix exit codes
3. **User Guidance**: Suggest corrective actions
4. **Contextual Information**: Include relevant details

### Configuration

1. **Environment Parity**: Consistent across environments
2. **Secure Defaults**: Safe configuration values
3. **Override Hierarchy**: Command line > Environment > File
4. **Validation**: Validate configuration at startup

### Testing

1. **Test Pyramid**: Unit > Integration > E2E
2. **Mock External Dependencies**: Isolate unit tests
3. **Test Data Builders**: Maintainable test data
4. **Assertion Clarity**: Clear test intentions

This knowledge base provides comprehensive understanding of the MmsRelay Console Client architecture, patterns, and best practices for development and maintenance.