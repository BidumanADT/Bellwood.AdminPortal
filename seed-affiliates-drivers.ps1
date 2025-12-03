# Seed Affiliates and Drivers to AdminAPI
# PowerShell 5.1 Compatible
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Bellwood Elite - Seed Affiliates & Drivers" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
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

# Step 2: Use built-in seed endpoint
Write-Host "Step 2: Seeding default affiliates and drivers..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $seedResponse = Invoke-RestMethod -Uri "$apiBaseUrl/dev/seed-affiliates" `
        -Method POST `
        -Headers $headers `
        -UseBasicParsing

    Write-Host "? Default affiliates seeded!" -ForegroundColor Green
    Write-Host "  Added: $($seedResponse.added) affiliate(s)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? Failed to seed defaults: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Step 3: Add Charlie's Affiliate
Write-Host "Step 3: Creating Charlie's affiliate (Downtown Express)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    $charlieAffiliateBody = @{
        name = "Downtown Express"
        pointOfContact = "Charlie Manager"
        phone = "312-555-7890"
        email = "charlie@downtownexpress.com"
        streetAddress = "456 State Street"
        city = "Chicago"
        state = "IL"
    } | ConvertTo-Json

    $affiliateResponse = Invoke-RestMethod -Uri "$apiBaseUrl/affiliates" `
        -Method POST `
        -Headers $headers `
        -Body $charlieAffiliateBody `
        -UseBasicParsing

    $charlieAffiliateId = $affiliateResponse.id
    Write-Host "? Downtown Express affiliate created!" -ForegroundColor Green
    Write-Host "  Affiliate ID: $charlieAffiliateId" -ForegroundColor White
    Write-Host ""

    # Step 4: Add Charlie as a Driver
    Write-Host "Step 4: Adding Charlie as a driver..." -ForegroundColor Yellow
    
    $charlieDriverBody = @{
        name = "Charlie"
        phone = "312-555-CHAS"
        userUid = "charlie-uid-001"
    } | ConvertTo-Json

    $driverResponse = Invoke-RestMethod -Uri "$apiBaseUrl/affiliates/$charlieAffiliateId/drivers" `
        -Method POST `
        -Headers $headers `
        -Body $charlieDriverBody `
        -UseBasicParsing

    Write-Host "? Charlie added as driver!" -ForegroundColor Green
    Write-Host "  Driver ID: $($driverResponse.id)" -ForegroundColor White
    Write-Host "  UserUID: charlie-uid-001" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? Failed to create Charlie's affiliate/driver: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Step 5: List all affiliates
Write-Host "Step 5: Listing all affiliates..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $affiliates = Invoke-RestMethod -Uri "$apiBaseUrl/affiliates/list" `
        -Method GET `
        -Headers $headers `
        -UseBasicParsing

    Write-Host "? Current affiliates in system:" -ForegroundColor Green
    Write-Host ""
    
    foreach ($affiliate in $affiliates) {
        Write-Host "  ?? $($affiliate.name)" -ForegroundColor Cyan
        Write-Host "     Contact: $($affiliate.pointOfContact)" -ForegroundColor White
        Write-Host "     Phone: $($affiliate.phone)" -ForegroundColor White
        Write-Host "     Email: $($affiliate.email)" -ForegroundColor White
        Write-Host "     Drivers: $($affiliate.drivers.Count)" -ForegroundColor White
        
        foreach ($driver in $affiliate.drivers) {
            Write-Host "       ?? $($driver.name) - $($driver.phone)" -ForegroundColor Gray
            if ($driver.userUid) {
                Write-Host "          UserUID: $($driver.userUid)" -ForegroundColor DarkGray
            }
        }
        Write-Host ""
    }
}
catch {
    Write-Host "? Failed to list affiliates: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seeding Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Start AdminPortal: dotnet run" -ForegroundColor White
Write-Host "2. Login as alice/password" -ForegroundColor White
Write-Host "3. Navigate to Affiliates page" -ForegroundColor White
Write-Host "4. View affiliates and drivers" -ForegroundColor White
Write-Host "5. Assign Charlie to a booking" -ForegroundColor White
Write-Host "6. Login to driver app as Charlie (UID: charlie-uid-001)" -ForegroundColor White
Write-Host ""
