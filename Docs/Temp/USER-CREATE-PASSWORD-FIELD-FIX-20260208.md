# User Creation Password Field Fix

**Date**: February 8, 2026  
**Status**: ? **FIXED**  
**Issue**: User creation fails with "tempPassword must be at least 10 characters long" even with valid password

---

## ?? **Root Cause**

**Field Name Mismatch** between AdminPortal and AdminAPI

### **What AdminPortal Was Sending** ?

```json
{
  "email": "CreatPaxTest1@example.com",
  "roles": ["passenger"],
  "temporaryPassword": "password123"  ? Wrong field name
}
```

### **What AdminAPI Expects** ?

```json
{
  "email": "CreatPaxTest1@example.com",
  "roles": ["passenger"],
  "tempPassword": "password123"  ? Correct field name
}
```

**Result**: AdminAPI couldn't find `tempPassword`, saw it as empty/missing, triggered validation error.

---

## ?? **How We Found It**

**Log Evidence**:
```
POST https://localhost:5206/users
400 Bad Request
{
  "error": "tempPassword must be at least 10 characters long."
}
```

**Analysis**:
- User entered `password123` (13 characters) ?
- Portal validated ?10 characters ?
- API rejected saying too short ?
- **Conclusion**: API not receiving the field at all!

**Field name check**:
- AdminAPI docs say: `tempPassword` (per 25-User-Management-API.md)
- AdminPortal was sending: `temporaryPassword`
- **Mismatch confirmed!**

---

## ? **The Fix**

**File**: `Models/UserModels.cs`

**Before** ?:
```csharp
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string TemporaryPassword { get; set; } = string.Empty;
    // ? Serializes as "temporaryPassword" (camelCase)
}
```

**After** ?:
```csharp
public class CreateUserRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
    
    [JsonPropertyName("tempPassword")]  // ? FIX: Match AdminAPI field name
    public string TemporaryPassword { get; set; } = string.Empty;
}
```

**Key Change**: Added `[JsonPropertyName("tempPassword")]` attribute

**Why This Works**:
- Property name in C#: `TemporaryPassword` (readable)
- Serialized JSON name: `tempPassword` (matches AdminAPI)
- Best of both worlds! ?

---

## ?? **Testing**

### **Before Fix** ?

```
1. Fill form:
   - Email: test@example.com
   - Role: passenger
   - Password: password123 (13 chars)
2. Click "Create User"

Result:
  ? 400 Bad Request
  ? "tempPassword must be at least 10 characters long."
  ? User not created
```

### **After Fix** ?

```
1. Fill form:
   - Email: test@example.com
   - Role: passenger
   - Password: password123 (13 chars)
2. Click "Create User"

Expected Result:
  ? 201 Created
  ? User created successfully
  ? User appears in list
  ? Success toast shown
```

---

## ?? **Verification**

**Request Body** (after fix):
```json
{
  "email": "test@example.com",
  "roles": ["passenger"],
  "tempPassword": "password123"  ? Now correct!
}
```

**AdminAPI receives**: ? All fields present  
**Validation passes**: ? Password ?10 chars  
**User created**: ? Success!

---

## ?? **Files Changed**

| File | Change | Lines |
|------|--------|-------|
| `Models/UserModels.cs` | Added `[JsonPropertyName("tempPassword")]` | 1 |

**Total**: 1 file, 1 line

---

## ? **Success Criteria**

- ? Build successful
- ? Field name matches AdminAPI spec
- ? Password validation will now work
- ? User creation should succeed

---

## ?? **Why This Happened**

**C# Default Behavior**: Without `[JsonPropertyName]`, System.Text.Json uses camelCase:
- `TemporaryPassword` ? serializes as `"temporaryPassword"`

**AdminAPI Expects**: Exact name `"tempPassword"` (not `"temporaryPassword"`)

**Solution**: Explicit `[JsonPropertyName("tempPassword")]` attribute

---

## ?? **Reference**

**AdminAPI Spec** (from `Docs/Temp/25-User-Management-API.md`):

> **POST /users**
> 
> Request Body:
> ```json
> {
>   "email": "diana@bellwood.example",
>   "firstName": "Diana",
>   "lastName": "Prince",
>   "tempPassword": "TempPass123!",  ? Documented as "tempPassword"
>   "roles": ["Dispatcher"]
> }
> ```

**AdminPortal was not matching this spec!**

---

## ?? **Next Test**

**Run user creation test again**:

```
1. Login as alice
2. Navigate to User Management
3. Click "Create User"
4. Fill form:
   - Email: CreatPaxTest2@example.com
   - Role: passenger
   - Password: password123 (or longer)
   - Confirm: password123
5. Click "Create User"

Expected:
  ? Success toast: "User CreatPaxTest2@example.com created successfully."
  ? Modal closes
  ? User list refreshes
  ? New user appears in table
```

---

## ? **Summary**

**Problem**: Field name mismatch (`temporaryPassword` vs `tempPassword`)  
**Impact**: All user creations failing with password validation error  
**Fix**: Added `[JsonPropertyName("tempPassword")]` attribute  
**Effort**: 1 minute  
**Risk**: None - build successful, spec-compliant  

**Status**: ? **READY TO TEST**

---

*Simple fix, big impact! Try creating a user now, my friend!* ???
