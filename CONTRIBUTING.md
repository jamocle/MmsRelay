# Contributing to MmsRelay

Welcome to MmsRelay! This guide helps new contributors get started with the project, regardless of their experience level.

## 🎯 What is MmsRelay?

**Simple Version**: MmsRelay is a web service that sends text messages (SMS) and multimedia messages (MMS) through Twilio's phone service.

**Technical Version**: MmsRelay is a .NET 8 Web API service that provides a simplified HTTP interface for sending SMS and MMS messages via Twilio's cloud communications platform, with enterprise patterns like circuit breakers, retry logic, and health checks.

---

## 🚀 Quick Start for New Contributors

### Step 1: Get Your Development Environment Ready

**What You Need:**
- **Visual Studio** (recommended) or **VS Code** with C# extension
- **.NET 8 SDK** - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- **Git** - For source control
- **Twilio Account** (free) - For testing message sending

**Check Your Setup:**
```bash
# Should show version 8.0.x or higher
dotnet --version

# Should work without errors
git --version
```

### Step 2: Get the Code

```bash
# Clone the repository
git clone https://github.com/jamocle/MmsRelay.git
cd MmsRelay

# Create a new branch for your changes
git checkout -b feature/my-new-feature
```

### Step 3: Set Up Your Test Environment

