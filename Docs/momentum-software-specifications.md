# Momentum — Software Specifications Document

## 1. Overview

**Momentum** is a multi-user personal wellness tracking web application. This document describes the technical architecture, frameworks, layers, and implementation decisions that govern how the application is built.

---

## 2. Solution Architecture

The application is structured as a **Visual Studio Solution** containing multiple projects with clear separation of concerns.

---

## 3. Technology Stack

### 3.1 Frontend

| Technology | Purpose |
|---|---|
| Blazor WebAssembly (.NET 10) | SPA frontend running in the browser |
| Custom HTML/CSS | All UI markup — plain HTML elements styled with scoped CSS classes and shared design tokens |
| `momentum-theme.css` | Global design token file — CSS custom properties for all colors, radii, and spacing |
| Custom inline SVG | All charts (bar, line, donut, sparkline) and all icons rendered as inline SVG in Razor components |
| MudBlazor (`ISnackbar` only) | Toast notifications only — retained until a custom `ToastHost` component is implemented; no other MudBlazor components are used |
| C# | Primary programming language |

> **Important:** All pages use custom HTML/CSS — MudBlazor page components have been fully removed. Bootstrap must NOT be included. All data visualizations use custom SVG — do not introduce ApexCharts.Blazor or any other charting library. The `Blazor-ApexCharts` NuGet package has been fully removed (KI-010, commit `6b4c29f`).

### 3.2 Backend

| Technology | Purpose |
|---|---|
| ASP.NET Core Web API (.NET 10) | RESTful API layer |
| C# | Primary programming language |
| ASP.NET Core Identity (AddIdentityCore) | User account management |
| JWT (JSON Web Tokens) | Authentication and authorization |
| Serilog | Server-side logging |

### 3.3 Data Access

| Technology | Purpose |
|---|---|
| Entity Framework Core (.NET 10) | ORM for database access |
| LINQ | Query language for data access |
| SQL Server | Relational database (production) |
| SQL Server LocalDB or SQL Server Express | Local development database |

---

## 4. Authentication & Authorization

### 4.1 Authentication Strategy
- **JWT (JSON Web Tokens)** are used for authentication between the Blazor WASM client and the ASP.NET Core API.
- Users log in with email and password. The API validates credentials and returns a signed JWT token.
- The Blazor client stores the token and includes it in the Authorization header of all subsequent API requests.

### 4.2 ASP.NET Core Identity
- ASP.NET Core Identity manages user accounts, password hashing, and credential validation.
- The Identity user model is extended to include a `DisplayName` field for personalization.

### 4.3 Authorization
- All API endpoints (except login and registration) require a valid JWT token.
- All data queries are automatically scoped to the authenticated user's ID — no user can access another user's data.
- Blazor routes are protected; unauthenticated users are redirected to the login page.

### 4.4 Token Management
- Tokens are stored in **`localStorage`** under the key `"authToken"` — persists across browser sessions until expiry or explicit logout.
- **JWT lifetime:** 10,080 minutes (7 days), configured via `Jwt:ExpiryMinutes` in `appsettings.json`; overridable via Azure App Service environment variable `Jwt__ExpiryMinutes`.
- `JwtAuthStateProvider.GetAuthenticationStateAsync` checks `jwt.ValidTo < DateTime.UtcNow` on every auth-state evaluation. Expired tokens are immediately removed from `localStorage` and the user is treated as anonymous.
- `AuthMessageHandler` intercepts any API 401 response and calls `MarkUserAsLoggedOut()`, which removes the token from `localStorage` and broadcasts logged-out state.
- **Refresh tokens are not implemented.** Long-term plan documented in `Docs/session-persistence-design-spec.md` §6 — to be implemented before PWA/mobile work.

