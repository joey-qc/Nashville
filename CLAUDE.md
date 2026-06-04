# CLAUDE.md — Momentum Project Skills File

This file defines the coding conventions, architectural patterns, design rules, and technology decisions for the **Momentum** wellness tracking application. Claude must read and follow all instructions in this file for every task in this project, regardless of how the request is phrased.

---

## Startup Instruction

Before performing any implementation task:
1. Read this entire CLAUDE.md file.
2. Read any referenced documentation files relevant to the requested task.
3. Treat CLAUDE.md as the authoritative project operating guide.

## 1. Project Identity

- **Application Name:** Momentum
- **Purpose:** Personal wellness activity tracker with weighted scoring across life categories
- **Architecture:** Blazor WebAssembly frontend + ASP.NET Core Web API backend + SQL Server database
- **Multi-user:** All data is scoped to the authenticated user. Never query or return data without filtering by the current user's ID.

---

## 3. Technology Rules

### 3.1 UI Components — Custom HTML/CSS (MudBlazor being phased out)
Momentum is moving away from MudBlazor toward fully custom Razor markup, plain HTML/CSS, shared design tokens (`momentum-theme.css`), and inline SVG.

**For all new code and any page being modified or converted:**
- Use plain HTML elements (`<button>`, `<input>`, `<textarea>`, `<select>`, etc.) styled with scoped CSS classes.
- Use CSS variables from `Momentum.Client/wwwroot/css/momentum-theme.css` — never hardcode colors, radii, or spacing.
- Use inline SVG for all icons and charts — no icon font or third-party icon library.
- **Do NOT introduce any MudBlazor components or packages.** MudBlazor has been fully removed (KI-009, 2026-06-04). No exceptions.
- **Bootstrap must NEVER be used or referenced** anywhere in the project — not in markup, not in CSS, not in NuGet packages.

**All pages use custom HTML/CSS:** Home, Add Entry (`/log`), View Log (`/log/detail`), Trends (`/reports`), Balance (`/reports/balance`), Manage Activities (`/activities`), Settings (`/settings`), Login (`/login`), Register (`/register`).
**Toast notifications** use the native `ToastService` + `ToastHost`. Inject `ToastService` and call `Toast.Show("message", ToastType.Success/Error/Warning/Info)`. Never use MudBlazor.

### 3.2 Charting — Custom SVG
- All charts (bar, line, donut, sparkline) are implemented as **custom inline SVG** rendered directly in Razor components.
- ApexCharts.Blazor, Chart.js, Plotly, and all third-party charting libraries must NOT be used. The `Blazor-ApexCharts` NuGet package has been removed (KI-010, 2026-06-04).
- Chart data is fetched via `ReportsService` and `ScoreService`; rendering is done with SVG markup and C# computed layout values.

### 3.3 Backend Framework
- **ASP.NET Core Web API (.NET 10)**
- All API endpoints are prefixed with `/api/`
- Use attribute routing on all controllers
- Return appropriate HTTP status codes (200, 201, 204, 400, 401, 404, 409, 500)

### 3.4 ORM — Entity Framework Core
- **Entity Framework Core** is the ORM. Never use raw ADO.NET or Dapper.
- All queries use **LINQ** — never raw SQL strings unless absolutely necessary for performance.
- All migrations live in `Momentum.Infrastructure/Migrations/`
- Always use async/await for all database operations (`ToListAsync`, `FirstOrDefaultAsync`, etc.)

### 3.5 Authentication — JWT
- **JWT Bearer tokens** are used for all authentication.
- **ASP.NET Core Identity** manages user accounts and password hashing.
- All controllers except `AuthController` must have `[Authorize]` attribute.
- Never expose password hashes or JWT secrets in responses or logs.

### 3.6 Logging — Serilog
- **Serilog** is used for all server-side logging.
- Never use `Console.WriteLine` for logging — always use the injected `ILogger`.
- Log authentication events, API errors, and database exceptions at minimum.

---

## 4. Category Color Scheme

These colors are the single source of truth for category representation throughout the entire application. Apply them consistently in charts, chips, badges, and any other UI element that represents a category.

