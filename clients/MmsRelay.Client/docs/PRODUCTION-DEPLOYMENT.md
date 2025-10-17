# MmsRelay Console Client - Production Deployment Guide

## Overview

This guide covers deploying the MmsRelay Console Client in production environments. The client is designed as a CLI tool that can be deployed as a standalone executable, Docker container, or integrated into CI/CD pipelines and automation workflows.

## Deployment Options

### 1. Self-Contained Executable

Create a single-file executable with all dependencies included:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# ARM64 (Linux/macOS)
dotnet publish -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

### 2. Framework-Dependent Deployment

Smaller deployment requiring .NET 8 runtime on target system:

```bash
# Cross-platform
dotnet publish -c Release --no-self-contained -p:PublishSingleFile=true

# Specific runtime
dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true
```

### 3. Docker Container

#### Multi-stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["clients/MmsRelay.Client/MmsRelay.Client.csproj", "clients/MmsRelay.Client/"]
COPY ["src/MmsRelay/MmsRelay.csproj", "src/MmsRelay/"]

# Restore dependencies
RUN dotnet restore "clients/MmsRelay.Client/MmsRelay.Client.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/clients/MmsRelay.Client"
RUN dotnet build "MmsRelay.Client.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MmsRelay.Client.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app
USER appuser

# Set entry point
ENTRYPOINT ["dotnet", "MmsRelay.Client.dll"]
```

#### Build and Run Docker Image

```bash
# Build image
docker build -t mmsrelay-client:latest -f clients/MmsRelay.Client/Dockerfile .

# Run container
docker run --rm mmsrelay-client:latest --help

# Run with environment variables
docker run --rm \
  -e MmsRelay__BaseUrl="https://api.mmsrelay.com" \
  -e MmsRelay__ApiKey="prod-api-key" \
  mmsrelay-client:latest send --to +15551234567 --body "Hello from Docker!"
```

## Configuration Management

### 1. Environment Variables

Production configuration should use environment variables:

```bash
# Required configuration
export MMSRELAY__BASEURL="https://api.mmsrelay.com"
export MMSRELAY__APIKEY="your-production-api-key"

# Optional configuration
export MMSRELAY__TIMEOUTSECONDS="30"
export MMSRELAY__RETRYCOUNT="3"
export MMSRELAY__CIRCUITBREAKERTHRESHOLD="5"
export MMSRELAY__CIRCUITBREAKERDURATIONSECONDS="60"

# Logging configuration
export SERILOG__MINIMUMLEVEL__DEFAULT="Information"
export SERILOG__MINIMUMLEVEL__OVERRIDE__MICROSOFT="Warning"
```

### 2. Configuration File

Create `appsettings.Production.json` for production settings:

```json
{
  "MmsRelay": {
    "BaseUrl": "https://api.mmsrelay.com",
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
        "System": "Warning",
        "MmsRelay.Client": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/mmsrelay-client/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ]
  }
}
```

### 3. Azure Key Vault Integration

For Azure environments, integrate with Key Vault:

```csharp
// Program.cs additions for Azure Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}
```

## Security Configuration

### 1. API Key Management

Never hardcode API keys. Use secure configuration:

```bash
# Azure Key Vault
az keyvault secret set --vault-name "mmsrelay-vault" --name "ApiKey" --value "your-secure-api-key"

# Kubernetes Secret
kubectl create secret generic mmsrelay-config \
  --from-literal=ApiKey="your-secure-api-key" \
  --from-literal=BaseUrl="https://api.mmsrelay.com"

# AWS Systems Manager Parameter Store
aws ssm put-parameter --name "/mmsrelay/ApiKey" --value "your-secure-api-key" --type "SecureString"
```

### 2. TLS/SSL Configuration

Ensure secure communication:

```json
{
  "MmsRelay": {
    "BaseUrl": "https://api.mmsrelay.com",
    "HttpClientSettings": {
      "UseCertificateValidation": true,
      "MinTlsVersion": "1.2",
      "CertificatePinning": {
        "Enabled": true,
        "Thumbprints": ["SHA256:fingerprint1", "SHA256:fingerprint2"]
      }
    }
  }
}
```

### 3. Network Security

Configure network-level security:

```bash
# Firewall rules (iptables)
iptables -A OUTPUT -p tcp --dport 443 -d api.mmsrelay.com -j ACCEPT
iptables -A OUTPUT -p tcp --dport 80,443 -j DROP

