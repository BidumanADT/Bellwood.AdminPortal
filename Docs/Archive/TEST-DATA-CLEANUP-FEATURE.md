# Test Data Cleanup Feature - Implementation Summary

**Date**: February 4, 2026  
**Feature**: Optional test data cleanup before running test suite  
**Status**: ? **IMPLEMENTED**

---

## ?? Problem Solved

**Issue**: Test scripts seed data (affiliates, drivers, bookings, quotes) during execution. Running tests multiple times can create duplicate or inconsistent data, leading to test failures or unreliable results.

**Solution**: Added optional `-ClearTestData` parameter to the master test script that clears all test affiliates and drivers before running tests, ensuring a clean slate.

---

## ?? Implementation Details

### Changes Made

**File Modified**: `Scripts/test-adminportal-complete.ps1`

**Added Parameter**:
```powershell
param(
    # ... existing parameters
    [switch]$ClearTestData  # NEW: Clear test data before running tests
)
```

**Added Cleanup Step** (Step 0):
```powershell
# STEP 0: Clear Previous Test Data (Optional - Controlled by Parameter)

if ($ClearTestData) {
    # 1. Authenticate as admin (alice)
    # 2. Fetch all affiliates from AdminAPI
    # 3. Delete each affiliate (cascade deletes drivers)
    # 4. Report results
}
```

---

## ?? Usage

### Recommended Usage (Clean Slate)

```powershell
# Clear test data before running
.\test-adminportal-complete.ps1 -ClearTestData
```

**What Happens**:
1. ? Script authenticates as alice
2. ? Fetches all affiliates from AdminAPI
3. ? Deletes all affiliates (and cascade-deleted drivers)
4. ? Runs all tests (which will seed fresh data)
5. ? Tests run against clean, consistent data

**Output Example**:
```
????????????????????????????????????????????????????????????????????
?  STEP 0: Clearing Previous Test Data                            ?
????????????????????????????????????????????????????????????????????

??  This will delete all test affiliates and drivers...
  ? Authenticated for cleanup
  Found 3 affiliate(s) to delete...
    ? Deleted: Chicago Limo Service (and 2 driver(s))
    ? Deleted: Suburban Chauffeurs (and 1 driver(s))
    ? Deleted: Downtown Express (and 1 driver(s))
  ? Cleanup complete!
```

---

### Standard Usage (No Cleanup)

```powershell
# Run tests without clearing data (default)
.\test-adminportal-complete.ps1
```

**What Happens**:
1. ??  Cleanup step skipped (Step 0 not executed)
2. ? Tests run against existing data
3. ??  May encounter duplicate data or inconsistent state

---

## ?? Test Data Lifecycle

### What Gets Seeded During Tests

**Affiliates & Drivers**:
- Seeded by: `seed-affiliates-drivers.ps1` (called during tests)
- Endpoint: `POST /dev/seed-affiliates` + manual affiliate/driver creation
- Data: Chicago Limo Service, Suburban Chauffeurs, Downtown Express

**Bookings**:
- Seeded by: `seed-admin-api.ps1` (if called)
- Endpoint: `POST /bookings/seed`
- Data: 3 test bookings with various statuses

**Quotes**:
- Seeded by: `seed-quotes.ps1` (if called)
- Endpoint: `POST /quotes/seed`
- Data: Test quote requests

---

### What Gets Cleaned Up

**With `-ClearTestData`**:
- ? All affiliates (from AdminAPI)
- ? All drivers (cascade deleted with affiliates)
- ? Bookings (historical data preserved)
- ? Quotes (historical data preserved)

**Rationale**: Bookings and quotes are not deleted because:
1. They represent historical transactions
2. No duplicate key conflicts
3. Useful for testing data accumulation
4. Easier to inspect after test runs

---

## ?? When to Use -ClearTestData

### ? **Use `-ClearTestData` When**:

**First-time test run**:
```powershell
.\test-adminportal-complete.ps1 -ClearTestData
```

**Previous test run failed/interrupted**:
```powershell
# Clean up partial state
.\test-adminportal-complete.ps1 -ClearTestData
```

**Testing data consistency**:
```powershell
# Ensure no leftover data from previous runs
.\test-adminportal-complete.ps1 -ClearTestData
```

**Before important demos**:
```powershell
# Clean slate for presentations
.\test-adminportal-complete.ps1 -ClearTestData
```

---

### ? **Don't Use `-ClearTestData` When**:

**Rapid iteration testing**:
```powershell
# Run multiple times quickly
.\test-adminportal-complete.ps1  # No cleanup
.\test-adminportal-complete.ps1  # No cleanup
.\test-adminportal-complete.ps1  # No cleanup
```