| Category | Color | Hex |
|---|---|---|
| Physical | Bright Green | #76E04A |
| Mental | Sky Blue | #5BC8FF |
| Spiritual | Soft Purple | #B894FF |
| Social | Amber | #F7B500 |
| Housekeeping | Salmon | #FF9472 |
| All (reporting filter) | Medium Gray | #9E9E9E |

- Category colors are stored in the `Categories` database table and returned via `CategoryDto.ColorHex`. Never hardcode hex values inline in components — always read the color from the `CategoryDto` returned by the API or the client `CategoryService`.
- The `Categories` table is seeded with these five rows (IDs 1–5 in the order above) via EF Core `HasData` in `AppDbContext`. They are not user-editable.

---

## 5. Naming Conventions

### C# General
- **PascalCase** for class names, method names, properties, and constants
- **camelCase** for local variables and method parameters
- **_camelCase** (underscore prefix) for private fields
- Interfaces are prefixed with `I` (e.g., `IActivityRepository`)
- Async methods are suffixed with `Async` (e.g., `GetActivitiesAsync`)

### Blazor Components
- Component files use **PascalCase** (e.g., `ActivityCard.razor`, `CategoryChip.razor`)
- Component parameters use **PascalCase**
- Event callbacks are named with `On` prefix (e.g., `OnActivitySelected`)

### API Controllers
- Controllers are named with `Controller` suffix (e.g., `ActivitiesController`)
- Action methods are named clearly by HTTP verb and resource (e.g., `GetActivities`, `CreateActivity`, `UpdateActivity`, `DeleteActivity`)

### DTOs
- DTOs are suffixed with `Dto` (e.g., `ActivityDto`, `ActivityLogDto`)
- Request DTOs are suffixed with `RequestDto` (e.g., `LoginRequestDto`)
- Response DTOs are suffixed with `ResponseDto` or just `Dto`

### Database Entities
- Entity class names match the table name in singular form (e.g., `Activity`, `ActivityLog`)
- Primary keys are always named `Id`
- Foreign keys follow the pattern `{EntityName}Id` (e.g., `ActivityId`, `UserId`)

---

## 6. Architectural Patterns

### 6.2 Dependency Injection
- Use constructor injection for all dependencies.
- Never use service locator pattern or `IServiceProvider` directly unless absolutely necessary.
- Register services with the appropriate lifetime (Scoped for repositories and DbContext, Singleton for configuration services, Transient for lightweight stateless services).

### 6.3 DTOs — Never Expose Entity Models to the Client
- API controllers always return DTOs, never EF Core entity models.
- Map between entities and DTOs in the service or controller layer.
- DTOs live in `Momentum.Shared` so both Client and Server can reference them.

### 6.4 Client-Side Caching
- The activity library is fetched once on login and cached in `ActivityService` in memory.
- The category list is fetched once on login and cached in `CategoryService` in memory.
- Both caches use a lazy-load pattern: `GetAllAsync()` checks `_cache` and calls `LoadAsync()` if null, so components that inject the service never need to worry about load order.
- The activity cache is invalidated and refreshed on any CRUD operation in the Manage Activities screen.
- Never make redundant API calls for data that is already cached.

### 6.5 User Data Isolation
- Every repository method that queries user-specific data must filter by `UserId`.
- Never return data without scoping it to the authenticated user.
- The `UserId` is always extracted from the JWT claims on the server — never trust a `UserId` passed in the request body.

---

## 7. Blazor-Specific Rules

- Use `@inject` for dependency injection in Razor components.
- Use `EventCallback` for component event handling — not plain C# `Action` or `Func`.
- Use `StateHasChanged()` only when necessary — prefer Blazor's automatic re-render triggers.
- Navigation uses `NavigationManager` — never use raw anchor tags for in-app navigation.
- All API calls from the client go through service classes in `Momentum.Client/Services/` — never call `HttpClient` directly from a page or component.
- Protected routes use `[Authorize]` attribute or `<AuthorizeView>` component — never implement manual auth checks in page code.

### 7.1 Namespace Notes
- MudBlazor has been fully removed. `@using MudBlazor` no longer exists in `_Imports.razor`. Do not reference MudBlazor types.
- ApexCharts has been removed. No namespace conflicts exist.

---

## 8. Error Handling Rules

