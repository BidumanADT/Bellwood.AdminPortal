# Phase B - Quote Lifecycle Manual Test Guide

**Document Type**: Testing Guide  
**Created**: January 28, 2026  
**Status**: ?? Ready for Alpha Testing

---

## ?? Overview

This guide provides step-by-step instructions for manually testing the Phase B quote lifecycle features in the Bellwood AdminPortal. Use this in conjunction with the automated smoke test script `test-phase-b-quote-lifecycle.ps1`.

**Phase B Features**:
- ? New quote statuses (Pending, Acknowledged, Responded, Accepted, Cancelled)
- ? Dispatcher acknowledgment workflow
- ? Price/ETA estimation with placeholder warnings
- ? Status-driven UI panels
- ? Pending quote notification badge

---

## ?? Prerequisites

**Before Starting**:
1. ? All three servers running:
   - AuthServer: `https://localhost:5001`
   - AdminAPI: `https://localhost:5206`
   - AdminPortal: `https://localhost:7257`

2. ? Test data seeded:
   ```powershell
   .\Scripts\seed-quotes.ps1
   ```

3. ? Test user credentials ready:
   - **alice** / **password** (admin role)
   - **diana** / **password** (dispatcher role)

4. ? Run automated smoke test first:
   ```powershell
   .\Scripts\test-phase-b-quote-lifecycle.ps1
   ```

---

## ?? Test Suite

### Test 1: Quote List Page with New Filters

**Objective**: Verify new Phase B status filters appear and function correctly

**Steps**:
1. Navigate to `https://localhost:7257`
2. Login as **alice** / **password**
3. Click **Quotes** in navigation menu
4. **Verify**: Filter buttons display correctly:
   - ? All (with count)
   - ? Pending (with count)
   - ? Acknowledged (with count)
   - ? Responded (with count)
   - ? Accepted (with count)
   - ? Cancelled (with count)

5. Click each filter button
6. **Verify**: Quote list updates to show only quotes with that status

**Expected Result**: ? All new status filters work correctly

**Screenshot Location**: `Docs/Screenshots/phase-b-quote-filters.png` (optional)

---

### Test 2: Pending Quote - Acknowledgment Workflow

**Objective**: Verify dispatcher can acknowledge new quote requests

**Steps**:
1. From Quotes page, click filter **Pending**
2. Click on any quote card with "Pending" status
3. **Verify**: Quote detail page loads
4. **Verify**: Left column shows quote information:
   - ? Quote ID
   - ? Status badge shows "Pending"
   - ? Created timestamp
   - ? Booker information
   - ? Trip details
   - ? Special requests (if any)

5. **Verify**: Right column shows "?? New Quote Request" panel with:
   - ? Blue info card styling
   - ? Description: "This quote is awaiting acknowledgment..."
   - ? "Acknowledgment Notes (Optional)" textarea
   - ? "? Acknowledge Quote" button

6. Type test note: "Reviewing this quote request"
7. Click **Acknowledge Quote** button
8. **Verify**: Success message appears: "Quote acknowledged successfully!"
9. **Verify**: Page updates to show "Acknowledged" panel

**Expected Result**: ? Quote successfully acknowledged, UI transitions to next step

**Common Issues**:
- ? Button disabled: Check that AdminAPI acknowledge endpoint is working
- ? 403 error: Verify user has staff role (admin or dispatcher)

---

### Test 3: Acknowledged Quote - Price/ETA Response

**Objective**: Verify dispatcher can enter price and ETA estimates

**Steps**:
1. On quote detail page (after acknowledgment from Test 2)
2. **Verify**: Right panel shows "? Quote Acknowledged - Enter Estimate" with:
   - ? Yellow warning card styling
   - ? Warning message: "?? Placeholder Estimates"
   - ? Warning text: "These are manual estimates until Limo Anywhere integration..."
   - ? "Estimated Price ($)" input with $ prefix
   - ? "Estimated Pickup Time" datetime-local input
   - ? "Response Notes (Optional)" textarea
   - ? "?? Send Response to Customer" button (disabled initially)

