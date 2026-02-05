# Test Phase 2.1: JWT Decoding & Role Extraction
# PowerShell 5.1 Compatible
# Tests that JWT tokens are properly decoded and claims extracted

param(
    [string]$AuthServerUrl = "https://localhost:5001",
    [string]$AdminPortalUrl = "https://localhost:7257"
)

# Import test helpers
Import-Module "$PSScriptRoot\Test-Helpers.psm1" -Force

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 2.1: JWT Decoding & Role Extraction Tests" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$testsPassed = 0
$testsFailed = 0
$testResults = @()

# Initialize SSL trust
Initialize-SSLTrust

# Test 1: Login as Admin (alice) and verify JWT contains role claim
Write-Host "Test 1: Login as admin (alice) and verify JWT role claim" -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "alice"
        password = "password"
    } | ConvertTo-Json
    
    $response = Invoke-SafeRestMethod -Uri "$AuthServerUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    if ($response.accessToken) {
        $claims = Parse-JWT -Token $response.accessToken
        
        if ($claims.role -eq "admin") {
            Write-Host "  ? PASS: JWT contains role claim 'admin'" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Role Claim (admin)"
                Result = "PASS"
                Details = "Role: $($claims.role), UserId: $($claims.userId)"
            }
        }
        else {
            Write-Host "  ? FAIL: Expected role 'admin', got '$($claims.role)'" -ForegroundColor Red
            $testsFailed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Role Claim (admin)"
                Result = "FAIL"
                Details = "Expected: admin, Got: $($claims.role)"
            }
        }
        
        # Verify userId claim exists
        if ($claims.userId) {
            Write-Host "  ? PASS: JWT contains userId claim" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "JWT UserId Claim"
                Result = "PASS"
                Details = "UserId: $($claims.userId)"
            }
        }
        else {
            Write-Host "  ? FAIL: JWT missing userId claim" -ForegroundColor Red
            $testsFailed++
            $testResults += [PSCustomObject]@{
                Test = "JWT UserId Claim"
                Result = "FAIL"
                Details = "userId claim not found"
            }
        }
        
        # Verify username (sub) claim
        if ($claims.sub -eq "alice") {
            Write-Host "  ? PASS: JWT contains correct username (sub)" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Username Claim"
                Result = "PASS"
                Details = "Username: $($claims.sub)"
            }
        }
        else {
            Write-Host "  ? FAIL: Expected username 'alice', got '$($claims.sub)'" -ForegroundColor Red
            $testsFailed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Username Claim"
                Result = "FAIL"
                Details = "Expected: alice, Got: $($claims.sub)"
            }
        }
    }
    else {
        Write-Host "  ? FAIL: No access token returned" -ForegroundColor Red
        $testsFailed += 3
    }
}
catch {
    Write-Host "  ? FAIL: Login request failed - $_" -ForegroundColor Red
    $testsFailed += 3
}

Write-Host ""

# Test 2: Login as Dispatcher (diana) and verify JWT contains role claim
Write-Host "Test 2: Login as dispatcher (diana) and verify JWT role claim" -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "diana"
        password = "password"
    } | ConvertTo-Json
    
    $response = Invoke-SafeRestMethod -Uri "$AuthServerUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    if ($response.accessToken) {
        $claims = Parse-JWT -Token $response.accessToken
        
        if ($claims.role -eq "dispatcher") {
            Write-Host "  ? PASS: JWT contains role claim 'dispatcher'" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Role Claim (dispatcher)"
                Result = "PASS"
                Details = "Role: $($claims.role), UserId: $($claims.userId)"
            }
        }
        else {
            Write-Host "  ? FAIL: Expected role 'dispatcher', got '$($claims.role)'" -ForegroundColor Red
            $testsFailed++
            $testResults += [PSCustomObject]@{
                Test = "JWT Role Claim (dispatcher)"
                Result = "FAIL"
                Details = "Expected: dispatcher, Got: $($claims.role)"
            }
        }
    }
    else {
        Write-Host "  ? FAIL: No access token returned" -ForegroundColor Red
        $testsFailed++
    }
}
catch {
    Write-Host "  ? FAIL: Login request failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Test 3: Verify refresh token is returned
Write-Host "Test 3: Verify refresh token is returned on login" -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "alice"
        password = "password"
    } | ConvertTo-Json
    
    $response = Invoke-SafeRestMethod -Uri "$AuthServerUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    if ($response.refreshToken) {
        Write-Host "  ? PASS: Refresh token returned" -ForegroundColor Green
        $testsPassed++
        $testResults += [PSCustomObject]@{
            Test = "Refresh Token Returned"
            Result = "PASS"
            Details = "RefreshToken present, length: $($response.refreshToken.Length)"
        }
    }
    else {
        Write-Host "  ? FAIL: No refresh token returned" -ForegroundColor Red
        $testsFailed++
        $testResults += [PSCustomObject]@{
            Test = "Refresh Token Returned"
            Result = "FAIL"
            Details = "refreshToken not present in response"
        }
    }
}
catch {
    Write-Host "  ? FAIL: Login request failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host ""

# Display detailed results
$testResults | Format-Table -AutoSize

# Return exit code
if ($testsFailed -eq 0) {
    Write-Host "? All JWT decoding tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "? Some JWT decoding tests failed!" -ForegroundColor Red
    exit 1
}
