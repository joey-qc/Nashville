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

> **Important:** All pages use custom HTML/CSS — MudBlazor page components have been fully removed. Bootstrap must NOT be included. All data visualizations use custom SVG — do not introduce ApexCharts.Blazor or any other charting library. The `Blazor-ApexCharts` NuGet package is a leftover reference and should be removed (KI-010).

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
```

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

#### User Settings
| Method | Route | Description |
|---|---|---|
| GET | /api/settings | Get current user's settings and profile |
| PUT | /api/settings | Update user profile and settings |

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

- All API endpoints except `/api/auth/login` and `/api/auth/register` require JWT Bearer authentication.
- All database queries are automatically filtered by the authenticated user's ID — row-level data isolation is enforced at the repository layer.
- Passwords are hashed using ASP.NET Core Identity's default PBKDF2 hashing.
- JWT secrets are stored in configuration and never exposed to the client.
- HTTPS is enforced in production.
- CORS is configured to allow requests only from the known client origin.

---

## 12. Testing (Momentum.Tests)

- Unit tests are written using **xUnit**.
- Key areas to test:
  - Repository methods (data access logic)
  - API controller actions
  - Score calculation logic
  - Activity deletion business rules
  - Authentication flows

---

## 13. Future Considerations (Out of Scope for v1)

The following are noted for future development and should be kept in mind when making architectural decisions — avoid patterns that would make these difficult to add later:

- Integration with wearable devices (Fitbit, Apple Watch, etc.) for biometric data
- Calendar app integration
- Mobile application (MAUI Blazor Hybrid)
- Push notifications and activity reminders
- Data export (CSV, PDF)
- AI-generated wellness insights

---

*Momentum — Software Specifications Document*
*Version 1.6 — §4.4: updated token management to reflect AUTH-001 — 7-day JWT lifetime, localStorage storage confirmed, stale-token proactive cleanup, refresh tokens documented as not-yet-implemented*