3. Enter test values:
   - **Estimated Price**: `125.50`
   - **Estimated Pickup Time**: Tomorrow at 2:00 PM
   - **Response Notes**: "Estimate based on standard route pricing"

4. **Verify**: "Send Response" button becomes enabled
5. Click **Send Response to Customer** button
6. **Verify**: Success message: "Response sent to customer with estimate: $125.50"
7. **Verify**: Page updates to show "Responded" panel

**Expected Result**: ? Response sent successfully, UI shows read-only response summary

**Common Issues**:
- ? Button stays disabled: Ensure both price and pickup time are filled
- ? Validation error: Check that price is >= 0 and pickup time is future date

---

### Test 4: Responded Quote - Read-Only Display

**Objective**: Verify dispatcher sees response summary while awaiting customer

**Steps**:
1. On quote detail page (after responding from Test 3)
2. **Verify**: Right panel shows "? Response Sent - Awaiting Customer" with:
   - ? Green success card styling
   - ? Message: "The customer has been provided with the following estimate..."
   - ? Estimated Price displayed with green styling: "$125.50"
   - ? Yellow "Placeholder" badge next to price
   - ? Estimated Pickup Time displayed
   - ? Response Notes displayed
   - ? Info alert: "?? Next Steps: The customer will accept or cancel..."

3. **Verify**: No editable fields or action buttons (read-only)
4. **Verify**: Quote information panel (left) shows:
   - ? "Acknowledged: [timestamp]"
   - ? "Responded: [timestamp]"
   - ? "Workflow Notes" section with response notes

**Expected Result**: ? Dispatcher can review response but cannot edit while waiting

---

### Test 5: Accepted Quote - Booking Link

**Objective**: Verify accepted quotes show booking information

**Prerequisites**: Need an Accepted quote (may require passenger app or API simulation)

**Steps**:
1. From Quotes page, click filter **Accepted**
2. Click on any accepted quote
3. **Verify**: Right panel shows "?? Quote Accepted - Booking Created" with:
   - ? Blue primary card styling
   - ? Success message: "? This quote has been accepted..."
   - ? Booking ID displayed in monospace font
   - ? "?? View Booking Details" button

4. Click **View Booking Details** button
5. **Verify**: Navigation to `/bookings/{bookingId}` page
6. **Verify**: Booking detail page loads correctly

**Expected Result**: ? Navigation to related booking works correctly

**If No Accepted Quotes**:
- Run this PowerShell to simulate acceptance:
  ```powershell
  $token = # ... get auth token ...
  Invoke-RestMethod -Uri "https://localhost:5206/quotes/{quoteId}/accept" `
    -Headers @{ "Authorization" = "Bearer $token"; "X-Admin-ApiKey" = "dev-secret-123" } `
    -Method Post
  ```

---

### Test 6: Cancelled Quote - Read-Only View

**Objective**: Verify cancelled quotes display appropriately

**Steps**:
1. From Quotes page, click filter **Cancelled**
2. Click on any cancelled quote
3. **Verify**: Right panel shows "? Quote Cancelled" with:
   - ? Red danger card styling
   - ? Message: "This quote has been cancelled and is now closed..."
   - ? Cancelled timestamp displayed
   - ? No action buttons (read-only)

**Expected Result**: ? Cancelled quotes are clearly marked and read-only

---

### Test 7: Pending Quote Notification Badge

**Objective**: Verify navigation menu shows pending quote count

**Steps**:
1. Ensure at least 1 pending quote exists (run `seed-quotes.ps1` if needed)
2. Navigate to any page in AdminPortal
3. **Verify**: Navigation menu "Quotes" item shows:
   - ? Red badge with count (e.g., "3") next to "Quotes"
   - ? Badge is clearly visible

