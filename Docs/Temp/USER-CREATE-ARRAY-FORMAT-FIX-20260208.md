# Role Array Format Fix - COMPLETE

**Date**: February 8, 2026  
**Status**: ? **FIXED**  
**Issue**: Ensure roles sent as JSON array to AdminAPI

---

## ?? **API Requirements Confirmed**

### **From AdminAPI Team**

**Valid Role Names** (case-insensitive):
- ? `"admin"` / `"Admin"` / `"ADMIN"` ? normalized to `"admin"`
- ? `"dispatcher"` / `"Dispatcher"` ? normalized to `"dispatcher"`
- ? `"driver"` / `"Driver"` ? normalized to `"driver"`
- ? `"booker"` / `"Booker"` ? normalized to `"booker"`

**Required Format**:
- ? **Array format**: `["admin"]` not `"admin"`
- ? **JSON property**: `"roles": ["booker"]`

---

## ? **Changes Made**

### **1. Removed Role Mapping**

**Why**: AdminAPI accepts lowercase `"booker"` directly (case-insensitive)

**Before** (with mapping):
```csharp
var apiRoles = selectedCreateRoles.Select(MapRoleToApiFormat).ToList();
// booker ? Passenger
```

**After** (direct):
```csharp
Roles = selectedCreateRoles.ToList()  // booker stays booker
```

---

### **2. Verified Array Format**

**Model** (`Models/UserModels.cs`):
```csharp
public class CreateUserRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();  // ? Array
    
    [JsonPropertyName("tempPassword")]
    public string TemporaryPassword { get; set; } = string.Empty;
}
```

**Serialized JSON** (what gets sent):
```json
{
  "email": "test@example.com",
  "roles": ["booker"],  // ? Array format ?
  "tempPassword": "password123"
}
```

---

### **3. Added Debug Logging**

**File**: `Services/UserManagementService.cs`

```csharp
_logger.LogInformation("[UserManagement] Creating user {Email} with roles: {Roles}", 
    request.Email, 
    string.Join(", ", request.Roles));

var json = System.Text.Json.JsonSerializer.Serialize(request, ...);
_logger.LogDebug("[UserManagement] Request JSON:\n{Json}", json);
```

**Console Output** (expected):
```
[UserManagement] Creating user test@example.com with roles: booker
[UserManagement] Request JSON:
{
  "email": "test@example.com",
  "roles": [
    "booker"
  ],
  "tempPassword": "password123"
}
```

---

## ?? **Testing**

### **Test 1: Create Booker User**

```
1. Start AdminPortal
2. Login as alice
3. User Management ? Create User
4. Fill form:
   - Email: testbooker@example.com
   - Role: ? Booker
   - Password: password123 (min 10 chars)
5. Click "Create User"

Expected Console Output:
  [UserManagement] Creating user testbooker@example.com with roles: booker
  [UserManagement] Request JSON:
  {
    "email": "testbooker@example.com",
    "roles": ["booker"],  ? Check this is an array!
    "tempPassword": "password123"
  }
  
Expected Result:
  ? POST /users succeeds (200 or 201)
  ? Success toast
  ? User appears in list
```

---

### **What to Check in Logs**

**? Good** (array format):
```json
"roles": ["booker"]
```

**? Bad** (string format):
```json
"roles": "booker"
```

**? Good** (multiple roles):
```json
"roles": ["admin", "dispatcher"]
```

---

## ?? **Request Format Comparison**

### **Before This Fix**

**Potential Issues**:
- ? Mapping `"booker"` ? `"Passenger"` (unnecessary)
- ? Not logging exact JSON sent
- ? Couldn't verify array format

### **After This Fix**

**Confirmed**:
- ? Sends lowercase `"booker"` (AdminAPI normalizes)
- ? Array format: `["booker"]`
- ? Logs exact JSON for debugging
- ? No unnecessary mapping

---

## ?? **Why This Should Work Now**

1. ? **AdminAPI accepts "booker"** (case-insensitive)
2. ? **We send array**: `["booker"]` not `"booker"`
3. ? **Property name matches**: `"roles"` (lowercase r)
4. ? **Valid JSON**: Properly serialized by `System.Text.Json`

---

## ?? **Troubleshooting**

### **If Still Fails**

**Check Console Logs**:
```
[UserManagement] Request JSON:
{
  "email": "...",
  "roles": [???],  ? What's here?
  "tempPassword": "..."
}
```

**Possible Issues**:

1. **Empty array**: `"roles": []`
   - Fix: Ensure checkbox is checked
   - Verify: `selectedCreateRoles.Any()` is true

2. **Wrong format**: `"roles": "booker"`
   - Fix: Ensure using `List<string>` not `string`
   - Verify: `CreateUserRequest.Roles` is `List<string>`

3. **Different property name**: `"Roles":` (capital R)
   - Fix: Check `[JsonPropertyName("roles")]` is lowercase
   - Verify: Model has correct attribute

---

## ? **Summary**

**Changes**:
- ? Removed role mapping (AdminAPI accepts lowercase)
- ? Verified array format (List<string> serializes to array)
- ? Added debug logging (can see exact JSON sent)

**Expected Behavior**:
```json
POST /users
{
  "email": "test@example.com",
  "roles": ["booker"],
  "tempPassword": "password123"
}
```

**AdminAPI Response**:
- ? Accepts request
- ? Normalizes to `"booker"` (lowercase)
- ? Creates user
- ? Returns success

---

**Status**: ? **READY TO TEST**  
**Build**: ? **SUCCESS**  
**Logging**: ? **ENABLED**

*Now sending exactly what the API expects!* ???