### 4.5 AI Integration Authentication (AI-001 v1)
- `GET /api/ai/today` (`AiController`, `Momentum.API`) uses a **shared API key** in the `X-Momentum-AI-Key` request header — separate from JWT Bearer auth, since this is a single server-to-server integration rather than a per-user login session.
- The configured key is compared against the request header using a fixed-time comparison (`CryptographicOperations.FixedTimeEquals`) to avoid a timing side-channel; a missing or mismatched key returns `401 Unauthorized`.
- The endpoint resolves a single **configured AI user** via `UserManager<ApplicationUser>.FindByEmailAsync(Ai:UserEmail)` — it does not accept a user identifier from the caller. If the key is valid but `Ai:ApiKey`, `Ai:UserEmail` is unset, or the configured email doesn't resolve to a user, the endpoint returns `500` (treated as a configuration error, not a caller error).
- No refresh/rotation mechanism exists in v1 — rotating the key means updating the `Ai:ApiKey` environment variable and restarting the API.

---

## 5. Data Models

### 5.1 Domain Entities (Momentum.Domain/Entities)

> **Note:** Dimensions are a first-class database entity seeded at migration time. The old `Category` C# enum has been removed. Dimensions are returned to the client as `CategoryDto` (the DTO retains its original name for API compatibility). Persisted names now match user-facing names (Body / Mind / Spirit / Connections / Responsibilities) since migration `DIM001_RenameDimensions`.

### 5.2 Entity Models (Momentum.Domain/Entities)

#### Dimension
Represents a wellness dimension. Seeded at migration time — not user-editable.
```
- Id       : int (PK) — Body=1, Mind=2, Spirit=3, Connections=4, Responsibilities=5
- Name     : string   — persisted name matches user-facing display name (aligned by DIM-001 migration)
- ColorHex : string  (hex color code, e.g. "#76E04A")
```

#### ApplicationUser
Extends ASP.NET Core Identity's `IdentityUser`. Uses `AddIdentityCore` (not `AddIdentity`) to avoid cookie auth scheme conflict with JWT.
```
- DisplayName : string
```

#### Activity
Represents an activity in the user's library.
```
- Id            : int (PK)
- UserId        : string (FK to ApplicationUser)
- Name          : string
- Description   : string? (optional, max 500 chars)
- Dimensions    : List<ActivityDimension> (join table)
- DefaultPoints : int (positive or negative)
- IsArchived    : bool (soft delete flag)
- CreatedAt     : DateTime
- UpdatedAt     : DateTime
```

#### ActivityDimension (Join Table)
```
- ActivityId  : int (FK to Activity)
- DimensionId : int (FK to Dimension)
```

#### ActivityLog
Represents a single logged occurrence of an activity.
```
- Id              : int (PK)
- UserId          : string (FK to ApplicationUser)
- ActivityId      : int (FK to Activity)
- LoggedAt        : DateTime (date and time of the activity)
- PointsRecorded  : int (actual points at time of logging, may differ from default)
- Notes           : string? (optional) — stored as sanitized HTML; blank/whitespace-only is normalized to NULL by ActivityLogService
- CreatedAt       : DateTime
- CheckIns        : ICollection<CheckIn> (navigation — zero or many check-ins linked to this log entry)
```

#### CheckIn
Represents a point-in-time state snapshot (outcomes/how the user feels). Separate from ActivityLog which captures behaviors/inputs. Added by migration `CHK001_AddCheckIn`.
```
- Id            : int (PK)
- UserId        : string (FK to ApplicationUser)
- CheckedInAt   : DateTime — user-editable effective timestamp; used for analytics, display, and sorting
- BodyScore     : int (-5…+5, 0 = baseline)
- EnergyScore   : int (-5…+5, 0 = baseline)
- MoodScore     : int (-5…+5, 0 = baseline)
- ActivityLogId : int? (nullable FK to ActivityLog — SetNull on delete; null = standalone check-in)
- CreatedAt     : DateTime — internal audit timestamp; never used for analytics or display
- ActivityLog   : ActivityLog? (optional navigation)
```
Indexes: `IX_CheckIns_UserId` (all queries scoped by user), `IX_CheckIns_CheckedInAt` (time-ordered display), `IX_CheckIns_ActivityLogId` (FK lookup).

### 5.3 DTOs (Momentum.Shared)
All data transferred between client and server uses DTOs (Data Transfer Objects), not entity models directly. DTOs are defined in the Momentum.Shared project and referenced by both Client and Server projects.