# Network policies (Kubernetes)
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: mmsrelay-client-netpol
spec:
  podSelector:
    matchLabels:
      app: mmsrelay-client
  policyTypes:
  - Egress
  egress:
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 443
```

## Kubernetes Deployment

### 1. Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mmsrelay-client
  labels:
    app: mmsrelay-client
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mmsrelay-client
  template:
    metadata:
      labels:
        app: mmsrelay-client
    spec:
      containers:
      - name: mmsrelay-client
        image: mmsrelay-client:latest
        args: ["health"]
        env:
        - name: DOTNET_ENVIRONMENT
          value: "Production"
        - name: MMSRELAY__BASEURL
          valueFrom:
            configMapKeyRef:
              name: mmsrelay-config
              key: BaseUrl
        - name: MMSRELAY__APIKEY
          valueFrom:
            secretKeyRef:
              name: mmsrelay-secrets
              key: ApiKey
        resources:
          requests:
            memory: "64Mi"
            cpu: "50m"
          limits:
            memory: "128Mi"
            cpu: "100m"
        livenessProbe:
          exec:
            command:
            - dotnet
            - MmsRelay.Client.dll
            - health
          initialDelaySeconds: 30
          periodSeconds: 60
        readinessProbe:
          exec:
            command:
            - dotnet
            - MmsRelay.Client.dll
            - health
          initialDelaySeconds: 5
          periodSeconds: 30
      restartPolicy: Always
```

### 2. ConfigMap and Secret

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: mmsrelay-config
data:
  BaseUrl: "https://api.mmsrelay.com"
  TimeoutSeconds: "30"
  RetryCount: "3"
---
apiVersion: v1
kind: Secret
metadata:
  name: mmsrelay-secrets
type: Opaque
data:
  ApiKey: <base64-encoded-api-key>
```

### 3. Job for One-time Execution

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: mms-send-job
spec:
  template:
    spec:
      containers:
      - name: mmsrelay-client
        image: mmsrelay-client:latest
        args: 
        - "send"
        - "--to"
        - "+15551234567"
        - "--body"
        - "Scheduled message from Kubernetes"
        env:
        - name: MMSRELAY__BASEURL
          valueFrom:
            configMapKeyRef:
              name: mmsrelay-config
              key: BaseUrl
        - name: MMSRELAY__APIKEY
          valueFrom:
            secretKeyRef:
              name: mmsrelay-secrets
              key: ApiKey
      restartPolicy: Never
  backoffLimit: 3
```

## CI/CD Pipeline Integration

### 1. Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - clients/MmsRelay.Client/*

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  projectPath: 'clients/MmsRelay.Client'

stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '$(projectPath)/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build application'
      inputs:
        command: 'build'
        projects: '$(projectPath)/*.csproj'
        arguments: '--configuration $(buildConfiguration) --no-restore'
    
    - task: DotNetCoreCLI@2
      displayName: 'Run tests'
      inputs:
        command: 'test'
        projects: 'tests/MmsRelay.Client.Tests/*.csproj'
        arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'
    
    - task: DotNetCoreCLI@2
      displayName: 'Publish application'
      inputs:
        command: 'publish'
        projects: '$(projectPath)/*.csproj'
        arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory) --no-build'
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish artifacts'
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'mmsrelay-client'

- stage: Deploy
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployToProduction
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: Docker@2
            displayName: 'Build and push Docker image'
            inputs:
              containerRegistry: 'DockerHub'
              repository: 'your-org/mmsrelay-client'
              command: 'buildAndPush'
              Dockerfile: '$(Pipeline.Workspace)/mmsrelay-client/Dockerfile'
              tags: |
                $(Build.BuildId)
                latest
```

### 2. GitHub Actions Workflow

```yaml
# .github/workflows/client-deploy.yml
name: Deploy MmsRelay Client

on:
  push:
    branches: [main]
    paths: ['clients/MmsRelay.Client/**']

