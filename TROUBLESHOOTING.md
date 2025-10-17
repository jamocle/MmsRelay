# MmsRelay Troubleshooting Guide

This guide helps you diagnose and fix common issues with MmsRelay. Start with the most common problems and work your way down.

## Quick Diagnostics Checklist

Before diving into specific issues, run through this quick checklist:

```bash
# 1. Is the service running?
curl http://localhost:5000/health/live

# 2. Can it connect to Twilio?  
curl http://localhost:5000/health/ready

# 3. Check the logs
# Windows: Check console output or Windows Event Log
# Linux: journalctl -u mmsrelay -f
# Docker: docker logs <container-name>
```

---

## Common Issues & Solutions

### 🚫 Service Won't Start

#### **Issue**: Service fails to start with configuration errors

**Symptoms:**
- "Configuration validation failed" errors
- Service exits immediately after starting
- Cannot bind to port errors

**Solutions:**

1. **Check Configuration:**
```bash
# Verify Twilio settings are present
cd src/MmsRelay

# Development
dotnet user-secrets list

# Production  
echo $TWILIO__ACCOUNTSID
echo $TWILIO__AUTHTOKEN
echo $TWILIO__FROMPHONENUMBER
```

2. **Fix Missing Configuration:**
```bash
# Development setup
dotnet user-secrets set "twilio:accountSid" "ACxxxxxxxxxxxxx"
dotnet user-secrets set "twilio:authToken" "your_auth_token"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"

# Production setup
export TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxx
export TWILIO__AUTHTOKEN=your_auth_token
export TWILIO__FROMPHONENUMBER=+15551234567
```

3. **Port Already in Use:**
```bash
# Check what's using port 5000/5001
# Windows:
netstat -ano | findstr :5000

# Linux/Mac:
lsof -i :5000

# Change port if needed
export ASPNETCORE_URLS="http://localhost:8080;https://localhost:8081"
```

4. **Permissions Issues:**
```bash
# Windows: Run as Administrator
# Linux: Check file permissions
chmod +x MmsRelay
```

---

### 📱 Phone Number & Validation Issues

#### **Issue**: "Phone number must be in E.164 format"

**Symptoms:**
- HTTP 400 Bad Request
- Validation error messages about phone format

**The E.164 Format Rule:**
- Must start with `+`
- Country code next (1 for US/Canada, 44 for UK, etc.)
- Phone number with NO spaces, dashes, or parentheses

**Examples:**

| ❌ Wrong | ✅ Correct | Country |
|----------|------------|---------|
| 5551234567 | +15551234567 | US |
| 1-555-123-4567 | +15551234567 | US |
| (555) 123-4567 | +15551234567 | US |
| 020 1234 5678 | +442012345678 | UK |
| 030-12345678 | +4930123456789 | Germany |

**Quick Fix Script:**
```bash
#!/bin/bash
# format-phone.sh - Convert US phone numbers to E.164

format_us_phone() {
    local phone="$1"
    # Remove everything except digits
    digits=$(echo "$phone" | sed 's/[^0-9]//g')
    
    # Add +1 if it's 10 digits (US number without country code)
    if [ ${#digits} -eq 10 ]; then
        echo "+1$digits"
    elif [ ${#digits} -eq 11 ] && [[ $digits == 1* ]]; then
        echo "+$digits"
    else
        echo "Invalid US phone number: $phone" >&2
        return 1
    fi
}

# Usage examples:
format_us_phone "555-123-4567"     # Returns: +15551234567
format_us_phone "(555) 123-4567"   # Returns: +15551234567  
format_us_phone "15551234567"      # Returns: +15551234567
```

---

### 🌐 Network & Connection Issues

#### **Issue**: "Unable to connect to MmsRelay service"

**Symptoms:**
- Connection refused errors
- Timeout errors
- Network unreachable

**Step-by-Step Diagnosis:**

