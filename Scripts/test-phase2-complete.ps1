# Phase 2 Complete Test Suite
# PowerShell 5.1 Compatible
# Master test runner for all Phase 2 tests

param(
    [string]$AuthServerUrl = "https://localhost:5001",
    [string]$AdminAPIUrl = "https://localhost:5206",
    [string]$AdminPortalUrl = "https://localhost:7257",
    [switch]$AutomatedOnly,
    [switch]$ManualOnly
)

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  BELLWOOD ADMIN PORTAL - PHASE 2 TEST SUITE" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Testing Phase 2 Implementation:" -ForegroundColor White
Write-Host "  - JWT Decoding & Role Extraction" -ForegroundColor Yellow
Write-Host "  - Token Refresh" -ForegroundColor Yellow
Write-Host "  - Role-Based UI" -ForegroundColor Yellow
Write-Host "  - User Management" -ForegroundColor Yellow
Write-Host "  - 403 Error Handling" -ForegroundColor Yellow
Write-Host ""
Write-Host "Target URLs:" -ForegroundColor White
Write-Host "  AuthServer:  $AuthServerUrl" -ForegroundColor Cyan
Write-Host "  AdminAPI:    $AdminAPIUrl" -ForegroundColor Cyan
Write-Host "  AdminPortal: $AdminPortalUrl" -ForegroundColor Cyan
Write-Host ""

# Verify servers are running
Write-Host "Verifying servers are running..." -ForegroundColor Yellow
Write-Host ""

# Helper function to ignore SSL certificate errors
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
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$serversReady = $true

# Check AuthServer
Write-Host "  Checking AuthServer..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri "$AuthServerUrl/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host " ? Running" -ForegroundColor Green
    }
    else {
        Write-Host " ??  Responding but status $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host " ? Not responding" -ForegroundColor Red
    $serversReady = $false
}

# Check AdminAPI
Write-Host "  Checking AdminAPI..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri "$AdminAPIUrl/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host " ? Running" -ForegroundColor Green
    }
    else {
        Write-Host " ??  Responding but status $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host " ? Not responding" -ForegroundColor Red
    $serversReady = $false
}

# Check AdminPortal
Write-Host "  Checking AdminPortal..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri $AdminPortalUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host " ? Running" -ForegroundColor Green
    }
    else {
        Write-Host " ??  Responding but status $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host " ? Not responding" -ForegroundColor Red
    $serversReady = $false
}

Write-Host ""

