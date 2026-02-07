# AdminPortal Complete Automated Test Suite

**Created**: February 3, 2026  
**Version**: 1.0  
**Status**: ? Production Ready

---

## ?? Overview

The **AdminPortal Complete Automated Test Suite** is a master orchestrator that runs all automated tests for the Bellwood AdminPortal in a single command. It provides comprehensive coverage of all implemented features and generates detailed test reports.

**Script Location**: `Scripts/test-adminportal-complete.ps1`

---

## ?? What Gets Tested

### Phase 1: Core Infrastructure
- ? API connectivity (AuthServer, AdminAPI, AdminPortal)
- ? Health endpoint availability
- ? SSL/HTTPS configuration
- ? Basic authentication flow

### Phase 2: Authentication & Authorization
- ? JWT token decoding & role extraction
- ? Automatic token refresh (55-minute expiry)
- ? User management API endpoints
- ? Role assignment functionality
- ? 403 Forbidden error handling

### Phase B: Quote Lifecycle
- ? Quote list retrieval
- ? Quote acknowledgment workflow
- ? Price estimation (placeholder)
- ? Quote response flow
- ? Status transitions (Pending ? Acknowledged ? Responded)

### Integration Tests
- ? End-to-end workflows
- ? Cross-service communication
- ? Error handling across services

---

## ?? Quick Start

### Basic Usage

```powershell
# Run all tests with default settings
.\Scripts\test-adminportal-complete.ps1
```

### Advanced Usage

```powershell
# Run with custom URLs
.\Scripts\test-adminportal-complete.ps1 `
    -AuthServerUrl "https://localhost:5001" `
    -AdminAPIUrl "https://localhost:5206" `
    -AdminPortalUrl "https://localhost:7257"

# Skip server health checks (if you know they're running)
.\Scripts\test-adminportal-complete.ps1 -SkipServerCheck

# Enable verbose output for debugging
.\Scripts\test-adminportal-complete.ps1 -Verbose

# Stop on first failure (for faster debugging)
.\Scripts\test-adminportal-complete.ps1 -StopOnFailure
```

---

## ?? Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `AuthServerUrl` | string | `https://localhost:5001` | AuthServer base URL |
| `AdminAPIUrl` | string | `https://localhost:5206` | AdminAPI base URL |
| `AdminPortalUrl` | string | `https://localhost:7257` | AdminPortal base URL |
| `SkipServerCheck` | switch | `false` | Skip initial server health checks |
| `Verbose` | switch | `false` | Enable verbose logging |
| `StopOnFailure` | switch | `false` | Stop test suite on first failure |
| `ClearTestData` | switch | `false` | **NEW**: Delete all test affiliates/drivers before running tests |

---

## ?? Example Output

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?        BELLWOOD ADMIN PORTAL - COMPLETE TEST SUITE              ?
?                                                                  ?
????????????????????????????????????????????????????????????????????

?? Testing All AdminPortal Functionality (Automated Tests Only)

Test Categories:
  ? Phase 1: Core Authentication & Authorization
  ? Phase 2: JWT, Token Refresh, User Management
  ? Phase 3: Driver Tracking (GPS & SignalR)
  ? Phase B: Quote Lifecycle Management
  ? Integration: End-to-End Workflows

Target Environment:
  AuthServer:  https://localhost:5001
  AdminAPI:    https://localhost:5206
  AdminPortal: https://localhost:7257

Test Start Time: 2026-02-03 14:30:00

????????????????????????????????????????????????????????????????????
?  STEP 1: Server Health Checks                                   ?
????????????????????????????????????????????????????????????????????

  Checking AuthServer... ? Running
  Checking AdminAPI... ? Running
  Checking AdminPortal... ? Running

????????????????????????????????????????????????????????????????????
?  STEP 2: Phase 1 - Core Infrastructure Tests                    ?
????????????????????????????????????????????????????????????????????

??????????????????????????????????????????????????????????????????
?? Test [1]: API Connectivity & Health
   Category: Phase 1: Core
   Script: test-api-connection.ps1
??????????????????????????????????????????????????????????????????

[Test output...]

? PASSED: API Connectivity & Health (2.3s)

????????????????????????????????????????????????????????????????????
?  STEP 3: Phase 2 - Authentication & Authorization Tests         ?
????????????????????????????????????????????????????????????????????

