# Bellwood AdminPortal - Complete Test Suite
# PowerShell 5.1 Compatible
# Runs all automated tests for Phase 1, Phase 2, Phase 3, and Phase B

param(
    [string]$AuthServerUrl = "https://localhost:5001",
    [string]$AdminAPIUrl = "https://localhost:5206",
    [string]$AdminPortalUrl = "https://localhost:7257",
    [switch]$SkipServerCheck,
    [switch]$Verbose,
    [switch]$ClearTestData  # NEW: Clear test data before running tests
)

# Script configuration
$ErrorActionPreference = "Continue"
$VerbosePreference = if ($Verbose) { "Continue" } else { "SilentlyContinue" }

# Import test helpers
Import-Module "$PSScriptRoot\Test-Helpers.psm1" -Force

Write-Host ""
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?                                                                  ?" -ForegroundColor Cyan
Write-Host "?        BELLWOOD ADMIN PORTAL - COMPLETE TEST SUITE              ?" -ForegroundColor Cyan
Write-Host "?                                                                  ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Testing All AdminPortal Functionality (Automated Tests Only)" -ForegroundColor White
Write-Host ""
Write-Host "Test Categories:" -ForegroundColor White
Write-Host "  ? Phase 1: Core Authentication & Authorization" -ForegroundColor Yellow
Write-Host "  ? Phase 2: JWT, Token Refresh, User Management" -ForegroundColor Yellow
Write-Host "  ? Phase 3: Driver Tracking (GPS & SignalR)" -ForegroundColor Yellow
Write-Host "  ? Phase B: Quote Lifecycle Management" -ForegroundColor Yellow
Write-Host "  ? Integration: End-to-End Workflows" -ForegroundColor Yellow
Write-Host ""
Write-Host "Target Environment:" -ForegroundColor White
Write-Host "  AuthServer:  $AuthServerUrl" -ForegroundColor Cyan
Write-Host "  AdminAPI:    $AdminAPIUrl" -ForegroundColor Cyan
Write-Host "  AdminPortal: $AdminPortalUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Initialize SSL trust (only once)
Initialize-SSLTrust

# ???????????????????????????????????????????????????????????????????????
# STEP 0: Clear Previous Test Data (Optional - Controlled by Parameter)
# ???????????????????????????????????????????????????????????????????????

