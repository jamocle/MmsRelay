# MmsRelay Glossary - Technical Terms Explained Simply

This glossary explains technical terms used throughout the MmsRelay project in beginner-friendly language.

## Communication & Messaging

### **MMS (Multimedia Messaging Service)**
A way to send text messages with attachments (pictures, videos, documents) to mobile phones. Think of it as SMS texting but with the ability to include files.

**Example**: Sending "Order confirmed!" with a receipt PDF attached.

### **SMS (Short Message Service)**  
Regular text messaging without attachments - just plain text up to 160 characters.

### **E.164 Phone Number Format**
The international standard for writing phone numbers:
- Always starts with `+`
- Country code (US=1, UK=44, Germany=49, etc.)
- Phone number with no spaces, dashes, or parentheses
- **Examples**: `+15551234567` (US), `+447123456789` (UK), `+4915123456789` (Germany)

### **Twilio**
A cloud service that connects software applications to phone networks. Instead of building your own connection to mobile carriers worldwide, you pay Twilio to handle the complexity.

**Analogy**: Like using UberEats instead of delivering food yourself - Twilio handles the "last mile" to phones.

## Software Architecture

### **API (Application Programming Interface)**
A set of rules that allows different software applications to communicate. Think of it as a menu at a restaurant - it shows what you can order (endpoints) and how to order it (request format).

**Example**: `POST /mms` means "send an MMS message using this endpoint."

### **Clean Architecture**
A way of organizing code into layers, like floors in a building:
```
┌─────────────────────┐
│   API Layer        │ ← What users interact with (HTTP endpoints)
├─────────────────────┤  
│ Application Layer  │ ← Business rules and logic
├─────────────────────┤
│Infrastructure Layer│ ← External services (databases, Twilio, etc.)
└─────────────────────┘
```

**Benefit**: Changes in one layer don't break the others.

### **SOLID Principles**
Five rules for writing maintainable code:
- **S**ingle Responsibility: Each class does one thing well
- **O**pen/Closed: Easy to extend, hard to break
- **L**iskov Substitution: Subclasses work like their parents
- **I**nterface Segregation: Small, focused contracts
- **D**ependency Inversion: Depend on contracts, not concrete implementations

### **Dependency Injection (DI)**
Instead of creating objects directly in your code, you ask the framework to provide them. Makes testing and maintenance easier.

```csharp
// ❌ Bad - Hard to test
public class EmailService
{
    private HttpClient _client = new HttpClient(); // Creates directly
}

// ✅ Good - Easy to test
public class EmailService  
{
    public EmailService(HttpClient client) // Framework provides it
    {
        _client = client;
    }
}
```

### **Separation of Concerns**
Different parts of your code handle different responsibilities. Like a restaurant where cooks cook, waiters serve, and cashiers handle money - each person has one job.

## Reliability & Error Handling

### **Polly Resilience Policies**
A library that adds "safety nets" when calling external services. Like having backup plans when things go wrong.

**Three main patterns:**
1. **Timeout**: "Give up if it takes too long"
2. **Retry**: "Try again if it fails"  
3. **Circuit Breaker**: "Stop trying if it keeps failing"

**Real-world analogy**: Calling a friend
- Timeout: Hang up after 30 seconds
- Retry: Call again if busy
- Circuit Breaker: Stop calling if they never answer

### **Circuit Breaker Pattern**
Prevents cascade failures by "tripping" like an electrical circuit breaker when too many requests fail.

**States:**
- **Closed**: Normal operation, requests go through
- **Open**: Too many failures, reject requests immediately  
- **Half-Open**: Test if service recovered

### **Exponential Backoff**
When retrying failed requests, wait progressively longer between attempts (1s, 2s, 4s, 8s...). Prevents overwhelming a struggling service.

**Analogy**: If someone doesn't answer the door, don't keep knocking every second - wait longer each time.

### **Jitter**
Adding random delays to prevent many clients from retrying at exactly the same time.

**Analogy**: If everyone tries to call customer service right at 9 AM, add random delays so they don't all call simultaneously.

## HTTP & Web

### **HTTP Status Codes**
Standard numbers that tell you what happened with a web request:
- **200 OK**: Everything worked perfectly
- **201 Created**: Something new was created successfully  
- **400 Bad Request**: You made a mistake in your request
- **401 Unauthorized**: You need to log in or provide credentials
- **404 Not Found**: The thing you're looking for doesn't exist
- **429 Too Many Requests**: You're making requests too quickly
- **500 Internal Server Error**: Something broke on the server side
- **503 Service Unavailable**: Server is temporarily down

### **JSON (JavaScript Object Notation)**
A way to structure data that both humans and computers can easily read and write.

```json
{
  "name": "John Smith",
  "age": 30,
  "phoneNumber": "+15551234567",
  "orderItems": ["pizza", "soda"]
}
```

### **REST API**
A style of building web APIs using standard HTTP methods:
- **GET**: Retrieve data ("show me order #123")
- **POST**: Create something new ("create a new order")
- **PUT**: Update something completely ("replace order #123")  
- **DELETE**: Remove something ("cancel order #123")

