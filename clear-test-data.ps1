# Clear All Test Data - Affiliates and Drivers
# PowerShell 5.1 Compatible
Write-Host "========================================" -ForegroundColor Red
Write-Host "Bellwood Elite - Clear All Test Data" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "WARNING: This will delete ALL affiliates and drivers!" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "Are you sure you want to proceed? Type 'YES' to confirm"

if ($confirmation -ne "YES") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Proceeding with data wipe..." -ForegroundColor Red
Write-Host ""

# Configuration
$apiBaseUrl = "https://localhost:5206"
$authServerUrl = "https://localhost:5001"

# For PowerShell 5.1 - ignore certificate validation
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
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Step 1: Get JWT Token
Write-Host "Step 1: Authenticating with AuthServer..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "alice"
        password = "password"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$authServerUrl/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -UseBasicParsing

    $token = $loginResponse.accessToken
    Write-Host "? Authentication successful!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "? Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure AuthServer is running on $authServerUrl" -ForegroundColor Yellow
    exit 1
}

# Step 2: Get all affiliates
Write-Host "Step 2: Fetching all affiliates..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $affiliates = Invoke-RestMethod -Uri "$apiBaseUrl/affiliates/list" `
        -Method GET `
        -Headers $headers `
        -UseBasicParsing

    Write-Host "? Found $($affiliates.Count) affiliate(s)" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "? Failed to fetch affiliates: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Delete all affiliates (and their drivers via cascade)
Write-Host "Step 3: Deleting all affiliates and drivers..." -ForegroundColor Yellow
$deletedCount = 0
$failedCount = 0

foreach ($affiliate in $affiliates) {
    try {
        $headers = @{
            "Authorization" = "Bearer $token"
        }

        Invoke-RestMethod -Uri "$apiBaseUrl/affiliates/$($affiliate.id)" `
            -Method DELETE `
            -Headers $headers `
            -UseBasicParsing | Out-Null

        Write-Host "  ? Deleted: $($affiliate.name) (and $($affiliate.drivers.Count) driver(s))" -ForegroundColor Gray
        $deletedCount++
    }
    catch {
        Write-Host "  ? Failed to delete: $($affiliate.name)" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Red
Write-Host "Data Wipe Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Affiliates deleted: $deletedCount" -ForegroundColor White
Write-Host "  Failed deletions: $failedCount" -ForegroundColor White
Write-Host ""

if ($deletedCount -gt 0) {
    Write-Host "All test data has been cleared!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Run seed script to add fresh data:" -ForegroundColor White
    Write-Host "   .\seed-affiliates-drivers.ps1" -ForegroundColor Cyan
    Write-Host ""
}
