# MmsRelay Console Client - Developer Setup Guide

## Prerequisites

### Required Software

- **.NET 8 SDK** (8.0.100 or later)
- **Visual Studio 2022** (17.8+) or **VS Code** with C# extension
- **Git** (2.30+)
- **PowerShell** (Windows) or **Bash** (macOS/Linux)

### Verify Installation

```bash
# Check .NET version
dotnet --version
# Should output: 8.0.xxx

# Check SDK installation
dotnet --list-sdks
# Should include: 8.0.xxx

# Verify C# compiler
dotnet new console --dry-run
# Should complete without errors
```

## Repository Setup

### 1. Clone the Repository

```bash
git clone https://github.com/jamocle/MmsRelay.git
cd MmsRelay
```

### 2. Solution Structure

```
MmsRelay/
├── MmsRelay.sln                    # Main solution file
├── src/
│   └── MmsRelay/                   # Main service project
├── clients/
│   └── MmsRelay.Client/            # Console client project
├── tests/
│   ├── MmsRelay.Tests/             # Service tests
│   └── MmsRelay.Client.Tests/      # Client tests
└── docs/                           # Documentation
```

### 3. Restore Dependencies

```bash
# From solution root
dotnet restore

# Verify all projects restore successfully
dotnet build
```

## Development Environment Configuration

### 1. User Secrets Setup

The console client uses .NET User Secrets for development configuration:

```bash
# Navigate to client project
cd clients/MmsRelay.Client

# Initialize user secrets
dotnet user-secrets init

# Set development configuration
dotnet user-secrets set "MmsRelay:BaseUrl" "http://localhost:8080"
dotnet user-secrets set "MmsRelay:TimeoutSeconds" "30"
dotnet user-secrets set "MmsRelay:RetryCount" "3"

# For testing with real service (optional)
dotnet user-secrets set "MmsRelay:ApiKey" "your-development-api-key"
```

### 2. Configuration Files

The client uses a layered configuration approach:

```json
// appsettings.json (default settings)
{
  "MmsRelay": {
    "BaseUrl": "http://localhost:8080",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 60
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

```json
// appsettings.Development.json (development overrides)
{
  "MmsRelay": {
    "BaseUrl": "http://localhost:8080"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### 3. IDE Configuration

#### Visual Studio 2022

1. **Solution Configuration**:
   - Set `MmsRelay.Client` as startup project for client development
   - Configure multiple startup projects to run both service and client

2. **Debug Settings**:
   ```json
   // launchSettings.json
   {
     "profiles": {
       "MmsRelay.Client": {
         "commandName": "Project",
         "commandLineArgs": "send --help",
         "environmentVariables": {
           "DOTNET_ENVIRONMENT": "Development"
         }
       },
       "Send MMS": {
         "commandName": "Project",
         "commandLineArgs": "send --to +15551234567 --body \"Test message\" --verbose",
         "environmentVariables": {
           "DOTNET_ENVIRONMENT": "Development"
         }
       },
       "Health Check": {
         "commandName": "Project",
         "commandLineArgs": "health --verbose",
         "environmentVariables": {
           "DOTNET_ENVIRONMENT": "Development"
         }
       }
     }
   }
   ```

#### VS Code

1. **Launch Configuration** (`.vscode/launch.json`):
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": "Launch Client",
         "type": "coreclr",
         "request": "launch",
         "program": "${workspaceFolder}/clients/MmsRelay.Client/bin/Debug/net8.0/MmsRelay.Client.dll",
         "args": ["--help"],
         "cwd": "${workspaceFolder}/clients/MmsRelay.Client",
         "console": "integratedTerminal",
         "stopAtEntry": false,
         "env": {
           "DOTNET_ENVIRONMENT": "Development"
         }
       },
       {
         "name": "Send MMS (Debug)",
         "type": "coreclr",
         "request": "launch",
         "program": "${workspaceFolder}/clients/MmsRelay.Client/bin/Debug/net8.0/MmsRelay.Client.dll",
         "args": ["send", "--to", "+15551234567", "--body", "Test message", "--verbose"],
         "cwd": "${workspaceFolder}/clients/MmsRelay.Client",
         "console": "integratedTerminal",
         "stopAtEntry": false,
         "env": {
           "DOTNET_ENVIRONMENT": "Development"
         }
       }
     ]
   }
   ```

2. **Tasks Configuration** (`.vscode/tasks.json`):
   ```json
   {
     "version": "2.0.0",
     "tasks": [
       {
         "label": "build-client",
         "command": "dotnet",
         "type": "process",
         "args": ["build", "${workspaceFolder}/clients/MmsRelay.Client"],
         "group": "build",
         "presentation": {
           "echo": true,
           "reveal": "silent",
           "focus": false,
           "panel": "shared"
         }
       },
       {
         "label": "test-client",
         "command": "dotnet",
         "type": "process",
         "args": ["test", "${workspaceFolder}/tests/MmsRelay.Client.Tests"],
         "group": "test",
         "presentation": {
           "echo": true,
           "reveal": "always",
           "focus": false,
           "panel": "shared"
         }
       }
     ]
   }
   ```

## Development Workflow

### 1. Building the Client

```bash
# Build client project only
cd clients/MmsRelay.Client
dotnet build

# Build entire solution
cd ../..
dotnet build

