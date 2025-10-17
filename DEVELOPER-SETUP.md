# MmsRelay - Developer Setup Guide

## 🛠️ Prerequisites

### Required Software
- **Visual Studio 2022** (17.8 or later) OR **VS Code** with C# extension
- **.NET 8 SDK** (8.0.100 or later)
- **Git** for source control
- **Postman** or **curl** for API testing (optional but recommended)

### Recommended Tools
- **Docker Desktop** (for containerization testing)
- **Windows Terminal** (better PowerShell experience)
- **Azure CLI** (if deploying to Azure)
- **Twilio CLI** (for Twilio account management)

## 🚀 Development Environment Setup

### 1. Clone and Build
```bash
# Clone the repository
git clone https://github.com/jamocle/MmsRelay.git
cd MmsRelay

# Restore dependencies and build
dotnet restore
dotnet build

# Run tests to verify setup
dotnet test
```

### 2. Twilio Account Setup

**Get Twilio Credentials:**
1. Sign up at [Twilio Console](https://console.twilio.com/)
2. Navigate to **Account Dashboard**
3. Copy your **Account SID** and **Auth Token**
4. Purchase a phone number or set up a Messaging Service

**Find Your Credentials:**
- **Account SID**: Starts with "AC..." (found on dashboard)
- **Auth Token**: Click the eye icon to reveal (found on dashboard)
- **Phone Number**: Format as E.164 (e.g., +15551234567)
- **Messaging Service SID**: Optional, starts with "MG..." (Messaging > Services)

### 3. Configure User Secrets (Recommended)

**Initialize User Secrets:**
```bash
# Navigate to the main project
cd src/MmsRelay

# Initialize user secrets (creates unique ID)
dotnet user-secrets init

# Set Twilio configuration
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_actual_auth_token_here"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"

# Optional: Set messaging service instead of phone number
dotnet user-secrets set "twilio:messagingServiceSid" "MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# Verify secrets are set
dotnet user-secrets list
```

**User Secrets Benefits:**
- Keeps sensitive data out of source control
- Works seamlessly with appsettings.json
- Automatically loaded in Development environment
- Stored securely in user profile

### 4. Alternative: Environment Variables

**PowerShell (Windows):**
```powershell
# Set for current session
$env:TWILIO__ACCOUNTSID = "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
$env:TWILIO__AUTHTOKEN = "your_actual_auth_token_here"  
$env:TWILIO__FROMPHONENUMBER = "+15551234567"

# Set permanently (optional)
[Environment]::SetEnvironmentVariable("TWILIO__ACCOUNTSID", "ACxxxxxxxx", "User")
[Environment]::SetEnvironmentVariable("TWILIO__AUTHTOKEN", "your_token", "User")
[Environment]::SetEnvironmentVariable("TWILIO__FROMPHONENUMBER", "+15551234567", "User")
```

**Command Prompt (Windows):**
```cmd
set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
set TWILIO__AUTHTOKEN=your_actual_auth_token_here
set TWILIO__FROMPHONENUMBER=+15551234567
```

**Bash (Linux/macOS):**
```bash
export TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
export TWILIO__AUTHTOKEN=your_actual_auth_token_here
export TWILIO__FROMPHONENUMBER=+15551234567
```

## ▶️ Running the Application

### Option 1: Visual Studio

1. **Open Solution**: Open `MmsRelay.sln` in Visual Studio
2. **Set Startup Project**: Right-click `MmsRelay` project → Set as Startup Project
3. **Configure Launch Profile**: 
   - Right-click project → Properties → Debug → General
   - Ensure "Launch profile" is set to "MmsRelay"
4. **Start Debugging**: Press `F5` or click the green play button
5. **Verify**: Application starts on `http://localhost:5000` or `https://localhost:5001`

### Option 2: VS Code

1. **Open Folder**: Open the root `MmsRelay` folder in VS Code
2. **Install Extensions**: C# extension should auto-install
3. **Configure Launch**: VS Code should auto-generate `.vscode/launch.json`
4. **Start Debugging**: Press `F5` or go to Run and Debug panel
5. **Verify**: Check the terminal for startup messages

### Option 3: Command Line

```bash
# Navigate to project directory
cd src/MmsRelay

# Run in development mode
dotnet run

# Or run with specific environment
dotnet run --environment Development

# Or run with watch (auto-reload on changes)
dotnet watch run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: MmsRelay started via Serilog static API 👋
info: MmsRelay started via ASP.NET Core ILogger 👋
info: MmsRelay started via scoped ILogger<Program> 👋
```

## 🧪 Testing the Application

### Health Check Test
```bash
# Test health endpoint
curl http://localhost:5000/health/live

# Expected response: HTTP 200 OK
```

### Send Test MMS
```bash
# Send test MMS (replace with your test phone number)
curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "+15551234567",
    "body": "Test message from MmsRelay development",
    "mediaUrls": ["https://httpbin.org/image/png"]
  }'

# Expected response: HTTP 202 Accepted
{
  "provider": "twilio",
  "providerMessageId": "SM...",
  "status": "queued",
  "providerMessageUri": "https://api.twilio.com/..."
}
```

### Validation Testing
```bash
# Test validation - invalid phone number
curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "invalid-phone",
    "body": "Test"
  }'

# Expected response: HTTP 400 Bad Request with validation errors
```

## 🐛 Debugging Techniques

### 1. Breakpoint Debugging

**In Visual Studio:**
- Set breakpoints by clicking in the left margin
- Press `F5` to start debugging
- Use `F10` (step over) and `F11` (step into)
- Inspect variables in Locals/Watch windows

**In VS Code:**
- Set breakpoints by clicking in the left margin
- Press `F5` to start debugging
- Use debugging controls in the debug toolbar

### 2. Logging Analysis

**Enable Debug Logging:**
```json
// In appsettings.Development.json
{
  "logging": {
    "levelSwitch": "Debug",
    "logLevel": {
      "MmsRelay": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**View Structured Logs:**
- Console output shows JSON structured logs
- Look for correlation IDs in log entries
- Filter by log level and source

### 3. HTTP Request Debugging

**Enable HttpClient Logging:**
```json
// In appsettings.Development.json
{
  "logging": {
    "logLevel": {
      "System.Net.Http.HttpClient": "Debug"
    }
  }
}
```

**Analyze Polly Policies:**
- Set breakpoints in policy handlers
- Watch retry attempts and delays
- Monitor circuit breaker state changes

## ⚙️ Development Configuration

### appsettings.Development.json (Recommended)
```json
{
  "logging": {
    "levelSwitch": "Debug",
    "logLevel": {
      "MmsRelay": "Debug",
      "Microsoft.AspNetCore": "Information",
      "System.Net.Http.HttpClient": "Warning"
    }
  },
  "twilio": {
    "requestTimeoutSeconds": 30,
    "retry": {
      "maxRetries": 2,
      "baseDelayMs": 100
    },
    "circuitBreaker": {
      "samplingDurationSeconds": 30,
      "failureRatio": 0.8,
      "minThroughput": 5,
      "breakDurationSeconds": 15
    }
  }
}
```

**Development Settings Rationale:**
- **Shorter timeouts**: Faster feedback during development
- **Fewer retries**: Quicker failure detection
- **Relaxed circuit breaker**: More forgiving during testing
- **Debug logging**: Detailed information for troubleshooting

## 🔧 IDE Configuration

### Visual Studio Setup

**Recommended Extensions:**
- **SonarLint**: Code quality analysis
- **CodeMaid**: Code cleanup and formatting
- **Productivity Power Tools**: Enhanced IDE features

**Project Settings:**
1. **Properties → Debug → General**:
   - Launch: Project
   - Launch browser: Unchecked (API project)
   - Environment variables: Set if not using User Secrets

2. **Properties → Build**:
   - Treat warnings as errors: True (for production quality)
   - Warning level: 4

### VS Code Setup

**Recommended Extensions:**
```json
// .vscode/extensions.json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-vscode.vscode-json",
    "bradlc.vscode-tailwindcss",
    "ms-vscode.powershell"
  ]
}
```

**Workspace Settings:**
```json
// .vscode/settings.json
{
  "dotnet.defaultSolution": "MmsRelay.sln",
  "files.exclude": {
    "**/bin": true,
    "**/obj": true
  },
  "csharp.semanticHighlighting.enabled": true,
  "editor.formatOnSave": true
}
```

## 🔍 Troubleshooting Common Issues

### Issue 1: Twilio Authentication Errors
**Symptoms**: HTTP 401 Unauthorized from Twilio API
**Solutions**:
```bash
# Verify credentials are set
dotnet user-secrets list

