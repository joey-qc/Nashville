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
| MudBlazor 9.4+ | UI component library (replaces Bootstrap entirely — do NOT include Bootstrap) |
| ApexCharts.Blazor 6.1+ | Charting and graphing library for all data visualizations |
| C# | Primary programming language |

> **Important:** MudBlazor is the sole UI component library. Bootstrap must NOT be included in scaffolding or referenced anywhere in the project.

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
- Tokens are stored client-side (in-memory or local storage — choose the more secure option per current Blazor best practices).
- Token expiration and refresh strategy should follow ASP.NET Core Identity best practices.

---

## 5. Data Models

### 5.1 Domain Entities (Momentum.Domain/Entities)

> **Note:** The `Category` C# enum has been removed. Categories are now a first-class database entity, seeded at migration time and returned to the client as `CategoryDto`.

### 5.2 Entity Models (Momentum.Domain/Entities)

#### Category
Represents a wellness category. Seeded at migration time — not user-editable.
```
- Id       : int (PK) — Physical=1, Mental=2, Spiritual=3, Social=4, Housekeeping=5
- Name     : string
- ColorHex : string  (hex color code, e.g. "#4CAF50")
```

#### ApplicationUser
Extends ASP.NET Core Identity's `IdentityUser`. Uses `AddIdentityCore` (not `AddIdentity`) to avoid cookie auth scheme conflict with JWT.
```
- DisplayName : string
- Theme : string  ("light" or "dark")
```

#### Activity
Represents an activity in the user's library.
```
- Id            : int (PK)
- UserId        : string (FK to ApplicationUser)
- Name          : string
- Description   : string? (optional, max 500 chars)
- Categories    : List<ActivityCategory> (join table)
- DefaultPoints : int (positive or negative)
- IsArchived    : bool (soft delete flag)
- CreatedAt     : DateTime
- UpdatedAt     : DateTime
```

#### ActivityCategory (Join Table)
```
- ActivityId  : int (FK to Activity)
- CategoryId  : int (FK to Category)
```

#### ActivityLog
Represents a single logged occurrence of an activity.
```
- Id              : int (PK)
- UserId          : string (FK to ApplicationUser)
- ActivityId      : int (FK to Activity)
- LoggedAt        : DateTime (date and time of the activity)
- PointsRecorded  : int (actual points at time of logging, may differ from default)
- Notes           : string? (optional)
- CreatedAt       : DateTime
```

### 5.3 DTOs (Momentum.Shared)
All data transferred between client and server uses DTOs (Data Transfer Objects), not entity models directly. DTOs are defined in the Momentum.Shared project and referenced by both Client and Server projects.

Key DTOs include:
- `ActivityDto` — for activity library data; includes `Description`, `List<CategoryDto> Categories`
- `ActivityLogDto` — for log entry data; includes `List<CategoryDto> Categories`
- `CategoryDto` — for category data (Id, Name, ColorHex); returned by `GET /api/categories`
- `ScoreSummaryDto` — for landing page score totals
- `DailyScoreDto` — for reporting chart data points; category breakdown keyed by `int` (CategoryId)
- `UserSettingsDto` — for profile and settings
- `LoginRequestDto` / `LoginResponseDto` — for authentication
- `RegisterRequestDto` — for new user registration
- `CreateActivityDto` / `UpdateActivityDto` — include optional `Description` (`[MaxLength(500)]`), use `List<int> CategoryIds` (not enum values)

---

## 6. Data Access Layer

### 6.1 DbContext
- `AppDbContext` (in `Momentum.Infrastructure`) inherits from `IdentityDbContext<ApplicationUser>`.
- Registered in the API project's dependency injection container.
- Connection string is read from configuration (see Section 9).
- `Category` rows are seeded via `HasData` — five fixed rows matching the color scheme table.

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
- **Balance** (`/reports/balance`) — pie chart of category point distribution for the selected period (week/month/year); uses `ReportsService.GetBalanceAsync`.

Both pages use `@using ApexCharts` and must qualify conflicting MudBlazor types (see CLAUDE.md Section 7.1).

### 8.3 Routing & Authentication
- Blazor routing is used for client-side navigation.
- All routes except `/login` and `/register` are protected with an `[Authorize]` attribute.
- Unauthenticated users are redirected to `/login`.

### 8.4 Theme Support
- MudBlazor's built-in theming is used for light and dark mode.
- The user's theme preference is loaded from their settings on login and applied application-wide.
- Theme changes in Settings are saved to the server and applied immediately without requiring a page reload.

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
- MudBlazor Snackbar or Dialog components are used to display error and success notifications.

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
*Version 1.3 — Added GET /api/logs/{id} and GET /api/reports/balance endpoints; added Balance report page documentation*