4. Wait 30 seconds (polling interval)
5. Acknowledge a pending quote (Test 2)
6. Wait 30 seconds
7. **Verify**: Badge count decreases by 1

**Expected Result**: ? Badge updates automatically every 30 seconds

**Common Issues**:
- ? Badge not showing: Check browser console for errors
- ? Count incorrect: Refresh page manually to force update

---

### Test 8: Placeholder Warning Visibility

**Objective**: Verify placeholder warnings are prominent

**Steps**:
1. Navigate to any Acknowledged quote
2. **Verify**: Warning alert box is visible with:
   - ? Yellow background
   - ? Warning icon "??"
   - ? Bold text: "Placeholder Estimates"
   - ? Clear explanation about Limo Anywhere integration

3. Enter price and send response
4. Navigate to Responded quote
5. **Verify**: "Placeholder" badge appears next to estimated price

**Expected Result**: ? Users are clearly warned about placeholder nature of estimates

---

### Test 9: Workflow Notes vs Admin Notes

**Objective**: Verify distinction between workflow notes and admin notes

**Steps**:
1. Navigate to any quote detail page
2. Check left information panel
3. **Verify**: If admin notes exist:
   - ? "Admin Notes" section with gray background
   - ? Contains internal admin notes

4. **Verify**: If workflow notes exist:
   - ? "Workflow Notes" section with blue background
   - ? Contains acknowledgment/response notes

5. **Verify**: Both sections can coexist independently

**Expected Result**: ? Clear visual distinction between note types

---

### Test 10: Cross-Browser Compatibility

**Objective**: Verify Phase B UI works across browsers

**Browsers to Test**:
- ? Microsoft Edge (Chromium)
- ? Google Chrome
- ? Firefox
- ? Safari (if on Mac)

**Steps for Each Browser**:
1. Navigate to AdminPortal
2. Login successfully
3. Navigate to Quotes page
4. Click on a Pending quote
5. **Verify**: All UI elements render correctly:
   - Status-driven panels
   - Form inputs
   - Datetime picker
   - Buttons
   - Success/error messages

**Expected Result**: ? Consistent UI across all browsers

---

## ?? Bug Reporting Template

**Title**: [Phase B] [Component] Brief description

**Environment**:
- AdminPortal Version: Phase B (Jan 28, 2026)
- Browser: [e.g., Chrome 120]
- User Role: [admin/dispatcher]

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. Step 3

**Expected Behavior**:
[What should happen]

**Actual Behavior**:
[What actually happened]

**Screenshots**:
[Attach screenshots if applicable]

**Console Errors** (if any):
```
[Paste browser console errors]
```

**Additional Context**:
[Any other relevant information]

---

## ? Test Completion Checklist

After completing all tests, verify:

- [ ] All 10 manual tests passed
- [ ] Automated smoke test passed (`test-phase-b-quote-lifecycle.ps1`)
- [ ] No console errors in browser
- [ ] All placeholder warnings clearly visible
- [ ] Navigation badge updates correctly
- [ ] Status transitions work smoothly
- [ ] No broken links or 404 errors
- [ ] Success/error messages display appropriately

**Sign-off**:
- Tester Name: _______________
- Date: _______________
- Result: ? PASS / ? FAIL (circle one)
- Notes: _________________________________________________

---

## ?? Related Documentation

- [Alpha Test Preparation](../Docs/Temp/alpha-test-preparation.md) - Overall alpha test plan
- [Quote Management](../Docs/11-Quote-Management.md) - Quote feature documentation
- [Scripts Reference](../Docs/31-Scripts-Reference.md) - All test scripts
- [API Reference](../Docs/20-API-Reference.md) - AdminAPI endpoints

---

**Last Updated**: January 28, 2026  
**Status**: ?? Ready for Alpha Testing  
**Version**: Phase B

---

*This manual test guide complements the automated smoke test. Use both for comprehensive Phase B validation.* ??
