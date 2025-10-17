# MmsRelay

**What is MmsRelay?** A web service that makes it easy to send text messages (SMS) and multimedia messages (MMS) through your applications. Think of it as a simple bridge between your software and Twilio's phone service.

**Perfect for:** E-commerce order notifications, system alerts, marketing messages, appointment reminders, and any application that needs to send text messages to phones.

## 📱 What is SMS and MMS?

- **SMS** (Short Message Service): Plain text messages up to 160 characters - like "Your order #1234 has shipped!"
- **MMS** (Multimedia Messaging Service): Messages with images, videos, or longer text - like "Here's a photo of your delivered package: [image]"
- **Twilio**: A cloud service that handles the technical complexity of sending messages through phone carriers worldwide

## ✨ Key Features

- 🚀 **Simple HTTP API** - Send messages with a basic web request (no complex setup)
- 📱 **Works Worldwide** - Supports international phone numbers in proper E.164 format
- 🛡️ **Reliable & Safe** - Automatically retries failed sends and protects against service outages
- 📊 **Production Ready** - Built-in health monitoring and structured logging
- 🔧 **Easy to Deploy** - Works on Windows, Linux, Docker, or cloud platforms
- 📚 **Well Documented** - Comprehensive guides for beginners and experts
- 🧪 **Fully Tested** - Extensive test suite ensures everything works correctly

## 🚀 Quick Start (5 Minutes)

### What You Need First

**Software Requirements:**
- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download) (free)
- **Git** - For downloading the code
- **Text Editor** - Visual Studio, VS Code, or any editor you like

