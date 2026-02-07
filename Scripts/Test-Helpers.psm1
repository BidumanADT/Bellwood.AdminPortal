# Test Helper Functions
# Shared utilities for all AdminPortal test scripts
# PowerShell 5.1 Compatible

# Function to initialize SSL trust (only once per session)
function Initialize-SSLTrust {
    # Check if type already exists
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
}

# Function to make web requests with proper error handling
function Invoke-SafeWebRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$ContentType = "application/json",
        [int]$TimeoutSec = 30
    )
    
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
            TimeoutSec = $TimeoutSec
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = $ContentType
        }
        
        return Invoke-WebRequest @params
    }
    catch {
        throw $_
    }
}

# Function to make REST requests with proper error handling
function Invoke-SafeRestMethod {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$ContentType = "application/json",
        [int]$TimeoutSec = 30
    )
    
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            TimeoutSec = $TimeoutSec
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = $ContentType
        }
        
        return Invoke-RestMethod @params
    }
    catch {
        throw $_
    }
}

# Function to parse JWT token
function Parse-JWT {
    param([string]$Token)
    
    try {
        $parts = $Token.Split('.')
        if ($parts.Length -ne 3) {
            return $null
        }
        
        $payload = $parts[1]
        # Add padding if needed
        while ($payload.Length % 4 -ne 0) {
            $payload += "="
        }
        
        $bytes = [System.Convert]::FromBase64String($payload)
        $json = [System.Text.Encoding]::UTF8.GetString($bytes)
        return $json | ConvertFrom-Json
    }
    catch {
        Write-Verbose "Error parsing JWT: $_"
        return $null
    }
}

# Export functions for use in other scripts
Export-ModuleMember -Function Initialize-SSLTrust, Invoke-SafeWebRequest, Invoke-SafeRestMethod, Parse-JWT
