# ?? End-to-End Testing Guide - Driver Assignment Feature

## Prerequisites

### Required Services
- ? **BellwoodAuthServer** (port 5001)
- ? **Bellwood.AdminApi** (port 5206)  
- ? **Bellwood.AdminPortal** (port 7257)
- ? **BellwoodMobileApp** (for driver app testing)

### Test Accounts
- **Admin Staff**: alice / password
- **Test Driver**: Charlie (UserUID: charlie-uid-001)

---

## ?? Quick Start - Complete Setup

### Step 1: Start All Services

**Terminal 1 - AuthServer:**
```powershell
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
```
? Wait for: `Now listening on: https://localhost:5001`

**Terminal 2 - AdminAPI:**
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```
? Wait for: `Now listening on: https://localhost:5206`

**Terminal 3 - AdminPortal:**
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
dotnet run
```
? Wait for: `Now listening on: https://localhost:7257`

---

### Step 2: Seed Test Data

**Run the seeding script:**
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
.\seed-affiliates-drivers.ps1
```

**Expected output:**
```
========================================
Bellwood Elite - Seed Affiliates & Drivers
========================================

Step 1: Authenticating with AuthServer...
? Authentication successful!

Step 2: Seeding default affiliates and drivers...
? Default affiliates seeded!
  Added: 2 affiliate(s)

Step 3: Creating Charlie's affiliate (Downtown Express)...
? Downtown Express affiliate created!
  Affiliate ID: [generated-id]

Step 4: Adding Charlie as a driver...
? Charlie added as driver!
  Driver ID: [generated-id]
  UserUID: charlie-uid-001

Step 5: Listing all affiliates...
? Current affiliates in system:

  ?? Chicago Limo Service
     Contact: John Smith
     Phone: 312-555-1234
     Email: dispatch@chicagolimo.com
     Drivers: 2
       ?? Michael Johnson - 312-555-0001
          UserUID: driver-001
       ?? Sarah Lee - 312-555-0002
          UserUID: driver-002

  ?? Suburban Chauffeurs
     Contact: Emily Davis
     Phone: 847-555-9876
     Email: emily@suburbanchauffeurs.com
     Drivers: 1
       ?? Robert Brown - 847-555-1000
          UserUID: driver-003

  ?? Downtown Express
     Contact: Charlie Manager
     Phone: 312-555-7890
     Email: charlie@downtownexpress.com
     Drivers: 1
       ?? Charlie - 312-555-CHAS
          UserUID: charlie-uid-001

========================================
Seeding Complete!
========================================
```

---

## ?? Test Scenarios

### Test 1: View Affiliates Management

**Steps:**
1. Open browser: `https://localhost:7257`
2. Login: **alice** / **password**
3. Click **"Affiliates"** in sidebar
4. Verify you see 3 affiliates:
   - Chicago Limo Service (2 drivers)
   - Suburban Chauffeurs (1 driver)
   - Downtown Express (1 driver - Charlie)

**Expected Result:**
- ? Grid view with 3 affiliate cards
- ? Each card shows name, contact, phone, email
- ? Driver count displayed
- ? View/Edit/Delete buttons visible

**Screenshot checkpoint:**
```
??????????????????? ??????????????????? ???????????????????
? Chicago Limo    ? ? Suburban Chauff ? ? Downtown Express?
? Contact: John   ? ? Contact: Emily  ? ? Contact: Charlie?
? ?? 312-555-1234 ? ? ?? 847-555-9876 ? ? ?? 312-555-7890 ?
? 2 driver(s)     ? ? 1 driver(s)     ? ? 1 driver(s)     ?
? [View][Edit]    ? ? [View][Edit]    ? ? [View][Edit]    ?
??????????????????? ??????????????????? ???????????????????
```

---

### Test 2: Create New Affiliate

**Steps:**
1. On Affiliates page, click **"+ Create Affiliate"**
2. Fill form:
   - Name: **"Premium Rides"**
   - Point of Contact: **"John Manager"**
   - Phone: **"773-555-1111"**
   - Email: **"john@premiumrides.com"**
   - City: **"Chicago"**
   - State: **"IL"**
3. Click **"Save"**

**Expected Result:**
- ? Success message: "Affiliate created successfully!"
- ? New card appears in grid
- ? Shows "Premium Rides" with 0 drivers
- ? Form closes automatically

**Bug Fix Verification:**
- ? No JSON serialization error
- ? Affiliate saves successfully
- ? Drivers list initialized as empty array

---

### Test 3: Add Driver to Affiliate

**Steps:**
1. Click **"View Details"** on **"Premium Rides"**
2. Verify affiliate info displays correctly
3. Click **"+ Add Driver"**
4. Fill form:
   - Driver Name: **"Test Driver"**
   - Phone: **"773-555-9999"**
5. Click **"Save Driver"**

**Expected Result:**
- ? Success message: "Driver 'Test Driver' added successfully!"
- ? Driver appears in table
- ? Table shows: Test Driver | 773-555-9999 | [Edit][Delete]
- ? Form closes
- ? Driver count updates

---

### Test 4: View Booking with Unassigned Driver

**Steps:**
1. Click **"Bookings"** in sidebar
2. View booking cards
3. Look for **"Driver:"** field on each card

**Expected Result:**
- ? Most bookings show: **"Driver: Unassigned"** (yellow text)
- ? Text is visible and clear
- ? Cards are clickable

**Visual:**
```
???????????????????????????????????
? Alice Morgan          [CONFIRMED]
? SUV • 12/20/2024 2:00 PM        ?
? ?? O'Hare Airport               ?
? Booker: John Doe                ?
? Driver: ?? Unassigned      ? NEW?
???????????????????????????????????
```

---

### Test 5: Assign Charlie to a Booking

**Steps:**
1. Click on any booking card
2. Booking detail page opens
3. Scroll to **"Driver Assignment"** section (right side)
4. Verify: **"Current Driver: Unassigned"** (yellow)
5. Click on **"Downtown Express"** to expand
6. Verify: Shows 1 driver: **"Charlie - 312-555-CHAS"**
7. Click **"Assign"** button next to Charlie

**Expected Result:**
- ? Button shows spinner during assignment
- ? Success message appears:
  ```
  ? Driver assigned successfully! Charlie will handle this booking.
  Affiliate has been notified via email.
  ```
- ? **"Current Driver"** updates to: **"? Charlie"** (green text)
- ? Email sent to charlie@downtownexpress.com (check AdminAPI logs)

**Email Content (check AdminAPI logs):**
```
To: charlie@downtownexpress.com
Subject: Bellwood Elite - Driver Assignment - [Date/Time]

Driver Information:
  Name: Charlie
  Phone: 312-555-CHAS

Booking Details:
  Reference ID: [booking-id]
  Passenger: [passenger-name]
  Pickup: [location]
  Date/Time: [datetime]
  Vehicle: [class]
```

---

### Test 6: Verify Assignment on Bookings List

**Steps:**
1. Click **"? Back to Bookings"**
2. Find the booking you just assigned
3. Look at **"Driver:"** field

**Expected Result:**
- ? Shows: **"Driver: ? Charlie"** (green text)
- ? No longer shows "Unassigned"
- ? Assignment persists after navigation

---

### Test 7: Quick-Add Driver During Assignment

**Steps:**
1. Click different booking card
2. On detail page, expand **"Chicago Limo Service"**
3. Click **"+ Add Driver"** button (below existing drivers)
4. Inline form appears
5. Fill:
   - Driver Name: **"Quick Driver"**
   - Phone: **"312-555-FAST"**
6. Click **"Save"**
7. Verify new driver appears in list
8. Click **"Assign"** on the newly added driver

**Expected Result:**
- ? Form appears inline (not a modal)
- ? Driver added successfully
- ? Appears in list immediately
- ? Can assign to booking right away
- ? No need to refresh or navigate away

---

### Test 8: Reassign Driver

**Steps:**
1. Go to booking already assigned to Charlie
2. Expand **"Chicago Limo Service"**
3. Click **"Assign"** on **"Michael Johnson"**

**Expected Result:**
- ? Previous assignment (Charlie) replaced
- ? New assignment: **"? Michael Johnson"**
- ? Success message shows
- ? Email sent to Chicago Limo Service affiliate

---

### Test 9: Delete Affiliate (Cascade Test)

**Steps:**
1. Go to **"Affiliates"** page
2. Click **"Delete"** on **"Premium Rides"**
3. Confirmation modal appears

**Expected Content:**
```
?? This will also delete 1 driver(s) associated with this affiliate.
```

4. Verify warning shows driver count
5. Click **"Cancel"** first (test cancel)
6. Click **"Delete"** again
7. Click **"Delete"** in modal

**Expected Result:**
- ? Modal shows cascade warning
- ? Cancel closes modal without deleting
- ? Delete removes affiliate and driver
- ? Success message: "Affiliate 'Premium Rides' deleted successfully."
- ? Card disappears from grid

---

### Test 10: Driver App Integration

**Steps:**
1. Ensure Charlie is assigned to at least one booking
2. Open driver mobile app
3. Login as **Charlie** (use UserUID: charlie-uid-001 if needed)
4. View **"My Rides"** screen

**Expected Result:**
- ? Charlie sees only bookings assigned to him
- ? Booking details match AdminPortal
- ? Can accept/complete rides
- ? Driver app filters by `AssignedDriverUid` correctly

---

## ?? UI/UX Verification

### Icon Updates

**Main Page:**
- ? Bellwood Elite logo displayed (if file added)
- ? Fallback emoji ?? shows if logo missing
- ? Quotes card uses ?? (document) instead of ??

**Navbar:**
- ? Logo appears next to "Bellwood Elite Admin"
- ? Logo size: 32x32px
- ? Fallback emoji if logo missing

