# Alpha: User Management (Admin Portal)

**Last Updated**: February 8, 2026  
**Status**: ✅ **READY FOR ALPHA TESTING**

---

## 📋 Overview

User Management allows admin users to:
- Create new users with email and role assignment
- View all system users with their roles
- Change user roles
- Enable/disable users (when backend supports it)

**Important**: All user management operations now go through **AdminAPI**, not AuthServer directly.

---

## 🔗 API Integration

### Endpoints Used

| Operation | Method | Endpoint | Description |
|-----------|--------|----------|-------------|
| List Users | GET | `/users/list?take=50&skip=0` | Get paginated user list |
| Create User | POST | `/users` | Create new user with email, roles, password |
| Update Role | PUT | `/users/{userId}/roles` | Change user's role (single role) |
| Disable User | PUT | `/users/{userId}/disable` | Disable user account |
| Enable User | PUT | `/users/{userId}/enable` | Enable user account |

**Base URL**: AdminAPI (`https://localhost:5206`)  
**Authentication**: JWT Bearer token + X-Admin-ApiKey header  
**Authorization**: Admin role required for all operations

---

## 🧪 Manual Test Steps

### Prerequisites
1. **AdminAPI running** on `https://localhost:5206`
2. **AuthServer running** on `https://localhost:5001`
3. **AdminPortal running** on `https://localhost:7257`
4. Admin user credentials (e.g., `alice` / `password`)

---

### Test 1: Create User
1. Log in as an Admin user (`alice`)
2. Navigate to **Administration → User Management**
3. Click **Create User** button
4. Fill in the form:
   - **Email**: `test.user@example.com`
   - **Role**: Select one role (e.g., `driver`)
   - **Temporary Password**: Enter password ≥10 characters
   - **Confirm Password**: Re-enter password
5. Click **Create User**
6. **Expected**:
   - ✅ Success toast: "User test.user@example.com created successfully"
   - ✅ Modal closes automatically
   - ✅ User list refreshes
   - ✅ New user appears in the table

**API Call Made**:
```http
POST /users
Authorization: Bearer {admin-token}
X-Admin-ApiKey: {api-key}

{
  "email": "test.user@example.com",
  "roles": ["driver"],
  "temporaryPassword": "TempPass123"
}
```

---

### Test 2: Assign Role Changes
1. From the User Management list, find a user (e.g., `charlie`)
2. Click **Edit Roles** button
3. Select a different role (e.g., change from `driver` to `dispatcher`)
4. Click **Save Roles**
5. **Expected**:
   - ✅ Success toast: "Role updated for charlie to dispatcher"
   - ✅ Modal closes
   - ✅ User list refreshes
   - ✅ Table shows updated role: "Dispatcher"

**API Call Made**:
```http
PUT /users/{userId}/roles
Authorization: Bearer {admin-token}
X-Admin-ApiKey: {api-key}

{
  "role": "dispatcher"
}
```

**Note**: Uses `userId` (GUID), not `username`

---

### Test 3: Verify Non-Admin Cannot See the Page
1. Log out if currently logged in
2. Log in as a non-admin user:
   - Username: `diana` (dispatcher)
   - Password: `password`
3. **Expected**:
   - ❌ **User Management** navigation item is **NOT visible** in the sidebar
4. Attempt to navigate directly to `/admin/users` in the address bar
5. **Expected**:
   - ❌ Page does not load OR
   - ❌ Redirected to login page OR
   - ❌ "Access denied" message shown

**Authorization Check**:
```csharp
@attribute [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
```

---

### Test 4: Verify User List Loads
1. Log in as admin (`alice`)
2. Navigate to User Management
3. **Expected**:
   - ✅ Table displays all users
   - ✅ Columns shown: Username, Email, Roles, Created At, Modified At, Actions
   - ✅ Role displayed correctly (e.g., "Admin", "Driver", "Dispatcher")
   - ✅ All text is readable (good contrast)

---

### Test 5: Disable/Enable User (If Supported)
**Note**: This feature may not be implemented yet in AdminAPI.

1. Find a user in the table
2. Look for **Disable** or **Enable** button
3. **If button shows** "🚧 Disable: Not Yet Available":
   - ✅ Expected - feature not yet implemented
   - ✅ No error shown
4. **If button is clickable**:
   - Click the button
   - Verify success/error toast
   - Verify user status updates

**API Calls** (when implemented):
```http
PUT /users/{userId}/disable
PUT /users/{userId}/enable
```

---

## 🔍 Troubleshooting

### Issue: "Failed to create user: 405 Method Not Allowed"

**Cause**: AdminAPI `/users` endpoint not implemented  
**Fix**: Ensure AdminAPI has `POST /users` endpoint  
**Verify**: Check AdminAPI logs for endpoint routing

---

### Issue: "Failed to update role: 404 Not Found"

**Cause 1**: AdminAPI `/users/{userId}/roles` endpoint not implemented  
**Cause 2**: Invalid `userId` (empty or malformed)  
**Fix**: 
- Verify AdminAPI has `PUT /users/{userId}/roles` endpoint
- Check that `user.Id` is populated correctly from API response

---

### Issue: Role column shows blank

**Cause**: JSON property name mismatch  
**Fix**: Ensure `UserDto` has:
```csharp
[JsonPropertyName("role")]
public string Role { get; set; } = string.Empty;
```

---

### Issue: "Access denied" when admin tries to create user

**Cause**: JWT token missing or invalid  
**Fix**: 
1. Check console for authentication errors
2. Verify JWT token is being sent: Open DevTools → Network → Request Headers
3. Ensure `GetAuthorizedClientAsync()` adds both API key AND JWT token

---

## ✅ Expected Behavior Summary

| Action | Expected Result |
|--------|----------------|
| **Create User** | POST to `/users`, success toast, modal closes, list refreshes |
| **Edit Role** | PUT to `/users/{userId}/roles`, success toast, modal closes, role updates |
| **Non-Admin Access** | Page not accessible, navigation hidden |
| **List Users** | GET from `/users/list`, table populates with username, email, roles |
| **Disable/Enable** | PUT to `/users/{userId}/disable` or `/enable`, graceful 404 handling |

---

## 📝 Test Checklist

**Before Alpha Release**:
- [ ] User creation succeeds without 405 errors
- [ ] Created users appear in the list
- [ ] Role changes persist and display correctly
- [ ] Non-admin users cannot access page
- [ ] All API calls go to AdminAPI (not AuthServer)
- [ ] Success/error messages are user-friendly
- [ ] No sensitive data logged to console
- [ ] Passwords are never displayed or logged

---

## 🔗 Related Documentation

- `Services/UserManagementService.cs` - Service implementation
- `Components/Pages/Admin/UserManagement.razor` - UI component
- `Models/UserModels.cs` - Data models
- `Docs/23-Security-Model.md` - Authorization details
- `Docs/Archive/20-API-Reference.md` - AdminAPI endpoints

---

**Last Updated**: February 8, 2026  
**Status**: ✅ **Fixed for Alpha**  
**Changes**: Updated to use AdminAPI endpoints consistently

---

*All user management operations now go through AdminAPI with proper authentication and authorization!* 🎯✨