### **Endpoint**
A specific URL and HTTP method combination that does one thing.
**Example**: `POST /mms` is an endpoint that sends MMS messages.

## Development & Testing

### **Async/Await**
A way to handle operations that take time (like web requests) without freezing your program.

**Analogy**: Like ordering coffee - you don't stand there blocking other customers while they make it. You wait to the side and they call your name when ready.

```csharp
// ❌ Blocks everything until done
var result = SlowDatabaseQuery();

// ✅ Lets other things happen while waiting
var result = await SlowDatabaseQueryAsync();
```

### **Unit Testing**
Testing individual pieces of code in isolation, like testing each ingredient before making a recipe.

**Example**: Test that phone number validation correctly rejects invalid numbers.

### **Integration Testing**  
Testing how different parts work together, like testing the whole recipe instead of just ingredients.

**Example**: Test the complete flow from HTTP request to Twilio API call.

### **Test Coverage**
The percentage of your code that's tested. 80% coverage means tests exercise 80% of your code lines.

### **Mocking**
Creating fake versions of external services for testing. Instead of calling real Twilio, use a fake that always succeeds.

**Analogy**: Using a flight simulator instead of a real airplane for pilot training.

## Configuration & Deployment

### **Environment Variables**
Settings stored outside your code that can change without rebuilding the application.

**Example**: `TWILIO_API_KEY=abc123` stored on the server, not in code.

### **User Secrets**
A secure way to store sensitive configuration (like API keys) during development. Kept outside your project folder so they don't get committed to Git.

### **appsettings.json**
Configuration files that contain settings for your application:
- `appsettings.json`: Default settings
- `appsettings.Development.json`: Overrides for local development
- `appsettings.Production.json`: Overrides for production servers

### **Docker Container**
A package that includes your application and everything it needs to run, like shipping your app in a box with all required tools included.

**Analogy**: Like a food truck that brings its own kitchen, ingredients, and staff - everything needed to operate.

### **Health Checks**
Special endpoints that tell monitoring systems whether your service is working properly.
- `/health/live`: "Am I running?" (liveness probe)
- `/health/ready`: "Am I ready to handle requests?" (readiness probe)

## Logging & Monitoring

### **Structured Logging**
Instead of writing log messages as plain text, use a consistent format (usually JSON) that machines can easily parse and search.

```csharp
// ❌ Hard to search
logger.Log("User John sent message to +15551234567");

// ✅ Easy to search and analyze  
logger.Log("Message sent", new { UserId = "john", PhoneNumber = "+15551234567" });
```

### **Serilog**
A popular .NET logging library that writes structured logs in JSON format.

### **Correlation ID**
A unique identifier that tracks a single request through all the systems it touches. Like a tracking number for a package that works across multiple shipping companies.

### **OpenTelemetry**
A standard way to collect performance data (traces, metrics, logs) from applications. Helps you understand how fast things are and where problems occur.

## Command Line & Tools

### **CLI (Command Line Interface)**
A way to interact with programs using text commands instead of clicking buttons. More powerful and automatable than graphical interfaces.

### **Flag/Option/Parameter**
Additional information you provide to commands:
```bash
dotnet run -- send --to "+15551234567" --body "Hello"
#              ↑    ↑                   ↑
#            command  flags            flag values
```

### **Terminal/Command Prompt/PowerShell**
Applications where you type commands:
- **Windows**: Command Prompt or PowerShell
- **Mac**: Terminal  
- **Linux**: Terminal or Shell

### **Git**
A system for tracking changes to code over time. Like "track changes" in Microsoft Word but much more powerful.

**Key concepts:**
- **Repository**: A project folder with change tracking
- **Commit**: A snapshot of your code at a point in time
- **Branch**: A separate line of development
- **Merge**: Combining changes from different branches

## Security

### **JWT (JSON Web Token)**
A secure way to pass information between systems. Contains encoded data about who you are and what you're allowed to do.

**Analogy**: Like a wristband at a concert that proves you paid and shows which areas you can access.

### **HTTPS**
Encrypted communication between browsers/applications and servers. Prevents eavesdropping.

**Visual cue**: The lock icon in your browser's address bar.

### **HSTS (HTTP Strict Transport Security)**
Forces browsers to always use HTTPS when talking to your server, even if someone tries to trick them into using HTTP.

### **API Key**
A secret password that applications use to authenticate with services like Twilio.

**Important**: Never put API keys in code - always use environment variables or secure configuration.

---

## Common Patterns & Idioms

### **"Fail Fast"**
Detect and report errors as early as possible rather than continuing with invalid data.

### **"Fail Closed"**
When something goes wrong, default to the secure/safe option rather than the permissive one.

### **"Fire and Forget"**
Start an operation but don't wait for it to complete. Like dropping a letter in a mailbox.

### **"Request/Response"**
The basic pattern of web communication: you ask for something (request) and get an answer (response).

### **"Producer/Consumer"**
One part creates data (producer) and another part processes it (consumer). Like an assembly line.

---

This glossary will help you understand the technical discussions throughout the MmsRelay documentation. When you encounter unfamiliar terms, refer back to this document for clear, jargon-free explanations.