**Get Free Twilio Credentials:**
1. Sign up at [https://www.twilio.com/try-twilio](https://www.twilio.com/try-twilio)
2. Go to Console Dashboard
3. Copy your Account SID, Auth Token, and Trial Phone Number

**Configure for Development:**
```bash
cd src/MmsRelay

# Store your Twilio credentials safely (they won't be committed to Git)
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_auth_token"  
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"
```

### Step 4: Verify Everything Works

```bash
# Run all tests (should pass)
cd tests/MmsRelay.Tests
dotnet test

# Start the service
cd ../../src/MmsRelay
dotnet run

# In another terminal, test it works
curl http://localhost:5000/health/live
# Should return: {"status":"Healthy"}
```

---

## 🎯 Types of Contributions We Welcome

### 🐛 Bug Fixes
**Perfect for beginners!**

Look for issues labeled `good first issue` or `bug`. These are usually:
- Typos in documentation
- Simple logic errors
- Missing validation
- Test improvements

### 📚 Documentation Improvements
**Great for learning the codebase!**

- Fix unclear explanations
- Add missing examples
- Improve error messages
- Add beginner-friendly guides

### ✨ New Features
**For more experienced contributors**

- New message providers (beyond Twilio)
- Additional health checks
- Performance improvements
- New API endpoints

### 🧪 Testing
**Excellent for understanding how code works!**

- Add missing test cases
- Improve test coverage
- Add integration tests
- Performance tests

---

## 📁 Understanding the Project Structure

```
MmsRelay/
├── src/MmsRelay/                    # Main web service
│   ├── Program.cs                   # Application startup
│   ├── Api/                         # Web API controllers and middleware
│   ├── Application/                 # Business logic (what the app does)
│   │   ├── Models/                  # Data structures for requests/responses
│   │   └── Validation/              # Rules to check if requests are valid
│   └── Infrastructure/              # External services (Twilio integration)
│       └── Twilio/
├── clients/MmsRelay.Client/         # Console app that uses the service
│   └── Program.cs                   # Command-line interface
├── tests/MmsRelay.Tests/            # All unit tests
│   ├── SendMmsRequestValidatorTests.cs  # Tests for input validation
│   └── TwilioMmsSenderTests.cs         # Tests for Twilio integration
├── docs/                            # Documentation files
├── README.md                        # Project overview
├── TROUBLESHOOTING.md              # Common problems and solutions
└── EXAMPLES.md                     # Real-world usage examples
```

**Key Concepts:**
- **API Controllers** (in `Api/`): Handle incoming HTTP requests
- **Application Services** (in `Application/`): Business logic that doesn't depend on external services
- **Infrastructure** (in `Infrastructure/`): Code that talks to external systems (Twilio, databases, etc.)
- **Models** (in `Application/Models/`): Data structures that define what information we work with

---

## 🔧 Development Workflow

### Before Making Changes

1. **Understand the Issue:**
   - Read the GitHub issue description carefully
   - Ask questions in comments if anything is unclear
   - Look at related code and tests

2. **Write Tests First** (Test-Driven Development):
   ```bash
   # Create a failing test for what you want to build
   cd tests/MmsRelay.Tests
   # Edit or create test file
   dotnet test  # Should fail because feature doesn't exist yet
   ```

3. **Make Your Changes:**
   - Keep changes small and focused
   - Follow existing code patterns
   - Add comments explaining "why", not just "what"

4. **Verify Your Changes:**
   ```bash
   # Run all tests
   dotnet test
   
   # Test the API manually
   cd ../../src/MmsRelay
   dotnet run
   # Test your changes with curl or the console client
   ```

### Code Style Guidelines

**C# Code Style:**
```csharp
// ✅ Good: Clear, descriptive names
public async Task<SendMmsResult> SendMmsAsync(SendMmsRequest request)
{
    // Use guard clauses for validation
    if (request is null)
        throw new ArgumentNullException(nameof(request));
    
    // Use var when type is obvious
    var twilioMessage = await _twilioClient.CreateMessageAsync(/* ... */);
    
    // Return meaningful results
    return new SendMmsResult
    {
        Provider = "twilio",
        ProviderMessageId = twilioMessage.Sid,
        Status = twilioMessage.Status.ToString().ToLowerInvariant()
    };
}

// ❌ Bad: Unclear names and logic
public async Task<object> Send(object req)
{
    var x = await client.Create(req);
    return x;
}
```

**JSON Naming:**
```json
{
  "// Use camelCase for JSON properties": "",
  "to": "+15551234567",
  "fromPhoneNumber": "+15551234567",
  "isValidPhoneNumber": true
}
```

**File Organization:**
- One class per file
- File name matches class name
- Group related files in folders
- Keep files under 300 lines when possible

### Testing Guidelines

**Write Tests That:**
- Test behavior, not implementation details
- Have descriptive names that explain what they're testing
- Are independent (each test can run alone)
- Use realistic test data

**Test Example:**
```csharp
[Fact]
public async Task SendMmsAsync_WithValidRequest_ReturnsSuccessResult()
{
    // Arrange - Set up test data
    var request = new SendMmsRequest
    {
        To = "+15551234567",
        Body = "Test message"
    };
    
    // Mock external dependencies so tests are fast and reliable
    var mockTwilioClient = new Mock<ITwilioRestClient>();
    mockTwilioClient
        .Setup(x => x.CreateMessageAsync(It.IsAny<CreateMessageOptions>()))
        .ReturnsAsync(new MessageResource { Sid = "SM123", Status = MessageResource.StatusEnum.Queued });
    
    var sender = new TwilioMmsSender(mockTwilioClient.Object, Options.Create(new TwilioOptions()));
    
    // Act - Do the thing we're testing
    var result = await sender.SendMmsAsync(request);
    
    // Assert - Check it worked correctly
    Assert.Equal("twilio", result.Provider);
    Assert.Equal("SM123", result.ProviderMessageId);
    Assert.Equal("queued", result.Status);
}
```

---

## 🔍 How to Find Good First Issues

### 1. GitHub Issue Labels

Look for issues tagged with:
- `good first issue` - Specifically chosen for beginners
- `documentation` - Usually easier than code changes
- `bug` - Often simpler than new features
- `help wanted` - Maintainers specifically want community help

### 2. Documentation Tasks

Easy wins include:
- Fixing typos or grammar errors
- Adding missing code examples
- Clarifying confusing explanations
- Adding FAQ entries for common questions

### 3. Simple Bug Fixes

Look for:
- Missing validation on API inputs
- Inconsistent error messages
- Missing unit tests for existing code
- Performance improvements (like caching)

### 4. Test Coverage Gaps

```bash
# See what's not tested
dotnet test --collect:"XPlat Code Coverage"
# Use a tool like ReportGenerator to see coverage report
```

---

## 📝 Submitting Your Contribution

### 1. Before Submitting

**Final Checklist:**
```bash
# All tests pass
dotnet test

# Code builds without warnings
dotnet build --configuration Release --no-restore /warnaserror

# Service still works
cd src/MmsRelay
dotnet run
curl http://localhost:5000/health/live

# Your changes work as expected
# Test manually or with the console client
```

### 2. Commit Your Changes

**Good Commit Messages:**
```bash
# ✅ Good: Clear, specific, explains "why"
git commit -m "Fix phone number validation to allow international formats

The validator was rejecting valid international numbers like +44xxx.
Updated regex pattern to accept country codes 1-3 digits.

Fixes #123"

# ❌ Bad: Vague, no context
git commit -m "fixed bug"
git commit -m "updated code"
```

**Commit Best Practices:**
- Make small, logical commits
- Each commit should work by itself
- Group related changes together
- Don't commit personal configuration or credentials

### 3. Create a Pull Request

**Pull Request Template:**
```markdown
## What This Changes
Brief description of what your PR does.

## Why This Change Is Needed
Explain the problem you're solving or feature you're adding.

## How to Test
Step-by-step instructions for reviewers to test your changes.

## Checklist
- [ ] Tests pass locally
- [ ] Added tests for new functionality  
- [ ] Updated documentation if needed
- [ ] Manually tested changes work
- [ ] No breaking changes (or noted in description)

## Related Issues
Closes #123
```

**What Happens Next:**
1. Automated tests run on your PR
2. A maintainer reviews your code
3. You might get feedback to address
4. Once approved, your changes get merged!

---

## 🤝 Getting Help

### Questions About Contributing?

**Before asking, check:**
1. This CONTRIBUTING.md file
2. [FAQ.md](FAQ.md) for common questions  
3. [EXAMPLES.md](EXAMPLES.md) for usage patterns
4. Existing GitHub issues and discussions

### Where to Ask

1. **GitHub Discussions** - General questions about contributing
2. **GitHub Issues** - Specific bugs or feature requests  
3. **PR Comments** - Questions about your specific changes
4. **Discord/Slack** (if available) - Real-time help

### What Information to Include

When asking for help, include:
```bash
# Your environment
dotnet --version
git --version
# OS (Windows 11, Ubuntu 22.04, macOS 13, etc.)

# What you're trying to do
"I'm trying to add validation for international phone numbers"

# What you tried  
"I updated the regex pattern in SendMmsRequestValidator.cs"

# What happened
"Tests are failing with error: ArgumentException: Invalid phone format"

# Relevant code (without sensitive data)
```

---

## 📊 Understanding the Technical Architecture

### Clean Architecture Pattern

MmsRelay uses "Clean Architecture" which organizes code in layers:

```
┌─────────────────────────────────────────┐
│ API Layer (Controllers, Middleware)     │  ← HTTP requests come here
├─────────────────────────────────────────┤
│ Application Layer (Business Logic)      │  ← Core rules and workflows  
├─────────────────────────────────────────┤
│ Infrastructure Layer (Twilio, etc.)     │  ← External services
└─────────────────────────────────────────┘
```

**Key Rules:**
- **Inner layers never depend on outer layers**
- **Application layer defines interfaces, Infrastructure implements them**
- **Business logic doesn't know about HTTP, databases, or Twilio**

**Why This Matters for Contributors:**
- You can test business logic without HTTP or Twilio
- Adding new message providers (SMS, email, etc.) is easy
- Code is organized and predictable

### Dependency Injection

**What It Is**: Instead of creating objects directly, we "inject" them.

```csharp
// ❌ Bad: Hard to test, tightly coupled
public class MmsController
{
    public async Task<IActionResult> SendMms(SendMmsRequest request)
    {
        var twilioClient = new TwilioRestClient("account", "token"); // Hard-coded!
        var sender = new TwilioMmsSender(twilioClient);
        var result = await sender.SendMmsAsync(request);
        return Ok(result);
    }
}

// ✅ Good: Easy to test, flexible
public class MmsController
{
    private readonly IMmsSender _mmsSender;
    
    public MmsController(IMmsSender mmsSender) // Injected!
    {
        _mmsSender = mmsSender;
    }
    
    public async Task<IActionResult> SendMms(SendMmsRequest request)
    {
        var result = await _mmsSender.SendMmsAsync(request);
        return Ok(result);
    }
}
```

**Benefits for Contributors:**
- Easy to mock dependencies in tests
- Easy to swap implementations (different SMS providers)
- Configuration handled centrally

### Health Checks

**What They Do**: Tell you if the service is working properly.

- `/health/live` - "Is the service running?" (Always returns OK if service responds)
- `/health/ready` - "Can the service do its job?" (Checks Twilio connectivity)

**Why They Matter:**
- Kubernetes/Docker can restart unhealthy services
- Load balancers can route around unhealthy instances
- Monitoring systems can alert when services fail

### Circuit Breaker Pattern

**What It Does**: Protects the service when Twilio is having problems.

```
Normal Operation:  [MmsRelay] ──✅──> [Twilio API]

Twilio Problems:   [MmsRelay] ──❌──> [Twilio API] (5 failures)
                   [MmsRelay] ──🚫──> [Circuit OPEN - fail fast]

Recovery:          [MmsRelay] ──🔍──> [Twilio API] (test call)
                   [MmsRelay] ──✅──> [Circuit CLOSED - resume]
```

**Why It's Important:**
- Prevents cascading failures
- Fails fast instead of waiting for timeouts
- Automatically recovers when Twilio is healthy again

---

## 🔧 Advanced Development Topics

### Adding a New Message Provider

Want to add support for SendGrid, AWS SES, or another service?

1. **Create the Interface Implementation:**
```csharp
// In Infrastructure/SendGrid/
public class SendGridMmsSender : IMmsSender
{
    public async Task<SendMmsResult> SendMmsAsync(SendMmsRequest request)
    {
        // SendGrid-specific implementation
    }
}
```

2. **Add Configuration:**
```csharp
public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
```

3. **Register in DI Container:**
```csharp
// In Program.cs
builder.Services.Configure<SendGridOptions>(
    builder.Configuration.GetSection("SendGrid"));
builder.Services.AddScoped<IMmsSender, SendGridMmsSender>();
```

4. **Add Tests:**
```csharp
public class SendGridMmsSenderTests
{
    [Fact]
    public async Task SendMmsAsync_WithValidRequest_CallsSendGridCorrectly()
    {
        // Test implementation
    }
}
```

### Performance Optimization

Common areas for optimization:

1. **Async/Await Usage:**
```csharp
// ✅ Good: Properly async
public async Task<SendMmsResult> SendMmsAsync(SendMmsRequest request)
{
    var message = await _twilioClient.CreateMessageAsync(options);
    return MapToResult(message);
}

// ❌ Bad: Blocking async code
public SendMmsResult SendMms(SendMmsRequest request)  
{
    var message = _twilioClient.CreateMessageAsync(options).Result; // Deadlock risk!
    return MapToResult(message);
}
```

2. **Memory Allocation:**
```csharp
// ✅ Good: Minimal allocations
public string FormatPhoneNumber(string phoneNumber)
{
    return phoneNumber.StartsWith('+') ? phoneNumber : $"+1{phoneNumber}";
}

// ❌ Bad: Unnecessary string operations
public string FormatPhoneNumber(string phoneNumber)
{
    return phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "")
                     .Insert(0, phoneNumber.StartsWith("+") ? "" : "+1");
}
```

3. **HTTP Client Usage:**
```csharp
// ✅ Good: Reuse HttpClient
builder.Services.AddHttpClient<TwilioMmsSender>();

// ❌ Bad: Create new HttpClient each time (port exhaustion)
public async Task SendAsync()
{
    using var client = new HttpClient(); // Don't do this!
}
```

### Security Considerations

1. **Never Log Sensitive Data:**
```csharp
// ✅ Good: Safe logging
_logger.LogInformation("Sending MMS to {PhoneNumberMask}", 
    MaskPhoneNumber(request.To));

// ❌ Bad: Logs real phone numbers
_logger.LogInformation("Sending MMS to {PhoneNumber}", request.To);
```

2. **Validate All Inputs:**
```csharp
public class SendMmsRequestValidator : AbstractValidator<SendMmsRequest>
{
    public SendMmsRequestValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{1,14}$") // E.164 format
            .WithMessage("Phone number must be in E.164 format");
            
        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(1600); // SMS/MMS limits
    }
}
```

3. **Secure Configuration:**
```csharp
// ✅ Good: Use User Secrets for development
dotnet user-secrets set "twilio:authToken" "real_token"

// ❌ Bad: Hard-code secrets
var authToken = "ACxxxxx"; // Never do this!
```

---

## 📈 Measuring Success

### Code Quality Metrics

```bash
# Test Coverage
dotnet test --collect:"XPlat Code Coverage"

# Code Analysis
dotnet build --configuration Release /p:TreatWarningsAsErrors=true

# Performance Benchmarks  
# (Consider adding BenchmarkDotNet for performance-critical code)
```

### Performance Benchmarks

For changes that might affect performance:

```bash
# Measure response times
curl -w "@curl-format.txt" -o /dev/null -s \
  -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{"to": "+15551234567", "body": "Performance test"}'
```

### Documentation Quality

- Are examples clear and working?
- Can a new developer follow the setup instructions?
- Are error messages helpful?

---

Thank you for contributing to MmsRelay! Every contribution, no matter how small, helps make this project better for everyone. 🎉