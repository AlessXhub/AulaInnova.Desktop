# Frontend and Backend Integration Guide

## Current architecture

```text
WPF Views
  -> ViewModels and commands
    -> service contracts
      -> typed HTTP/JWT adapters
        -> ASP.NET Core .NET 10 API
          -> EF Core / ASP.NET Identity / SQL Server
```

The UI does not access SQL Server. Domain authorization and critical rules are enforced by the API.

## Project layers

- `Views/`: WPF presentation and reusable resource consumption.
- `ViewModels/`: page state, validation, commands, sorting, filtering and navigation.
- `Models/`: session, page and record contracts used by the prototype.
- `Services/`: stable frontend contracts, HTTP transport, JWT session and API adapters. `DemoDataService` remains only as an isolated smoke-test fixture.
- `Infrastructure/`: binding, commands, dialogs and converters.
- `Themes/`: palette, typography and shared controls.
- `Assets/`: packaged brand and font assets.

## Connected services

### `IAuthenticationService`

`ApiAuthenticationService` calls `POST /api/Auth/login`, reads the Identity role from the JWT and stores the token in process memory. Disabled or locked accounts are rejected by the API. Expired sessions are rejected before authenticated requests. The frontend role menu is presentation logic, not an authorization boundary.

### `IAulaInnovaDataService`

The service centralizes:

- paged-source records for people, administrative catalogs and school operations;
- logical creation, correction and deactivation;
- schedule reads and assignment creation;
- report generation;
- dashboard, attendance and visual summary data;
- change notifications used to refresh related screens.

`ApiAulaInnovaDataService` maps API DTOs to presentation models and converts API error envelopes into safe user-facing messages. ViewModels contain no HTTP code.

## Current API coverage

- User authentication and local logout.
- Account and role administration.
- Person, enrollment, group, classroom, subject and staff administration.
- Versioned schedule reads, assignment creation, coverage needs and human-verification reads.
- Attendance, institutional access, presence and classroom movement queries.
- Manual contingency, incident and operational reads.
- Logical deactivation of supported administrative catalogs and audit reads.
- Server-calculated reports and statistics with period/population/denominator.
- Authorized class context and aggregated visual-result history.
- Aggregate visual-result reads without images or NFC identities.

The desktop-specific read model is exposed below `/api/workspace`; typed administrative mutations keep using `/api/admin/*`. NFC credential and station endpoints remain in the backend for future mobile integration but are not exposed in the desktop UI.

## Suggested adapter boundary

Keep `IAuthenticationService` and replace `IAulaInnovaDataService` with smaller domain contracts when the API contracts stabilize, for example people, schedules, records, reports and visual summaries. Map API DTOs to presentation models in the adapter. Do not bind Views directly to transport DTOs.

## Pending backend contracts

- HTTPS and server certificate validation.
- Refresh-token lifecycle and server-side token revocation.
- Scoped workspace reads for Profesor and Auxiliar; the current workspace facade is restricted to Administrador to avoid cross-group disclosure.
- Negative authorization tests for cross-student, cross-group and cross-classroom access.
- Omission of incident-author identity from any student-facing API response.
- Password recovery.
- Typed desktop writes for attendance, movements, incidents, coverage and visual context.
- Internal notices, acknowledgement and closing rules.

## Hardware and external dependencies

- Android NFC application for credential assignment and station setup using integrated phone NFC.
- PN532-ESP32 station firmware, logical credentials and local persistent queue where the fixed-station architecture remains applicable.
- Idempotent event ingestion using the same physical-read ID on retries.
- Visual-processing service that emits only approved aggregated indicators.
- Institutional authorization and approved parameters before real-person visual trials.

The desktop app should consume central results; it must not communicate directly with NFC readers, cameras or SQL Server.

## Runtime configuration

1. The default API base URL is `http://localhost:5077/`.
2. Override it with `AULAINNOVA_API_URL` when needed.
3. Configure `Jwt:Secret` and `BootstrapAdmin:*` with .NET User Secrets in the API project.
4. Start the API before the desktop client.
5. Use the smoke-test `--api` option for a real login/dashboard/report/schedule contract check.

## Decisions still required

- API versioning strategy.
- Production token storage and refresh strategy.
- Permission matrix by action and school scope.
- Production SQL Server edition, collation and hosting.
- Coverage verification owners, states and timing.
- Institutional-notice recipients, validity and closure.
- Visual indicator units, interval length, tolerances and method version.
- Final mobile NFC and station setup contract.
- Institutional approval of the provisional retention policy.
- Final incident uniqueness/index rule in the API contract, reflecting the latest approved thesis decision.
