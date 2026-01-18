# Admin Portal Team - AuthServer Phase 2 Q&A

**Date:** January 18, 2026  
**From:** AuthServer Team  
**To:** Admin Portal Team  
**Re:** Phase 2 User Management Endpoints & Diana User

---

## Question 1: User Management Endpoints

### ✅ **Answer: Partially - Here's What Exists**

---

### **Endpoint 1: List All Users**

**❌ Does NOT exist:** `GET /api/admin/users`

**✅ What DOES exist:**
- `GET /api/admin/users/drivers` - Lists users with driver role only
- `GET /dev/user-info/{username}` - Gets specific user info (dev/diagnostic)

**Current Response Format (GET /api/admin/users/drivers):**
```json
[
  {
    "userId": "a1b2c3d4-...",
    "username": "charlie",
    "userUid": "driver-001"
  }
]
```

**⚠️ Gap Identified:** No endpoint to list ALL users across ALL roles.

---

### **Endpoint 2: Change User Role**

**✅ EXISTS:** `PUT /api/admin/users/{username}/role`

**Location:** Minimal API endpoint in `Program.cs` (line ~460)

**Authorization:** Requires `AdminOnly` policy ✅

**Request Format:**
```http
PUT /api/admin/users/alice/role
Authorization: Bearer <admin-jwt>
Content-Type: application/json

{
  "role": "dispatcher"
}
```

**Actual Response Format:**
```json
{
  "message": "Successfully assigned role 'dispatcher' to user 'alice'.",
  "username": "alice",
  "previousRoles": ["admin"],
  "newRole": "dispatcher"
}
```

**Differences from Expected:**
| Expected | Actual | Notes |
|----------|--------|-------|
| `{ "success": true }` | `{ "message": "..." }` | More descriptive |
| `"newRole"` | ✅ `"newRole"` | Matches |
| No previous roles | ✅ `"previousRoles"` array | Extra info |

**Valid Roles:**
- `admin`
- `dispatcher`
- `booker`
- `driver`

**Error Responses:**

**400 Bad Request (Invalid Role):**
```json
{
  "error": "Invalid role 'invalidrole'. Valid roles are: admin, dispatcher, booker, driver"
}
```

**404 Not Found (User Not Found):**
```json
{
  "error": "User 'unknown' not found."
}
```

**Already Has Role:**
```json
{
  "message": "User 'alice' already has role 'dispatcher'.",
  "username": "alice",
  "role": "dispatcher",
  "previousRoles": ["dispatcher"]
}
```

---

## Question 2: Does Diana Exist?

### ✅ **Answer: YES - Diana Exists with Dispatcher Role**

**User Details:**
- **Username:** `diana`
- **Password:** `password`
- **Role:** `dispatcher`
- **Email:** `diana.dispatcher@bellwood.example`
- **Email Confirmed:** `true`

**Created By:** Phase 2 seeding (activated in Program.cs)

**Seeding Code Location:** `Data/Phase2RolePreparation.cs`

**When Created:** On application startup (after Phase 2 activation)

**Verification:**

You can verify Diana exists by calling:

**Option 1: Login**
```bash
curl -X POST https://localhost:5001/login \
  -H "Content-Type: application/json" \
  -d '{"username":"diana","password":"password"}'
```

**Expected Response:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "token": "eyJ..."
}
```

**JWT Payload:**
```json
{
  "sub": "diana",
  "uid": "guid-xxx...",
  "userId": "guid-xxx...",
  "role": "dispatcher",
  "email": "diana.dispatcher@bellwood.example",
  "exp": 1704996000
}
```

**Option 2: Diagnostic Endpoint**
```bash
curl https://localhost:5001/dev/user-info/diana
```

**Expected Response:**
```json
{
  "userId": "guid-xxx...",
  "username": "diana",
  "email": "diana.dispatcher@bellwood.example",
  "roles": ["dispatcher"],
  "userClaims": [
    {
      "type": "email",
      "value": "diana.dispatcher@bellwood.example"
    }
  ],
  "jwtClaimsPreview": [...],
  "diagnostics": {
    "hasDriverRole": false,
    "hasCustomUid": false,
    "identityGuid": "guid-xxx...",
    "hasEmail": true,
    "phase1Ready": true
  }
}
```

---

## 🔧 Recommendations for Admin Portal

### **Short-Term Solution (Use Existing Endpoints)**

**For User Management UI:**

1. **List Users:** Use `GET /dev/user-info/{username}` for individual users
   - **Limitation:** No bulk list endpoint yet
   - **Workaround:** Maintain known user list or add endpoint (see below)

2. **Change Role:** Use `PUT /api/admin/users/{username}/role` ✅
   - **Works perfectly** for your needs
   - Handle response format differences in your code

3. **Test with Diana:** ✅ Already exists, ready to test

---

### **Recommended: Add Missing Endpoint**

**New Endpoint Needed:** `GET /api/admin/users`

**Suggested Implementation:**

```csharp
// In AdminUsersController.cs