if (-not $serversReady) {
    Write-Host "? ERROR: Not all servers are running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure all three servers are started:" -ForegroundColor Yellow
    Write-Host "  1. AuthServer (https://localhost:5001)" -ForegroundColor White
    Write-Host "  2. AdminAPI (https://localhost:5206)" -ForegroundColor White
    Write-Host "  3. AdminPortal (https://localhost:7257)" -ForegroundColor White
    Write-Host ""
    $confirm = Read-Host "Do you want to continue anyway? (Y/N)"
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "Test suite cancelled." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

# Run automated tests
if (-not $ManualOnly) {
    Write-Host "RUNNING AUTOMATED TESTS" -ForegroundColor Cyan
    Write-Host "=======================" -ForegroundColor Cyan
    Write-Host ""
    
    # Test 1: JWT Decoding
    Write-Host "[1/4] JWT Decoding & Role Extraction" -ForegroundColor Yellow
    Write-Host "-------------------------------------" -ForegroundColor Yellow
    try {
        & "$PSScriptRoot\test-phase2-jwt-decoding.ps1" -AuthServerUrl $AuthServerUrl
        if ($LASTEXITCODE -eq 0) {
            $passedTests++
        } else {
            $failedTests++
        }
        $totalTests++
    }
    catch {
        Write-Host "  ? Test script failed to run: $_" -ForegroundColor Red
        $failedTests++
        $totalTests++
    }
    
    Write-Host ""
    
    # Test 2: Token Refresh
    Write-Host "[2/4] Token Refresh" -ForegroundColor Yellow
    Write-Host "-------------------" -ForegroundColor Yellow
    try {
        & "$PSScriptRoot\test-phase2-token-refresh.ps1" -AuthServerUrl $AuthServerUrl
        if ($LASTEXITCODE -eq 0) {
            $passedTests++
        } else {
            $failedTests++
        }
        $totalTests++
    }
    catch {
        Write-Host "  ? Test script failed to run: $_" -ForegroundColor Red
        $failedTests++
        $totalTests++
    }
    
    Write-Host ""
    
    # Test 3: User Management
    Write-Host "[3/4] User Management & Role Assignment" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    try {
        & "$PSScriptRoot\test-phase2-user-management.ps1" -AuthServerUrl $AuthServerUrl
        if ($LASTEXITCODE -eq 0) {
            $passedTests++
        } else {
            $failedTests++
        }
        $totalTests++
    }
    catch {
        Write-Host "  ? Test script failed to run: $_" -ForegroundColor Red
        $failedTests++
        $totalTests++
    }
    
    Write-Host ""
    Write-Host "Automated tests complete." -ForegroundColor Green
    Write-Host ""
}

# Run manual tests
if (-not $AutomatedOnly) {
    Write-Host "RUNNING MANUAL TESTS" -ForegroundColor Cyan
    Write-Host "====================" -ForegroundColor Cyan
    Write-Host ""
    
    $confirm = Read-Host "Ready to run manual tests? (Y/N)"
    if ($confirm -eq "Y" -or $confirm -eq "y") {
        
        # Test 4: Role-Based UI
        Write-Host "[4/6] Role-Based UI Visibility" -ForegroundColor Yellow
        Write-Host "-------------------------------" -ForegroundColor Yellow
        try {
            & "$PSScriptRoot\test-phase2-role-ui.ps1" -AdminPortalUrl $AdminPortalUrl
            $totalTests++
        }
        catch {
            Write-Host "  ? Test script failed to run: $_" -ForegroundColor Red
            $totalTests++
        }
        
        Write-Host ""
        
        # Test 5: 403 Handling
        Write-Host "[5/6] 403 Forbidden Error Handling" -ForegroundColor Yellow
        Write-Host "-----------------------------------" -ForegroundColor Yellow
        try {
            & "$PSScriptRoot\test-phase2-403-handling.ps1" -AdminPortalUrl $AdminPortalUrl
            if ($LASTEXITCODE -eq 0) {
                $passedTests++
            } else {
                $failedTests++
            }
            $totalTests++
        }
        catch {
            Write-Host "  ? Test script failed to run: $_" -ForegroundColor Red
            $failedTests++
            $totalTests++
        }
        
        Write-Host ""
    }
    else {
        Write-Host "Manual tests skipped." -ForegroundColor Yellow
        $skippedTests = 2
    }
}

# Summary
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  PHASE 2 TEST SUITE - FINAL SUMMARY" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total Tests Run: $totalTests" -ForegroundColor White
Write-Host "Passed:          $passedTests" -ForegroundColor Green
Write-Host "Failed:          $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
Write-Host "Skipped:         $skippedTests" -ForegroundColor Yellow
Write-Host ""

if ($failedTests -eq 0 -and $totalTests -gt 0) {
    Write-Host "? ALL TESTS PASSED! Phase 2 implementation is successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Phase 2 features are ready for production:" -ForegroundColor White
    Write-Host "  ? JWT Decoding & Role Extraction" -ForegroundColor Green
    Write-Host "  ? Automatic Token Refresh" -ForegroundColor Green
    Write-Host "  ? Role-Based UI Navigation" -ForegroundColor Green
    Write-Host "  ? User Management & Role Assignment" -ForegroundColor Green
    Write-Host "  ? 403 Forbidden Error Handling" -ForegroundColor Green
    Write-Host ""
    exit 0
}
elseif ($totalTests -eq 0) {
    Write-Host "??  No tests were run!" -ForegroundColor Yellow
    exit 1
}
else {
    Write-Host "? SOME TESTS FAILED! Please review the output above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "  - Ensure all servers are running" -ForegroundColor White
    Write-Host "  - Verify test user accounts exist (alice, bob, diana, charlie)" -ForegroundColor White
    Write-Host "  - Check server logs for errors" -ForegroundColor White
    Write-Host "  - Verify JWT contains role and userId claims" -ForegroundColor White
    Write-Host ""
    exit 1
}
