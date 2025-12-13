# ?? Bellwood Admin Portal - Quick Start Guide

## Start Services (3 Terminals)

### Terminal 1: AuthServer
```bash
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
```
? Listening on: `https://localhost:5001`

### Terminal 2: AdminAPI
```bash
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```
? Listening on: `https://localhost:5206`

### Terminal 3: AdminPortal
```bash
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal

# First time only: Seed test data
.\seed-admin-api.ps1

# Start the portal
dotnet run
```
? Listening on: `https://localhost:7257`

---

## Login Credentials

| Username | Password | Role |
|----------|----------|------|
| alice    | password | Staff |
| bob      | password | Staff |

---

## What Was Fixed

| Issue | Status |
|-------|--------|
| JSON syntax error in appsettings | ? Fixed |
| Missing namespace in IAdminApiKeyProvider | ? Fixed |
| Disconnected auth state | ? Fixed |
| Non-functional authorization | ? Fixed |
| Invalid layout structure | ? Fixed |
| Navigation reload issues | ? Fixed |

---

## Expected Flow

```
1. Navigate to https://localhost:7257
   ?
2. Auto-redirect to /login
   ?
3. Enter: alice / password
   ?
4. Click Login
   ?
5. Redirect to /bookings
   ?
6. See 3 test bookings displayed
```

---

## Troubleshooting

### Blank Page?
1. Check browser console (F12) for errors
2. Verify all 3 services are running
3. Check Network tab for failed API calls
4. Run `.\test-api-connection.ps1` to verify AdminAPI

### 401 Unauthorized?
- Verify API key in `appsettings.Development.json` matches AdminAPI
- Default: `dev-secret-123`

### No Bookings?
- Run: `.\seed-admin-api.ps1`
- Or manually: `curl -X POST https://localhost:5206/bookings/seed -k`

---

## Test Data (3 Bookings)

| Passenger | Vehicle | Status | Pickup |
|-----------|---------|--------|--------|
| Taylor Reed | SUV | Requested | O'Hare FBO |
| Jordan Chen | Sedan | Confirmed | Langham Hotel |
| Derek James | S-Class | Completed | O'Hare Intl |

---

## Key Files

| File | Purpose |
|------|---------|
| `appsettings.Development.json` | API configuration & keys |
| `Components/Pages/Login.razor` | Login form & auth flow |
| `Components/Pages/Bookings.razor` | Main dashboard |
| `Services/JwtAuthenticationStateProvider.cs` | Blazor auth bridge |
| `Program.cs` | DI & service registration |

---

## Quick Commands

```bash
# Build project
dotnet build

# Run project
dotnet run

# Seed test data
.\seed-admin-api.ps1

# Test API connection
.\test-api-connection.ps1
```

---

## Browser DevTools - What to Check

### Console Tab
Should see:
```
Bookings: OnInitializedAsync running
API Key added: dev-secret-123
Bearer token added
Fetching bookings from AdminAPI...
Loaded 3 bookings
Filtered to 3 bookings with status: All
```

### Network Tab (Filter: Fetch/XHR)
Request to `/bookings/list?take=100`:
- Status: `200 OK`
- Headers: `X-Admin-ApiKey: dev-secret-123`
- Response: JSON array with 3 objects

---

## ?? Documentation

- **README.md** - Full documentation & architecture
- **COMPLETE_FIX_SUMMARY.md** - Detailed fix explanations
- **QUICK_START.md** - This file!

---

## ? Success Checklist

- [ ] All 3 services running
- [ ] Can access login page
- [ ] Can login with alice/password
- [ ] Redirects to bookings automatically
- [ ] Bookings page shows 3 items
- [ ] Can filter by status
- [ ] Can search bookings
- [ ] Refresh button works

---

## ?? You're All Set!

Everything is configured and ready to go. Just start the three services and navigate to `https://localhost:7257`.

**Questions?** Check the detailed docs in `README.md` or `COMPLETE_FIX_SUMMARY.md`.