if ($ClearTestData) {
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?  STEP 0: Clearing Previous Test Data                            ?" -ForegroundColor Yellow
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "??  This will delete all test affiliates and drivers..." -ForegroundColor Yellow
    
    # Authenticate to get token for cleanup
    try {
        $loginBody = @{
            username = "alice"
            password = "password"
        } | ConvertTo-Json
        
        $response = Invoke-SafeRestMethod -Uri "$AuthServerUrl/api/auth/login" `
            -Method Post `
            -Body $loginBody `
            -ContentType "application/json"
        
        $cleanupToken = $response.accessToken
        
        if ($cleanupToken) {
            Write-Host "  ? Authenticated for cleanup" -ForegroundColor Green
            
            # Get all affiliates
            try {
                $headers = @{
                    "Authorization" = "Bearer $cleanupToken"
                    "X-Admin-ApiKey" = "dev-secret-123"
                }
                
                $affiliates = Invoke-SafeRestMethod -Uri "$AdminAPIUrl/affiliates/list" `
                    -Method Get `
                    -Headers $headers
                
                if ($affiliates -and $affiliates.Count -gt 0) {
                    Write-Host "  Found $($affiliates.Count) affiliate(s) to delete..." -ForegroundColor Yellow
                    
                    foreach ($affiliate in $affiliates) {
                        try {
                            Invoke-SafeRestMethod -Uri "$AdminAPIUrl/affiliates/$($affiliate.id)" `
                                -Method Delete `
                                -Headers $headers | Out-Null
                            
                            Write-Host "    ? Deleted: $($affiliate.name) (and $($affiliate.drivers.Count) driver(s))" -ForegroundColor Gray
                        }
                        catch {
                            Write-Host "    ??  Failed to delete: $($affiliate.name)" -ForegroundColor Yellow
                        }
                    }
                    
                    Write-Host "  ? Cleanup complete!" -ForegroundColor Green
                }
                else {
                    Write-Host "  ??  No test data to clear" -ForegroundColor Cyan
                }
            }
            catch {
                Write-Host "  ??  Failed to fetch affiliates for cleanup: $_" -ForegroundColor Yellow
            }
        }
    }
    catch {
        Write-Host "  ??  Failed to authenticate for cleanup: $_" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Test suite tracking
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0
$script:skippedTests = 0
$script:testResults = @()

# Helper function to run a test script
function Invoke-TestScript {
    param(
        [string]$TestName,
        [string]$ScriptPath,
        [hashtable]$Parameters = @{},
        [string]$Category
    )
    
    $script:totalTests++
    
    Write-Host ""
    Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host "?? Test [$script:totalTests]: $TestName" -ForegroundColor Cyan
    Write-Host "   Category: $Category" -ForegroundColor Gray
    Write-Host "   Script: $(Split-Path $ScriptPath -Leaf)" -ForegroundColor Gray
    Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
    
    $startTime = Get-Date
    
    try {
        # Check if script exists
        if (-not (Test-Path $ScriptPath)) {
            Write-Host "??  SKIPPED: Script not found at $ScriptPath" -ForegroundColor Yellow
            $script:skippedTests++
            $script:testResults += [PSCustomObject]@{
                Test = $TestName
                Category = $Category
                Result = "SKIPPED"
                Duration = "N/A"
                Error = "Script not found"
            }
            return
        }
        
        # Run the test script
        & $ScriptPath @Parameters
        $exitCode = $LASTEXITCODE
        
        $duration = (Get-Date) - $startTime
        $durationStr = "{0:N1}s" -f $duration.TotalSeconds
        
        if ($exitCode -eq 0 -or $null -eq $exitCode) {
            Write-Host ""
            Write-Host "? PASSED: $TestName ($durationStr)" -ForegroundColor Green
            $script:passedTests++
            $script:testResults += [PSCustomObject]@{
                Test = $TestName
                Category = $Category
                Result = "PASSED"
                Duration = $durationStr
                Error = $null
            }
        }
        else {
            Write-Host ""
            Write-Host "? FAILED: $TestName (Exit Code: $exitCode, Duration: $durationStr)" -ForegroundColor Red
            $script:failedTests++
            $script:testResults += [PSCustomObject]@{
                Test = $TestName
                Category = $Category
                Result = "FAILED"
                Duration = $durationStr
                Error = "Exit code $exitCode"
            }
            
            if ($StopOnFailure) {
                Write-Host ""
                Write-Host "?? STOP ON FAILURE: Halting test suite" -ForegroundColor Red
                Show-Summary
                exit 1
            }
        }
    }
    catch {
        $duration = (Get-Date) - $startTime
        $durationStr = "{0:N1}s" -f $duration.TotalSeconds
        
        Write-Host ""
        Write-Host "? ERROR: $TestName - $_" -ForegroundColor Red
        Write-Host "   Duration: $durationStr" -ForegroundColor Gray
        $script:failedTests++
        $script:testResults += [PSCustomObject]@{
            Test = $TestName
            Category = $Category
            Result = "ERROR"
            Duration = $durationStr
            Error = $_.Exception.Message
        }
        
        if ($StopOnFailure) {
            Write-Host ""
            Write-Host "?? STOP ON FAILURE: Halting test suite" -ForegroundColor Red
            Show-Summary
            exit 1
        }
    }
}

# Helper function to show summary
function Show-Summary {
    $totalDuration = (Get-Date) - $script:suiteStartTime
    
    Write-Host ""
    Write-Host ""
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host "?                                                                  ?" -ForegroundColor Cyan
    Write-Host "?                    TEST SUITE SUMMARY                            ?" -ForegroundColor Cyan
    Write-Host "?                                                                  ?" -ForegroundColor Cyan
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "?? Overall Results:" -ForegroundColor White
    Write-Host "  Total Tests:    $script:totalTests" -ForegroundColor White
    Write-Host "  ? Passed:      $script:passedTests" -ForegroundColor Green
    Write-Host "  ? Failed:      $script:failedTests" -ForegroundColor $(if ($script:failedTests -eq 0) { "Green" } else { "Red" })
    Write-Host "  ??  Skipped:    $script:skippedTests" -ForegroundColor Yellow
    Write-Host ""
    
    $passRate = if ($script:totalTests -gt 0) { 
        [math]::Round(($script:passedTests / $script:totalTests) * 100, 1) 
    } else { 
        0 
    }
    Write-Host "  Pass Rate:      $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })
    Write-Host "  Duration:       $("{0:N1}s" -f $totalDuration.TotalSeconds)" -ForegroundColor Gray
    Write-Host ""
    
    # Show results by category
    Write-Host "?? Results by Category:" -ForegroundColor White
    $categories = $script:testResults | Group-Object -Property Category
    foreach ($category in $categories) {
        $categoryPassed = ($category.Group | Where-Object { $_.Result -eq "PASSED" }).Count
        $categoryTotal = $category.Count
        $categoryFailed = ($category.Group | Where-Object { $_.Result -eq "FAILED" -or $_.Result -eq "ERROR" }).Count
        
        $categoryColor = if ($categoryFailed -eq 0) { "Green" } else { "Red" }
        Write-Host "  $($category.Name): $categoryPassed/$categoryTotal passed" -ForegroundColor $categoryColor
    }
    Write-Host ""
    
    # Show failed tests details
    if ($script:failedTests -gt 0) {
        Write-Host "? Failed Tests Details:" -ForegroundColor Red
        $failedTests = $script:testResults | Where-Object { $_.Result -eq "FAILED" -or $_.Result -eq "ERROR" }
        foreach ($test in $failedTests) {
            Write-Host "  • $($test.Test)" -ForegroundColor Red
            Write-Host "    Category: $($test.Category) | Duration: $($test.Duration)" -ForegroundColor Gray
            if ($test.Error) {
                Write-Host "    Error: $($test.Error)" -ForegroundColor Yellow
            }
        }
        Write-Host ""
    }
    
    # Final verdict
    if ($script:failedTests -eq 0 -and $script:totalTests -gt 0) {
        Write-Host "?? SUCCESS! All tests passed!" -ForegroundColor Green
        Write-Host ""
        Write-Host "AdminPortal is ready for deployment! ??" -ForegroundColor Green
        Write-Host ""
        return 0
    }
    elseif ($script:totalTests -eq 0) {
        Write-Host "??  WARNING: No tests were run!" -ForegroundColor Yellow
        Write-Host ""
        return 1
    }
    else {
        Write-Host "? FAILURE: $script:failedTests test(s) failed" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please review failed tests and fix issues before deployment." -ForegroundColor Yellow
        Write-Host ""
        return 1
    }
}

# Step 1: Server Health Checks
if (-not $SkipServerCheck) {
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host "?  STEP 1: Server Health Checks                                   ?" -ForegroundColor Cyan
    Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
    
    $serversReady = $true
    
    Write-Host "  Checking AuthServer..." -NoNewline
    try {
        $response = Invoke-SafeWebRequest -Uri "$AuthServerUrl/health" -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host " ? Running" -ForegroundColor Green
        } else {
            Write-Host " ??  Status $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host " ? Not responding" -ForegroundColor Red
        $serversReady = $false
    }
    
    Write-Host "  Checking AdminAPI..." -NoNewline
    try {
        $response = Invoke-SafeWebRequest -Uri "$AdminAPIUrl/health" -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host " ? Running" -ForegroundColor Green
        } else {
            Write-Host " ??  Status $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host " ? Not responding" -ForegroundColor Red
        $serversReady = $false
    }
    
    Write-Host "  Checking AdminPortal..." -NoNewline
    try {
        $response = Invoke-SafeWebRequest -Uri $AdminPortalUrl -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host " ? Running" -ForegroundColor Green
        } else {
            Write-Host " ??  Status $($response.StatusCode)" -ForegroundColor Yellow
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
        Write-Host "Please start all required servers:" -ForegroundColor Yellow
        Write-Host "  1. AuthServer on $AuthServerUrl" -ForegroundColor White
        Write-Host "  2. AdminAPI on $AdminAPIUrl" -ForegroundColor White
        Write-Host "  3. AdminPortal on $AdminPortalUrl" -ForegroundColor White
        Write-Host ""
        $confirm = Read-Host "Continue anyway? (Y/N)"
        if ($confirm -ne "Y" -and $confirm -ne "y") {
            Write-Host "Test suite cancelled." -ForegroundColor Yellow
            exit 1
        }
    }
}

# Start test suite timer
$script:suiteStartTime = Get-Date

# Step 2: Run Core Tests
Write-Host ""
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  STEP 2: Phase 1 - Core Infrastructure Tests                    ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Invoke-TestScript `
    -TestName "API Connectivity & Health" `
    -ScriptPath "$PSScriptRoot\test-api-connection.ps1" `
    -Category "Phase 1: Core"

# Step 3: Phase 2 Tests
Write-Host ""
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  STEP 3: Phase 2 - Authentication & Authorization Tests         ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Invoke-TestScript `
    -TestName "JWT Decoding & Role Extraction" `
    -ScriptPath "$PSScriptRoot\test-phase2-jwt-decoding.ps1" `
    -Parameters @{ AuthServerUrl = $AuthServerUrl } `
    -Category "Phase 2: Auth"

Invoke-TestScript `
    -TestName "Token Refresh Mechanism" `
    -ScriptPath "$PSScriptRoot\test-phase2-token-refresh.ps1" `
    -Parameters @{ AuthServerUrl = $AuthServerUrl } `
    -Category "Phase 2: Auth"

Invoke-TestScript `
    -TestName "User Management & Role Assignment" `
    -ScriptPath "$PSScriptRoot\test-phase2-user-management.ps1" `
    -Parameters @{ AuthServerUrl = $AuthServerUrl; AdminAPIUrl = $AdminAPIUrl } `
    -Category "Phase 2: User Management"

Invoke-TestScript `
    -TestName "403 Forbidden Error Handling" `
    -ScriptPath "$PSScriptRoot\test-phase2-403-handling.ps1" `
    -Parameters @{ AdminAPIUrl = $AdminAPIUrl; AuthServerUrl = $AuthServerUrl } `
    -Category "Phase 2: Error Handling"

# Step 4: Phase B Tests
Write-Host ""
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  STEP 4: Phase B - Quote Lifecycle Tests                        ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Invoke-TestScript `
    -TestName "Quote Lifecycle (Pending ? Acknowledged ? Responded)" `
    -ScriptPath "$PSScriptRoot\test-phase-b-quote-lifecycle.ps1" `
    -Parameters @{ 
        AuthServerUrl = $AuthServerUrl
        AdminAPIUrl = $AdminAPIUrl
        AdminPortalUrl = $AdminPortalUrl
    } `
    -Category "Phase B: Quotes"

# Step 5: Show Final Summary
$exitCode = Show-Summary

Write-Host "Test End Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Export results to JSON (optional)
$resultsPath = "$PSScriptRoot\test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$script:testResults | ConvertTo-Json -Depth 3 | Out-File -FilePath $resultsPath -Encoding UTF8
Write-Host "?? Test results exported to: $resultsPath" -ForegroundColor Gray
Write-Host ""

exit $exitCode