Key DTOs include:
- `ActivityDto` — for activity library data; includes `Description`, `List<CategoryDto> Categories`
- `ActivityLogDto` — for log entry data; includes `List<CategoryDto> Categories` (the log entry's saved dimension snapshot)
- `CategoryDto` — for dimension data (Id, Name, ColorHex); returned by `GET /api/categories`; named `CategoryDto` for historical reasons
- `ScoreSummaryDto` — for landing page score totals
- `DailyScoreDto` — for reporting chart data points; dimension breakdown keyed by `int` (DimensionId)
- `UserSettingsDto` — for profile and settings
- `LoginRequestDto` / `LoginResponseDto` — for authentication
- `RegisterRequestDto` — for new user registration
- `CreateActivityDto` / `UpdateActivityDto` — include optional `Description` (`[MaxLength(500)]`), use `List<int> CategoryIds` for dimension selection (field named `CategoryIds` for historical compatibility)
- `CreateActivityLogDto` — includes optional `List<int>? DimensionIds`; when provided the submitted IDs become the log entry's dimension snapshot; when null the activity's current dimensions are used as the default; `Notes` has `[MaxLength(10000)]`
- `UpdateActivityLogDto` — includes optional `List<int>? DimensionIds`; when provided the entry's snapshot is fully replaced; when null and the activity changed, snapshot is re-derived from the new activity; when null and activity unchanged, existing snapshot is preserved; `Notes` has `[MaxLength(10000)]`
- `ActivityLogService.SanitizeNotes()` — `public static` helper; applied to Notes on every create and update; strips disallowed HTML tags/attributes via `HtmlSanitizer` (allowlist: p, br, strong, em, b, i, u, ul, ol, li — includes `b`/`i` because browser `execCommand` outputs these); uses `KeepChildNodes = true` so structural wrappers (e.g. the `<div>` `execCommand` puts around a list when text precedes it) are unwrapped while allowed children survive; script/style tags are still removed; normalizes blank content to null
- `RichNotesEditor` — Blazor component (`Momentum.Client/Components/`) replacing the plain textarea on Add/Edit Log Entry; `contenteditable` + custom toolbar; JS interop via `wwwroot/js/richNotesEditor.js`; parent reads content via `GetContentAsync()` at submit time; `ShouldRender()` returns false after first render to prevent Blazor from overwriting user edits
- `CheckInDto` — response DTO for a check-in record; includes `Id`, `UserId`, `CheckedInAt`, `BodyScore`/`EnergyScore`/`MoodScore`, `ActivityLogId`, `ActivityName` (display-only — the linked activity's name, null for standalone), and `CreatedAt` (audit-only)
- `CreateCheckInRequestDto` — `CheckedInAt?` (optional, defaults to server UTC now), `BodyScore`/`EnergyScore`/`MoodScore` (int, `[Range(-5, 5)]`), `ActivityLogId?` (optional)
- `UpdateCheckInRequestDto` — `CheckedInAt` (required), scores with `[Range(-5, 5)]`, `ActivityLogId?`; pass null ActivityLogId to detach from a log entry
- `AiTodayResponseDto` / `AiTodayEntryDto` (AI-001) — AI-safe response for `GET /api/ai/today`; see §7.2 for the full shape and the fields deliberately excluded (Notes, UserId, ActivityId, log Id, CreatedAt, user profile data)

---

## 6. Data Access Layer

### 6.1 DbContext
- `AppDbContext` (in `Momentum.Infrastructure`) inherits from `IdentityDbContext<ApplicationUser>`.
- Registered in the API project's dependency injection container.
- Connection string is read from configuration (see Section 9).
- `Dimension` rows are seeded via `HasData` — five fixed rows. Persisted names (Body/Mind/Spirit/Connections/Responsibilities) now match user-facing display names. IDs and colors are immutable.

### 6.2 Repository Pattern
- Each entity has a corresponding repository interface and implementation.
- Repositories encapsulate all EF Core and LINQ queries.
- Interfaces are defined in `Momentum.Application/Interfaces/`; implementations are in `Momentum.Infrastructure/Repositories/`.

Key repositories:
- `IActivityRepository` / `ActivityRepository`
- `IActivityLogRepository` / `ActivityLogRepository`
- `ICheckInRepository` / `CheckInRepository` (CHK-002 Phase 2; `GetByIdAsync`/`GetByDateRangeAsync` eager-load `ActivityLog.Activity` so `CheckInDto.ActivityName` can be projected — Phase 5B)

`IAiWellnessQueryService` / `AiWellnessQueryService` (`Momentum.Application/Services`, AI-001) is **not** a repository — it's an application service that reuses the existing `IActivityLogRepository.GetByDateRangeAsync` (no new repository or raw SQL was added).

### 6.3 Migrations
- EF Core migrations are used to manage database schema changes.
- Migration files are stored in `Momentum.Infrastructure/Migrations/`.

---

## 7. API Layer (Momentum.API)

### 7.1 API Design
- RESTful API design.
- All endpoints are prefixed with `/api/`.
- All endpoints (except auth) require JWT Bearer token authorization.
- Responses use standard HTTP status codes.

### 7.2 Endpoints

#### Authentication
| Method | Route | Description |
|---|---|---|
| POST | /api/auth/register | Register a new user account |
| POST | /api/auth/login | Authenticate and return JWT token |

#### Categories
| Method | Route | Description |
|---|---|---|
| GET | /api/categories | Get all categories (Id, Name, ColorHex) |

#### Activities (Library Management)
| Method | Route | Description |
|---|---|---|
| GET | /api/activities | Get all active activities for the current user |
| GET | /api/activities/frequent | Get most frequently logged activities (for quick pick) |
| POST | /api/activities | Create a new activity |
| PUT | /api/activities/{id} | Update an existing activity |
| DELETE | /api/activities/{id} | Delete or archive an activity (see deletion logic) |

#### Activity Logs
| Method | Route | Description |
|---|---|---|
| GET | /api/logs | Get activity logs (supports date range query params) |
| GET | /api/logs/{id} | Get a single log entry by ID |
| POST | /api/logs | Create a new log entry |
| PUT | /api/logs/{id} | Update an existing log entry |
| DELETE | /api/logs/{id} | Delete a log entry |

#### Score Summary
| Method | Route | Description |
|---|---|---|
| GET | /api/scores/summary | Get today, this week, and this month totals |
| GET | /api/scores/weekly-comparison | Get current week vs last week comparison data |

#### Reporting
| Method | Route | Description |
|---|---|---|
| GET | /api/reports/daily?days=30&categoryId= | Daily totals for the past N days, optionally filtered by category ID |
| GET | /api/reports/weekly?weeks=52&categoryId= | Weekly totals for the past N weeks, optionally filtered by category ID |
| GET | /api/reports/monthly?months=12&categoryId= | Monthly totals for the past N months, optionally filtered by category ID |
| GET | /api/reports/balance?period=week\|month\|year | Category point totals for the Balance pie chart |

#### Check-Ins (CHK-002 Phase 2)
| Method | Route | Description |
|---|---|---|
| GET | /api/checkins | Get check-ins for the current user (supports `from`/`to` date range query params) |
| GET | /api/checkins/{id} | Get a single check-in by ID |
| POST | /api/checkins | Create a new check-in |
| PUT | /api/checkins/{id} | Update an existing check-in |
| DELETE | /api/checkins/{id} | Delete a check-in |

Score fields (`BodyScore`, `EnergyScore`, `MoodScore`) must each be in `[-5, 5]`; the API returns `400` if out of range. If `ActivityLogId` is provided, the referenced log must belong to the current user; the API returns `400` otherwise. `UserId` is always derived from JWT claims — never trusted from the request body. `CreatedAt` is set server-side at creation and never exposed as editable.

#### User Settings
| Method | Route | Description |
|---|---|---|
| GET | /api/settings | Get current user's settings and profile |
| PUT | /api/settings | Update user profile and settings |

#### AI Integration (AI-001 — read-only v1)
| Method | Route | Description |
|---|---|---|
| GET | /api/ai/today?localOffsetMinutes= | AI-safe snapshot of the configured AI user's activity logs for the local-calendar-day (today) |

`GET /api/ai/today` is intentionally **not** part of the JWT bearer pipeline — it is a single-configured-user, server-to-server integration point, not a per-end-user endpoint. It uses its own authentication mechanism (see §4.5) and is registered with `[AllowAnonymous]` so the JWT scheme is never evaluated for it. `localOffsetMinutes` is optional; when omitted, the server falls back to `Ai:DefaultLocalOffsetMinutes` from configuration (default `0`/UTC if that's also unset).

Response shape (`AiTodayResponseDto`, `Momentum.Shared`):
```
- Date        : DateOnly       — the resolved local calendar day
- TotalPoints : int            — sum of PointsRecorded for the day
- EntryCount  : int            — number of activity log entries for the day
- Entries     : List<AiTodayEntryDto>
    - LoggedAt     : DateTime  — UTC instant
    - ActivityName : string
    - Points       : int       — PointsRecorded for this entry
    - Dimensions   : List<string> — dimension names for this entry's saved snapshot
```
**Deliberately excluded** from the response: `Notes` (journal/rich-text content), `UserId`, `ActivityId`, `ActivityLog.Id`, `CreatedAt`, and any `ApplicationUser` profile fields. `IAiWellnessQueryService`/`AiWellnessQueryService` (`Momentum.Application`) build this DTO directly from `IActivityLogRepository.GetByDateRangeAsync` — no raw SQL, no new repository method.

### 7.3 Activity Deletion Logic (Server-Side)
When a DELETE request is received for an activity:
1. Query the database for any `ActivityLog` records associated with this activity and user.
2. If no logs exist: permanently delete the activity.
3. If logs exist: return a `409 Conflict` response with a payload indicating the number of associated logs and the two available options (archive or cascade delete). The client presents these options to the user.
4. A separate endpoint or query parameter handles the confirmed cascade delete.

---

## 8. Frontend Layer (Momentum.Client)

### 8.2 Client-Side Caching
- On successful login, the client fetches the user's full activity library (`/api/activities`) and the category list (`/api/categories`) in parallel and caches both in memory.
- `ActivityService` caches activities; `CategoryService` caches categories. Both live in `Momentum.Client/Services/`.
- Both services use a lazy-load pattern: `GetAllAsync()` returns the cached value or fetches if null — components do not need to manage load order.
- The activity cache is invalidated and refreshed whenever the user adds, edits, archives, or deletes an activity in the Manage Activities screen.
- The category cache is stable (categories are seeded and not user-editable) and only cleared on logout.

### 8.2a Reports Pages
The Reports section of the client contains two distinct pages:
- **Trends** (`/reports`) — bar chart of daily/weekly/monthly point totals with category filter; uses `ReportsService.GetDailyAsync`, `GetWeeklyAsync`, `GetMonthlyAsync`.
- **Balance** (`/reports/balance`) — donut chart of category point distribution for the selected period (week/month/year); uses `ReportsService.GetBalanceAsync`.

Both pages render charts as **custom inline SVG** — no charting library is imported.

### 8.2b Check-In Pages & Flow
- `CheckInService` (`Momentum.Client/Services/`) wraps `/api/checkins`: `CreateAsync` (POST), `GetAllAsync` (wide-range GET, newest first), `GetMostRecentAsync` (first of `GetAllAsync`, for form preload), `UpdateAsync` (PUT), `DeleteAsync` (DELETE).
- **Check-Ins history (`/check-ins`, `CheckIns.razor` — CHK-002 Phase 5B):** lists check-ins newest first via `GetAllAsync`; shows Body/Energy/Mood and `After: {ActivityName}` for linked entries. Inline edit (date/time + scores via `UpdateAsync`, preserving `ActivityLogId`) and delete (trash→confirm via `DeleteAsync`). Accepts `?editId={id}` to auto-open that row's inline editor (used by View Log). No charts/search/pagination.
- **View Log Details integration (`ActivityDetail.razor` — CHK-002 Phase 6A):** the "Details" toggle (renamed from "Notes") reveals, per entry, the note plus the entry's linked check-ins and a "+ Add Check-In" action. View Log loads check-ins via `CheckInService.GetAllAsync()` and groups them client-side by `ActivityLogId` (no DTO/API change). Row click → `/check-ins?editId={id}`; delete → `DeleteAsync`; add → `/check-in?activityLogId={logId}&from={name}`.
- **Return-context pattern:** `CheckIn.razor` and `CheckIns.razor` accept an optional `returnUrl` query param; when present they navigate there after save/skip/cancel (otherwise default behavior). View Log passes an encoded `returnUrl` (`/log/detail?period={p}&details=true`) so add/edit flows return to the same view with Details expanded. Generic and reusable by any caller.
- **Time handling:** check-ins are stored UTC. `CheckInService.Map` marks `CheckedInAt`/`CreatedAt` as `DateTimeKind.Utc` (EF returns them `Unspecified`) so the `UtcDateTimeConverter` serializes a correct `Z` without re-converting against the server timezone — this prevented a double-shift on non-UTC hosts (KI-017). The client UTC-tags on read, sorts newest-first by the true instant, displays via `ToLocalTime()`, and converts edits back with `SpecifyKind(Local).ToUniversalTime()`.
- `/check-in` (`CheckIn.razor`) is the Check-In form. It accepts two optional query parameters:
  - `activityLogId` (int) — when present, the saved check-in is linked to that activity log (post-activity flow); when absent, the check-in is standalone.
  - `from` (string) — display-only activity name for the "After: {activity}" context label.
- **Post-activity flow (CHK-002 Phase 4):** on a successful **new** log creation, `LogActivity.razor` navigates to `/check-in?activityLogId={newId}&from={name}`. In linked mode the form offers Save or **Skip**; both then navigate to Home (`/`). Skipping creates no check-in. The **edit** log path does not enter this flow (returns to View Log). Standalone `/check-in` behavior is unchanged (stays on page after save).
- No new API surface — ownership of `activityLogId` is enforced by the existing `CheckInService` server validation (400 if the log is not the user's).

### 8.3 Routing & Authentication
- Blazor routing is used for client-side navigation.
- All routes except `/login` and `/register` are protected with an `[Authorize]` attribute.
- Unauthenticated users are redirected to `/login`.

### 8.4 Theme
- The application uses **permanent dark mode** defined entirely via CSS custom properties in `Momentum.Client/wwwroot/css/momentum-theme.css`.
- There is no theme toggle or per-user theme preference — `ApplicationUser` does not store a `Theme` field.
- All colors, radii, and spacing are consumed as `var(--token-name)` references throughout scoped page CSS files. MudBlazor theming (`MudThemeProvider`) is not used for visual styling.

---

## 9. Configuration & Environment

### 9.1 AI Integration Configuration (AI-001)
Read by `AiController` from `IConfiguration`:

| Key | Sensitive? | Where set |
|---|---|---|
| `Ai:ApiKey` | Yes — shared secret | Environment variable only (`Ai__ApiKey`) — **omitted from `appsettings.json`**, same convention as `Jwt:Key` |
| `Ai:UserEmail` | Treat as sensitive (identifies a real account) | Environment variable only (`Ai__UserEmail`) — omitted from `appsettings.json` |
| `Ai:DefaultLocalOffsetMinutes` | No | `appsettings.json` (default `"0"`); overridable via `Ai__DefaultLocalOffsetMinutes` |

If `Ai:ApiKey` or `Ai:UserEmail` is unset (or the configured email doesn't resolve to a user), `GET /api/ai/today` returns `500` rather than silently falling back to any default — there is no built-in fallback user or key.

### 9.3 Production Configuration
- Connection strings and secrets are stored as **Azure App Service environment variables** — never hardcoded or committed to source control.
- Production database is **Azure SQL Database**.
- Frontend is hosted on **Azure Static Web Apps** or **Azure App Service**.

---

## 10. Logging & Error Handling

### 10.1 Server-Side Logging
- **Serilog** is used for all server-side logging.
- Logs are written to both the console (for development) and rolling daily log files.
- The following events are always logged:
  - API requests and responses (at Information level)
  - Authentication events (login, logout, failed attempts)
  - Database errors and exceptions
  - Unhandled exceptions

### 10.2 Client-Side Error Handling
- API errors are caught in client service classes and surfaced to the UI as friendly error messages.
- No technical stack traces or exception details are shown to the user.
- `ISnackbar` (MudBlazor, retained temporarily) is used to display error and success toast notifications. This will be replaced by a custom `ToastHost` component once implemented (see KI-009).

### 10.3 Global Exception Handler
- A global exception handling middleware is registered in the ASP.NET Core pipeline.
- Unhandled exceptions return a standardized JSON error response with an appropriate HTTP status code.

---

## 11. Security Requirements

- All API endpoints except `/api/auth/login`, `/api/auth/register`, and `GET /api/ai/today` require JWT Bearer authentication. `GET /api/ai/today` instead requires a valid `X-Momentum-AI-Key` header (see §4.5) — it is `[AllowAnonymous]` with respect to the JWT scheme by design, not an oversight.
- All database queries are automatically filtered by the authenticated user's ID — row-level data isolation is enforced at the repository layer.
- Passwords are hashed using ASP.NET Core Identity's default PBKDF2 hashing.
- JWT secrets are stored in configuration and never exposed to the client.
- The AI API key (`Ai:ApiKey`) is likewise stored only in configuration/environment variables, never committed to source control, and never returned in any response.
- HTTPS is enforced in production.
- CORS is configured to allow requests only from the known client origin. `GET /api/ai/today` is a server-to-server endpoint and is not expected to be called from the Blazor client, but is still subject to the same CORS policy as all other API routes.

---

## 12. Testing (Momentum.Tests)

- Unit tests are written using **xUnit**, with **NSubstitute** for mocking dependencies.
- Key areas to test:
  - Repository methods (data access logic)
  - API controller actions
  - Score calculation logic
  - Activity deletion business rules
  - Authentication flows
- `Momentum.Tests` references `Momentum.API` and takes a `FrameworkReference` to `Microsoft.AspNetCore.App` (added for AI-001) so controller-level behavior — not just service-level logic — can be unit tested directly (e.g. `AiControllerTests` constructs `AiController` with a `DefaultHttpContext` and asserts on the returned `IActionResult`, without a full `WebApplicationFactory`/HTTP host). `UserManager<ApplicationUser>` is mocked via `Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null)` — its members are virtual, so this is a standard, well-documented NSubstitute pattern.

---

## 13. Future Considerations (Out of Scope for v1)

The following are noted for future development and should be kept in mind when making architectural decisions — avoid patterns that would make these difficult to add later:

- Integration with wearable devices (Fitbit, Apple Watch, etc.) for biometric data
- Calendar app integration
- Mobile application (MAUI Blazor Hybrid)
- Push notifications and activity reminders
- Data export (CSV, PDF)
- AI-generated wellness insights — **a minimal read-only foundation now exists** (AI-001, `GET /api/ai/today`, §7.2/§4.5); actual AI-generated analysis/insights, additional query endpoints (e.g. date ranges, trends), and any write capability remain future work

---

*Momentum — Software Specifications Document*
*Version 1.8 — §5.3, §6.2, §7.2: CheckIn DTOs, repository, and API endpoints added (CHK-002 Phase 2)*
*Version 1.9 — §8.2b: Check-In client service, page query params, and post-activity flow documented (CHK-002 Phase 4); no API/schema change*
*Version 1.10 — §5.3/§6.2/§8.2b: CheckInDto gains ActivityName (display-only, projected from eager-loaded ActivityLog.Activity); Check-Ins history screen with edit/delete (CHK-002 Phase 5B); no new endpoints*
*Version 1.11 — §8.2b: documented check-in UTC time handling (CheckInService.Map marks timestamps Utc; client sorts/displays in local time) — fixes KI-017 double-shift on non-UTC hosts*
*Version 1.12 — §8.2b: View Log Details integration (client-side grouping of check-ins by ActivityLogId; `/check-ins?editId`) — CHK-002 Phase 6A; no DTO/API change*
*Version 1.13 — §8.2b: generic `returnUrl` navigation pattern for check-in add/edit (returns to View Log context) — CHK-002 Phase 6A polish*
*Version 1.14 — AI-001: §4.5 (AI API-key auth), §5.3 (AiTodayResponseDto/AiTodayEntryDto), §6.2 (AiWellnessQueryService reuses IActivityLogRepository), §7.2 (GET /api/ai/today), §9.1 (Ai:ApiKey/UserEmail/DefaultLocalOffsetMinutes config), §11 (security notes), §12 (Momentum.Tests now references Momentum.API + AspNetCore.App for controller tests), §13 (AI-generated insights foundation) added*