**Testing against existing data**:
```powershell
# Want to keep current affiliates/drivers
.\test-adminportal-complete.ps1  # No cleanup
```

**Debugging specific data issues**:
```powershell
# Need to preserve current state
.\test-adminportal-complete.ps1  # No cleanup
```

---

## ?? Technical Details

### Authentication for Cleanup

**Login Credentials**:
- Username: `alice`
- Password: `password`
- Role: `admin`

**Why Admin Role**:
- Only admins can delete affiliates
- Cleanup requires DELETE /affiliates/{id} endpoint
- Non-admin users would get 403 Forbidden

### Error Handling

**If Authentication Fails**:
```
??  Failed to authenticate for cleanup: [error]
```
- Script continues to tests
- Cleanup skipped (non-fatal)

**If Affiliate Deletion Fails**:
```
??  Failed to delete: Downtown Express
```
- Logs warning
- Continues deleting other affiliates
- Test suite proceeds

**If No Test Data Exists**:
```
??  No test data to clear
```
- No action taken
- Tests proceed normally

---

## ??? Configuration

### Default Behavior (No Cleanup)

**Without parameter**:
```powershell
.\test-adminportal-complete.ps1
```

**Equivalent to**:
```powershell
.\test-adminportal-complete.ps1 -ClearTestData:$false
```

### Explicit Cleanup

**With parameter**:
```powershell
.\test-adminportal-complete.ps1 -ClearTestData
```

**Equivalent to**:
```powershell
.\test-adminportal-complete.ps1 -ClearTestData:$true
```

---

## ?? Related Scripts

### Manual Cleanup Script

**If you want to ONLY clear data (no tests)**:
```powershell
.\clear-test-data.ps1
```

**Prompts for confirmation**:
- Type "YES" to confirm deletion
- Shows warning about data loss
- No tests are run

### Cleanup + Re-seed Workflow

**Manual workflow (outside test suite)**:
```powershell
# 1. Clear all data
.\clear-test-data.ps1
# Type "YES" to confirm

# 2. Seed fresh data
.\seed-affiliates-drivers.ps1

# 3. Optionally seed bookings/quotes
.\seed-admin-api.ps1
.\seed-quotes.ps1
```

**Automated workflow (test suite)**:
```powershell
# All-in-one: Clean + Seed + Test
.\test-adminportal-complete.ps1 -ClearTestData
```

---

## ? Testing the Cleanup Feature

### Test Scenario 1: Clean Slate

**Steps**:
```powershell
# 1. Seed some data manually
.\seed-affiliates-drivers.ps1

# 2. Run tests WITH cleanup
.\test-adminportal-complete.ps1 -ClearTestData

# 3. Verify cleanup happened
# Should see "Deleted: X affiliate(s)" in output
```

**Expected**:
- ? Cleanup step executes
- ? Affiliates deleted
- ? Tests run with fresh data

---

### Test Scenario 2: No Cleanup

**Steps**:
```powershell
# 1. Seed some data manually
.\seed-affiliates-drivers.ps1

# 2. Run tests WITHOUT cleanup
.\test-adminportal-complete.ps1

# 3. Check for duplicate data issues
```

**Expected**:
- ??  Cleanup step skipped
- ??  May see duplicate affiliate errors
- ??  Data accumulates across runs

---

## ?? Best Practices

### Recommended Workflow

**Daily Development**:
```powershell
# Morning: Start fresh
.\test-adminportal-complete.ps1 -ClearTestData

# During day: Quick tests (no cleanup)
.\test-adminportal-complete.ps1

# End of day: Clean up
.\clear-test-data.ps1
```

**Before Commits**:
```powershell
# Always use clean slate for final verification
.\test-adminportal-complete.ps1 -ClearTestData -Verbose
```

**CI/CD Pipeline**:
```yaml
# Always use -ClearTestData in automated builds
steps:
  - name: Run Tests
    run: .\test-adminportal-complete.ps1 -ClearTestData
```

---

## ?? Summary

**Feature**: Optional test data cleanup  
**Parameter**: `-ClearTestData`  
**Default**: No cleanup (backward compatible)  
**Cleanup Scope**: Affiliates and drivers only  
**Safety**: Non-fatal errors, continues to tests  
**Recommendation**: Use `-ClearTestData` for clean-slate testing

**Files Modified**:
- ? `Scripts/test-adminportal-complete.ps1` - Cleanup logic
- ? `Scripts/README-Complete-Test-Suite.md` - Documentation

**Ready for Use**: ? **YES**

---

**Last Updated**: February 4, 2026  
**Status**: ?? **READY FOR TESTING**

---

*This feature ensures test runs start with a clean, consistent data state, improving test reliability and reducing false positives from leftover test data.*
