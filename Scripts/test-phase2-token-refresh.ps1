# Test Phase 2.2: Token Refresh
# PowerShell 5.1 Compatible
# Tests automatic token refresh functionality

param(
    [string]$AuthServerUrl = "https://localhost:5001"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 2.2: Token Refresh Tests" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$testsPassed = 0
$testsFailed = 0
$testResults = @()

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

# Test 1: Login and capture refresh token
Write-Host "Test 1: Login and verify refresh token is captured" -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "alice"
        password = "password"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$AuthServerUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    if ($response.refreshToken -and $response.accessToken) {
        Write-Host "  ? PASS: Both access token and refresh token received" -ForegroundColor Green
        $testsPassed++
        $testResults += [PSCustomObject]@{
            Test = "Refresh Token Capture"
            Result = "PASS"
            Details = "Access token length: $($response.accessToken.Length), Refresh token length: $($response.refreshToken.Length)"
        }
        
        # Store tokens for next test
        $script:accessToken = $response.accessToken
        $script:refreshToken = $response.refreshToken
    }
    else {
        Write-Host "  ? FAIL: Missing access token or refresh token" -ForegroundColor Red
        $testsFailed++
        $testResults += [PSCustomObject]@{
            Test = "Refresh Token Capture"
            Result = "FAIL"
            Details = "AccessToken present: $($null -ne $response.accessToken), RefreshToken present: $($null -ne $response.refreshToken)"
        }
    }
}
catch {
    Write-Host "  ? FAIL: Login failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Test 2: Use refresh token to get new access token
Write-Host "Test 2: Use refresh token to obtain new access token" -ForegroundColor Yellow
try {
    if ($script:refreshToken) {
        $refreshBody = @{
            grant_type = "refresh_token"
            refresh_token = $script:refreshToken
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$AuthServerUrl/connect/token" `
            -Method Post `
            -ContentType "application/json" `
            -Body $refreshBody `
            -ErrorAction Stop
        
        if ($response.access_token) {
            Write-Host "  ? PASS: New access token obtained via refresh token" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "Token Refresh"
                Result = "PASS"
                Details = "New access token length: $($response.access_token.Length)"
            }
            
            # Verify new token is different
            if ($response.access_token -ne $script:accessToken) {
                Write-Host "  ? PASS: New token is different from original" -ForegroundColor Green
                $testsPassed++
                $testResults += [PSCustomObject]@{
                    Test = "Token Uniqueness"
                    Result = "PASS"
                    Details = "New token differs from original"
                }
            }
            else {
                Write-Host "  ??  WARNING: New token is identical to original" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  ? FAIL: No access token in refresh response" -ForegroundColor Red
            $testsFailed++
            $testResults += [PSCustomObject]@{
                Test = "Token Refresh"
                Result = "FAIL"
                Details = "access_token not present in response"
            }
        }
    }
    else {
        Write-Host "  ??  SKIP: No refresh token available from Test 1" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ? FAIL: Token refresh failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Test 3: Manual verification of auto-refresh
Write-Host "Test 3: Manual verification of auto-refresh timer" -ForegroundColor Yellow
Write-Host "  This test requires manual verification in the AdminPortal" -ForegroundColor White
Write-Host ""
Write-Host "  Steps to verify:" -ForegroundColor White
Write-Host "  1. Login to AdminPortal as alice" -ForegroundColor Cyan
Write-Host "  2. Open browser developer console (F12)" -ForegroundColor Cyan
Write-Host "  3. Navigate to /main page" -ForegroundColor Cyan
Write-Host "  4. Look for console log: '[Main] Token auto-refresh started'" -ForegroundColor Cyan
Write-Host "  5. Look for log: '[TokenRefresh] Token will be refreshed in XX minutes'" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Did you see the auto-refresh logs in console? (Y/N/S to skip)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "  ? PASS: Auto-refresh timer started" -ForegroundColor Green
    $testsPassed++
    $testResults += [PSCustomObject]@{
        Test = "Auto-Refresh Timer"
        Result = "PASS (Manual)"
        Details = "User confirmed auto-refresh logs visible"
    }
}
elseif ($confirm -eq "S" -or $confirm -eq "s") {
    Write-Host "  ??  SKIP: Test skipped by user" -ForegroundColor Yellow
}
else {
    Write-Host "  ? FAIL: Auto-refresh not working" -ForegroundColor Red
    $testsFailed++
    $testResults += [PSCustomObject]@{
        Test = "Auto-Refresh Timer"
        Result = "FAIL (Manual)"
        Details = "User reported auto-refresh logs not visible"
    }
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
    Write-Host "? All token refresh tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "? Some token refresh tests failed!" -ForegroundColor Red
    exit 1
}
