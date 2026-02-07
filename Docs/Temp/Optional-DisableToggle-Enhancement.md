# Optional UX Enhancement: Disable Toggle "Coming Soon" Badge

**Status**: Optional (Not Required for Alpha)  
**Effort**: 5 minutes  
**Risk**: None (cosmetic only)  
**Benefit**: Clearer UX for alpha testers

---

## Current Behavior

**When disable endpoint returns 404/501**:
1. User clicks toggle ? error toast shown
2. Toggle buttons hidden for all users
3. Page must be reloaded to try again

**UX Issue**: Testers might think the feature broke, not that it's unimplemented

---

## Proposed Enhancement

**Instead of hiding toggle**, show a badge indicating feature is coming soon.

---

## Implementation Plan

### Option 1: "Coming Soon" Badge (Recommended)

**File**: `Components/Pages/Admin/UserManagement.razor`

**Change** (around line 90):
```razor
<!-- BEFORE -->
@if (disableEndpointAvailable)
{
    <button class="btn btn-sm @(user.IsDisabled ? "btn-outline-success" : "btn-outline-danger")"
            @onclick="() => ToggleUserDisabled(user)"
            disabled="@(statusUpdatingUserId == user.Id)">
        @if (statusUpdatingUserId == user.Id)
        {
            <span class="spinner-border spinner-border-sm"></span>
        }
        else
        {
            <span>@(user.IsDisabled ? "Enable" : "Disable")</span>
        }
    </button>
}

<!-- AFTER -->
@if (disableEndpointAvailable)
{
    <button class="btn btn-sm @(user.IsDisabled ? "btn-outline-success" : "btn-outline-danger")"
            @onclick="() => ToggleUserDisabled(user)"
            disabled="@(statusUpdatingUserId == user.Id)">
        @if (statusUpdatingUserId == user.Id)
        {
            <span class="spinner-border spinner-border-sm"></span>
        }
        else
        {
            <span>@(user.IsDisabled ? "Enable" : "Disable")</span>
        }
    </button>
}
else
{
    <span class="badge bg-secondary" 
          title="User disable feature coming in next release"
          style="cursor: help;">
        ?? Disable: Not Yet Available
    </span>
}
```

**Code block** (around line 235):
```csharp
// Change default value to false
private bool disableEndpointAvailable = false; // Start as unavailable for alpha
```

**Result**:
- Badge shown instead of empty space
- Tooltip explains feature is coming
- No confusing "broken" behavior

---

### Option 2: Disabled Button (Alternative)

```razor
@if (disableEndpointAvailable)
{
    <!-- Active toggle button -->
}
else
{
    <button class="btn btn-sm btn-outline-secondary" 
            disabled
            title="Disable feature not yet implemented">
        Disable (Coming Soon)
    </button>
}
```

---

### Option 3: Do Nothing (Current Behavior)

**Keep as-is**: Hide toggle after first 404

**Pros**:
- Simple
- No extra code
- Works fine for internal alpha

**Cons**:
- Might confuse testers ("Where did the button go?")
- Requires page reload to retry

---

## Testing After Change

1. Load User Management page
2. **Verify**: All users show "?? Disable: Not Yet Available" badge
3. Hover over badge
4. **Verify**: Tooltip shows explanation
5. Try clicking badge
6. **Verify**: Nothing happens (not clickable)

---

## Rollback Plan

If enhancement causes issues:

**Revert to original**:
```razor
@if (disableEndpointAvailable)
{
    <!-- Toggle button -->
}
<!-- Remove the else block -->
```

**AND**:
```csharp
private bool disableEndpointAvailable = true; // Back to original default
```

---

## Recommendation

**For Alpha**: **Option 1** (Badge)

**Why**:
- Clear communication to testers
- No functionality broken
- Easy to understand
- Sets correct expectations

**When to implement**: After alpha testing feedback, if testers report confusion

**When NOT to implement**: If AdminAPI team implements disable endpoint before alpha

---

## Alternative: Feature Flag Approach

**For future scalability**:

```csharp
// appsettings.json
{
  "Features": {
    "UserDisable": false  // Enable in future release
  }
}

// UserManagement.razor.cs
private bool disableEndpointAvailable;

protected override void OnInitialized()
{
    disableEndpointAvailable = Configuration.GetValue<bool>("Features:UserDisable");
    // ...
}
```

**Benefit**: Can enable/disable without code changes

---

**Decision**: Up to you based on alpha tester feedback!

**Current Status**: ? Working correctly, enhancement is cosmetic only