env:
  DOTNET_VERSION: '8.0'
  PROJECT_PATH: 'clients/MmsRelay.Client'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore ${{ env.PROJECT_PATH }}
    
    - name: Build application
      run: dotnet build ${{ env.PROJECT_PATH }} --configuration Release --no-restore
    
    - name: Run tests
      run: dotnet test tests/MmsRelay.Client.Tests --configuration Release --no-build
    
    - name: Publish application
      run: |
        dotnet publish ${{ env.PROJECT_PATH }} \
          --configuration Release \
          --runtime linux-x64 \
          --self-contained true \
          -p:PublishSingleFile=true \
          --output ./publish
    
    - name: Build Docker image
      run: |
        docker build -t ${{ secrets.DOCKER_REGISTRY }}/mmsrelay-client:${{ github.sha }} \
          -t ${{ secrets.DOCKER_REGISTRY }}/mmsrelay-client:latest \
          -f ${{ env.PROJECT_PATH }}/Dockerfile .
    
    - name: Push to registry
      run: |
        echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
        docker push ${{ secrets.DOCKER_REGISTRY }}/mmsrelay-client:${{ github.sha }}
        docker push ${{ secrets.DOCKER_REGISTRY }}/mmsrelay-client:latest
```

## Monitoring and Observability

### 1. Application Insights (Azure)

```csharp
// Program.cs additions for Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
});
```

### 2. Prometheus Metrics

```csharp
// Add metrics collection
builder.Services.AddSingleton<IMetricsRoot>(provider =>
{
    var metrics = new MetricsBuilder()
        .Configuration.Configure(options =>
        {
            options.GlobalTags.Add("service", "mmsrelay-client");
            options.GlobalTags.Add("environment", builder.Environment.EnvironmentName);
        })
        .OutputMetrics.AsPrometheusPlainText()
        .Build();
    return metrics;
});
```

### 3. Structured Logging for Production

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "https://seq.company.com",
          "apiKey": "your-seq-api-key"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId"],
    "Properties": {
      "Application": "MmsRelay.Client"
    }
  }
}
```

## Performance Optimization

### 1. Runtime Configuration

```xml
<!-- MmsRelay.Client.runtimeconfig.json -->
{
  "runtimeOptions": {
    "configProperties": {
      "System.GC.Server": true,
      "System.GC.Concurrent": true,
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
    }
  }
}
```

### 2. AOT (Ahead-of-Time) Compilation

```xml
<!-- MmsRelay.Client.csproj additions for AOT -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  <TrimMode>link</TrimMode>
</PropertyGroup>
```

### 3. Memory Optimization

```bash
# Set memory limits for containers
docker run --memory="64m" --memory-swap="64m" mmsrelay-client:latest

# Kubernetes resource limits
resources:
  requests:
    memory: "32Mi"
    cpu: "10m"
  limits:
    memory: "64Mi"
    cpu: "50m"
```

## Backup and Recovery

### 1. Configuration Backup

```bash
# Backup configuration
kubectl get configmap mmsrelay-config -o yaml > mmsrelay-config-backup.yaml
kubectl get secret mmsrelay-secrets -o yaml > mmsrelay-secrets-backup.yaml

# Restore configuration
kubectl apply -f mmsrelay-config-backup.yaml
kubectl apply -f mmsrelay-secrets-backup.yaml
```

### 2. Application Rollback

```bash
# Kubernetes rollback
kubectl rollout undo deployment/mmsrelay-client

# Docker rollback
docker pull mmsrelay-client:previous-tag
docker tag mmsrelay-client:previous-tag mmsrelay-client:latest
```

## Troubleshooting Production Issues

### 1. Health Checks

```bash
# Manual health check
./MmsRelay.Client health --service-url "https://api.mmsrelay.com" --verbose

# Kubernetes health check
kubectl exec deployment/mmsrelay-client -- dotnet MmsRelay.Client.dll health
```

### 2. Log Analysis

```bash
# View application logs
kubectl logs deployment/mmsrelay-client -f

# Search for errors
kubectl logs deployment/mmsrelay-client | grep -i error

# Export logs
kubectl logs deployment/mmsrelay-client --since=1h > app-logs.txt
```

### 3. Performance Monitoring

```bash
# Container resource usage
docker stats mmsrelay-client

# Kubernetes resource usage
kubectl top pods -l app=mmsrelay-client

# Memory dump (if needed)
dotnet-dump collect -p $(pgrep -f MmsRelay.Client)
```

This production deployment guide ensures secure, scalable, and maintainable deployment of the MmsRelay Console Client.