# 16. Observability and Correlation Tracing (Alpha)

## Overview
AdminPortal now emits structured logs with Serilog and propagates `X-Correlation-Id` across inbound and outbound HTTP calls.

## Structured logging fields
The portal logs include:
- `service`: `AdminPortal`
- `environment`: `Alpha`
- `correlationId`
- `requestPath`
- `method`
- `statusCode`
- `elapsedMs`
- `userId` (when an authenticated user is available)

Inbound requests are logged by middleware with success and exception paths. Exception logs include an `errorId`.

## Correlation flow
1. Portal reads `X-Correlation-Id` from incoming requests.
2. If absent, portal generates a GUID.
3. Portal writes the correlation ID back to the response header.
4. Outbound calls through named HTTP clients automatically include the current correlation ID.

Configured clients:
- `AuthServer`
- `AdminAPI`

## Outbound logging
Outbound HTTP calls are logged with:
- `outboundService`
- `path`
- `method`
- `statusCode`
- `elapsedMs`
- `correlationId`

Sensitive values (tokens and authorization headers) are intentionally not logged.

## Tracing a request end-to-end
1. Capture `X-Correlation-Id` from an AdminPortal response.
2. Search AdminPortal logs for that `correlationId`.
3. Search AdminAPI/AuthServer logs for the same `correlationId`.
4. Use `errorId` from AdminPortal exception entries to identify specific failures quickly.
