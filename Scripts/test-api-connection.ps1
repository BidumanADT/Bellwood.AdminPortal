# Test AdminAPI Connection
# PowerShell 5.1 Compatible
Write-Host "Testing Bellwood AdminPortal – AdminAPI Connection" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

$apiKey = "dev-secret-123"
$apiBaseUrl = "https://localhost:5206"
$authServerUrl = "https://localhost:5001"

# For PowerShell 5.1 - ignore certificate validation (only if not already defined)
if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
    add-type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
}
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Test 0: Authenticate to get JWT token
Write-Host "[0/3] Authenticating with AuthServer..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "alice"
        password = "password"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$authServerUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -UseBasicParsing `
        -ErrorAction Stop

    $token = $loginResponse.accessToken
    Write-Host "? Authentication successful" -ForegroundColor Green
}
catch {
    Write-Host "? Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "??  Cannot proceed without authentication" -ForegroundColor Yellow
    exit 1
}

# Prepare headers with both API key and JWT token
$headers = @{
    "X-Admin-ApiKey" = $apiKey
    "Authorization" = "Bearer $token"
}

# Test 1: Health Check
Write-Host "[1/3] Testing health endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "$apiBaseUrl/health" -UseBasicParsing -ErrorAction Stop
    Write-Host "? Health check passed" -ForegroundColor Green
}
catch {
    Write-Host "? Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Seed Bookings
Write-Host "[2/3] Seeding test bookings..." -ForegroundColor Yellow
try {
    $seed = Invoke-WebRequest -Uri "$apiBaseUrl/bookings/seed" `
        -Method POST `
        -Headers $headers `
        -UseBasicParsing `
        -ErrorAction Stop
    
    $seedData = $seed.Content | ConvertFrom-Json
    Write-Host "? Seeded $($seedData.added) bookings" -ForegroundColor Green
}
catch {
    Write-Host "??  Seed failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   This may be expected if bookings already exist" -ForegroundColor Gray
}

# Test 3: Fetch Bookings List
Write-Host "[3/3] Fetching bookings list..." -ForegroundColor Yellow
try {
    $bookings = Invoke-WebRequest -Uri "$apiBaseUrl/bookings/list?take=10" `
        -Headers $headers `
        -UseBasicParsing `
        -ErrorAction Stop
    
    $bookingData = $bookings.Content | ConvertFrom-Json
    Write-Host "? Fetched $($bookingData.Count) bookings" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sample bookings:" -ForegroundColor Cyan
    $bookingData | Select-Object -First 3 | ForEach-Object {
        Write-Host "  - $($_.PassengerName) | $($_.VehicleClass) | $($_.Status)" -ForegroundColor White
    }
}
catch {
    Write-Host "? Fetch failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "? All tests passed! AdminAPI is ready." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Ensure AuthServer is running on https://localhost:5001" -ForegroundColor White
Write-Host "2. Run the AdminPortal: dotnet run" -ForegroundColor White
Write-Host "3. Navigate to https://localhost:7257" -ForegroundColor White
Write-Host "4. Login with alice/password or bob/password" -ForegroundColor White