1. **Check Service Status:**
```bash
# Is the process running?
# Windows:
tasklist | findstr MmsRelay

# Linux:
ps aux | grep MmsRelay
systemctl status mmsrelay
```

2. **Test Local Connectivity:**
```bash
# Test if port is open locally
telnet localhost 5000

# Alternative test
curl -v http://localhost:5000/health/live
```

3. **Test Remote Connectivity:**
```bash
# From another machine
curl -v http://your-server:5000/health/live

# Check if firewall is blocking
# Windows Firewall
netsh advfirewall firewall show rule name="MmsRelay"

# Linux iptables  
iptables -L | grep 5000
```

4. **DNS Issues:**
```bash
# Test DNS resolution
nslookup your-mmsrelay-server.com

# Test direct IP
curl http://192.168.1.100:5000/health/live
```

**Common Fixes:**

```bash
# Open Windows Firewall port
netsh advfirewall firewall add rule name="MmsRelay" dir=in action=allow protocol=TCP localport=5000

# Open Linux firewall port (ufw)
ufw allow 5000

# Open Linux firewall port (iptables)
iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
```

---

### 🔐 Authentication & Twilio Issues

#### **Issue**: "Authentication failed" (HTTP 401)

**Symptoms:**
- HTTP 401 Unauthorized responses
- "Invalid credentials" messages from Twilio
- Health check fails on `/health/ready`

**Diagnosis Steps:**

1. **Verify Credentials Format:**
```bash
# Account SID should start with "AC"
echo $TWILIO__ACCOUNTSID | grep "^AC"

# Auth Token should be 32 characters
echo $TWILIO__AUTHTOKEN | wc -c  # Should be 33 (including newline)
```

2. **Test Credentials Directly with Twilio:**
```bash
# Test with curl (replace with your credentials)
curl -X GET "https://api.twilio.com/2010-04-01/Accounts/ACxxxxx/Messages.json" \
  -u "ACxxxxx:your_auth_token"

# Should return JSON, not 401 error
```

3. **Check Credential Sources:**
```bash
# Development - User Secrets
cd src/MmsRelay
dotnet user-secrets list | grep twilio

# Production - Environment Variables
env | grep TWILIO

# Configuration files (should NOT contain credentials)
grep -r "AC[0-9a-f]" . --exclude-dir=bin --exclude-dir=obj
```

**Common Fixes:**

```bash
# Reset User Secrets
cd src/MmsRelay
dotnet user-secrets clear
dotnet user-secrets set "twilio:accountSid" "AC_your_real_sid"
dotnet user-secrets set "twilio:authToken" "your_real_token"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"

# Verify Twilio Console
# 1. Go to https://console.twilio.com
# 2. Check Account SID and Auth Token match exactly
# 3. Ensure phone number is verified and SMS-enabled
```

---

### 📞 Message Delivery Issues

#### **Issue**: Messages not being delivered to recipients

**Important**: MmsRelay only handles sending to Twilio. Delivery issues are usually on Twilio's side.

**Diagnosis Steps:**

1. **Check MmsRelay Response:**
```bash
# Successful MmsRelay response looks like:
{
  "provider": "twilio",
  "providerMessageId": "SM1234567890abcdef",
  "status": "queued",
  "providerMessageUri": "https://api.twilio.com/..."
}
```

2. **Check Twilio Console:**
- Go to https://console.twilio.com/us1/monitor/logs/sms
- Find your message by the `providerMessageId`
- Check delivery status and error codes

3. **Common Delivery Issues:**

| Twilio Status | Meaning | Fix |
|---------------|---------|-----|
| `queued` | Waiting to send | Normal - wait a few minutes |
| `sent` | Delivered to carrier | Normal - message sent successfully |
| `delivered` | Confirmed delivery | Best case - recipient got it |
| `undelivered` | Failed to deliver | Check phone number, carrier issues |
| `failed` | Sending failed | Check error code in Twilio Console |

**Common Delivery Problems:**

