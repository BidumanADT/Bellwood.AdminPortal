# Seed AdminAPI with test quotes
Write-Host "Seeding AdminAPI with test quotes..." -ForegroundColor Cyan

try {
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

    $response = Invoke-WebRequest -Uri "https://localhost:5206/quotes/seed" `
        -Method POST `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "? Quotes seeded successfully!" -ForegroundColor Green
    Write-Host $response.Content
}
catch {
    Write-Host "? Failed to seed quotes: $($_.Exception.Message)" -ForegroundColor Red
}