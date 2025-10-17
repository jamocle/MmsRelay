# MmsRelay Console Client

**What is this?** A simple command-line tool that lets you send text messages (SMS) and multimedia messages (MMS) from your computer's terminal or command prompt.

**Perfect for:** Testing your MmsRelay service, sending automated notifications from scripts, system administration tasks, or quick one-off messages.

## 🚀 5-Minute Quick Start

### What You Need

- **.NET 8** installed on your computer ([download here](https://dotnet.microsoft.com/download))
- **MmsRelay service** running somewhere (see main [README.md](../../README.md) for setup)
- A **phone number** to send messages to (your own is perfect for testing!)

### Step 1: Get the Console Client

```bash
# Get the code (if you don't have it already)
git clone https://github.com/jamocle/MmsRelay.git
cd MmsRelay/clients/MmsRelay.Client

# Build the client (downloads dependencies)
dotnet build
```

### Step 2: Send Your First Message

```bash
# Send a simple text message (replace with your phone number)
dotnet run -- send --to "+15551234567" --body "Hello from MmsRelay console!"

# Check that your MmsRelay service is working  
dotnet run -- health
```

**You should see:**
```
✅ Message sent successfully!
Provider: twilio
Message ID: SM1234567890abcdef
Status: queued
```

### Step 3: Try More Features

```bash
# Send a message with an image
dotnet run -- send --to "+15551234567" \
  --body "Check out this image!" \
  --media "https://httpbin.org/image/png"

# Send to a different MmsRelay service
dotnet run -- send --to "+15551234567" \
  --body "Using production service" \
  --service-url "https://api.yourdomain.com"

# Get detailed output for troubleshooting
dotnet run -- send --to "+15551234567" \
  --body "Debug message" \
  --verbose
```

## 📋 All Available Commands

### Send Messages

```bash
# Basic text message
dotnet run -- send --to "+15551234567" --body "Your message here"

# Text message with image  
dotnet run -- send --to "+15551234567" \
  --body "Look at this!" \
  --media "https://example.com/image.jpg"

# Multiple images (up to 10 for MMS)
dotnet run -- send --to "+15551234567" \
  --body "Multiple attachments" \
  --media "https://example.com/image1.jpg" \
  --media "https://example.com/image2.jpg" \
  --media "https://example.com/document.pdf"

# Just an image (no text)
dotnet run -- send --to "+15551234567" \
  --media "https://example.com/image.jpg"
```

### Check Service Health

```bash
# Quick health check
dotnet run -- health

# Check a different service
dotnet run -- health --service-url "https://api.yourdomain.com"
```

### Get Help

```bash
# See all available commands
dotnet run -- --help

# Get help for a specific command
dotnet run -- send --help
dotnet run -- health --help
```

## 🛠️ Advanced Features

### Phone Number Formats

**✅ Correct (E.164 format):**
- US: `+15551234567`
- UK: `+442012345678` 
- Canada: `+15551234567`
- Germany: `+4930123456789`

**❌ Incorrect:**
- `555-123-4567` (missing country code and +)
- `(555) 123-4567` (has parentheses)
- `15551234567` (missing +)

### Media File Types

**Supported formats:**
- **Images**: JPEG, PNG, GIF
- **Documents**: PDF
- **Videos**: MP4, MOV (small files only)

**Requirements:**
- Must be publicly accessible URLs (not local files)
- Maximum 10 media attachments per message
- Each file must be under 5MB
- Total message size limit varies by carrier

### Custom Service URLs

```bash
# Development server
dotnet run -- send --to "+15551234567" --body "Dev test" \
  --service-url "http://localhost:5000"

# Staging environment  
dotnet run -- send --to "+15551234567" --body "Staging test" \
  --service-url "https://staging-api.yourdomain.com"

# Production with HTTPS
dotnet run -- send --to "+15551234567" --body "Production message" \
  --service-url "https://api.yourdomain.com"
```

### Verbose Mode for Troubleshooting

```bash
# See detailed HTTP requests and responses
dotnet run -- send --to "+15551234567" --body "Debug message" --verbose
```

**Verbose output includes:**
- Full HTTP request details (URL, headers, body)
- Complete HTTP response (status, headers, body)
- Timing information
- Error details if something goes wrong

## 🔧 Building and Distribution
### Create Standalone Executable (No .NET Required)

**For Distribution:**
```bash
# Create a single file that works on any Windows computer (no .NET install needed)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# The result is a single .exe file you can copy anywhere
# Located in: bin/Release/net8.0/win-x64/publish/MmsRelay.Client.exe

# For Linux servers
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# For Mac
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

**Using the Standalone Executable:**
```bash
# Windows
MmsRelay.Client.exe send --to "+15551234567" --body "Hello!"

# Linux/Mac  
./MmsRelay.Client send --to "+15551234567" --body "Hello!"
```

### Create Distribution Package

```bash
# Build everything and create a zip file for distribution
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
cd bin/Release/net8.0/win-x64/publish
zip -r MmsRelay-Client-Windows.zip MmsRelay.Client.exe
```

## 🔧 Real-World Usage Examples

### Automated Order Notifications

```bash
#!/bin/bash
# Script: notify-customer.sh
# Usage: ./notify-customer.sh "+15551234567" "12345" "shipped"

CUSTOMER_PHONE=$1
ORDER_NUMBER=$2  
STATUS=$3

dotnet run -- send \
  --to "$CUSTOMER_PHONE" \
  --body "Order #$ORDER_NUMBER status: $STATUS. Track at https://example.com/track/$ORDER_NUMBER" \
  --service-url "https://api.yourdomain.com"
```

### System Health Monitoring

```bash
#!/bin/bash
# Script: check-and-alert.sh
# Checks service health and alerts if down

if ! dotnet run -- health --service-url "https://api.yourdomain.com" > /dev/null 2>&1; then
    echo "Service is down! Sending alert..."
    dotnet run -- send \
        --to "+15551234567" \
        --body "ALERT: MmsRelay service is down at $(date)" \
        --service-url "https://backup-service.yourdomain.com"
fi
```

### Batch Message Sending

```bash
#!/bin/bash
# Script: send-batch.sh
# Send messages to multiple customers

# Read phone numbers from file (one per line)
while IFS= read -r phone_number; do
    if [[ ! -z "$phone_number" ]]; then
        echo "Sending to $phone_number..."
        dotnet run -- send \
            --to "$phone_number" \
            --body "Weekly newsletter: Check out our latest updates!" \
            --media "https://example.com/newsletter.jpg"
        
        # Wait 2 seconds to respect rate limits
        sleep 2
    fi
done < customer_phones.txt
```

### Windows Task Scheduler Integration

```batch
@echo off
REM Script: daily-reminder.bat
REM Schedule this in Windows Task Scheduler for daily execution

cd "C:\MmsRelay\clients\MmsRelay.Client"
dotnet.exe run -- send --to "+15551234567" --body "Daily backup completed successfully at %DATE% %TIME%"

if errorlevel 1 (
    echo Failed to send notification
    exit /b 1
) else (
    echo Notification sent successfully
    exit /b 0
)
```

## ⚙️ Configuration Options

### Environment Variables (For Scripts)

Set these to avoid repeating options:

```bash
# Windows Command Prompt
set MMSRELAY__BASEURL=https://api.yourdomain.com
set MMSRELAY__TIMEOUTSECONDS=60

# Windows PowerShell
$env:MMSRELAY__BASEURL = "https://api.yourdomain.com"
$env:MMSRELAY__TIMEOUTSECONDS = "60"

# Linux/Mac Bash
export MMSRELAY__BASEURL="https://api.yourdomain.com"
export MMSRELAY__TIMEOUTSECONDS=60

# Then use without --service-url
dotnet run -- send --to "+15551234567" --body "Uses environment variable URL"
```

### Configuration File (appsettings.json)

Create `appsettings.json` in the client folder:

```json
{
  "MmsRelay": {
    "BaseUrl": "https://api.yourdomain.com",
    "TimeoutSeconds": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

## 🐛 Troubleshooting Common Problems

### "Phone number must be in E.164 format"

**Problem:** You get validation errors about phone number format.

**Solution:** Always use the international format:
- ✅ `+15551234567` (correct)
- ❌ `555-123-4567` (missing country code)
- ❌ `(555) 123-4567` (has formatting)

### "Unable to connect to MmsRelay service"

**Problem:** Can't reach the service.

**Solutions:**
1. **Check if service is running:**
   ```bash
   # Test if the service is up
   curl http://localhost:5000/health/live
   # Or
   dotnet run -- health --service-url "http://localhost:5000"
   ```

2. **Check the URL:**
   ```bash
   # Try different URLs
   dotnet run -- health --service-url "http://localhost:5000"
   dotnet run -- health --service-url "http://localhost:8080"  
   ```

3. **Check your network:**
   ```bash
   ping yourdomain.com
   ```

### "HTTP 401 Unauthorized"

**Problem:** Service returns authentication errors.

**Solution:** Check your MmsRelay service configuration - it might need API keys or have authentication enabled.

### Media Files Not Working

**Problem:** Images or attachments don't send.

**Common Issues:**
- ❌ Local file paths: `file:///C:/image.jpg` (won't work)
- ✅ Public URLs: `https://example.com/image.jpg` (works)
- ❌ Private URLs that require login (won't work)
- ❌ Files too large (>5MB per file)

**Test your media URLs:**
```bash
# Can you access it in a browser? 
curl -I https://example.com/image.jpg
# Should return HTTP 200 OK
```

### Verbose Mode for Debugging

**See exactly what's happening:**
```bash
dotnet run -- send --to "+15551234567" --body "Debug test" --verbose
```

**Output shows:**
- Exact HTTP request sent
- Full HTTP response received  
- Timing information
- Any retry attempts

## 🚀 Advanced Integration Examples

### GitHub Actions (CI/CD)

**Notify team when deployment completes:**
```yaml
# .github/workflows/deploy.yml
name: Deploy and Notify

on: 
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - name: Deploy application
      run: |
        # Your deployment commands here
        echo "Deploying application..."
        
    - name: Notify team of successful deployment
      if: success()
      run: |
        ./MmsRelay.Client send \
          --to "${{ secrets.TEAM_PHONE }}" \
          --body "✅ Deployment successful for commit ${{ github.sha }}" \
          --service-url "${{ secrets.MMSRELAY_URL }}"
          
    - name: Notify team of failed deployment  
      if: failure()
      run: |
        ./MmsRelay.Client send \
          --to "${{ secrets.TEAM_PHONE }}" \
          --body "❌ Deployment FAILED for commit ${{ github.sha }}" \
          --service-url "${{ secrets.MMSRELAY_URL }}"
```

### PowerShell Script (Windows Automation)

**Monitor disk space and alert:**
```powershell
# Script: DiskSpaceMonitor.ps1
param(
    [string]$AlertPhone = "+15551234567",
    [int]$ThresholdPercent = 80
)

$drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}

foreach ($drive in $drives) {
    $freePercent = ($drive.FreeSpace / $drive.Size) * 100
    $usedPercent = 100 - $freePercent
    
    if ($usedPercent -gt $ThresholdPercent) {
        $message = "⚠️ DISK ALERT: Drive $($drive.DeviceID) is $([math]::Round($usedPercent, 1))% full on $env:COMPUTERNAME"
        
        & dotnet run -- send --to $AlertPhone --body $message
        Write-Host "Alert sent for drive $($drive.DeviceID)"
    }
}
```

### Linux Cron Job (Scheduled Messages)

```bash
# Add to crontab: crontab -e
# Send daily backup reminder at 2 AM
0 2 * * * cd /opt/mmsrelay && ./MmsRelay.Client send --to "+15551234567" --body "Daily backup starting on $(hostname)"

# Send weekly report on Mondays at 9 AM
0 9 * * 1 cd /opt/mmsrelay && ./MmsRelay.Client send --to "+15551234567" --body "Weekly server report: All systems operational"
```

### Docker Integration

**Dockerfile for containerized usage:**
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish/ .

ENTRYPOINT ["./MmsRelay.Client"]
CMD ["--help"]
```

**Use in Docker Compose:**
```yaml
# docker-compose.yml
version: '3.8'
services:
  notification-sender:
    build: .
    environment:
      - MMSRELAY__BASEURL=https://api.yourdomain.com
    command: ["send", "--to", "+15551234567", "--body", "Service started"]
```

## 🎯 Performance Tips & Best Practices

### For High-Volume Usage

**Batch Processing with Rate Limiting:**
```bash
#!/bin/bash
# Send to 100 customers with 2-second delays (respects Twilio rate limits)

while IFS=, read -r phone message; do
    echo "Sending to $phone..."
    dotnet run -- send --to "$phone" --body "$message"
    
    # Wait 2 seconds between messages (30 messages/minute max)
    sleep 2
done < customer_list.csv
```

### Memory and CPU Optimization

**Long-running Service Pattern:**
```csharp
// For applications that send many messages, consider hosting
// the client as a background service instead of spawning processes

// Program.cs - Host as a background service
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<MessageSendingService>();
    })
    .Build();

await host.RunAsync();
```

### Error Recovery Strategies

**Robust Retry Script:**
```bash
#!/bin/bash
# retry-send.sh - Retry failed messages with exponential backoff

send_with_retry() {
    local phone=$1
    local message=$2
    local max_attempts=3
    local delay=1
    
    for ((i=1; i<=max_attempts; i++)); do
        if dotnet run -- send --to "$phone" --body "$message"; then
            echo "✅ Message sent successfully to $phone"
            return 0
        else
            echo "❌ Attempt $i failed, waiting ${delay}s..."
            sleep $delay
            delay=$((delay * 2))  # Exponential backoff
        fi
    done
    
    echo "🚫 Failed to send to $phone after $max_attempts attempts"
    return 1
}

# Usage
send_with_retry "+15551234567" "Important notification"
```

## 📚 Additional Resources

### Documentation Links
- **[Main README](../../README.md)** - MmsRelay service setup and overview
- **[FAQ](../../FAQ.md)** - Common questions and answers  
- **[Examples](../../EXAMPLES.md)** - Real-world usage scenarios
- **[Troubleshooting](../../TROUBLESHOOTING.md)** - Problem solving guide
- **[Contributing](../../CONTRIBUTING.md)** - How to contribute to the project

### Getting Help

**Before asking for help, try:**
1. Check the [Troubleshooting](#-troubleshooting-common-problems) section above
2. Enable `--verbose` mode to see detailed error information
3. Test with the `health` command to verify service connectivity
4. Check that phone numbers are in correct E.164 format

**Where to get help:**
- � **GitHub Issues** - Bug reports and feature requests
- � **GitHub Discussions** - General questions and community help  
- � **Documentation** - Comprehensive guides and examples

**When reporting issues, include:**
- Operating system and .NET version
- Complete command you ran and error message
- Output from `--verbose` mode
- Whether the `health` command works

---

**Ready to start sending messages?** 🚀 Go back to the [Quick Start](#-5-minute-quick-start) section and send your first message!