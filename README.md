# MmsRelay

A modern .NET 8 minimal API service for relaying MMS messages through Twilio with enterprise-grade reliability patterns.

## Features

- ✅ **Clean Architecture** - Separation of concerns with Application/Infrastructure layers
- ✅ **FluentValidation** - E.164 phone number validation and business rules
- ✅ **Polly Resilience** - Timeout, retry with exponential backoff + jitter, circuit breaker
- ✅ **Serilog Logging** - Structured JSON logging with multiple output targets
- ✅ **Twilio Integration** - Form-encoded API calls with typed HttpClient
- ✅ **Health Checks** - Live/ready endpoints for monitoring
- ✅ **OpenAPI Documentation** - Comprehensive Swagger/OpenAPI spec
- ✅ **Production Ready** - Multiple deployment options (xcopy, Docker, systemd, IIS)

## Quick Start

### Prerequisites

- .NET 8 SDK
- Twilio account with Account SID, Auth Token, and phone number

### 1. Clone and Build

```bash
git clone <repository-url>
cd MmsRelay
dotnet restore
dotnet build
```

### 2. Configure Secrets (Development)

```bash
cd src/MmsRelay
dotnet user-secrets init
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_actual_auth_token_here"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"
```

### 3. Run the Service

```bash
cd src/MmsRelay
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Health Check: `http://localhost:5000/health/live`
- OpenAPI: `http://localhost:5000/swagger`

### 4. Send Test MMS

```bash
curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "+15551234567",
    "body": "Test message from MmsRelay",
    "mediaUrls": ["https://httpbin.org/image/png"]
  }'
```

## Architecture

### Project Structure

```
src/
├── MmsRelay/                     # Main API project
│   ├── Api/                      # HTTP endpoint definitions
│   ├── Application/              # Business logic and models
│   │   ├── Models/               # Request/response DTOs
│   │   └── Validation/           # FluentValidation rules
│   └── Infrastructure/           # External service integrations
│       └── Twilio/               # Twilio-specific implementation
tests/
└── MmsRelay.Tests/               # Unit tests
```

### Key Components

- **Program.cs** - Application startup and service configuration
- **MmsEndpoints.cs** - HTTP API endpoint definitions with OpenAPI documentation
- **ServiceCollectionExtensions.cs** - DI container configuration with Polly policies
- **TwilioMmsSender.cs** - Twilio integration with form-encoded API calls
- **SendMmsRequestValidator.cs** - E.164 phone validation and business rules

### Resilience Patterns

The service implements a three-layer Polly policy:

1. **Timeout Policy** (30s) - Prevents hanging requests
2. **Retry Policy** - Exponential backoff with jitter (5 retries)
3. **Circuit Breaker** - Fails fast when Twilio is unavailable

## Deployment

### Production (Recommended: xcopy)

The **xcopy deployment method** is prioritized for production due to its simplicity and reliability:

```bash
# Publish self-contained
dotnet publish src/MmsRelay/MmsRelay.csproj -c Release \
  --self-contained true --runtime win-x64 -p:PublishSingleFile=true

# Deploy to target machine
xcopy publish\* C:\MmsRelay\ /E /Y
```

For complete deployment instructions including Docker, systemd, and IIS options, see:
- **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)** - Comprehensive deployment guide

### Development Setup

For development environment setup, debugging, and IDE configuration, see:
- **[DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)** - Complete developer guide

## Configuration

### Production Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
TWILIO__AUTHTOKEN=your_actual_auth_token_here
TWILIO__FROMPHONENUMBER=+15551234567
URLS=http://localhost:8080
```

### Optional Configuration

- **Messaging Service SID** - Use instead of phone number for advanced features
- **Custom Retry Settings** - Adjust retry count, delays, and circuit breaker thresholds
- **Logging Levels** - Configure Serilog output for different environments

## API Documentation

### Send MMS Endpoint

**POST** `/mms`

```json
{
  "to": "+15551234567",
  "body": "Message text",
  "mediaUrls": ["https://example.com/image.jpg"]
}
```

**Response** (202 Accepted):
```json
{
  "provider": "twilio",
  "providerMessageId": "SM...",
  "status": "queued",
  "providerMessageUri": "https://api.twilio.com/..."
}
```

### Health Checks

- **GET** `/health/live` - Liveness probe
- **GET** `/health/ready` - Readiness probe (includes Twilio connectivity)

## Testing

Run the test suite:

```bash
dotnet test
```

Test coverage includes:
- FluentValidation rules for phone numbers and message content
- Twilio integration with mocked HTTP responses
- Error handling and resilience patterns

## Documentation

- **[knowledge.md](knowledge.md)** - Complete architecture and code walkthrough
- **[DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)** - Development environment setup
- **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)** - Production deployment guide

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

For questions, issues, or feature requests, please open an issue on GitHub.