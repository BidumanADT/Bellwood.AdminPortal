# AdminPortal UI Status Mapping Guide

**Document Type**: Integration Reference - UI Implementation  
**Target Audience**: AdminPortal Development Team  
**Last Updated**: January 27, 2026  
**AdminAPI Version**: Phase Alpha (1.0.0)  
**Status**: ? Production Ready

---

## ?? Overview

This document explains how to map AdminAPI status values to user-friendly display labels in the AdminPortal UI.

**The Issue**: AdminAPI uses technical status names (e.g., `"Pending"`) while your UI mockups may show different labels.

**The Solution**: Map API status values to UI display labels in your frontend code.

---

## ?? Status Terminology

### AdminAPI Status Enum (Technical Names)

The AdminAPI uses these **exact** status values:

```json
{
  "id": "quote-abc123",
  "status": "Pending",       // ? API returns this exact string
  "bookerName": "Chris Bailey",
  "passengerName": "Jordan Chen"
}
```

**Phase Alpha Status Values**:

| API Status | Description |
|------------|-------------|
| `Pending` | Initial passenger request (awaiting dispatcher acknowledgment) |
| `Acknowledged` | Dispatcher acknowledged receipt, preparing response |
| `Responded` | Dispatcher provided price/ETA to passenger |
| `Accepted` | Passenger accepted quote (booking created) |
| `Cancelled` | Quote cancelled |

**Legacy Status Values** (for backward compatibility):

| API Status | Description |
|------------|-------------|
| `InReview` | Deprecated - use `Acknowledged` |
| `Priced` | Deprecated - use `Responded` |
| `Sent` | Deprecated - use `Responded` |
| `Closed` | Deprecated - no longer used |
| `Rejected` | Admin rejected quote |

---

## ? UI Implementation (TypeScript/JavaScript)

### Status Display Mapper

Create a helper function to map API status values to user-friendly labels:

```typescript
// File: src/services/quote-status-mapper.ts

/**
 * Maps API status names to user-friendly UI labels.
 * AdminAPI uses technical terms, UI shows friendly terms.
 */
export function getQuoteStatusDisplay(apiStatus: string): string {
  const statusMap: Record<string, string> = {
    // Phase Alpha statuses
    "Pending": "Pending",              // ? Already user-friendly
    "Acknowledged": "Acknowledged",
    "Responded": "Responded",
    "Accepted": "Accepted",
    "Cancelled": "Cancelled",
    
    // Legacy statuses (rare, for backward compatibility)
    "InReview": "In Review",
    "Priced": "Priced",
    "Rejected": "Rejected",
    "Closed": "Closed",
    "Sent": "Sent"
  };
  
  return statusMap[apiStatus] || apiStatus; // Fallback to original if not found
}

/**
 * Gets Bootstrap/UI color class for status badges.
 */
export function getQuoteStatusColor(apiStatus: string): string {
  const colorMap: Record<string, string> = {
    "Pending": "warning",        // Yellow/Orange
    "Acknowledged": "info",      // Blue
    "Responded": "primary",      // Purple/Primary color
    "Accepted": "success",       // Green
    "Cancelled": "secondary",    // Gray
    "Rejected": "danger"         // Red
  };
  
  return colorMap[apiStatus] || "default";
}

/**
 * Gets icon for status (optional enhancement).
 */
export function getQuoteStatusIcon(apiStatus: string): string {
  const iconMap: Record<string, string> = {
    "Pending": "?",           // Hourglass
    "Acknowledged": "???",      // Eye
    "Responded": "??",        // Money bag
    "Accepted": "?",         // Check mark
    "Cancelled": "?",        // X mark
    "Rejected": "??"          // No entry
  };
  
  return iconMap[apiStatus] || "??";
}
```

---

## ?? UI Component Examples

### React Component

```tsx
// File: src/components/QuoteList.tsx

import React from 'react';
import { getQuoteStatusDisplay, getQuoteStatusColor } from '../services/quote-status-mapper';

interface Quote {
  id: string;
  status: string;
  passengerName: string;
  pickupLocation: string;
}

export const QuoteList: React.FC<{ quotes: Quote[] }> = ({ quotes }) => {
  return (
    <table className="table">
      <thead>
        <tr>
          <th>Quote ID</th>
          <th>Passenger</th>
          <th>Pickup</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {quotes.map(quote => (
          <tr key={quote.id}>
            <td>{quote.id}</td>
            <td>{quote.passengerName}</td>
            <td>{quote.pickupLocation}</td>
            <td>
              <span className={`badge bg-${getQuoteStatusColor(quote.status)}`}>
                {getQuoteStatusDisplay(quote.status)}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
```

### Angular Component