**Twilio Account (Free):**
1. Sign up at [https://www.twilio.com/try-twilio](https://www.twilio.com/try-twilio)
2. Get $15 in free credits (enough for hundreds of test messages)
3. Note down your Account SID, Auth Token, and phone number

### Step 1: Get the Code

```bash
git clone https://github.com/jamocle/MmsRelay.git
cd MmsRelay
```

### Step 2: Set Up Your Credentials (Safely)

**Why we do this:** Your Twilio credentials are like passwords - we store them securely so they never accidentally get shared or committed to Git.

```bash
cd src/MmsRelay

# This creates a secure, local-only storage for your credentials
dotnet user-secrets init

# Add your Twilio credentials (replace with your real ones)
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_actual_auth_token_here"  
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"
```

**Where to find these values:**
- Go to [Twilio Console Dashboard](https://console.twilio.com)
- Account SID and Auth Token are on the main dashboard
- Phone number is in Phone Numbers > Manage > Active numbers

### Step 3: Start the Service

```bash
# Install any missing packages and run
dotnet run
```

**You should see:**
```
info: MmsRelay.Program[0] Starting MmsRelay service...
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14] Now listening on: https://localhost:5001
```

**Test it's working:**
- Open http://localhost:5000/health/live in your browser
- Should show: `{"status":"Healthy"}`

### Step 4: Send Your First Message

**Option A: Use the Web Interface**
1. Open http://localhost:5000/swagger in your browser
2. Click on "POST /mms" 
3. Click "Try it out"
4. Enter your phone number and message
5. Click "Execute"

**Option B: Use the Command Line**
```bash
# Replace with your actual phone number
curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "+15551234567", 
    "body": "Hello from MmsRelay! 🎉"
  }'
```

**Option C: Use the Console Client** (Easiest)
```bash
# Navigate to the console client
cd ../../clients/MmsRelay.Client

# Send a message (replace with your phone number)
dotnet run -- send --to "+15551234567" --body "Hello from MmsRelay console!"
```

**Success Response:**
```json
{
  "provider": "twilio",
  "providerMessageId": "SM1234567890abcdef",
  "status": "queued"
}
```

**Check your phone** - you should receive the message within a few seconds!

### 🎉 Congratulations!

You now have MmsRelay running and can send messages. Here's what happens next:

1. **Your message goes to Twilio** - They handle delivery to phone carriers
2. **Check delivery status** - Go to [Twilio Console > Monitor > Logs](https://console.twilio.com/us1/monitor/logs/sms)
3. **Learn more** - Check out our [FAQ](FAQ.md) and [Examples](EXAMPLES.md)

## 🏗️ How MmsRelay Works (Simple Overview)

### The Big Picture

```
Your App → MmsRelay → Twilio → Phone Carrier → Recipient's Phone
```

1. **Your application** sends an HTTP request to MmsRelay
2. **MmsRelay** validates the request and forwards it to Twilio  
3. **Twilio** handles the complex phone network delivery
4. **Your recipient** gets the message on their phone

### What's Inside MmsRelay

```
clients/MmsRelay.Client/          # Command-line tool for sending messages
src/MmsRelay/                     # Main web service
├── Api/                          # Handles incoming HTTP requests  
├── Application/                  # Business rules (validation, etc.)
│   ├── Models/                   # Data structures for requests/responses
│   └── Validation/               # Rules to check if phone numbers are valid
└── Infrastructure/               # Connects to external services (Twilio)
    └── Twilio/                   # Twilio-specific code
tests/MmsRelay.Tests/            # Automated tests to ensure everything works
```

### Why This Architecture?

**Clean Architecture Benefits:**
- ✅ **Easy to test** - Each piece can be tested independently
- ✅ **Easy to change** - Want to use a different SMS service? Just swap the Infrastructure layer
- ✅ **Easy to understand** - Clear separation between web API, business logic, and external services
- ✅ **Easy to maintain** - Changes in one area don't break others

### Built-in Reliability Features

MmsRelay automatically handles common problems:

**🔄 Automatic Retries**
- If Twilio is temporarily unavailable, MmsRelay waits and tries again
- Uses "exponential backoff" - waits longer between each retry attempt
- Gives up after 5 attempts to avoid infinite loops

**⚡ Circuit Breaker**
- If Twilio is completely down, MmsRelay "fails fast" instead of wasting time
- Automatically checks if Twilio is back up and resumes when possible
- Protects your application from cascading failures

**⏱️ Timeouts**
- Requests never hang forever - they timeout after 30 seconds
- Prevents resource leaks and keeps your system responsive

## 🚀 Deployment Options

### For Beginners: Simple Windows Deployment

The easiest way to run MmsRelay in production on Windows:

```bash
# Create a single executable file with everything included
dotnet publish src/MmsRelay/MmsRelay.csproj -c Release \
  --self-contained true --runtime win-x64 -p:PublishSingleFile=true

# Copy to your server (creates C:\MmsRelay folder)
xcopy publish\* C:\MmsRelay\ /E /Y

# Set your production credentials
set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
set TWILIO__AUTHTOKEN=your_actual_auth_token_here  
set TWILIO__FROMPHONENUMBER=+15551234567

# Run it
C:\MmsRelay\MmsRelay.exe
```

### Other Deployment Options

**🐳 Docker** (Great for cloud platforms)
- Self-contained, consistent across environments
- Easy scaling and management
- Works on any platform that supports Docker

**🐧 Linux with systemd** (For Linux servers)
- Runs as a background service
- Automatic startup on server reboot
- Built-in process monitoring

**🌍 Cloud Platforms**
- Azure App Service, AWS Elastic Beanstalk, Google Cloud Run
- Managed hosting with automatic scaling
- No server maintenance required

**📖 Complete Deployment Guides:**
- **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)** - Step-by-step for all platforms
- **[DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)** - Development environment setup

## ⚙️ Configuration Made Simple

### Required Settings (You Must Have These)

**For Development** (stored securely on your machine):
```bash
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_auth_token" 
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"
```

**For Production** (environment variables):
```bash
# Windows
set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxx
set TWILIO__AUTHTOKEN=your_auth_token
set TWILIO__FROMPHONENUMBER=+15551234567

# Linux/Mac
export TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxx
export TWILIO__AUTHTOKEN=your_auth_token  
export TWILIO__FROMPHONENUMBER=+15551234567
```

### Phone Number Format Rules

**✅ Correct Format (E.164):**
- Must start with `+`
- Country code next (1 for US/Canada, 44 for UK, etc.)
- No spaces, dashes, or parentheses

**Examples:**
- US: `+15551234567` (not `555-123-4567`)
- UK: `+442012345678` (not `020 1234 5678`)
- Germany: `+4930123456789` (not `030-12345678`)

### Optional Settings (Nice to Have)

**Change the Port:**
```bash
# Default is 5000/5001
set URLS=http://localhost:8080  
```

**Advanced Twilio Features:**
```bash  
# Use Messaging Service instead of phone number
set TWILIO__MESSAGINGSERVICESID=MGxxxxxxxxxxxxxxxx

# Adjust retry behavior
set TWILIO__REQUESTTIMEOUTSECONDS=60
set TWILIO__RETRY__MAXRETRIES=3
```

**Logging Levels:**
```bash
# Show more detailed logs for debugging
set LOGGING__LOGLEVEL__DEFAULT=Debug

# Show only errors and warnings
set LOGGING__LOGLEVEL__DEFAULT=Warning
```

## 📡 API Reference (How to Send Messages)

### Send a Text Message (SMS)

**HTTP Request:**
```bash
POST http://localhost:5000/mms
Content-Type: application/json

{
  "to": "+15551234567",
  "body": "Your order #1234 has been shipped!"
}
```

### Send a Message with Images (MMS)

**HTTP Request:**
```bash
POST http://localhost:5000/mms  
Content-Type: application/json

{
  "to": "+15551234567",
  "body": "Here's your delivery photo:",
  "mediaUrls": [
    "https://example.com/delivery-photo.jpg",
    "https://example.com/receipt.pdf"  
  ]
}
```

### What You Get Back

**Success Response (HTTP 202):**
```json
{
  "provider": "twilio",
  "providerMessageId": "SM1234567890abcdef",
  "status": "queued",
  "providerMessageUri": "https://api.twilio.com/2010-04-01/Accounts/ACxxxxx/Messages/SM1234567890abcdef.json"
}
```

**What This Means:**
- **provider**: Always "twilio" (we might support others in the future)
- **providerMessageId**: Twilio's unique ID for tracking this message
- **status**: "queued" means Twilio accepted it and will deliver it soon
- **providerMessageUri**: Link to check delivery status in Twilio Console

### Error Responses

**Invalid Phone Number (HTTP 400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "To": ["Phone number must be in E.164 format (e.g., +15551234567)"]
  }
}
```

**Service Unavailable (HTTP 503):**
```json
{
  "type": "https://httpstatuses.io/503", 
  "title": "Service Unavailable",
  "status": 503,
  "detail": "The circuit breaker is open due to too many failed requests to Twilio"
}
```

### Health Check Endpoints

**Check if Service is Running:**
```bash
GET http://localhost:5000/health/live
# Returns: {"status":"Healthy"}
```

**Check if Service Can Send Messages:**
```bash  
GET http://localhost:5000/health/ready
# Returns: {"status":"Healthy"} if Twilio is reachable
# Returns: {"status":"Unhealthy"} if Twilio is down
```

### Interactive API Documentation

**Swagger UI** (Best for Testing):
- Open http://localhost:5000/swagger in your browser
- Click "Try it out" on any endpoint
- Fill in the form and click "Execute"
- See the request/response in real time

**OpenAPI Spec** (For Code Generation):
- http://localhost:5000/swagger/v1/swagger.json
- Use with tools like Postman, Insomnia, or code generators

## 🧪 Testing & Quality Assurance

### Run All Tests

```bash
# Run the complete test suite
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests and see coverage
dotnet test --collect:"XPlat Code Coverage"
```

**What Gets Tested:**
- ✅ **Phone Number Validation** - Ensures E.164 format compliance
- ✅ **Twilio Integration** - Mocked API calls verify correct behavior
- ✅ **Error Handling** - Resilience patterns and circuit breakers
- ✅ **Configuration** - All settings load correctly
- ✅ **Health Checks** - Service monitoring works properly

### Testing Your Changes

**Before Committing Code:**
```bash
# 1. All tests pass
dotnet test

# 2. Code builds without warnings  
dotnet build --configuration Release /warnaserror

# 3. Service still works
cd src/MmsRelay
dotnet run
# Test in another terminal:
curl http://localhost:5000/health/live
```

## 📚 Complete Documentation

### 🚀 Getting Started Guides
- **[FAQ.md](FAQ.md)** - Common questions and answers for beginners
- **[EXAMPLES.md](EXAMPLES.md)** - Real-world usage scenarios with complete code
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Solutions to common problems
- **[GLOSSARY.md](GLOSSARY.md)** - Technical terms explained simply

### 🔧 Advanced Guides  
- **[DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)** - Complete development environment setup
- **[PRODUCTION-DEPLOYMENT.md](PRODUCTION-DEPLOYMENT.md)** - Production deployment for all platforms
- **[knowledge.md](knowledge.md)** - Deep dive into architecture and implementation

### 🤝 Contributing
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - How to contribute code, docs, or bug reports
- **[CHANGELOG.md](CHANGELOG.md)** - Version history and breaking changes

## 🤝 Contributing & Community

We welcome contributions from developers of all skill levels!

**Easy Ways to Help:**
- 📝 Fix typos or improve documentation
- 🐛 Report bugs or suggest improvements
- ✨ Add new features or message providers
- 🧪 Write tests or improve code coverage

**Getting Started:**
1. Read [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines
2. Look for issues labeled `good first issue`
3. Join discussions on GitHub Issues or Discussions

## 📄 License & Legal

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**What This Means:**
- ✅ Free to use in commercial projects
- ✅ Free to modify and distribute
- ✅ No warranty or liability from maintainers
- ✅ Must include original license in copies

## 🆘 Getting Help & Support

### 🔍 First, Check These Resources

1. **[FAQ.md](FAQ.md)** - Answers to common questions
2. **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Solutions to common problems  
3. **[EXAMPLES.md](EXAMPLES.md)** - Real-world usage patterns
4. **Existing GitHub Issues** - Your question might already be answered

### 💬 Ask for Help

**GitHub Issues** (Best for bugs and feature requests):
- Create a new issue with detailed information
- Include error messages, configuration, and steps to reproduce
- Tag with appropriate labels (bug, question, feature request)

**GitHub Discussions** (Best for questions):
- General questions about usage or architecture
- Community discussions and feature brainstorming
- Help with specific implementation challenges

**What to Include in Help Requests:**
- Operating system and .NET version
- MmsRelay version or Git commit
- Configuration details (remove sensitive credentials!)
- Complete error messages and stack traces
- Steps to reproduce the problem

### 🚨 Security Issues

For security vulnerabilities, please **DO NOT** create a public issue. Instead:
- Email security issues directly to maintainers
- Use GitHub's private security advisory feature
- Include full details and potential impact

---

**Ready to get started?** 🎉 Jump to the [Quick Start](#-quick-start-5-minutes) section above!