/// <summary>
/// Gets all users with their roles.
/// PHASE 2: Admin-only endpoint for user management.
/// </summary>
[HttpGet]
public async Task<IActionResult> GetAllUsers()
{
    var allUsers = _userManager.Users.ToList();
    var result = new List<UserInfo>();

    foreach (var user in allUsers)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var emailClaim = claims.FirstOrDefault(c => c.Type == "email");

        result.Add(new UserInfo
        {
            Username = user.UserName!,
            UserId = user.Id,
            Role = roles.FirstOrDefault() ?? "none", // Single role due to mutually exclusive strategy
            Email = emailClaim?.Value ?? user.Email ?? "",
            CreatedAt = user.LockoutEnd?.DateTime ?? DateTime.UtcNow // Placeholder - no creation date stored
        });
    }

    return Ok(result);
}

public class UserInfo
{
    public string Username { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

**Response Format:**
```json
[
  {
    "username": "alice",
    "userId": "a1b2c3d4-...",
    "role": "admin",
    "email": "alice@example.com",
    "createdAt": "2025-01-01T00:00:00Z"
  },
  {
    "username": "diana",
    "userId": "d1e2f3g4-...",
    "role": "dispatcher",
    "email": "diana.dispatcher@bellwood.example",
    "createdAt": "2026-01-13T00:00:00Z"
  }
]
```

**Note:** ASP.NET Core Identity doesn't track user creation date by default. The above uses `LockoutEnd` as a placeholder. For accurate creation tracking, we'd need to extend IdentityUser or use a custom table.

---

## 📋 Summary

| Requirement | Status | Endpoint | Notes |
|-------------|--------|----------|-------|
| List all users | ❌ Missing | *(recommend adding)* | Only driver list exists |
| Change user role | ✅ Exists | `PUT /api/admin/users/{username}/role` | Works perfectly |
| Diana user exists | ✅ Yes | *(seeded on startup)* | Ready for testing |
| Diana has dispatcher role | ✅ Yes | *(verified in tests)* | Confirmed working |

---

## 🎯 Action Items

### **For AuthServer Team (Us):**
- [ ] Add `GET /api/admin/users` endpoint to AdminUsersController
- [ ] Consider adding user creation timestamp tracking
- [ ] Update documentation with new endpoint

### **For Admin Portal Team (You):**
- [x] Use `PUT /api/admin/users/{username}/role` for role changes
- [x] Use diana/password for dispatcher testing
- [ ] Adapt UI to current response format (or wait for us to add list endpoint)
- [ ] Test role assignment functionality
- [ ] Implement role-based UI hiding

---

## 🔍 Additional Endpoints Available

**For your reference, here are ALL admin endpoints currently available:**

### **Driver Management (AdminUsersController):**
- `POST /api/admin/users/drivers` - Create driver user
- `GET /api/admin/users/drivers` - List driver users
- `PUT /api/admin/users/{username}/uid` - Update driver UID
- `GET /api/admin/users/by-uid/{userUid}` - Get user by UID
- `DELETE /api/admin/users/drivers/{username}` - Delete driver user

### **Role Management (Minimal API):**
- `PUT /api/admin/users/{username}/role` - Assign role (admin-only)

### **Diagnostic (Dev Only):**
- `GET /dev/user-info/{username}` - Get detailed user info
- `POST /dev/seed-drivers` - Seed test drivers

### **Health:**
- `GET /health` - Health check
- `GET /healthz` - Health check (alternate)

**All admin endpoints require AdminOnly authorization** (except dev and health endpoints).

---

## 🧪 Testing Recommendations

### **Test Scenario 1: List Users**
**Workaround until list endpoint is added:**
```javascript
// Known test users
const testUsers = ['alice', 'bob', 'chris', 'charlie', 'diana'];

// Fetch each user individually
const users = await Promise.all(
  testUsers.map(username => 
    fetch(`https://localhost:5001/dev/user-info/${username}`)
      .then(r => r.json())
  )
);
```

### **Test Scenario 2: Change Role**
```javascript
// Change alice to dispatcher
const response = await fetch('https://localhost:5001/api/admin/users/alice/role', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${adminToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ role: 'dispatcher' })
});

const result = await response.json();
// result.newRole === 'dispatcher'
// result.previousRoles === ['admin']
```

### **Test Scenario 3: Login as Diana**
```javascript
const response = await fetch('https://localhost:5001/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'diana',
    password: 'password'
  })
});

const { token } = await response.json();
// Decode token - should contain role: 'dispatcher'
```

---

## 💡 Need Help?

**If you need:**
1. The `GET /api/admin/users` endpoint implemented quickly
2. Different response format for role assignment
3. Additional user management endpoints
4. Custom fields added to user data

**Contact:** AuthServer Team  
**Slack:** #authserver-phase2  
**Response Time:** < 1 business day

---

## ✅ Quick Reference

**Test Users Available:**

| Username | Password | Role | Email |
|----------|----------|------|-------|
| alice | password | admin | *(none)* |
| bob | password | admin | *(none)* |
| chris | password | booker | chris.bailey@example.com |
| charlie | password | driver | *(none)* |
| **diana** | **password** | **dispatcher** | **diana.dispatcher@bellwood.example** |

**API Base URL:** `https://localhost:5001`

**Authorization Header:**
```
Authorization: Bearer <jwt-token>
```

**AdminOnly Endpoints Require:**
- User must have `admin` role
- Dispatchers will receive 403 Forbidden

---

**Status:** ✅ **Ready for Integration**  
**Last Updated:** January 13, 2026  
**Document Version:** 1.0

---

*Let us know if you need the missing `GET /api/admin/users` endpoint and we can add it within a few hours!* 🚀
