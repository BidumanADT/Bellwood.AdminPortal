# Test Phase 2.4: User Management & Role Assignment
# PowerShell 5.1 Compatible
# Tests user management functionality and role assignment

param(
    [string]$AuthServerUrl = "https://localhost:5001"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 2.4: User Management & Role Assignment Tests" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$testsPassed = 0
$testsFailed = 0
$testResults = @()

# Helper function to ignore SSL certificate errors (only if not already defined)
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
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Get admin token
Write-Host "Logging in as admin..." -ForegroundColor Yellow
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
    
    $adminToken = $response.accessToken
    Write-Host "  ? Admin login successful" -ForegroundColor Green
}
catch {
    Write-Host "  ? FAIL: Admin login failed - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 1: Get all users (admin endpoint)
Write-Host "Test 1: Get all users list (GET /api/admin/users)" -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }
    
    $users = Invoke-RestMethod -Uri "$AuthServerUrl/api/admin/users" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    if ($users.Count -gt 0) {
        Write-Host "  ? PASS: Retrieved $($users.Count) users" -ForegroundColor Green
        $testsPassed++
        $testResults += [PSCustomObject]@{
            Test = "Get All Users"
            Result = "PASS"
            Details = "Retrieved $($users.Count) users"
        }
        
        # Display users
        Write-Host "  Users:" -ForegroundColor Cyan
        foreach ($user in $users) {
            Write-Host "    - $($user.username) ($($user.role))" -ForegroundColor White
        }
        
        # Verify expected users exist
        $expectedUsers = @("alice", "bob", "diana", "charlie")
        foreach ($expectedUser in $expectedUsers) {
            if ($users | Where-Object { $_.username -eq $expectedUser }) {
                Write-Host "  ? User '$expectedUser' found" -ForegroundColor Green
            }
            else {
                Write-Host "  ??  WARNING: Expected user '$expectedUser' not found" -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host "  ? FAIL: No users returned" -ForegroundColor Red
        $testsFailed++
        $testResults += [PSCustomObject]@{
            Test = "Get All Users"
            Result = "FAIL"
            Details = "Empty user list"
        }
    }
}
catch {
    Write-Host "  ? FAIL: Get users failed - $_" -ForegroundColor Red
    $testsFailed++
    $testResults += [PSCustomObject]@{
        Test = "Get All Users"
        Result = "FAIL"
        Details = $_.Exception.Message
    }
}

Write-Host ""

# Test 2: Filter users by role
Write-Host "Test 2: Filter users by role (GET /api/admin/users?role=dispatcher)" -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }
    
    $dispatchers = Invoke-RestMethod -Uri "$AuthServerUrl/api/admin/users?role=dispatcher" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    if ($dispatchers.Count -gt 0) {
        Write-Host "  ? PASS: Retrieved $($dispatchers.Count) dispatcher(s)" -ForegroundColor Green
        $testsPassed++
        $testResults += [PSCustomObject]@{
            Test = "Filter Users by Role"
            Result = "PASS"
            Details = "Retrieved $($dispatchers.Count) dispatchers"
        }
        
        # Verify all returned users are dispatchers
        $allDispatchers = $true
        foreach ($user in $dispatchers) {
            if ($user.role -ne "dispatcher") {
                $allDispatchers = $false
                Write-Host "  ??  WARNING: User $($user.username) has role $($user.role), not dispatcher" -ForegroundColor Yellow
            }
        }
        
        if ($allDispatchers) {
            Write-Host "  ? All returned users are dispatchers" -ForegroundColor Green
            $testsPassed++
        }
    }
    else {
        Write-Host "  ??  WARNING: No dispatchers found" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ? FAIL: Filter users failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Test 3: Dispatcher cannot access user list (403 test)
Write-Host "Test 3: Dispatcher denied access to user list (403 expected)" -ForegroundColor Yellow
try {
    # Login as dispatcher
    $loginBody = @{
        username = "diana"
        password = "password"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$AuthServerUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $dispatcherToken = $response.accessToken
    
    # Try to access user list
    $headers = @{
        "Authorization" = "Bearer $dispatcherToken"
    }
    
    try {
        $users = Invoke-RestMethod -Uri "$AuthServerUrl/api/admin/users" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        Write-Host "  ? FAIL: Dispatcher was allowed to access user list!" -ForegroundColor Red
        $testsFailed++
        $testResults += [PSCustomObject]@{
            Test = "Dispatcher 403 Forbidden"
            Result = "FAIL"
            Details = "Dispatcher should not have access to user list"
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 403 -or $_.Exception.Message -like "*403*" -or $_.Exception.Message -like "*Forbidden*") {
            Write-Host "  ? PASS: Dispatcher correctly denied with 403 Forbidden" -ForegroundColor Green
            $testsPassed++
            $testResults += [PSCustomObject]@{
                Test = "Dispatcher 403 Forbidden"
                Result = "PASS"
                Details = "403 Forbidden returned as expected"
            }
        }
        else {
            Write-Host "  ? FAIL: Unexpected error: $_" -ForegroundColor Red
            $testsFailed++
        }
    }
}
catch {
    Write-Host "  ? FAIL: Dispatcher login failed - $_" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Test 4: Update user role (manual verification recommended)
Write-Host "Test 4: Update user role (Manual Test)" -ForegroundColor Yellow
Write-Host "  This test requires manual verification in AdminPortal UI" -ForegroundColor White
Write-Host ""
Write-Host "  Steps to test:" -ForegroundColor White
Write-Host "  1. Login to AdminPortal as alice" -ForegroundColor Cyan
Write-Host "  2. Navigate to User Management page" -ForegroundColor Cyan
Write-Host "  3. Find user 'charlie' (driver)" -ForegroundColor Cyan
Write-Host "  4. Click 'Change Role' button" -ForegroundColor Cyan
Write-Host "  5. Select role 'dispatcher'" -ForegroundColor Cyan
Write-Host "  6. Click 'Update Role'" -ForegroundColor Cyan
Write-Host "  7. Verify success message appears" -ForegroundColor Cyan
Write-Host "  8. Refresh page and verify charlie's role changed to dispatcher" -ForegroundColor Cyan
Write-Host "  9. Change charlie's role back to 'driver'" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Did role change work successfully? (Y/N/S to skip)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "  ? PASS: Role change successful" -ForegroundColor Green
    $testsPassed++
    $testResults += [PSCustomObject]@{
        Test = "Update User Role (Manual)"
        Result = "PASS"
        Details = "User confirmed role change worked"
    }
}
elseif ($confirm -eq "S" -or $confirm -eq "s") {
    Write-Host "  ??  SKIP: Test skipped by user" -ForegroundColor Yellow
}
else {
    Write-Host "  ? FAIL: Role change failed" -ForegroundColor Red
    $testsFailed++
    $testResults += [PSCustomObject]@{
        Test = "Update User Role (Manual)"
        Result = "FAIL"
        Details = "User reported role change failed"
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
    Write-Host "? All user management tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "? Some user management tests failed!" -ForegroundColor Red
    exit 1
}
