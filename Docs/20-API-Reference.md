# API Reference

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 17, 2026  
**Status**: ?? Draft

---

## ?? Overview

Complete reference of AdminAPI endpoints used by AdminPortal, including request/response formats, authentication requirements, and usage examples.

**Base URL**: `https://localhost:5206` (Development)  
**Authentication**: Bearer JWT tokens

**Target Audience**: Developers  
**Prerequisites**: Understanding of REST APIs, JWT auth

---

## ?? Authentication

All endpoints require JWT token:

```http
Authorization: Bearer {jwt_token}
X-Admin-ApiKey: {api_key}
```

---

## ?? Endpoint Categories

### Bookings Endpoints
[TODO: Add booking endpoints]

### Quotes Endpoints
[TODO: Add quote endpoints]

### Driver & Affiliate Endpoints
[TODO: Add driver/affiliate endpoints]

### Location & Tracking Endpoints
[TODO: Add GPS tracking endpoints]

---

## ?? HTTP Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| 200 | Success | Request processed |
| 401 | Unauthorized | Missing token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |

---

## ?? Related Documentation

- [Security Model](23-Security-Model.md)
- [Data Models](22-Data-Models.md)
- [SignalR Reference](21-SignalR-Reference.md)

---

**Last Updated**: January 17, 2026  
**Status**: ?? Draft - Content migration in progress
