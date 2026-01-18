# Test Phase 2.5: 403 Forbidden Error Handling
# PowerShell 5.1 Compatible
# Tests that 403 errors are handled gracefully with user-friendly messages

param(
    [string]$AdminPortalUrl = "https://localhost:7257"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 2.5: 403 Forbidden Error Handling Tests" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This is a MANUAL TEST GUIDE. Follow the steps below." -ForegroundColor Yellow
Write-Host ""

$testsPassed = 0
$testsFailed = 0

Write-Host "TEST 1: Dispatcher accessing User Management (403)" -ForegroundColor Cyan
Write-Host "---------------------------------------------------" -ForegroundColor Cyan
Write-Host "1. Login to AdminPortal as dispatcher:" -ForegroundColor White
Write-Host "   Username: diana" -ForegroundColor Yellow
Write-Host "   Password: password" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Try to navigate directly to:" -ForegroundColor White
Write-Host "   $AdminPortalUrl/admin/users" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Expected result:" -ForegroundColor White
Write-Host "   - Page should not load OR" -ForegroundColor Green
Write-Host "   - Error message displayed: 'Access denied. You do not have permission...'" -ForegroundColor Green
Write-Host "   - NO raw 403 error or exception details shown" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Did the 403 error display a user-friendly message? (Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 1 PASSED" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "? TEST 1 FAILED - Raw error or no friendly message" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "TEST 2: Dispatcher accessing OAuth Credentials (403)" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "While still logged in as dispatcher (diana):" -ForegroundColor White
Write-Host ""
Write-Host "1. Try to navigate to:" -ForegroundColor White
Write-Host "   $AdminPortalUrl/admin/credentials" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Expected result:" -ForegroundColor White
Write-Host "   - Access denied (page protection or friendly error)" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Was dispatcher blocked from OAuth Credentials page? (Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 2 PASSED" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "? TEST 2 FAILED - Dispatcher accessed admin page" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "TEST 3: Dispatcher accessing Billing Reports (403)" -ForegroundColor Cyan
Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host "While still logged in as dispatcher (diana):" -ForegroundColor White
Write-Host ""
Write-Host "1. Try to navigate to:" -ForegroundColor White
Write-Host "   $AdminPortalUrl/admin/billing" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Expected result:" -ForegroundColor White
Write-Host "   - Access denied (page protection or friendly error)" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Was dispatcher blocked from Billing Reports page? (Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 3 PASSED" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "? TEST 3 FAILED - Dispatcher accessed admin page" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "TEST 4: API 403 Error Handling (Affiliates)" -ForegroundColor Cyan
Write-Host "-------------------------------------------" -ForegroundColor Cyan
Write-Host "NOTE: This test assumes AdminAPI enforces role restrictions" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Login to AdminPortal as dispatcher (if not already)" -ForegroundColor White
Write-Host "2. Navigate to Affiliates page" -ForegroundColor White
Write-Host "3. If AdminAPI returns 403 for non-admin users:" -ForegroundColor White
Write-Host "   Expected: User-friendly error message displayed" -ForegroundColor Green
Write-Host "   'Access denied. You do not have permission to view affiliates.'" -ForegroundColor Green
Write-Host ""
Write-Host "4. If AdminAPI allows dispatcher access:" -ForegroundColor White
Write-Host "   Expected: Affiliates list displays normally" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Did affiliates page handle access correctly? (Y/N/S to skip)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 4 PASSED" -ForegroundColor Green
    $testsPassed++
}
elseif ($confirm -eq "S" -or $confirm -eq "s") {
    Write-Host "??  TEST 4 SKIPPED" -ForegroundColor Yellow
}
else {
    Write-Host "? TEST 4 FAILED - Error handling issue" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "TEST 5: Bookings/Quotes 403 Handling (if applicable)" -ForegroundColor Cyan
Write-Host "-----------------------------------------------------" -ForegroundColor Cyan
Write-Host "1. While logged in as dispatcher, navigate to:" -ForegroundColor White
Write-Host "   - Bookings page" -ForegroundColor Cyan
Write-Host "   - Quotes page" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. If 403 error occurs (based on AdminAPI policy):" -ForegroundColor White
Write-Host "   Expected: User-friendly error message" -ForegroundColor Green
Write-Host "   'Access denied. You don't have permission to view these records.'" -ForegroundColor Green
Write-Host ""
Write-Host "3. If dispatcher has access:" -ForegroundColor White
Write-Host "   Expected: Data loads normally" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Did bookings/quotes handle access correctly? (Y/N/S to skip)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 5 PASSED" -ForegroundColor Green
    $testsPassed++
}
elseif ($confirm -eq "S" -or $confirm -eq "s") {
    Write-Host "??  TEST 5 SKIPPED" -ForegroundColor Yellow
}
else {
    Write-Host "? TEST 5 FAILED - Error handling issue" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "TEST 6: Admin Has Full Access" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan
Write-Host "1. Logout and login as admin (alice/password)" -ForegroundColor White
Write-Host "2. Navigate to all pages:" -ForegroundColor White
Write-Host "   - Bookings ?" -ForegroundColor Green
Write-Host "   - Quotes ?" -ForegroundColor Green
Write-Host "   - Affiliates ?" -ForegroundColor Green
Write-Host "   - Live Tracking ?" -ForegroundColor Green
Write-Host "   - User Management ?" -ForegroundColor Green
Write-Host "   - OAuth Credentials ?" -ForegroundColor Green
Write-Host "   - Billing Reports ?" -ForegroundColor Green
Write-Host ""
Write-Host "3. Expected: All pages load successfully" -ForegroundColor White
Write-Host ""

$confirm = Read-Host "Did admin have access to all pages? (Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    Write-Host "? TEST 6 PASSED" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "? TEST 6 FAILED - Admin access restricted" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "? All 403 error handling tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}
else {
    Write-Host "? Some 403 error handling tests failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