```bash
# Test with your own phone first
dotnet run -- send --to "+1YOUR_ACTUAL_PHONE" --body "Test from MmsRelay"

# Common issues:
# 1. Invalid phone number
# 2. Recipient's carrier blocking messages  
# 3. Phone turned off or out of service
# 4. International messages require account approval
# 5. Landline numbers can't receive SMS/MMS
```

---

### 🚀 Performance Issues

#### **Issue**: Slow response times or timeouts

**Symptoms:**
- Requests taking longer than 30 seconds
- Timeout errors
- High memory or CPU usage

**Diagnosis:**

1. **Check Response Times:**
```bash
# Time a health check
time curl http://localhost:5000/health/live

# Time a message send
time curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{"to": "+15551234567", "body": "Performance test"}'
```

2. **Monitor Resources:**
```bash
# Windows
tasklist /fi "imagename eq MmsRelay.exe" /fo table

# Linux
top -p $(pgrep MmsRelay)
htop -p $(pgrep MmsRelay)
```

3. **Check Network Latency to Twilio:**
```bash
# Ping Twilio API
ping api.twilio.com

# Test HTTPS connection time
curl -w "@curl-format.txt" -o /dev/null -s https://api.twilio.com

# curl-format.txt contents:
#     time_namelookup:  %{time_namelookup}\n
#        time_connect:  %{time_connect}\n
#     time_appconnect:  %{time_appconnect}\n
#    time_pretransfer:  %{time_pretransfer}\n
#       time_redirect:  %{time_redirect}\n
#  time_starttransfer:  %{time_starttransfer}\n
#                     ----------\n
#          time_total:  %{time_total}\n
```

**Performance Fixes:**

1. **Increase Timeouts:**
```json
// appsettings.Production.json
{
  "twilio": {
    "requestTimeoutSeconds": 60,  // Increased from 30
    "retry": {
      "maxRetries": 5,
      "baseDelayMs": 1000
    }
  }
}
```

2. **Monitor Circuit Breaker:**
```bash
# Check for circuit breaker logs
grep -i "circuit" /var/log/mmsrelay.log
```

3. **Rate Limiting Issues:**
```bash
# If you're hitting Twilio rate limits, slow down requests
# Default for new Twilio accounts: 1 message per second

# Add delays between messages
for phone in "+15551234567" "+15551234568"; do
    dotnet run -- send --to "$phone" --body "Message"
    sleep 2  # 2-second delay
done
```

---

### 🔍 Debugging & Logging

#### **Issue**: Need more detailed information about what's happening

**Enable Debug Logging:**

1. **Console Client Verbose Mode:**
```bash
dotnet run -- send --to "+15551234567" --body "Debug test" --verbose
```

2. **Service Debug Logging:**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "MmsRelay": "Debug",
      "System.Net.Http.HttpClient": "Information"
    }
  }
}
```

3. **Production Logging:**
```bash
# Environment variable
export LOGGING__LOGLEVEL__DEFAULT=Information
export LOGGING__LOGLEVEL__MMSRELAY=Debug

# Restart service to pick up new log level
```

**Log Locations:**

```bash
# Console output (development)
dotnet run

# Windows Service logs
# Event Viewer > Windows Logs > Application

# Linux systemd logs  
journalctl -u mmsrelay -f --since "1 hour ago"

# Docker logs
docker logs mmsrelay-container --since=1h -f
```

**Useful Log Searches:**

```bash
# Find errors
grep -i error /var/log/mmsrelay.log

# Find Twilio API calls
grep -i "twilio" /var/log/mmsrelay.log  

# Find specific phone number
grep "+15551234567" /var/log/mmsrelay.log

# Find circuit breaker events
grep -i "circuit\|breaker" /var/log/mmsrelay.log

# Find retry attempts
grep -i retry /var/log/mmsrelay.log
```

---

### 🐳 Docker Issues

#### **Issue**: Docker container problems

**Common Docker Issues:**

1. **Container Won't Start:**
```bash
# Check container logs
docker logs mmsrelay-container

