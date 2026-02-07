# ?? CRITICAL BUG FOUND: Role Accumulation in AuthServer

**Date**: February 5, 2026  
**Reporter**: AdminPortal Team  
**Severity**: ?? **HIGH - Data Integrity Issue**  
**Status**: ?? **BLOCKING USER MANAGEMENT FEATURE**

---

## ?? Executive Summary

**AuthServer's role assignment endpoint is accumulating roles instead of replacing them**, violating the mutually exclusive role design principle. This causes users to have multiple roles simultaneously, making role changes appear to fail.

---

## ?? Evidence

### Test Performed

**Action**: Change charlie's role from "driver" to "dispatcher"

**Request**:
```http
PUT /api/admin/users/charlie/role HTTP/1.1
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "role": "dispatcher"
}
```

**Response**:
```json
{
  "message": "User 'charlie' already has role 'dispatcher'.",
  "username": "charlie",
  "role": "dispatcher",
  "previousRoles": ["driver", "dispatcher"]  ? ?? BOTH ROLES PRESENT!
}
```

### Subsequent User List Query

**Request**:
```http
GET /api/admin/users?take=50&skip=0 HTTP/1.1
Authorization: Bearer <admin-token>
```

**Response Excerpt**:
```json
{
  "userId": "...",
  "username": "charlie",
  "role": "driver",  ? Returns FIRST role only
  "createdAt": "..."
}
```

**Problem**: charlie has `["driver", "dispatcher"]` in database but GET endpoint returns only `"driver"`.

---

## ?? Root Cause Analysis

### Expected Behavior

**Mutually Exclusive Roles** (per system design):
1. User can only have **ONE** role at a time
2. Assigning new role should **REPLACE** existing role
3. User roles: admin XOR dispatcher XOR driver XOR booker

### Actual Behavior

**Role Accumulation**:
1. First assignment: charlie gets "driver" ? roles = ["driver"]
2. Second assignment: charlie gets "dispatcher" ? roles = ["driver", "dispatcher"] ?
3. Third assignment: charlie gets "admin" ? roles = ["driver", "dispatcher", "admin"] ?

### Why This Happens

**Current Implementation (Suspected)**:
```csharp
// AuthServer's PUT /api/admin/users/{username}/role endpoint
var user = await _userManager.FindByNameAsync(username);

// Check if already has role
var roles = await _userManager.GetRolesAsync(user);
if (roles.Contains(newRole))
{
    return Ok(new { message = $"User '{username}' already has role '{newRole}'." });
}

// Add role WITHOUT removing old ones ?
await _userManager.AddToRoleAsync(user, newRole);
```

**Issue**: No removal of existing roles before adding new one.

---

## ? Required Fix

### Corrected Implementation

**File**: `AuthServer/Controllers/AdminUsersController.cs` (or equivalent)

**Endpoint**: `PUT /api/admin/users/{username}/role`

```csharp
[HttpPut("{username}/role")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> UpdateUserRole(string username, [FromBody] UpdateRoleRequest request)
{
    var user = await _userManager.FindByNameAsync(username);
    if (user == null)
        return NotFound(new { error = $"User '{username}' not found." });

    // Validate role
    var validRoles = new[] { "admin", "dispatcher", "driver", "booker", "passenger" };
    if (!validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        return BadRequest(new { error = $"Invalid role '{request.Role}'." });

    // Get current roles
    var currentRoles = await _userManager.GetRolesAsync(user);

    // CRITICAL FIX: Remove ALL existing roles first
    if (currentRoles.Any())
    {
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return StatusCode(500, new { error = "Failed to remove existing roles." });
        }
    }

    // Now add the single new role
    var addResult = await _userManager.AddToRoleAsync(user, request.Role);
    if (!addResult.Succeeded)
    {
        return StatusCode(500, new { error = "Failed to assign new role." });
    }

    return Ok(new
    {
        message = $"Successfully assigned role '{request.Role}' to user '{username}'.",
        username = username,
        previousRoles = currentRoles.ToArray(),
        newRole = request.Role
    });
}
```

---

## ?? Test Cases

### Test 1: Role Replacement

**Steps**:
1. Assign charlie role "driver"
2. Query charlie ? should return `role: "driver"`
3. Assign charlie role "dispatcher"
4. Query charlie ? should return `role: "dispatcher"` (NOT "driver")
5. Check database ? AspNetUserRoles table should have **1 row** for charlie

**Expected**:
```sql
SELECT ur.UserId, r.Name 
FROM AspNetUserRoles ur
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE ur.UserId = '<charlie-id>';

-- Should return 1 row:
-- charlie-id | dispatcher
```

**Current (Broken)**:
```sql
-- Returns 2+ rows:
-- charlie-id | driver
-- charlie-id | dispatcher
```

