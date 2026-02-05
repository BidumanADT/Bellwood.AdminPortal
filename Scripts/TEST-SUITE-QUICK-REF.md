# AdminPortal Test Suite - Quick Reference Card

**Version**: 1.0 | **Date**: February 3, 2026

---

## ?? Quick Commands

```powershell
# Run complete test suite (recommended)
.\Scripts\test-adminportal-complete.ps1

# Run with verbose output (debugging)
.\Scripts\test-adminportal-complete.ps1 -Verbose

# Stop on first failure (faster feedback)
.\Scripts\test-adminportal-complete.ps1 -StopOnFailure

# Skip server checks (when servers already verified)
.\Scripts\test-adminportal-complete.ps1 -SkipServerCheck
```

---

## ?? Test Categories

| Category | Tests | Duration |
|----------|-------|----------|
| **Phase 1: Core** | API connectivity, health checks | ~2s |
| **Phase 2: Auth** | JWT, token refresh, 403 handling | ~5s |
| **Phase 2: Users** | User management, role assignment | ~3s |
| **Phase B: Quotes** | Quote lifecycle workflow | ~4s |
| **Total** | 6 automated tests | ~15s |

---

## ?? Prerequisites Checklist

- [ ] AuthServer running on `https://localhost:5001`
- [ ] AdminAPI running on `https://localhost:5206`
- [ ] AdminPortal running on `https://localhost:7257`
- [ ] Test users exist (alice, bob, diana, charlie)
- [ ] Test data seeded (run `seed-*.ps1` scripts)

---

## ?? Common Issues

| Issue | Quick Fix |
|-------|-----------|
| Server not responding | Start servers: `dotnet run` in each project |
| Authentication failed | Verify users exist, credentials correct |
| No test data | Run: `.\Scripts\seed-quotes.ps1` |
| Script not found | Ensure in `Scripts/` directory |
| SSL errors | Scripts handle this automatically |

---

## ?? Success Indicators

? **Green** = All tests passed (100%)  
?? **Yellow** = Some tests skipped  
? **Red** = Tests failed (check output)

**Exit Codes**:
- `0` = Success (all tests passed)
- `1` = Failure (one or more tests failed)

---

## ?? Output Files

- **JSON Report**: `Scripts/test-results-YYYYMMDD-HHmmss.json`
- **Console Output**: Real-time test progress
- **Summary**: Displayed at end of test run

---

## ?? Quick Start (3 Steps)

**Step 1**: Start all servers
```powershell
# Start each in separate terminal
dotnet run  # In AuthServer, AdminAPI, AdminPortal
```

**Step 2**: Seed test data
```powershell
.\Scripts\seed-quotes.ps1
```

**Step 3**: Run tests
```powershell
.\Scripts\test-adminportal-complete.ps1
```

---

## ?? Pro Tips

- Use `-Verbose` when debugging failures
- Check JSON output for detailed error info
- Run individual tests for faster iteration
- Use `-StopOnFailure` to fail fast during development

---

**More Info**: See `Scripts/README-Complete-Test-Suite.md`