# Check if image exists
docker images | grep mmsrelay

# Rebuild if needed
docker build -t mmsrelay .
```

2. **Environment Variables Not Working:**
```bash
# Check what environment variables the container sees
docker exec mmsrelay-container env | grep TWILIO

# Pass environment variables correctly
docker run -e TWILIO__ACCOUNTSID=ACxxxxx -e TWILIO__AUTHTOKEN=token mmsrelay
```

3. **Port Mapping Issues:**
```bash
# Check port mapping
docker ps | grep mmsrelay

# Correct port mapping
docker run -p 5000:8080 mmsrelay  # External:Internal
```

4. **Network Issues:**
```bash
# Test from inside container
docker exec -it mmsrelay-container curl http://localhost:8080/health/live

# Test network connectivity to Twilio
docker exec -it mmsrelay-container ping api.twilio.com
```

---

## Advanced Diagnostics

### Memory Leaks

```bash
# Monitor memory over time
# Linux
while true; do
    ps -o pid,vsz,rss,comm -p $(pgrep MmsRelay)
    sleep 60
done

# Windows PowerShell
while ($true) {
    Get-Process MmsRelay | Select-Object Id,WorkingSet,VirtualMemorySize
    Start-Sleep 60
}
```

### CPU Usage

```bash
# Linux - CPU percentage
top -p $(pgrep MmsRelay)

# Windows - CPU percentage  
Get-Counter "\Process(MmsRelay)\% Processor Time"
```

### Network Tracing

```bash
# Trace HTTP calls (Linux)
strace -e trace=network -p $(pgrep MmsRelay)

# Windows network tracing
netsh trace start capture=yes provider=Microsoft-Windows-HttpService

# Stop tracing
netsh trace stop
```

---

## Getting Help

If you've tried all the above steps and still have issues:

### Information to Gather

Before asking for help, collect this information:

```bash
# System information
uname -a                    # Linux/Mac
systeminfo                 # Windows

# .NET version
dotnet --version
dotnet --list-runtimes

# Service version  
./MmsRelay --version
# or
dotnet run -- --version

# Configuration (REMOVE SENSITIVE DATA)
dotnet user-secrets list    # Development
env | grep -v TOKEN         # Production (remove sensitive values)

# Recent logs (REMOVE PHONE NUMBERS AND TOKENS)
tail -100 /var/log/mmsrelay.log | sed 's/+1[0-9]\{10\}/+1XXXXXXXXXX/g'
```

### Where to Get Help

1. **Check the FAQ**: [FAQ.md](FAQ.md)
2. **Review Examples**: [EXAMPLES.md](EXAMPLES.md)  
3. **GitHub Issues**: Create an issue with:
   - What you were trying to do
   - What command you ran
   - What error you got
   - System information from above
4. **Community Discussions**: Join GitHub Discussions for questions

### What NOT to Include in Help Requests

- ❌ Real phone numbers
- ❌ Twilio Account SID or Auth Token
- ❌ Customer data or personal information
- ❌ Production API keys or secrets

### Sample Help Request Template

```
**What I'm trying to do:**
Send MMS notifications when orders are placed

**Environment:**
- OS: Ubuntu 22.04
- .NET: 8.0.1
- MmsRelay version: 1.0.0
- Deployment: Docker container

**What I ran:**
dotnet run -- send --to "+1XXXXXXXXXX" --body "Test message"

**What happened:**
HTTP 401 Unauthorized error

**Logs:**
[2024-01-15 12:00:00] ERROR: Authentication failed with Twilio API
[2024-01-15 12:00:00] DEBUG: Account SID: ACxxxxx (masked)

**What I've tried:**
- Verified credentials in Twilio Console
- Checked environment variables
- Tested direct Twilio API call (works)
```

---

This troubleshooting guide should help you resolve most common issues. Remember that many "MmsRelay problems" are actually Twilio configuration or network issues, so always check the Twilio Console first!