- All API controller actions are wrapped in try/catch blocks.
- Return `400 Bad Request` for validation errors with a descriptive message.
- Return `404 Not Found` when a resource doesn't exist.
- Return `409 Conflict` for the activity deletion scenario where logs exist.
- Return `500 Internal Server Error` only for unexpected exceptions — log the full exception with Serilog before returning.
- Client-side: display user-friendly error messages using `ToastService.Show(message, ToastType.Error)` — never show raw exception messages to the user.

---

## 9. Activity Deletion Business Rule

This is a critical business rule — implement exactly as described:

1. When a DELETE request is received for an activity, query for associated `ActivityLog` records for that user.
2. **If no logs exist:** permanently delete the activity. Return `204 No Content`.
3. **If logs exist:** return `409 Conflict` with a JSON body containing:
   - `logCount` — number of associated log entries
   - `options` — array with values `"archive"` and `"cascade"`
4. The client presents the user with two choices:
   - **Hide from future logging** — sets `IsArchived = true` on the activity, preserves all logs
   - **Delete this activity and all history** — deletes the activity and all associated `ActivityLog` records, then recalculates affected score totals
5. The confirmed action is sent back to the API with a query parameter: `?action=archive` or `?action=cascade`

---

## 10. Boilerplate Seed Data

When a new user registers, the following activities must be automatically seeded into their activity library. This is triggered in the registration flow via `ActivitySeedService`, not via EF Core seed data. Dimension IDs: Body=1, Mind=2, Spirit=3, Connections=4, Responsibilities=5.

| Activity Name | Dimensions | Default Points |
|---|---|---|
| Exercise / Gym | Body | +15 |
| Hiking | Body | +10 |
| Meditation | Mind, Spirit | +10 |
| Reading/Learning | Mind | +10 |
| Journaling | Mind, Spirit | +10 |
| Cooking a Healthy Meal | Body, Responsibilities | +10 |
| Cleaning / Organizing | Responsibilities | +5 |
| Socializing | Connections | +10 |
| Calling Family | Connections | +5 |
| Travel | Mind, Spirit | +10 |
| Watching Excessive TV | Mind | -5 |
| Skipping Sleep | Body, Mind | -5 |
| Alcohol / Drinking | Body, Mind | -5 |

Note: negative default points are intentional — they track detrimental habits. The scoring model supports negative `DefaultPoints` and negative `PointsRecorded`.

---

## 11. Security Rules

- Never log JWT secrets, passwords, or personally identifiable information.
- Never include sensitive data in URL parameters.
- Always validate and sanitize user input on the server side.
- CORS must be configured to allow only the known client origin — never use wildcard `*` in production.
- HTTPS must be enforced in production via ASP.NET Core HTTPS redirection middleware.

---

*Momentum — CLAUDE.md Skills File*
*Version 1.5 — MudBlazor fully removed (KI-009); ToastService + ToastHost replace ISnackbar; §3.1, §3.2, §7.1, §8 updated accordingly*


## Documentation Maintenance Rule

After every completed coding task, Claude Code must update documentation using these rules:

### Always update `/Docs/momentum-handoff.md`
Update current project status, completed work, remaining work, and any changed deployment/runtime notes.

### Feature changes
If a user-facing feature is added, removed, renamed, or behaviorally changed:
- Update `/Docs/momentum-functional-requirements.md`
- Also update `/Docs/momentum-roadmap.md` only if the feature was previously planned, deferred, newly discovered, or affects future direction.

### Bug fixes
If a bug is fixed:
- Update `/Docs/momentum-known-issues.md`
- Move the issue to Resolved Issues or mark it Resolved with date/context.

### UI/design changes
If styling, layout, tokens, buttons, forms, charts, navigation, responsiveness, or visual patterns change:
- Update `/Docs/momentum-design-system.md`

### Architecture/API/data changes
If the task changes data models, DTOs, API contracts, auth, services, caching, deployment, or cross-layer behavior:
- Update `/Docs/momentum-software-specifications.md`

### AI/codegen process changes
If the task creates or improves reusable prompts, discovers codegen pitfalls, or establishes AI workflow rules:
- Update `/Docs/momentum-codegen-prompts.md`

### Required response section
Every Claude Code response must include:

## Documentation Updates

- Updated:
  - file path — summary of change
- Not updated:
  - file path — reason