**To add logo:**
1. Place `bellwood_elite_icon.png` in `wwwroot\images\`
2. Recommended size: 512x512px (will be scaled)
3. Format: PNG with transparency
4. Refresh browser (Ctrl+F5)

---

## ?? Error Scenario Testing

### Test 11: API Not Running

**Steps:**
1. Stop AdminAPI (Ctrl+C in Terminal 2)
2. In AdminPortal, try to view Affiliates

**Expected Result:**
- ? Error message: "Failed to load affiliates: [error details]"
- ? No crash or blank page
- ? Graceful error display

**Recovery:**
1. Restart AdminAPI
2. Click anywhere to navigate
3. Return to Affiliates
4. Data loads successfully

---

### Test 12: Invalid Form Submission

**Steps:**
1. Click **"+ Create Affiliate"**
2. Leave **Name** field empty
3. Fill only **Phone**
4. Click **"Save"**

**Expected Result:**
- ? Error message: "Please fill in all required fields (Name, Phone, Email)."
- ? Form doesn't close
- ? No API call made
- ? Can fix and resubmit

---

### Test 13: Network Timeout

**Steps:**
1. Pause AdminAPI (use debugger breakpoint)
2. Try to assign driver
3. Wait for timeout

**Expected Result:**
- ? Spinner shows during wait
- ? Eventually shows error message
- ? Doesn't crash the page
- ? Can retry after resuming API

---

## ?? Test Results Checklist

### Core Functionality
- [ ] Seed script runs successfully
- [ ] View all affiliates
- [ ] Create new affiliate (bug fix verified)
- [ ] Edit affiliate
- [ ] Delete affiliate with cascade warning
- [ ] View affiliate details
- [ ] Add driver to affiliate
- [ ] View booking details
- [ ] Assign driver to booking
- [ ] Reassign driver
- [ ] Quick-add driver during assignment
- [ ] Driver assignment persists
- [ ] Email notification sent (check logs)

### UI/UX
- [ ] Bellwood logo displays (or fallback emoji)
- [ ] Quotes uses document icon ??
- [ ] Driver status color coding (yellow/green)
- [ ] Hierarchical tree expands/collapses
- [ ] Loading spinners during operations
- [ ] Success messages clear and helpful
- [ ] Error messages informative

### Integration
- [ ] Driver app shows assigned rides
- [ ] Passenger app shows driver name
- [ ] AdminAPI logs email sending
- [ ] All three apps communicate correctly

### Error Handling
- [ ] API offline shows error
- [ ] Invalid form shows validation
- [ ] Network timeout handled gracefully
- [ ] 404 errors display properly

---

## ?? Known Issues & Workarounds

### Issue 1: Logo Not Appearing
**Symptom:** Fallback emoji shows instead of logo
**Fix:** 
1. Verify file exists: `wwwroot\images\bellwood_elite_icon.png`
2. Hard refresh browser: **Ctrl+Shift+R** or **Ctrl+F5**
3. Check browser console for 404 errors

### Issue 2: Driver Assignment Doesn't Persist
**Symptom:** Assignment works but disappears on refresh
**Check:**
1. Verify AdminAPI is running
2. Check AdminAPI console for errors
3. Verify `App_Data\bookings.json` updated
4. Restart AdminAPI if needed

### Issue 3: Email Not Sent
**Symptom:** Success message shows but no email
**Check:**
1. AdminAPI console logs for SMTP errors
2. Verify email configuration in `appsettings.json`
3. Email might be in SMTP queue (not instant)

---

## ?? Success Criteria

**Feature is fully functional when:**
- ? All 13 test scenarios pass
- ? No console errors in browser
- ? No exceptions in AdminAPI logs
- ? Charlie sees assigned rides in driver app
- ? Email notifications sent successfully
- ? UI matches premium design standards
- ? Logo displays correctly
- ? Create affiliate bug fixed (no JSON error)

---

## ?? Test Execution Log Template

```
Test Date: __________
Tester: __________
Environment: Development / Staging / Production

| Test # | Scenario | Pass/Fail | Notes |
|--------|----------|-----------|-------|
| 1 | View Affiliates | ? | |
| 2 | Create Affiliate | ? | |
| 3 | Add Driver | ? | |
| 4 | View Unassigned | ? | |
| 5 | Assign Charlie | ? | |
| 6 | Verify Assignment | ? | |
| 7 | Quick-Add Driver | ? | |
| 8 | Reassign Driver | ? | |
| 9 | Delete Affiliate | ? | |
| 10 | Driver App | ? | |
| 11 | API Error | ? | |
| 12 | Form Validation | ? | |
| 13 | Network Timeout | ? | |

Overall Result: ? Pass  ? Fail
```

---

## ?? Ready to Test!

Run through all scenarios in order. If any test fails, document the issue and refer to the troubleshooting section.

**Good luck with your testing!** ??