# Test credentials directly
curl -X GET "https://api.twilio.com/2010-04-01/Accounts/{AccountSid}.json" \
  -u "{AccountSid}:{AuthToken}"
```

### Issue 2: Port Already in Use
**Symptoms**: "Address already in use" error
**Solutions**:
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (replace PID)
taskkill /PID 1234 /F

# Or change port in launchSettings.json
```

### Issue 3: SSL Certificate Issues
**Symptoms**: SSL/HTTPS errors in development
**Solutions**:
```bash
# Trust development certificate
dotnet dev-certs https --trust

# Or disable HTTPS for development
dotnet run --urls "http://localhost:5000"
```

### Issue 4: Missing Dependencies
**Symptoms**: Build or runtime errors
**Solutions**:
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build

# Check for package updates
dotnet list package --outdated
```

## 📝 Git Workflow for Development

### Recommended .gitignore Additions
```
# User-specific files
*.user
*.userosscache
*.sln.docstates

# User secrets
**/secrets.json
**/secrets.env

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/

# IDE files
.vs/
.vscode/
*.swp
*.swo
*~

# Logs
logs/
*.log
```

### Development Workflow
```bash
# Create feature branch
git checkout -b feature/new-feature

# Make changes and test
dotnet test
dotnet run

# Commit changes
git add .
git commit -m "Add new feature"

# Push and create PR
git push origin feature/new-feature
```

## 🚀 Quick Start Summary

```bash
# 1. Clone and build
git clone <repo> && cd MmsRelay && dotnet build

# 2. Set secrets
cd src/MmsRelay
dotnet user-secrets init
dotnet user-secrets set "twilio:accountSid" "ACxxxxx"
dotnet user-secrets set "twilio:authToken" "token"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"

# 3. Run and test
dotnet run
curl http://localhost:5000/health/live
```

This guide provides everything needed to get started with MmsRelay development quickly and securely!