??????????????????????????????????????????????????????????????????
?? Test [2]: JWT Decoding & Role Extraction
   Category: Phase 2: Auth
   Script: test-phase2-jwt-decoding.ps1
??????????????????????????????????????????????????????????????????

[Test output...]

? PASSED: JWT Decoding & Role Extraction (1.8s)

[... more tests ...]

????????????????????????????????????????????????????????????????????
?                                                                  ?
?                    TEST SUITE SUMMARY                            ?
?                                                                  ?
????????????????????????????????????????????????????????????????????

?? Overall Results:
  Total Tests:    6
  ? Passed:      6
  ? Failed:      0
  ??  Skipped:    0

  Pass Rate:      100.0%
  Duration:       12.4s

?? Results by Category:
  Phase 1: Core: 1/1 passed
  Phase 2: Auth: 2/2 passed
  Phase 2: User Management: 1/1 passed
  Phase 2: Error Handling: 1/1 passed
  Phase B: Quotes: 1/1 passed

?? SUCCESS! All tests passed!

AdminPortal is ready for deployment! ??

Test End Time: 2026-02-03 14:30:12

?? Test results exported to: Scripts\test-results-20260203-143012.json
```

---

## ?? Test Results Export

After each test run, results are automatically exported to a JSON file:

**Location**: `Scripts/test-results-YYYYMMDD-HHmmss.json`

**Format**:
```json
[
  {
    "Test": "API Connectivity & Health",
    "Category": "Phase 1: Core",
    "Result": "PASSED",
    "Duration": "2.3s",
    "Error": null
  },
  {
    "Test": "JWT Decoding & Role Extraction",
    "Category": "Phase 2: Auth",
    "Result": "PASSED",
    "Duration": "1.8s",
    "Error": null
  }
]
```

**Usage**: Import into CI/CD pipelines, generate reports, track test trends

---

## ?? Prerequisites

### Required Servers
All three servers must be running:
1. **AuthServer** on `https://localhost:5001`
2. **AdminAPI** on `https://localhost:5206`
3. **AdminPortal** on `https://localhost:7257`

### Required Test Data
Run these seed scripts before testing:
```powershell
# Seed affiliates and drivers
.\Scripts\seed-affiliates-drivers.ps1

# Seed quotes
.\Scripts\seed-quotes.ps1

# Seed bookings (optional)
.\Scripts\seed-admin-api.ps1
```

### Required Test Users
- **alice** / **password** (admin role)
- **bob** / **password** (admin role)
- **diana** / **password** (dispatcher role)
- **charlie** / **password** (driver role)

---

## ??? Troubleshooting

### Issue: "Server not responding"

**Symptom**: Health check fails for one or more servers

**Solution**:
```powershell
# Check if servers are running
Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" }

# Start servers manually
# AuthServer:
cd C:\Path\To\AuthServer
dotnet run

# AdminAPI:
cd C:\Path\To\AdminAPI
dotnet run

# AdminPortal:
cd C:\Path\To\AdminPortal
dotnet run
```

---

### Issue: "Test script not found"

**Symptom**: `??  SKIPPED: Script not found`

**Solution**: Ensure all test scripts exist in `Scripts/` folder:
- `test-api-connection.ps1`
- `test-phase2-jwt-decoding.ps1`
- `test-phase2-token-refresh.ps1`
- `test-phase2-user-management.ps1`
- `test-phase2-403-handling.ps1`
- `test-phase-b-quote-lifecycle.ps1`

---

### Issue: "Authentication failed"

**Symptom**: Tests fail with "Access denied" or "401 Unauthorized"

**Solution**:
1. Verify test users exist in AuthServer
2. Check credentials are correct (alice/password)
3. Verify AuthServer is running and accessible
4. Clear browser cache if testing via UI

---

### Issue: "No test data found"

**Symptom**: Tests fail because no quotes/users exist

**Solution**: Run seed scripts:
```powershell
.\Scripts\seed-affiliates-drivers.ps1
.\Scripts\seed-quotes.ps1
```

---

## ?? CI/CD Integration

### GitHub Actions Example