```typescript
// File: src/app/components/quote-list/quote-list.component.ts

import { Component, Input } from '@angular/core';
import { getQuoteStatusDisplay, getQuoteStatusColor } from '../../services/quote-status-mapper';

interface Quote {
  id: string;
  status: string;
  passengerName: string;
  pickupLocation: string;
}

@Component({
  selector: 'app-quote-list',
  template: `
    <table class="table">
      <thead>
        <tr>
          <th>Quote ID</th>
          <th>Passenger</th>
          <th>Pickup</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let quote of quotes">
          <td>{{ quote.id }}</td>
          <td>{{ quote.passengerName }}</td>
          <td>{{ quote.pickupLocation }}</td>
          <td>
            <span [class]="'badge bg-' + getStatusColor(quote.status)">
              {{ getStatusDisplay(quote.status) }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  `
})
export class QuoteListComponent {
  @Input() quotes: Quote[] = [];
  
  getStatusDisplay = getQuoteStatusDisplay;
  getStatusColor = getQuoteStatusColor;
}
```

### Vue.js Component

```vue
<!-- File: src/components/QuoteList.vue -->

<template>
  <table class="table">
    <thead>
      <tr>
        <th>Quote ID</th>
        <th>Passenger</th>
        <th>Pickup</th>
        <th>Status</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="quote in quotes" :key="quote.id">
        <td>{{ quote.id }}</td>
        <td>{{ quote.passengerName }}</td>
        <td>{{ quote.pickupLocation }}</td>
        <td>
          <span :class="`badge bg-${getStatusColor(quote.status)}`">
            {{ getStatusDisplay(quote.status) }}
          </span>
        </td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';
import { getQuoteStatusDisplay, getQuoteStatusColor } from '@/services/quote-status-mapper';

interface Quote {
  id: string;
  status: string;
  passengerName: string;
  pickupLocation: string;
}

defineProps<{
  quotes: Quote[];
}>();

const getStatusDisplay = getQuoteStatusDisplay;
const getStatusColor = getQuoteStatusColor;
</script>
```

---

## ?? Status Filter Implementation

### Client-Side Filtering (Phase Alpha)

Since AdminAPI doesn't support server-side status filtering in Phase Alpha, implement client-side filtering:

```typescript
// File: src/services/quote-filter.ts

export function filterQuotesByStatus(quotes: Quote[], statusFilter: string | null): Quote[] {
  if (!statusFilter || statusFilter === 'all') {
    return quotes; // Show all quotes
  }
  
  return quotes.filter(quote => quote.status === statusFilter);
}

// Usage in component:
const filteredQuotes = filterQuotesByStatus(allQuotes, selectedStatus);
```

### Filter UI Component (React Example)

```tsx
// File: src/components/QuoteStatusFilter.tsx

import React from 'react';

interface Props {
  selectedStatus: string;
  onStatusChange: (status: string) => void;
}

export const QuoteStatusFilter: React.FC<Props> = ({ selectedStatus, onStatusChange }) => {
  const statuses = [
    { value: 'all', label: 'All Quotes' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Acknowledged', label: 'Acknowledged' },
    { value: 'Responded', label: 'Responded' },
    { value: 'Accepted', label: 'Accepted' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];
  
  return (
    <div className="btn-group" role="group">
      {statuses.map(status => (
        <button
          key={status.value}
          type="button"
          className={`btn btn-outline-primary ${selectedStatus === status.value ? 'active' : ''}`}
          onClick={() => onStatusChange(status.value)}
        >
          {status.label}
        </button>
      ))}
    </div>
  );
};
```

---

## ?? Complete Status Reference

### Phase Alpha Workflow

```
Pending ? Acknowledged ? Responded ? Accepted
   ?           ?            ?
Cancelled   Cancelled    Cancelled
```

**Status Details**:

| API Status | UI Label | Color | Icon | Description | Actions Available |
|------------|----------|-------|------|-------------|-------------------|
| `Pending` | Pending | ?? Warning (Yellow) | ? | Awaiting dispatcher acknowledgment | **[Acknowledge]** (dispatcher only) |
| `Acknowledged` | Acknowledged | ?? Info (Blue) | ??? | Dispatcher acknowledged, preparing price | **[Send Response]** (dispatcher only) |
| `Responded` | Responded | ?? Primary (Purple) | ?? | Price/ETA sent to passenger | **[Accept]** (passenger only), **[Cancel]** |
| `Accepted` | Accepted | ? Success (Green) | ? | Passenger accepted, booking created | **[View Booking]** (read-only) |
| `Cancelled` | Cancelled | ? Secondary (Gray) | ? | Quote cancelled | None (terminal state) |

---

## ?? Implementation Checklist

- [ ] Create `quote-status-mapper.ts` helper file
- [ ] Import helper in quote list component
- [ ] Map API `status` field to UI display label
- [ ] Apply color classes based on status
- [ ] (Optional) Add status icons
- [ ] Implement client-side status filtering
- [ ] Add status filter buttons/dropdown
- [ ] Test all status values display correctly
- [ ] Verify badge colors match design system

---

## ? FAQ

**Q: Why doesn't the API support server-side status filtering?**  
A: Phase Alpha focuses on core lifecycle functionality. Server-side filtering will be added in Phase 3. Use client-side filtering for now (quotes list is small enough).

**Q: Can I change the status labels in the UI?**  
A: Yes! The `getQuoteStatusDisplay()` function is your single source of truth for UI labels. Change the mapping there and it updates everywhere.

**Q: What if a new status is added in the future?**  
A: The helper function has a fallback (`|| apiStatus`) that returns the API value if no mapping exists. Update the mapper when new statuses are added.

**Q: Should I validate status before sending to API?**  
A: Yes! The API will reject invalid transitions (FSM validation), but pre-validating in UI prevents unnecessary API calls.

---

## ?? Related Documentation

- `AdminPortal-Integration-Reference.md` - Complete API endpoint reference
- `20-API-Reference.md` - Full AdminAPI documentation
- `15-Quote-Lifecycle.md` - Phase Alpha implementation details

---

## ?? Support

**Questions?** Contact AdminAPI team:
- GitHub Issues: [AdminAPI Repository](https://github.com/BidumanADT/Bellwood.AdminApi/issues)
- Email: api-support@bellwood.com

---

**Document Version**: 1.0.0  
**AdminAPI Version**: Phase Alpha  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready
