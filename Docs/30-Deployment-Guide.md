# Deployment Guide

**Document Type**: Living Document - Deployment & Operations  
**Last Updated**: January 17, 2026  
**Status**: ? Production Ready

---

## ?? Overview

Complete deployment procedures for the Bellwood AdminPortal across local development, staging, and production environments.

**Target Framework**: .NET 8.0  
**Deployment Models**: Local Development, IIS, Azure App Service  
**Build Time**: ~2-3 minutes  
**Deployment Time**: ~15 minutes (coordinated release)

**Target Audience**: Developers, DevOps engineers, system administrators  
**Prerequisites**: .NET 8.0 SDK, target deployment environment, access credentials

---

## ?? Quick Start (Local Development)

### Prerequisites

**Required Software**:
- ? [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- ? Visual Studio 2022 or VS Code
- ? PowerShell 5.1 or higher
- ? Git for Windows

**Required Services** (Must be running):
- ? AuthServer on `https://localhost:5001`
- ? AdminAPI on `https://localhost:5206`

**Verification**:
```bash
dotnet --version
# Expected: 8.0.x or higher
```

---

### Start Services (3 Terminals)

#### Terminal 1: AuthServer
```bash
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
```
? Listening on: `https://localhost:5001`

#### Terminal 2: AdminAPI
```bash
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```
? Listening on: `https://localhost:5206`

#### Terminal 3: AdminPortal
```bash
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal

# First time only: Seed test data
.\Scripts\seed-admin-api.ps1

# Start the portal
dotnet run
```
? Listening on: `https://localhost:7257`

---

### Login Credentials

| Username | Password | Role | Purpose |
|----------|----------|------|---------|
| `alice` | `password` | admin | Full access to all features |
| `bob` | `password` | admin | Full access (second test account) |

---

### Expected Flow

```
1. Navigate to https://localhost:7257
   ?
2. Auto-redirect to /login
   ?
3. Enter: alice / password
   ?
4. Click Login
   ?
5. Redirect to /main dashboard
   ?
6. See bookings, quotes, and live tracking features
```

---

### Test Data (Seeded)

After running `seed-admin-api.ps1`, you'll have:

**3 Bookings**:

| Passenger | Vehicle | Status | Pickup |
|-----------|---------|--------|--------|
| Taylor Reed | SUV | Requested | O'Hare FBO |
| Jordan Chen | Sedan | Confirmed | Langham Hotel |
| Derek James | S-Class | Completed | O'Hare Intl |

**3 Affiliates**:
- Test Affiliate 1 (2 drivers)
- Test Affiliate 2 (1 driver)
- Test Affiliate 3 (0 drivers)

---

## ??? Local Development Setup

### Step 1: Clone Repository

```bash
git clone https://github.com/BidumanADT/Bellwood.AdminPortal.git
cd Bellwood.AdminPortal
```

---

### Step 2: Restore Dependencies

```bash
dotnet restore
```

**Expected Output**:
```
Restore succeeded.
```

---

### Step 3: Configure Settings

**File**: `appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": "dev-secret-123"
  },
  "AuthServer": {
    "LoginUrl": "https://localhost:5001/api/auth/login"
  },
  "GoogleMaps": {
    "ApiKey": ""  // Optional: Add your Google Maps API key
  }
}
```

**Required Settings**:
- `AdminAPI:BaseUrl` - Must match your AdminAPI URL
- `AdminAPI:ApiKey` - Must match AdminAPI's configured key (default: `dev-secret-123`)

**Optional Settings**:
- `GoogleMaps:ApiKey` - For full map functionality (get from [Google Cloud Console](https://console.cloud.google.com/))

---

### Step 4: Build Project

```bash
dotnet build
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.34
```

---

### Step 5: Run Project

```bash
dotnet run
```

**Expected Output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7257
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5072
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

### Step 6: Verify Installation

1. **Open Browser**: Navigate to `https://localhost:7257`
2. **Should Auto-Redirect**: To `/login` page
3. **Login**: Use `alice` / `password`
4. **Should Redirect**: To `/main` dashboard
5. **Verify Features**:
   - ? Bookings list loads
   - ? Quotes list loads
   - ? Live Tracking map displays
   - ? Navigation works

---

## ?? Testing Installation

### Quick Health Check

Run the test scripts provided:

```powershell
# Test AdminAPI connectivity
.\Scripts\test-api-connection.ps1

# Expected output:
# ? AdminAPI is reachable
# ? Health check passed
# ? Authentication working
```

---

### Browser DevTools Check

**Open DevTools** (F12) ? Console Tab

**Expected Logs**:
```
[Login] Auth state updated, navigating to /main
[AuthStateProvider] User authenticated: alice
[Bookings] OnInitializedAsync running
[Bookings] Loaded 3 bookings
[Bookings] Filtered to 3 bookings with status: All
```

**Network Tab** (Filter: Fetch/XHR):

Request to `https://localhost:5206/bookings/list?take=100`:
- ? Status: `200 OK`
- ? Headers include: `X-Admin-ApiKey: dev-secret-123`
- ? Headers include: `Authorization: Bearer {token}`
- ? Response: JSON array with booking objects

---

## ?? Build for Production

### Release Build

```bash
# Clean previous builds
dotnet clean

# Build in Release configuration
dotnet build --configuration Release
```

**Output Location**: `bin/Release/net8.0/`

---

### Publish Application

#### Self-Contained Deployment (Recommended for IIS)

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true -o ./publish
```

**Advantages**:
- ? Includes .NET runtime (no server-side .NET required)
- ? Isolated deployment
- ? Specific to target platform

---

#### Framework-Dependent Deployment

```bash
dotnet publish --configuration Release -o ./publish
```

**Advantages**:
- ? Smaller package size
- ? Leverages server's .NET installation

**Requirements**:
- ? Server must have .NET 8.0 Runtime installed

---

## ?? Environment Configuration

### Environment Variables

**Windows (PowerShell)**:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "https://+:443;http://+:80"
```

**Linux/Mac (Bash)**:
```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="https://+:443;http://+:80"
```

---

### Production Settings

**File**: `appsettings.Production.json` (create if not exists)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AdminAPI": {
    "BaseUrl": "https://api.bellwood.com",
    "ApiKey": "PRODUCTION-KEY-FROM-KEYVAULT"
  },
  "AuthServer": {
    "LoginUrl": "https://auth.bellwood.com/api/auth/login"
  },
  "GoogleMaps": {
    "ApiKey": "PRODUCTION-GOOGLE-MAPS-KEY"
  }
}
```

**?? Security**:
- Never commit production API keys to source control
- Use Azure Key Vault or environment variables for secrets
- Rotate keys regularly

---

## ??? IIS Deployment

### Prerequisites

**Windows Server Requirements**:
- ? Windows Server 2019 or later
- ? IIS 10.0 or later
- ? [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer)

**Install Hosting Bundle**:
1. Download from Microsoft
2. Run installer
3. Restart IIS: `iisreset`

---

### Deployment Steps

#### Step 1: Publish Application

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true -o C:\inetpub\wwwroot\BellwoodAdminPortal
```

---

#### Step 2: Create Application Pool

**IIS Manager**:
1. Open **IIS Manager**
2. Right-click **Application Pools** ? **Add Application Pool**
3. **Name**: `BellwoodAdminPortal`
4. **.NET CLR Version**: **No Managed Code**
5. **Managed Pipeline Mode**: Integrated
6. Click **OK**

**Advanced Settings**:
- **Identity**: ApplicationPoolIdentity
- **Start Mode**: AlwaysRunning (for production)

---

#### Step 3: Create IIS Site

**IIS Manager**:
1. Right-click **Sites** ? **Add Website**
2. **Site name**: `BellwoodAdminPortal`
3. **Application pool**: Select `BellwoodAdminPortal`
4. **Physical path**: `C:\inetpub\wwwroot\BellwoodAdminPortal`
5. **Binding**:
   - Type: `https`
   - Port: `443`
   - Host name: `admin.bellwood.com`
   - SSL certificate: Select your certificate
6. Click **OK**

---

#### Step 4: Configure WebSocket Protocol

**IIS Manager** ? **Server** ? **Server Manager**:
1. **Manage** ? **Add Roles and Features**
2. Navigate to: **Web Server (IIS)** ? **Web Server** ? **Application Development**
3. Check: ? **WebSocket Protocol**
4. Click **Install**

**Or via PowerShell**:
```powershell
Install-WindowsFeature -Name Web-WebSockets
```

---

#### Step 5: Configure web.config

**File**: `web.config` (auto-generated by publish)

Verify it includes:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <webSocket enabled="true" />
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath=".\Bellwood.AdminPortal.exe" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

---

#### Step 6: Set Permissions

**File Explorer**:
1. Navigate to `C:\inetpub\wwwroot\BellwoodAdminPortal`
2. Right-click ? **Properties** ? **Security**
3. Click **Edit** ? **Add**
4. Enter: `IIS AppPool\BellwoodAdminPortal`
5. Permissions: ? Read & Execute, ? Read

---

#### Step 7: Configure HTTPS

**Requirements**:
- Valid SSL certificate for `admin.bellwood.com`
- Certificate installed in Windows Certificate Store

**IIS Manager** ? **Site** ? **Bindings**:
- Type: `https`
- Port: `443`
- Host name: `admin.bellwood.com`
- SSL certificate: Select certificate

---

#### Step 8: Start Site

**IIS Manager**:
1. Select **BellwoodAdminPortal** site
2. Right panel: **Manage Website** ? **Start**

**Verify**:
```powershell
# Test health endpoint
Invoke-WebRequest -Uri "https://admin.bellwood.com/health" -UseBasicParsing
```

---

## ?? Azure App Service Deployment

### Create App Service

**Azure Portal**:
1. Create **App Service**
2. **Resource Group**: `Bellwood-Production`
3. **Name**: `bellwood-admin-portal`
4. **Publish**: Code
5. **Runtime stack**: .NET 8
6. **Operating System**: Windows
7. **Region**: Central US (or preferred)
8. **App Service Plan**: Standard S1 (minimum)

---

### Configure App Service

**Configuration** ? **Application Settings**:

```
ASPNETCORE_ENVIRONMENT = Production
AdminAPI__BaseUrl = https://bellwood-admin-api.azurewebsites.net
AdminAPI__ApiKey = @Microsoft.KeyVault(SecretUri=https://bellwood-kv.vault.azure.net/secrets/AdminApiKey/)
GoogleMaps__ApiKey = @Microsoft.KeyVault(SecretUri=https://bellwood-kv.vault.azure.net/secrets/GoogleMapsKey/)
```

**General Settings**:
- ? Web Sockets: **On**
- ? Always On: **On** (prevents app sleep)
- ? HTTPS Only: **On**

---

### Deploy to Azure

#### Option 1: Azure CLI

```bash
# Login to Azure
az login

# Publish to folder
dotnet publish --configuration Release -o ./publish

# Zip contents
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip

# Deploy
az webapp deployment source config-zip --resource-group Bellwood-Production --name bellwood-admin-portal --src app.zip
```

---

#### Option 2: Visual Studio

1. Right-click project ? **Publish**
2. Target: **Azure**
3. Specific target: **Azure App Service (Windows)**
4. Select: `bellwood-admin-portal`
5. Click **Publish**

---

#### Option 3: GitHub Actions

**File**: `.github/workflows/deploy-azure.yml`

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Publish
      run: dotnet publish --configuration Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: bellwood-admin-portal
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: ./publish
```

---

## ? Production Deployment Checklist

### Pre-Deployment

**Code Quality**:
- [ ] All tests passing
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] Code reviewed and approved
- [ ] No hardcoded secrets in source code

**Configuration**:
- [ ] Production `appsettings.json` configured
- [ ] SSL certificates acquired and installed
- [ ] API keys stored in Key Vault
- [ ] Database connection strings verified

**Dependencies**:
- [ ] AuthServer deployed and accessible
- [ ] AdminAPI deployed and accessible
- [ ] Health check endpoints responding

**Documentation**:
- [ ] Deployment runbook reviewed
- [ ] Rollback plan documented
- [ ] Support team notified

---

### Deployment

**Coordinated Release** (AdminPortal + AdminAPI):

1. **Deploy AdminAPI** (if not already in production)
   - [ ] Deploy to production
   - [ ] Verify health check: `/health`
   - [ ] Monitor logs for errors (first 5 minutes)
   - [ ] Test `/bookings/list` returns new fields
   - [ ] Verify SignalR hub is running

2. **Deploy AdminPortal**
   - [ ] Deploy to production (IIS or Azure)
   - [ ] Clear browser cache (if needed)
   - [ ] Test login flow
   - [ ] Verify SignalR connection established
   - [ ] Test real-time status update flow

3. **Smoke Tests**
   - [ ] Login works
   - [ ] Bookings list loads
   - [ ] Quotes list loads
   - [ ] Live Tracking map displays
   - [ ] Real-time updates functional
   - [ ] Refresh preserves data

---

### Post-Deployment

**Monitoring** (First 24 hours):

- [ ] SignalR connection success rate >99%
- [ ] `/bookings/list` response time <200ms
- [ ] Real-time update latency <2 seconds
- [ ] Error rate in browser console 0%
- [ ] No support tickets about "status not updating"

**Alerts**:
- [ ] Set up Application Insights monitoring
- [ ] Configure alerts for error rate >1%
- [ ] Configure alerts for response time >500ms
- [ ] Monitor SignalR connection failures

---

## ?? Rollback Plan

### If Critical Issues Occur

**AdminPortal Rollback**:
1. Revert to previous version
2. Clear application cache
3. Restart IIS site / Azure App Service
4. Verify old version working

**Communication**:
1. Notify team in #deployments channel
2. Update status page
3. Inform support team
4. Schedule hotfix if needed

**Rollback Commands** (IIS):
```powershell
# Stop site
Stop-Website -Name "BellwoodAdminPortal"

# Restore previous version
Copy-Item -Path "C:\inetpub\backups\BellwoodAdminPortal-v1.0" -Destination "C:\inetpub\wwwroot\BellwoodAdminPortal" -Recurse -Force

# Start site
Start-Website -Name "BellwoodAdminPortal"
```

---

## ?? Troubleshooting

### Issue: Application Won't Start

**Check**:
```powershell
# View event logs
Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 10
```

**Common Causes**:
- Missing .NET 8.0 Runtime
- Incorrect `web.config`
- Permission issues
- Port conflicts

**Fix**:
- Install [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download)
- Verify `web.config` exists and is correct
- Check IIS application pool permissions
- Ensure port 443 is available

---

### Issue: 401 Unauthorized on API Calls

**Check**:
- API key in `appsettings.json` matches AdminAPI
- JWT token valid (not expired)
- AdminAPI configured to accept JWT from AuthServer

**Fix**:
```powershell
# Verify API key
cat appsettings.Production.json | Select-String "ApiKey"

# Test API connection
Invoke-WebRequest -Uri "https://api.bellwood.com/health" -UseBasicParsing
```

---

### Issue: SignalR Connection Fails

**Check Browser Console**:
```
Failed to start the connection: Error: WebSocket failed to connect
```

**Check**:
- [ ] WebSocket Protocol enabled in IIS
- [ ] Firewall allows WebSocket connections
- [ ] HTTPS certificate valid
- [ ] AdminAPI `/hubs/location` endpoint accessible

**Fix** (IIS):
```powershell
# Enable WebSocket Protocol
Install-WindowsFeature -Name Web-WebSockets

# Restart IIS
iisreset
```

---

### Issue: Map Not Loading

**Check**:
- `GoogleMaps:ApiKey` configured in `appsettings.json`
- Maps JavaScript API enabled in Google Cloud Console
- API key restrictions allow production domain

**Fix**:
1. Get API key from [Google Cloud Console](https://console.cloud.google.com/)
2. Enable "Maps JavaScript API"
3. Add to `appsettings.Production.json`
4. Restart application

---

## ?? Monitoring & Health Checks

### Health Endpoints

**AdminPortal Health** (if implemented):
```bash
curl https://admin.bellwood.com/health
# Expected: {"status":"Healthy"}
```

**Dependency Health**:
```bash
# AuthServer
curl https://auth.bellwood.com/health

# AdminAPI
curl https://api.bellwood.com/health
```

---

### Application Insights

**Key Metrics** to monitor:

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| Request Rate | - | - |
| Response Time (p95) | <500ms | >1000ms |
| Failure Rate | <1% | >5% |
| SignalR Connections | - | Drop >20% |
| Memory Usage | <80% | >90% |

**Dashboard**: [Link to Application Insights dashboard]

---

## ?? Related Documentation

- [System Architecture](01-System-Architecture.md) - Technical design & components
- [Security Model](23-Security-Model.md) - Authentication & authorization
- [Troubleshooting](32-Troubleshooting.md) - Common issues & solutions
- [Scripts Reference](31-Scripts-Reference.md) - Automation scripts
- [Testing Guide](02-Testing-Guide.md) - Testing procedures

---

**Last Updated**: January 17, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This deployment guide covers local development through production deployment. Follow the checklist to ensure smooth deployments.* ???