```yaml
name: AdminPortal Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Start AuthServer
      run: |
        cd AuthServer
        dotnet run &
        Start-Sleep -Seconds 10
    
    - name: Start AdminAPI
      run: |
        cd AdminAPI
        dotnet run &
        Start-Sleep -Seconds 10
    
    - name: Start AdminPortal
      run: |
        cd AdminPortal
        dotnet run &
        Start-Sleep -Seconds 10
    
    - name: Run Test Suite
      run: |
        .\Scripts\test-adminportal-complete.ps1
    
    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: Scripts/test-results-*.json
```

---

### Azure DevOps Pipeline Example

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.0.x'

- task: PowerShell@2
  displayName: 'Run AdminPortal Test Suite'
  inputs:
    filePath: 'Scripts/test-adminportal-complete.ps1'
    arguments: '-Verbose'
    errorActionPreference: 'continue'
    failOnStderr: true

- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '**/test-results-*.json'
    testRunTitle: 'AdminPortal Automated Tests'
```

---

## ?? Continuous Improvement

### Adding New Tests

To add a new test to the suite:

**Step 1**: Create your test script in `Scripts/`
```powershell
# Example: Scripts/test-my-new-feature.ps1
param(
    [string]$AdminAPIUrl = "https://localhost:5206"
)

# Your test logic here
# Exit 0 for success, 1 for failure
exit 0
```

**Step 2**: Add it to the master suite
```powershell
# In test-adminportal-complete.ps1

Invoke-TestScript `
    -TestName "My New Feature Test" `
    -ScriptPath "$PSScriptRoot\test-my-new-feature.ps1" `
    -Parameters @{ AdminAPIUrl = $AdminAPIUrl } `
    -Category "Phase X: Feature Name"
```

**Step 3**: Update this README
- Add to "What Gets Tested" section
- Document any prerequisites
- Add troubleshooting tips if needed

---

## ?? Support

**Issues or Questions?**
- Check `Scripts/README-Phase2-Testing.md` for detailed test documentation
- Review `Docs/31-Scripts-Reference.md` for script reference
- Check test output JSON for detailed error messages

**Common Questions**:

**Q: How long should the test suite take?**  
A: Typically 10-20 seconds for all automated tests

**Q: Can I run tests in parallel?**  
A: Not recommended - tests may interfere with each other's state

**Q: What if a test is flaky?**  
A: Run with `-Verbose` to see detailed output, check server logs

**Q: Can I run individual tests?**  
A: Yes, run the specific test script directly:
```powershell
.\Scripts\test-phase2-jwt-decoding.ps1
```

---

## ?? Test Coverage Matrix

| Feature | Phase | Test Script | Coverage |
|---------|-------|-------------|----------|
| API Health | 1 | test-api-connection.ps1 | ? 100% |
| JWT Decoding | 2 | test-phase2-jwt-decoding.ps1 | ? 100% |
| Token Refresh | 2 | test-phase2-token-refresh.ps1 | ? 100% |
| User Management | 2 | test-phase2-user-management.ps1 | ? 100% |
| 403 Handling | 2 | test-phase2-403-handling.ps1 | ? 100% |
| Quote Lifecycle | B | test-phase-b-quote-lifecycle.ps1 | ? 100% |
| Driver Tracking | 3 | *(Future)* | ? Coming Soon |
| Audit Logs | 3 | *(Future)* | ? Coming Soon |

---

## ?? Success Criteria

**Test suite passes if**:
- ? All automated tests return exit code 0
- ? Pass rate >= 100%
- ? No critical errors in test output
- ? All servers remain responsive after testing

**Deploy to production if**:
- ? Test suite passes
- ? Manual smoke tests complete
- ? Code review approved
- ? Documentation updated

---

## ?? Future Enhancements

**Planned Additions**:
- [ ] Driver tracking SignalR tests
- [ ] Audit log verification tests
- [ ] Performance benchmarking tests
- [ ] Load testing (concurrent users)
- [ ] Database state validation
- [ ] Email notification tests
- [ ] PDF generation tests
- [ ] Report generation tests

---

## ?? Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-03 | Initial release with Phase 1, 2, and B tests |

---

**Last Updated**: February 3, 2026  
**Maintainer**: Bellwood Platform Team  
**Status**: ? Production Ready

---

*This test suite provides comprehensive automated testing for the AdminPortal. Run before every deployment to ensure quality!* ??
