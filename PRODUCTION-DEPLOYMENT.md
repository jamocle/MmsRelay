# 🏭 MmsRelay Production Deployment Guide

This comprehensive guide covers all deployment methods for the MmsRelay service in production environments. The **xcopy deployment method is recommended** for most scenarios due to its simplicity and reliability.

## Table of Contents

1. [Method 1: xcopy Deployment (Recommended)](#method-1-xcopy-deployment-recommended)
2. [Method 2: Docker Deployment](#method-2-docker-deployment)
3. [Method 3: Systemd Service (Linux)](#method-3-systemd-service-linux)
4. [Method 4: IIS Deployment (Windows)](#method-4-iis-deployment-windows)
5. [Load Balancer Configuration](#load-balancer-configuration)
6. [Monitoring and Observability](#monitoring-and-observability)
7. [Production Checklist](#production-checklist)
8. [Troubleshooting](#troubleshooting-production-issues)

---

## Method 1: xcopy Deployment (Recommended)

### Why xcopy Deployment?

**Benefits:**
- **Simple**: No Docker or complex setup required
- **Fast**: Quick to deploy and update
- **Familiar**: Standard Windows file operations
- **Portable**: Self-contained executable runs anywhere
- **Flexible**: Easy to customize for different environments
- **Scriptable**: Automated with batch files
- **Version Control**: Easy to track deployment packages
- **Low Overhead**: Minimal resource requirements
- **Reliable**: Proven deployment method with fewer moving parts

### Prerequisites

- Windows machine (Windows Server 2019+ recommended)
- .NET 8 Runtime (if using framework-dependent build) or use self-contained
- Twilio account credentials
- Network access to Twilio API (port 443 outbound)

### Step 1: Publish Self-Contained Application

```bash
# On development machine - Self-contained (includes .NET runtime)
dotnet publish src/MmsRelay/MmsRelay.csproj \
  -c Release \
  -o ./publish-selfcontained \
  --self-contained true \
  --runtime win-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

# OR Framework-dependent (requires .NET 8 installed on target)
dotnet publish src/MmsRelay/MmsRelay.csproj \
  -c Release \
  -o ./publish-framework \
  --self-contained false \
  --runtime win-x64
```

### Step 2: Prepare Deployment Package

```bash
# Create deployment folder structure
mkdir deployment
cd deployment
mkdir app
mkdir config
mkdir logs
mkdir scripts

# Copy published files
xcopy ..\publish-selfcontained\* app\ /E /Y
```

### Step 3: Create Production Configuration Files

**config/appsettings.Production.json:**
```json
{
  "urls": "http://localhost:8080",
  "logging": {
    "levelSwitch": "Warning",
    "logLevel": {
      "MmsRelay": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
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

**config/secrets.env (Create on target machine):**
```bash
# DO NOT include in source control
# Create this file on the target machine only
set ASPNETCORE_ENVIRONMENT=Production
set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
set TWILIO__AUTHTOKEN=your_actual_auth_token_here
set TWILIO__FROMPHONENUMBER=+15551234567
set URLS=http://localhost:8080
```

### Step 4: Create Deployment Scripts

**scripts/install.bat:**
```batch
@echo off
echo Installing MmsRelay Service...

REM Create application directory
if not exist "C:\MmsRelay" mkdir "C:\MmsRelay"
if not exist "C:\MmsRelay\logs" mkdir "C:\MmsRelay\logs"

REM Copy application files
xcopy "app\*" "C:\MmsRelay\" /E /Y /I

REM Copy configuration
copy "config\appsettings.Production.json" "C:\MmsRelay\appsettings.Production.json"

REM Set permissions (optional - for security)
REM icacls "C:\MmsRelay" /grant "IIS_IUSRS:(OI)(CI)F" /T

echo Installation complete!
echo.
echo Next steps:
echo 1. Create C:\MmsRelay\secrets.env with your Twilio credentials
echo 2. Run start.bat to start the service
echo 3. Test with: curl http://localhost:8080/health/live
pause
```

**scripts/start.bat:**
```batch
@echo off
echo Starting MmsRelay...

REM Load environment variables
if exist "C:\MmsRelay\secrets.env" (
    call "C:\MmsRelay\secrets.env"
) else (
    echo ERROR: secrets.env not found in C:\MmsRelay\
    echo Please create this file with your Twilio configuration
    pause
    exit /b 1
)

REM Change to application directory
cd /d "C:\MmsRelay"

REM Start the application
echo MmsRelay starting on %URLS%
echo Press Ctrl+C to stop...
MmsRelay.exe
```

**scripts/start-background.bat:**
```batch
@echo off
echo Starting MmsRelay in background...

REM Load environment variables
if exist "C:\MmsRelay\secrets.env" (
    call "C:\MmsRelay\secrets.env"
) else (
    echo ERROR: secrets.env not found in C:\MmsRelay\
    exit /b 1
)

REM Start as background process
cd /d "C:\MmsRelay"
start "MmsRelay" MmsRelay.exe

echo MmsRelay started in background
echo Check task manager for "MmsRelay" process
echo Logs will be in the console window that opened
```

**scripts/stop.bat:**
```batch
@echo off
echo Stopping MmsRelay...
taskkill /IM "MmsRelay.exe" /F 2>nul
if %errorlevel%==0 (
    echo MmsRelay stopped successfully
) else (
    echo MmsRelay was not running
)
pause
```

**scripts/install-service.bat (Optional - Windows Service):**
```batch
@echo off
echo Installing MmsRelay as Windows Service...

REM Requires NSSM (Non-Sucking Service Manager)
REM Download from: https://nssm.cc/download

if not exist "nssm.exe" (
    echo ERROR: nssm.exe not found
    echo Download from https://nssm.cc/download
    pause
    exit /b 1
)

REM Load environment variables
if exist "C:\MmsRelay\secrets.env" (
    call "C:\MmsRelay\secrets.env"
) else (
    echo ERROR: secrets.env not found
    pause
    exit /b 1
)

REM Install service
nssm install MmsRelay "C:\MmsRelay\MmsRelay.exe"
nssm set MmsRelay AppDirectory "C:\MmsRelay"
nssm set MmsRelay Description "MMS Relay Service"
nssm set MmsRelay Start SERVICE_AUTO_START

REM Set environment variables for service
nssm set MmsRelay AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
nssm set MmsRelay AppEnvironmentExtra TWILIO__ACCOUNTSID=%TWILIO__ACCOUNTSID%
nssm set MmsRelay AppEnvironmentExtra TWILIO__AUTHTOKEN=%TWILIO__AUTHTOKEN%
nssm set MmsRelay AppEnvironmentExtra TWILIO__FROMPHONENUMBER=%TWILIO__FROMPHONENUMBER%
nssm set MmsRelay AppEnvironmentExtra URLS=%URLS%

REM Configure logging
nssm set MmsRelay AppStdout "C:\MmsRelay\logs\stdout.log"
nssm set MmsRelay AppStderr "C:\MmsRelay\logs\stderr.log"

echo Service installed successfully!
echo Use 'net start MmsRelay' to start the service
echo Use 'net stop MmsRelay' to stop the service
echo Use 'nssm remove MmsRelay confirm' to uninstall
pause
```

### Step 5: Create README for Target Machine

**DEPLOYMENT-README.md:**
```markdown
# MmsRelay Deployment Instructions

## Prerequisites
- Windows machine
- .NET 8 Runtime (if using framework-dependent build)
- Twilio account credentials

## Installation Steps

1. **Copy deployment folder to target machine**
2. **Run as Administrator: scripts\install.bat**
3. **Create secrets file: C:\MmsRelay\secrets.env**
   ```
   set ASPNETCORE_ENVIRONMENT=Production
   set TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   set TWILIO__AUTHTOKEN=your_actual_auth_token_here
   set TWILIO__FROMPHONENUMBER=+15551234567
   set URLS=http://localhost:8080
   ```
4. **Test installation: scripts\start.bat**
5. **Verify: Open browser to http://localhost:8080/health/live**

## Running the Service

### Option 1: Manual Start/Stop
- Start: `scripts\start.bat`
- Stop: Press Ctrl+C or run `scripts\stop.bat`

### Option 2: Background Process
- Start: `scripts\start-background.bat`
- Stop: `scripts\stop.bat`

### Option 3: Windows Service (Recommended for Production)
- Install NSSM: Download from https://nssm.cc/download
- Run: `scripts\install-service.bat`
- Manage: `net start MmsRelay` / `net stop MmsRelay`

## Testing

Send test MMS:
```bash
curl -X POST http://localhost:8080/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "+15551234567",
    "body": "Test message from MmsRelay",
    "mediaUrls": ["https://example.com/image.jpg"]
  }'
```

## Troubleshooting

- **Check logs**: Look in C:\MmsRelay\logs\ or console output
- **Verify config**: Ensure secrets.env has correct Twilio credentials
- **Test connectivity**: curl http://localhost:8080/health/live
- **Port conflicts**: Change URLS in secrets.env if port 8080 is busy
```

### Step 6: Complete xcopy Deployment Process

**On Development Machine:**
```batch
REM 1. Build and publish
dotnet publish src/MmsRelay/MmsRelay.csproj -c Release -o ./publish-selfcontained --self-contained true --runtime win-x64 -p:PublishSingleFile=true

REM 2. Create deployment package
mkdir mmsrelay-deployment
cd mmsrelay-deployment
mkdir app config logs scripts

REM 3. Copy files
xcopy ..\publish-selfcontained\* app\ /E /Y
copy ..\config\* config\
copy ..\scripts\* scripts\
copy ..\DEPLOYMENT-README.md .

REM 4. Create deployment archive
tar -czf mmsrelay-deployment.tar.gz *
REM OR
7z a mmsrelay-deployment.zip *
```

**On Target Machine:**
```batch
REM 1. Extract deployment package
tar -xzf mmsrelay-deployment.tar.gz
REM OR
7z x mmsrelay-deployment.zip

REM 2. Run installation
cd mmsrelay-deployment
scripts\install.bat

REM 3. Configure secrets
notepad C:\MmsRelay\secrets.env

REM 4. Start service
scripts\start.bat
```

### xcopy Deployment Checklist

- [ ] Publish self-contained or verify .NET 8 on target
- [ ] Create deployment scripts and configuration
- [ ] Test deployment package locally
- [ ] Document target machine requirements
- [ ] Create secrets template (without actual values)
- [ ] Include troubleshooting guide
- [ ] Test rollback procedure

---

## Method 2: Docker Deployment

### When to Use Docker

- Container orchestration (Kubernetes, Docker Swarm)
- Consistent deployment across environments
- Easy scaling and load balancing
- Isolated runtime environment

### Step 1: Create Dockerfile

```dockerfile
# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MmsRelay/MmsRelay.csproj", "src/MmsRelay/"]
RUN dotnet restore "src/MmsRelay/MmsRelay.csproj"
COPY . .
WORKDIR "/src/src/MmsRelay"
RUN dotnet build "MmsRelay.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MmsRelay.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN addgroup --system --gid 1001 appgroup
RUN adduser --system --uid 1001 --ingroup appgroup appuser
USER appuser

ENTRYPOINT ["dotnet", "MmsRelay.dll"]
```

### Step 2: Create .dockerignore

```
**/bin
**/obj
**/.git
**/.vs
**/.vscode
**/node_modules
Dockerfile*
.dockerignore
README.md
knowledge.md
DEVELOPER-SETUP.md
PRODUCTION-DEPLOYMENT.md
```

### Step 3: Build and Deploy

```bash
# Build the Docker image
docker build -t mmsrelay:latest .

# Run locally for testing
docker run -d \
  --name mmsrelay \
  -p 8080:8080 \
  -e TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx \
  -e TWILIO__AUTHTOKEN=your_auth_token \
  -e TWILIO__FROMPHONENUMBER=+15551234567 \
  -e LOGGING__LEVELSWITCH=Information \
  mmsrelay:latest

# Deploy to production server
docker save mmsrelay:latest | gzip > mmsrelay-latest.tar.gz
scp mmsrelay-latest.tar.gz user@production-server:/tmp/
ssh user@production-server "cd /tmp && docker load < mmsrelay-latest.tar.gz"
```

### Step 4: Docker Compose for Production

```yaml
# docker-compose.production.yml
version: '3.8'

services:
  mmsrelay:
    image: mmsrelay:latest
    container_name: mmsrelay-prod
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TWILIO__ACCOUNTSID=${TWILIO_ACCOUNT_SID}
      - TWILIO__AUTHTOKEN=${TWILIO_AUTH_TOKEN}
      - TWILIO__FROMPHONENUMBER=${TWILIO_FROM_PHONE}
      - LOGGING__LEVELSWITCH=Warning
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  # Optional: Reverse proxy
  nginx:
    image: nginx:alpine
    container_name: mmsrelay-proxy
    restart: unless-stopped
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - mmsrelay
```

---

## Method 3: Systemd Service (Linux)

### When to Use Systemd

- Linux production servers
- Automatic service restart and management
- Native logging integration
- Resource limiting and security controls

### Step 1: Publish the Application

```bash
# On development machine
dotnet publish src/MmsRelay/MmsRelay.csproj \
  -c Release \
  -o ./publish \
  --self-contained false \
  --runtime linux-x64

# Transfer to production server
rsync -av ./publish/ user@production-server:/opt/mmsrelay/
```

### Step 2: Create Environment File

```bash
# /opt/mmsrelay/.env
ASPNETCORE_ENVIRONMENT=Production
TWILIO__ACCOUNTSID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
TWILIO__AUTHTOKEN=your_auth_token_here
TWILIO__FROMPHONENUMBER=+15551234567
LOGGING__LEVELSWITCH=Warning
URLS=http://localhost:8080
```

### Step 3: Create Systemd Service

```ini
# /etc/systemd/system/mmsrelay.service
[Unit]
Description=MMS Relay Service
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/mmsrelay/MmsRelay.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mmsrelay
User=mmsrelay
Group=mmsrelay
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/opt/mmsrelay/.env
WorkingDirectory=/opt/mmsrelay

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/mmsrelay
CapabilityBoundingSet=CAP_NET_BIND_SERVICE
AmbientCapabilities=CAP_NET_BIND_SERVICE

[Install]
WantedBy=multi-user.target
```

### Step 4: Setup and Start Service

```bash
# Create user
sudo useradd --system --shell /bin/false mmsrelay
sudo chown -R mmsrelay:mmsrelay /opt/mmsrelay
sudo chmod 600 /opt/mmsrelay/.env

# Install and start service
sudo systemctl daemon-reload
sudo systemctl enable mmsrelay
sudo systemctl start mmsrelay

# Check status
sudo systemctl status mmsrelay
sudo journalctl -u mmsrelay -f
```

---

## Method 4: IIS Deployment (Windows)

### When to Use IIS

- Windows Server environments
- Integration with existing IIS infrastructure
- Windows Authentication requirements
- Shared hosting scenarios

### Step 1: Publish Application

```bash
dotnet publish src/MmsRelay/MmsRelay.csproj \
  -c Release \
  -o ./publish \
  --runtime win-x64 \
  --self-contained false
```

### Step 2: Configure IIS

- Install ASP.NET Core Hosting Bundle
- Create Application Pool with .NET CLR Version = "No Managed Code"
- Set Process Model > Identity to ApplicationPoolIdentity
- Create IIS Application pointing to publish folder

### Step 3: Configure Environment Variables

Set in IIS Application Settings or web.config:

```xml
<configuration>
  <system.webServer>
    <aspNetCore processPath="dotnet" 
                arguments=".\MmsRelay.dll" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="TWILIO__ACCOUNTSID" value="ACxxxxxxxx" />
        <environmentVariable name="TWILIO__AUTHTOKEN" value="your_token" />
        <environmentVariable name="TWILIO__FROMPHONENUMBER" value="+15551234567" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

---

## Load Balancer Configuration

### NGINX Reverse Proxy

**nginx.conf:**
```nginx
upstream mmsrelay_backend {
    server 127.0.0.1:8080;
    # Add more instances for load balancing
    # server 127.0.0.1:8081;
    # server 127.0.0.1:8082;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload";

    location / {
        proxy_pass http://mmsrelay_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://mmsrelay_backend/health/live;
        access_log off;
    }
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}
```

---

## Monitoring and Observability

### Health Checks

**Add to ServiceCollectionExtensions.cs:**
```csharp
public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
{
    services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy())
        .AddCheck<TwilioHealthCheck>("twilio")
        .AddCheck("memory", () => 
        {
            var allocatedBytes = GC.GetTotalMemory(false);
            var threshold = 1024 * 1024 * 500; // 500MB
            return allocatedBytes < threshold 
                ? HealthCheckResult.Healthy($"Memory usage: {allocatedBytes / 1024 / 1024}MB")
                : HealthCheckResult.Degraded($"High memory usage: {allocatedBytes / 1024 / 1024}MB");
        });

    return services;
}
```

**Create TwilioHealthCheck.cs:**
```csharp
public class TwilioHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _options;

    public TwilioHealthCheck(HttpClient httpClient, IOptions<TwilioOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Twilio API is reachable")
                : HealthCheckResult.Degraded($"Twilio API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Twilio API is unreachable", ex);
        }
    }
}
```

### Logging in Production

**Structured Logging Configuration:**
```csharp
// In Program.cs for production
if (builder.Environment.IsProduction())
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Warning()
        .MinimumLevel.Override("MmsRelay", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "MmsRelay")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File(
            path: "/var/log/mmsrelay/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            formatter: new JsonFormatter())
        .CreateLogger();
}
```

---

## Production Checklist

### Pre-Deployment
- [ ] Remove sensitive data from appsettings.json
- [ ] Configure environment variables for secrets
- [ ] Test with production-like Twilio configuration
- [ ] Run security scan on Docker image (if using Docker)
- [ ] Performance test with expected load
- [ ] Validate SSL certificate configuration

### Deployment
- [ ] Deploy to staging environment first
- [ ] Run smoke tests on staging
- [ ] Deploy to production during maintenance window
- [ ] Verify health checks pass
- [ ] Test MMS sending functionality
- [ ] Monitor logs for errors

### Post-Deployment
- [ ] Set up monitoring alerts
- [ ] Configure log aggregation
- [ ] Document rollback procedures
- [ ] Train operations team on troubleshooting
- [ ] Schedule regular security updates

---

## Troubleshooting Production Issues

### Common Issues and Solutions

#### 1. Twilio Authentication Errors
```bash
# Check environment variables
docker exec mmsrelay-prod env | grep TWILIO  # Docker
# OR
systemctl show mmsrelay --property=Environment  # Systemd

# Verify credentials with Twilio API
curl -X GET "https://api.twilio.com/2010-04-01/Accounts/{AccountSid}.json" \
  -u "{AccountSid}:{AuthToken}"
```

#### 2. High Memory Usage
```bash
# Monitor container resources (Docker)
docker stats mmsrelay-prod

# Check systemd service memory (Linux)
systemctl status mmsrelay

# Check for memory leaks in logs
journalctl -u mmsrelay | grep -i "memory\|gc\|heap"
```

#### 3. Circuit Breaker Open
```bash
# Check Twilio service status
curl -s https://status.twilio.com/api/v2/status.json

# Review circuit breaker configuration
# Temporarily disable by setting failureRatio to 1.0
```

#### 4. Rate Limiting Issues
```bash
# Check Twilio account limits
curl -X GET "https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Usage/Records.json" \
  -u "{AccountSid}:{AuthToken}"
```

#### 5. Port Binding Issues
```bash
# Check what's using the port
netstat -tulpn | grep 8080  # Linux
netstat -ano | findstr :8080  # Windows

# Change port in configuration if needed
```

#### 6. SSL/TLS Issues
```bash
# Test SSL connectivity
openssl s_client -connect api.twilio.com:443

# Check certificate trust store
curl -v https://api.twilio.com/
```

---

## Deployment Comparison

| Feature | xcopy | Docker | Systemd | IIS |
|---------|-------|--------|---------|-----|
| **Complexity** | ⭐ Low | ⭐⭐⭐ Medium | ⭐⭐ Medium | ⭐⭐ Medium |
| **Setup Time** | ⭐⭐⭐ Fast | ⭐⭐ Medium | ⭐⭐ Medium | ⭐⭐ Medium |
| **Resource Usage** | ⭐⭐⭐ Low | ⭐⭐ Medium | ⭐⭐⭐ Low | ⭐⭐ Medium |
| **Scalability** | ⭐ Limited | ⭐⭐⭐ High | ⭐⭐ Medium | ⭐⭐ Medium |
| **Isolation** | ⭐ None | ⭐⭐⭐ High | ⭐⭐ Medium | ⭐⭐ Medium |
| **Rollback** | ⭐⭐⭐ Easy | ⭐⭐ Medium | ⭐⭐ Medium | ⭐⭐ Medium |
| **Monitoring** | ⭐ Basic | ⭐⭐⭐ Advanced | ⭐⭐⭐ Advanced | ⭐⭐ Medium |
| **Security** | ⭐⭐ Basic | ⭐⭐⭐ High | ⭐⭐⭐ High | ⭐⭐ Medium |

**Recommendation**: Start with **xcopy deployment** for simplicity and reliability. Consider Docker or systemd for more complex production environments requiring advanced monitoring, scaling, or security features.