# Build for specific configuration
dotnet build --configuration Release
```

### 2. Running the Client

```bash
# Run with dotnet run (development)
cd clients/MmsRelay.Client

# Show help
dotnet run -- --help

# Send MMS with text body
dotnet run -- send --to +15551234567 --body "Hello from development!"

# Send MMS with media
dotnet run -- send --to +15551234567 --media "https://example.com/image.jpg"

# Check service health
dotnet run -- health

# Use verbose logging
dotnet run -- send --to +15551234567 --body "Test" --verbose

# Use custom service URL
dotnet run -- send --to +15551234567 --body "Test" --service-url "https://api.example.com"
```

### 3. Testing

```bash
# Run all client tests
cd tests/MmsRelay.Client.Tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "MmsRelayHttpClientTests"

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests and generate coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### 4. Debugging

#### Command Line Debugging

```bash
# Enable verbose logging
dotnet run -- send --to +15551234567 --body "Test" --verbose

# Check configuration
dotnet run -- --help

# Validate service connectivity
dotnet run -- health --service-url "http://localhost:8080" --verbose
```

#### IDE Debugging

1. **Set Breakpoints**: In validation, HTTP client, or command handlers
2. **Configure Arguments**: Use launch profiles for different scenarios
3. **Watch Variables**: Monitor request/response objects
4. **Call Stack**: Trace execution through layers

### 5. Code Quality

#### Static Analysis

```bash
# Run code analysis
dotnet build --verbosity normal

# Format code
dotnet format

# Check for security vulnerabilities
dotnet list package --vulnerable
```

#### Testing Guidelines

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test HTTP client with mocked responses
3. **Command Tests**: Test CLI argument parsing and validation
4. **Error Handling**: Test exception scenarios

```csharp
// Example unit test
[TestMethod]
public async Task SendMmsAsync_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When(HttpMethod.Post, "*/mms")
        .Respond("application/json", JsonSerializer.Serialize(new SendMmsResult 
        { 
            Success = true, 
            MessageId = "test-123" 
        }));

    var client = CreateClient(mockHttp);
    var request = new SendMmsRequest 
    { 
        To = "+15551234567", 
        Body = "Test message" 
    };

    // Act
    var result = await client.SendMmsAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.MessageId.Should().Be("test-123");
}
```

## Local Development with MmsRelay Service

### 1. Start the Service

```bash
# In separate terminal, start the MmsRelay service
cd src/MmsRelay
dotnet run

# Service will be available at http://localhost:8080
```

### 2. Verify Service Health

```bash
# Check service health from client
cd clients/MmsRelay.Client
dotnet run -- health

# Expected output:
# [INFO] MmsRelay service is healthy
# Service Status: Healthy
# Response Time: 45ms
```

### 3. Test End-to-End Flow

```bash
# Send test MMS
dotnet run -- send --to +15551234567 --body "Hello from local development!" --verbose

# Expected output:
# [INFO] Sending MMS to +15551234567
# [INFO] MMS sent successfully with ID: msg_abc123
# Success: MMS sent successfully
# Message ID: msg_abc123
```

## Troubleshooting

### Common Issues

#### 1. Build Errors

```bash
# Clear build artifacts
dotnet clean
dotnet restore
dotnet build

# Check for missing dependencies
dotnet list package
```

#### 2. Configuration Issues

```bash
# Verify user secrets
dotnet user-secrets list

# Check configuration loading
dotnet run -- health --verbose
```

#### 3. Service Connection Issues

```bash
# Test service connectivity
curl http://localhost:8080/health

# Check firewall settings
netstat -an | findstr :8080

# Verify service is running
dotnet run --project src/MmsRelay
```

#### 4. Test Failures

```bash
# Run tests with detailed output
dotnet test --verbosity detailed

# Check test dependencies
dotnet restore tests/MmsRelay.Client.Tests

# Run specific failing test
dotnet test --filter "TestMethodName"
```

### Debug Logging

Enable detailed logging for troubleshooting:

```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "System.Net.Http": "Debug",
        "Microsoft.Extensions.Http": "Debug"
      }
    }
  }
}
```

### Performance Profiling

```bash
# Run with performance profiling
dotnet run --configuration Release -- send --to +15551234567 --body "Performance test"

# Monitor memory usage
dotnet-counters monitor --process-id <pid> --counters System.Runtime

# Analyze startup time
dotnet trace collect --process-id <pid> --duration 00:00:10
```

## Contributing Guidelines

### Code Style

1. **Follow C# Conventions**: Use Microsoft's C# coding conventions
2. **Async/Await**: Use async/await for I/O operations
3. **Nullable Reference Types**: Enabled by default in .NET 8
4. **Code Analysis**: Fix all compiler warnings

### Testing Requirements

1. **Test Coverage**: Maintain >90% code coverage
2. **Unit Tests**: Test all public methods
3. **Integration Tests**: Test HTTP client scenarios
4. **Error Cases**: Test all error conditions

### Documentation

1. **XML Comments**: Document all public APIs
2. **README Updates**: Update for new features
3. **CHANGELOG**: Document breaking changes

### Pull Request Process

1. **Feature Branch**: Create feature branch from main
2. **Tests Pass**: All tests must pass
3. **Code Review**: Peer review required
4. **Documentation**: Update relevant docs

This setup guide provides everything needed for productive MmsRelay Console Client development.