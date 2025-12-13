# Test AdminAPI Connection
Write-Host "Testing Bellwood AdminPortal ? AdminAPI Connection" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

$apiKey = "dev-secret-123"
$baseUrl = "https://localhost:5206"

# Test 1: Health Check
Write-Host "[1/3] Testing health endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "$baseUrl/health" -SkipCertificateCheck -ErrorAction Stop
    Write-Host "? Health check passed" -ForegroundColor Green
}
catch {
    Write-Host "? Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Seed Bookings
Write-Host "[2/3] Seeding test bookings..." -ForegroundColor Yellow
try {
    $seed = Invoke-WebRequest -Uri "$baseUrl/bookings/seed" -Method POST -SkipCertificateCheck -ErrorAction Stop
    $seedData = $seed.Content | ConvertFrom-Json
    Write-Host "? Seeded $($seedData.added) bookings" -ForegroundColor Green
}
catch {
    Write-Host "? Seed failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Fetch Bookings List
Write-Host "[3/3] Fetching bookings list..." -ForegroundColor Yellow
try {
    $headers = @{
        "X-Admin-ApiKey" = $apiKey
    }
    $bookings = Invoke-WebRequest -Uri "$baseUrl/bookings/list?take=10" `
        -Headers $headers `
        -SkipCertificateCheck `
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