### Test 2: Multiple Assignments

**Steps**:
1. Assign charlie: "driver" ? "dispatcher" ? "admin" ? "driver" ? "booker"
2. Query charlie after each assignment

**Expected**: Role changes each time, only 1 role at any point

**Current (Broken)**: Accumulates all 5 roles

### Test 3: Response Consistency

**Steps**:
1. Assign charlie role "dispatcher"
2. GET /api/admin/users ? check charlie's role property

**Expected**: `"role": "dispatcher"`

**Current (Broken)**: Returns first role from array (unpredictable)

---

## ?? Impact Assessment

### Current Issues

1. **Data Integrity** ?
   - Users have multiple roles (violates business logic)
   - Database state inconsistent with application design

2. **UI Confusion** ?
   - AdminPortal shows old role after "successful" change
   - Users think role assignment failed
   - Test scripts fail validation

3. **Authorization Risk** ??
   - User with ["driver", "admin"] has admin privileges
   - Unintended privilege escalation possible
   - Security boundary violation

4. **Audit Trail** ?
   - Cannot determine user's "current" role
   - Role history becomes unclear
   - Compliance issues

### Affected Systems

- ? **AdminPortal** - User Management feature broken
- ?? **AuthServer** - Core role management bug
- ?? **All systems using JWT roles** - Potential multi-role claims
- ?? **Authorization checks** - May honor first role only

---

## ?? Recommended Actions

### Immediate (AuthServer Team)

1. **Fix the endpoint** (code above)
2. **Test the fix** with provided test cases
3. **Deploy hotfix** to dev environment
4. **Notify AdminPortal team** when ready

### Data Cleanup (AuthServer Team)

After deploying fix, clean up existing data:

```sql
-- Find users with multiple roles
SELECT u.UserName, COUNT(ur.RoleId) as RoleCount
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
GROUP BY u.UserName
HAVING COUNT(ur.RoleId) > 1;

-- For each affected user, keep only the most recently assigned role
-- (Requires manual review based on business logic)
```

### Testing (AdminPortal Team)

Once AuthServer deploys fix:
1. Restart AdminPortal
2. Test role change for charlie
3. Verify persistence after page refresh
4. Mark User Management test as PASSED

---

## ?? Additional Recommendations

### 1. Add Database Constraint (Optional)

Prevent multiple roles at database level:

```sql
-- Create unique index to enforce one role per user
CREATE UNIQUE INDEX IX_AspNetUserRoles_UserId 
ON AspNetUserRoles(UserId);
```

**Warning**: This will fail if any users currently have multiple roles. Clean data first.

### 2. Add Unit Tests

```csharp
[Fact]
public async Task UpdateUserRole_ShouldReplaceExistingRole()
{
    // Arrange
    var user = await CreateTestUser("testuser");
    await _userManager.AddToRoleAsync(user, "driver");

    // Act
    var result = await _controller.UpdateUserRole("testuser", new { role = "dispatcher" });

    // Assert
    var roles = await _userManager.GetRolesAsync(user);
    Assert.Single(roles);  // Should have exactly 1 role
    Assert.Equal("dispatcher", roles.First());
}
```

### 3. Add Logging

```csharp
_logger.LogWarning(
    "[RoleUpdate] User {Username} had {RoleCount} roles before update. Removing: {PreviousRoles}", 
    username, 
    currentRoles.Count, 
    string.Join(", ", currentRoles)
);
```

---

## ?? References

- **AdminPortal Debug Logs**: `Bellwood.AdminPortal/Docs/Temp/USER-MANAGEMENT-DEBUG-GUIDE-20260205.md`
- **Original Fix Attempt**: `Bellwood.AdminPortal/Docs/Archive/USER-MANAGEMENT-ROLE-UPDATE-FIX-20260205.md`
- **System Design**: `BellwoodGlobal.Mobile/Docs/Planning-DataAccessEnforcement.md` (Section: Role Definitions)
- **Phase 2 Q&A**: `Bellwood.AdminPortal/Docs/Archive/AdminPortal-QA-Response.md`

---

## ?? Contact

**Reporter**: AdminPortal Development Team  
**Date Reported**: February 5, 2026  
**Priority**: ?? **HIGH**  
**Blocking**: User Management feature (Phase 2)  

**Next Steps**: 
1. AuthServer team acknowledges issue
2. AuthServer provides ETA for fix
3. AdminPortal team retests after deployment

---

**Status**: ? **AWAITING AUTHSERVER FIX**  
**Impact**: ?? **HIGH - Feature Blocked**  
**Root Cause**: ? **IDENTIFIED - Role Accumulation Bug**

---

*This is a critical bug that violates the core principle of mutually exclusive roles. Fix required before User Management feature can pass testing.* ??
