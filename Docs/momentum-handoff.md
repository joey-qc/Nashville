# Momentum — Project Handoff & Status

This file tracks the current state of the project, what has been completed, and what remains. Update after every completed task.

---

## Current Project Status

**Phase:** CHK-006 complete — automatic post-activity Check-In redirect retired; B/E/M reporting not started  
**Build Status:** ✅ All projects build clean (0 errors); 54/54 tests pass  
**Last Updated:** 2026-07-07

### v2 Migration Deployment Summary

| Item | Value |
|---|---|
| Migration | `20260529151638_V2_DimensionModel` |
| Commit deployed | `79a81b5` |
| Deployment date | 2026-05-29 |
| Deployment method | Azure App Service ZipDeploy (OneDeploy) |
| Database | MomentumDb (Azure SQL Serverless, West US 2) |
| Migration execution time | 1.47 seconds |
| API deployment time | 27.6 seconds |

**Post-migration production validation (all passed):**

| Table | Row Count |
|---|---|
| Dimensions | 5 |
| ActivityDimensions | 44 |
| ActivityLogs | 54 |
| ActivityLogEntryDimensions | 79 |

**Production smoke tests:** All 6 passed (GET /api/categories, GET /api/activities, POST /api/logs, ActivityLogEntryDimensions snapshot write verified, GET /api/scores/summary, GET /api/reports/balance).

---

## Architecture Overview

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly (.NET 10) |
| Backend | ASP.NET Core Web API (.NET 10) |
| Database | Azure SQL Server (Serverless tier) |
| Auth | JWT Bearer + ASP.NET Core Identity |
| ORM | Entity Framework Core |
| Logging | Serilog |
| Hosting | Azure (API + Blazor static files) |

### v2 Data Model (live as of 2026-05-29)

The `Category` / `ActivityCategory` entities have been renamed and extended:

| v1 (retired) | v2 (current) | Notes |
|---|---|---|
| `Categories` table | `Dimensions` table | 5 wellness dimensions |
| `ActivityCategories` table | `ActivityDimensions` table | Links activities to dimensions |
| *(did not exist)* | `ActivityLogEntryDimensions` table | Point-in-time dimension snapshot per log entry |

**`ActivityLogEntryDimensions`** is written at log creation time. The user can now supply an explicit `DimensionIds` list to override the default (the activity's current dimensions). When editing a log entry, the saved snapshot is loaded and the user can add/remove dimensions for that specific entry only. Changing a log entry's dimensions never modifies the parent activity's defaults.

Domain entities `Category.cs` and `ActivityCategory.cs` have been deleted. `Dimension`, `ActivityDimension`, and `ActivityLogEntryDimension` are the authoritative model.

User-facing terminology across all pages is now **"Dimension / Dimensions"** — the UI-to-data-model alignment is complete.

---

## Completed Work

### CHK-006 — Retire Automatic Post-Activity Check-In Redirect (2026-07-07)

- **Status:** ✅ Complete. Saving a new Activity Log entry no longer automatically navigates to the Check-In form. It now lands directly on **View Log / Today / Details ON** (`/log/detail?period=day&details=true`).
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. Client-only change — no server, DTO, API, service, or schema changes.
- **What shipped:**
  - `Momentum.Client/Pages/LogActivity.razor` — in `HandleSubmit`'s create path (`else` branch), the navigation target changed from `/check-in?activityLogId={result.Id}&from={activityName}` to `/log/detail?period=day&details=true`. Edit-path navigation is unchanged.
- **Not changed:** the `CheckIn` entity/model, the standalone `/check-in` flow, the persistent top "Check In" button, the Check-Ins history screen, and the "+ Add Check-In" action in View Log Details (`ActivityDetail.razor`) — a Check-In can still be linked to a freshly logged entry from there.
- **Docs:** `Docs/check-in-feature-design-spec.md` §7 marked retired (original design preserved inline for history); CHK-006 implementation section added. `Docs/momentum-functional-requirements.md` §6.4/§6.5/§11.2 updated (v1.21). `Docs/momentum-roadmap.md` post-activity flow bullet marked retired.

### CHK-002 Phase 6A — View Log Details Integration (2026-06-06)

- **Status:** ✅ Phase 6A complete. View Log's expandable details now surface linked check-ins with add/edit/delete in context.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. **No server/DTO/API/service change** (reuses `GetAllAsync`, `DeleteAsync`, `CheckInDto.ActivityLogId`), so no new server tests.
- **What shipped:**
  - `Momentum.Client/Pages/ActivityDetail.razor` + `.css` — "Notes" toggle renamed **Details** (`.details-toggle`), shown whenever entries exist; loads `_checkInsByLog` (client-side group of `GetAllAsync` by `ActivityLogId`); per-entry details section renders note + linked check-in rows (local time + B/E/M) + "+ Add Check-In". Row click → `/check-ins?editId={id}`; per-row trash→confirm delete via `CheckInService.DeleteAsync`; add → `/check-in?activityLogId={logId}&from={name}`.
  - `Momentum.Client/Pages/CheckIns.razor` — added `?editId={id}` query param that auto-opens that row's inline editor after load.
- **Isolation/time:** `GetAllAsync` is user-scoped and UTC-tagged; display uses `ToLocalTime()`. Deleting a check-in never affects the ActivityLog.
- **Unchanged:** post-activity flow; Edit Log Entry save does not create check-ins; standalone `/check-in`.
- **Polish (pre-commit):** linked check-in timestamps are now normal-weight/secondary (scores are primary); a generic `returnUrl` query pattern was added so add/edit launched from View Log returns to the same View Log context (period + Details expanded) after save/skip/cancel. `returnUrl` is honored by `CheckIn.razor` and `CheckIns.razor`; absent → existing behavior (standalone stays, post-activity → Home, history edit stays).

### CHK-002 Phase 5B — Check-Ins History Screen (2026-06-06)

- **Status:** ✅ Phase 5B complete. `/check-ins` is now a usable history list with inline edit and delete.
- **Build/tests:** ✅ 0 errors; 52/52 tests pass (2 new server tests for `ActivityName` projection).
- **What shipped:**
  - `Momentum.Shared/CheckInDto.cs` — added display-only `ActivityName`.
  - `Momentum.Infrastructure/Repositories/CheckInRepository.cs` — `GetByIdAsync`/`GetByDateRangeAsync` now `.Include` the linked `ActivityLog.Activity`.
  - `Momentum.Application/Services/CheckInService.cs` — `Map` populates `ActivityName` from `ActivityLog?.Activity?.Name`.
  - `Momentum.Client/Services/CheckInService.cs` — added `GetAllAsync`, `UpdateAsync`, `DeleteAsync`; `GetMostRecentAsync` delegates to `GetAllAsync`.
  - `Momentum.Client/Pages/CheckIns.razor` + `.css` — newest-first list; Body/Energy/Mood score pills; `After: {activity}` for linked, no label for standalone; inline edit (date/time + bounded steppers, link preserved, `CreatedAt` hidden); delete via trash→confirm/cancel; empty state.
  - `Momentum.Tests/CheckInServiceTests.cs` — 2 new tests (linked populates `ActivityName`, standalone is null).
- **No new API endpoints** — reuses GET (date-range), PUT, DELETE from Phase 2. Deleting a check-in never affects any ActivityLog.
- **Unchanged:** `/check-in` form, post-activity flow, persistent top "Check In" button.
- **Time-display fix (2026-06-06):** check-in times were showing in UTC on the dev box (e.g. 10:52 AM → 2:52 PM). Cause: `UtcDateTimeConverter.Write` double-shifted EF's `Unspecified` timestamps on a non-UTC host. Fix: `CheckInService.Map` marks `CheckedInAt`/`CreatedAt` as `DateTimeKind.Utc`; client `GetAllAsync` sorts newest-first by the true instant. UTC storage unchanged; production (UTC host) was already correct. See KI-017.

### CHK-002 Phase 5A — Check-In Navigation Structure (2026-06-06)

- **Status:** ✅ Phase 5A complete. Creation and history are now separate entry points.
- **Build/tests:** ✅ 0 errors; 50/50 tests pass (layout/routing only — no schema/API/service change; manual QA per project convention).
- **What shipped:**
  - `Momentum.Client/Layout/MainLayout.razor` + `.css` — removed the temporary left-nav "Check In" → `/check-in` link; added left-nav "Check Ins" → `/check-ins`; added a persistent top action button "Check In" → `/check-in` beside "+ Add Entry" (new `.topbar-actions` container, same `.topbar-cta` style). `PageTitle` adds the `/check-ins` case before `/check-in`; `PageTitleShort` returns "Manage" for `/activities`, rendered via `.title-full`/`.title-short` spans toggled at the 767px breakpoint. Mobile topbar tightened so two buttons never wrap.
  - `Momentum.Client/Pages/CheckIns.razor` + `.css` — new placeholder history page at `/check-ins` (`[Authorize]`); full list deferred.
- **Unchanged:** `/check-in` form (Phase 3) and post-activity flow (Phase 4) — only entry points moved.

### CHK-002 Phase 4 — Post-Activity Check-In Flow (2026-06-06)

- **Status:** ✅ Phase 4 complete. After saving a **new** activity log, the app routes to the Check-In form with the new `ActivityLogId` pre-populated; the user saves a linked check-in or skips.
- **Build/tests:** ✅ 0 errors; 50/50 tests pass (no schema/API/service changes — client-side flow only; manual QA per project convention).
- **What shipped:**
  - `Momentum.Client/Pages/LogActivity.razor` — create path now navigates to `/check-in?activityLogId={newId}&from={activityName}` instead of resetting in place. **Edit path unchanged** (still returns to View Log).
  - `Momentum.Client/Pages/CheckIn.razor` + `.css` — accepts optional `ActivityLogId` and display-only `from` query params; shows an `After: {activity}` context chip when linked; adds a **SKIP** button in linked mode; Save/Skip in linked mode navigate to Home. **Standalone `/check-in` behavior unchanged.**
- **Ownership:** linked-save reuses the Phase 2 server validation (`CheckInService` rejects an `ActivityLogId` not owned by the user → 400). No new validation code.
- **No schema / API / repository / service changes.** `ActivityLogId` SetNull-on-delete (Phase 1) untouched; linked check-ins are ordinary records.

### CHK-002 Phase 3 — Standalone Check-In Form UI (2026-06-05)

- **Status:** ✅ Phase 3 complete. Standalone Check-In page live at `/check-in`. Post-activity flow, history screen, View Log integration, and reporting deferred to later phases.
- **Build/tests:** ✅ 0 errors; 50/50 tests pass (no new server tests; client UI is manual-QA per project convention — no client test project exists).
- **What shipped:**
  - `Momentum.Client/Services/CheckInService.cs` — `CreateAsync` (POST, friendly null-on-failure) + `GetMostRecentAsync` (preload defaults); UTC tagging; registered Scoped in `Program.cs`.
  - `Momentum.Client/Pages/CheckIn.razor` + `.css` — `/check-in` page: date/time (default now), three bounded −5…+5 steppers (Body/Energy/Mood), smart preload from most recent check-in (else 0/0/0), `ToastService` feedback, custom HTML/CSS with design tokens, mobile-friendly. No notes field. Saves standalone (`ActivityLogId = null`).
  - `Momentum.Client/Layout/MainLayout.razor` — temporary "Check In" nav item after View Log; `PageTitle` maps `/check-in` → "Check In".
- **Save behavior:** stays on page, retains entered scores, resets timestamp to now (documented in design spec §18).
- **Navigation note:** the nav item is interim; the persistent "Check In" action button (design spec §14) replaces it in a later phase.

### CHK-002 Phase 2 — Check-In API + DTOs + Repository/Service (2026-06-05)

- **Status:** ✅ Phase 2 complete. API endpoints live; client service, UI, and navigation not yet started.
- **Build/tests:** ✅ 0 errors; 50/50 tests pass (15 new CheckInService tests).

**DTOs (`Momentum.Shared`):** `CheckInDto`, `CreateCheckInRequestDto`, `UpdateCheckInRequestDto` — all with `[Range(-5, 5)]` annotations on score fields.

**Repository:** `ICheckInRepository` + `CheckInRepository` — all queries scoped by `UserId`; `GetByDateRangeAsync`, `GetByIdAsync`, `AddAsync`, `SaveChangesAsync`, `DeleteAsync`.

**Service:** `ICheckInService` + `CheckInService` — score validation (`ArgumentException` for out-of-range); ActivityLogId ownership check (`ArgumentException` if log not found for current user); `CreatedAt` set server-side at create time; maps entities ↔ DTOs.

**Controller:** `CheckInsController` — `GET /api/checkins`, `GET /api/checkins/{id}`, `POST /api/checkins`, `PUT /api/checkins/{id}`, `DELETE /api/checkins/{id}`; `[Authorize]`; UserId from JWT; `ArgumentException` → 400 Bad Request.

**DI:** `ICheckInRepository` + `ICheckInService` registered as Scoped in `Program.cs`.

### CHK-002 Phase 1 — CheckIn Entity + Migration (2026-06-05)

- **Status:** ✅ Phase 1 complete. Migration generated and verified. API, UI, and navigation not yet started.
- **Build/tests:** ✅ 0 errors; 35/35 tests pass.
- **Migration:** `20260605193819_CHK001_AddCheckIn`
- **What shipped:**
  - `Momentum.Domain/Entities/CheckIn.cs` — new entity with `Id`, `UserId`, `CheckedInAt` (user-editable display timestamp), `BodyScore`/`EnergyScore`/`MoodScore` (int, −5…+5), `ActivityLogId?` (nullable FK), `CreatedAt` (internal audit), and `ActivityLog?` navigation.
  - `Momentum.Domain/Entities/ActivityLog.cs` — `ICollection<CheckIn> CheckIns` reverse navigation added.
  - `Momentum.Infrastructure/Data/AppDbContext.cs` — `DbSet<CheckIn>`, FK configured with `SetNull` on ActivityLog delete, indexes on `UserId` and `CheckedInAt`.
  - `Momentum.Infrastructure/Migrations/20260605193819_CHK001_AddCheckIn.cs` — creates `CheckIns` table; `Down()` drops it cleanly.
- **Not yet implemented:** DTOs, repository, API endpoints, client service, UI, navigation, reporting.

### CHK-003 — Check-Ins History Period Filtering (2026-06-21)

- **Status:** ✅ Complete. The Check-Ins history page now supports Day / Week / Month filtering with a single 30-day server fetch.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. No server/API/DTO/entity changes — client-only feature.
- **What shipped:**
  - `Momentum.Client/Services/CheckInService.cs` — added `GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)` for an explicit bounded fetch. `GetAllAsync` now delegates to it (no behavior change for `GetMostRecentAsync`).
  - `Momentum.Client/Pages/CheckIns.razor`:
    - Page loads a single 30-day window on init: local today − 29 days → tomorrow (converted to UTC, no new API endpoint).
    - `_checkIns` renamed to `_allCheckIns` (the full 30-day cache).
    - `FilteredCheckIns` computed property applies Day / Week / Month client-side: Day = today only, Week = last 7 days including today, Month = last 30 days (the full cached set).
    - `_period` string field, default `"day"`, bound to a period-pill `<select>`. Filter changes are instant — no additional network requests.
    - Header restructured with a left title block and right period-pill, matching the Balance page pattern exactly.
    - Empty state now differentiates "no check-ins ever" (existing message + CTA) from "none in this period" (`PeriodEmptyText`: "No check-ins today/this week/this month" + hint to widen the period).
    - `EditId` path auto-widens `_period` so the target row is visible in `FilteredCheckIns` before `StartEdit` is called.
  - `Momentum.Client/Pages/CheckIns.razor.css` — `.checkins-header` changed to `flex; space-between`; period-pill + related styles added (identical visual treatment to Balance page). Mobile: header stacks vertically at ≤540px.
- **Design decisions:**
  - 30-day server window was chosen over unbounded history to bound server cost from day one. All three filter periods fit within 30 days; "Month" is defined as the last 30 days, so the 30-day window is the exact minimum needed.
  - Client-side filtering keeps filter changes instant and avoids extra round-trips.
  - `GetAllAsync` (wide window, used by `GetMostRecentAsync` on the Check-In form) is preserved unchanged to avoid regressions on preloaded defaults.

### CHK-004 — Unified View Log Timeline (2026-06-21)

- **Status:** ✅ Complete. Standalone Check-Ins (those with no `ActivityLogId`) now appear as top-level rows in the View Log Details timeline alongside Activity Log entries, sorted newest-first.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. Client-only change — no server, DTO, API, service, or schema changes.
- **What shipped:**
  - `Momentum.Client/Pages/ActivityDetail.razor`:
    - Added `_standaloneCheckIns: List<CheckInDto>` — populated in `LoadLogs()` from the same `CheckInService.GetAllAsync()` call already used for linked check-ins. Filtered to `!ActivityLogId.HasValue && CheckedInAt in [from, to)` (date-period match, no dimension filter).
    - Added private `sealed record TimelineItem(ActivityLogDto? Log, CheckInDto? CheckIn, DateTime SortKey)` — discriminated union used by the unified loop.
    - Added `IsEmpty` — false (show content) when Details ON and standalones exist even if dimension filter hides all logs.
    - Added `TimelineItems` computed property — Details OFF: activity logs in existing API order; Details ON: logs + standalones merged, `OrderByDescending(SortKey)`.
    - Added `StandaloneCheckInTimestamp()` — same format logic as `LogTimestamp()` (time-only for Today, date+time for week/month).
    - Details toggle visibility updated to `FilteredLogs.Any() || _standaloneCheckIns.Any()` — toggle appears whenever there's something to reveal in Details mode.
    - Unified `@foreach (var item in TimelineItems)` loop replaces the old `@foreach (var log in FilteredLogs)`. `item.Log` branch = existing activity log card (unchanged); `item.CheckIn` branch = new standalone check-in card.
    - Standalone check-in card: heart badge (`.ci-badge`), "Check-In" title (`.log-name`), B/E/M scores in `.log-cats` using existing `.ci-metric`/`.ci-val` classes, right-aligned `.log-time`, two-step `.act-btn` delete. Entire card row navigates to `/check-ins?editId={id}&returnUrl={ReturnUrl}` on click, matching Activity Log row behavior. Reuses existing `ConfirmDeleteCheckIn`.
  - `Momentum.Client/Pages/ActivityDetail.razor.css`:
    - Added `.ci-badge { background: var(--surface-2); color: var(--text-muted); }` — neutral heart-icon badge, visually distinct from colorful activity-letter badges.
  - `Docs/check-in-feature-design-spec.md` — §10 rewritten to describe unified timeline, filtering rules, and empty states; CHK-004 implementation section added.
  - `Docs/momentum-functional-requirements.md` — §7.1 Details toggle expanded with unified timeline description, dimension-filter bypass rule; §11.4 updated; version bumped to v1.17.
- **Design decisions:**
  - Standalone check-ins use date filtering but bypass dimension filtering — Check-Ins are not dimension-based; hiding them by a dimension category filter would be misleading.
  - `_checkInsByLog` (for linked check-ins) still uses `GetAllAsync()` (5-year window) so check-ins with a `CheckedInAt` outside the current period (e.g., logged late-night, checked in after midnight) are still correctly associated with their parent log entry.
  - `TimelineItem` is a component-local record — no shared DTO needed since it's a pure UI concept.
  - Entire standalone card row is clickable (same as Activity Log rows) rather than only the title text, for consistency.

### CHK-005 — Default Check-In Return Behavior (2026-06-21)

- **Status:** ✅ Complete. All Check-In save / skip / cancel flows now return to View Log / Today / Details ON (`/log/detail?period=day&details=true`) when no explicit `returnUrl` is provided. Previously, standalone save stayed on the Check-In form and linked flows returned to Home (`/`).
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. Client-only change — no server, DTO, API, service, or schema changes.
- **What shipped:**
  - `Momentum.Client/Pages/CheckIn.razor` — Added `private const string DefaultReturn = "/log/detail?period=day&details=true"`. `HandleSubmit` post-toast navigation consolidated to a single expression (explicit `returnUrl` takes precedence, else `DefaultReturn`); the `IsLinked` branch is removed since both modes share the same fallback. `Skip` fallback changed from `"/"` to `DefaultReturn`.
  - `Momentum.Client/Pages/CheckIns.razor` — Added `private const string DefaultReturn`. `CancelEdit` replaced 6-line if/else with a single `NavigateTo` expression using the same precedence pattern. `SaveEdit` post-toast replaced 5-line if/await-Load block with one `NavigateTo` line.
  - `Docs/check-in-feature-design-spec.md` — Phase 4 save/skip behavior bullets updated; Phase 3 note updated; §11 (Edit Log Entry integration) marked retired; CHK-005 implementation section added; status line updated.
  - `Docs/momentum-functional-requirements.md` — §6.4, §6.5, §11.1, §11.2, §11.5 updated to reflect new destination; §11.6/§11.7 Phase 6B retirement documented; version bumped to v1.18.
- **CHK-002 Phase 6B retired:** "Edit Log Entry check-in list with add follow-up" is superseded by CHK-004 (unified View Log Details timeline). No equivalent functionality gap remains.
- **Design decisions:**
  - `DefaultReturn` is a `private const` in each page's `@code` block independently — the two call sites are in different pages; a shared static class would add cross-component coupling for two strings.
  - Explicit `returnUrl` always takes precedence — the View Log context-return pattern from CHK-006A is fully preserved.

### REP-001 — Journal Page v1 (2026-06-21)

- **Status:** ✅ Complete. New `/journal` page surfacing Journaling activity entries as a clean reading experience. Journal nav item added to primary left nav between Balance and Manage Activities.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. No schema, DTO, or API changes.
- **Approach:** Client-side filter on `ActivityName == "Journaling"` after `GetByDateRangeAsync` — no new endpoint needed. Check-ins loaded via `GetAllAsync()` (full history) so linked check-ins whose `CheckedInAt` differs from the parent log's `LoggedAt` are still associated correctly (matches ActivityDetail pattern).
- **Behavior:**
  - Default period: Week, anchor: Today. Supported periods: Day / Week / Month (same rolling-window semantics as other pages).
  - Permanently in Details ON mode — no toggle. Every entry shows: timestamp, rendered rich notes, linked check-ins with full Body / Energy / Mood score pills.
  - Entries sorted newest-first within the selected period.
  - Empty state: icon + "No journal entries yet." + body copy explaining the Journaling activity template + "Write Your First Entry" CTA → `/log`.
  - No dimension chips, no activity badge, no points — reading surface only.
- **Files created:**
  - `Momentum.Client/Pages/Journal.razor` — page component
  - `Momentum.Client/Pages/Journal.razor.css` — scoped CSS (period-controls, date-pill, entry cards, rich-text `::deep` rules, score pills, empty state, mobile)
- **Files modified:**
  - `Momentum.Client/Layout/MainLayout.razor` — Journal nav item (book icon, `/journal` path, between Balance group and Manage Activities); `PageTitle` switch updated for `/journal`
- **Design decisions:**
  - Score pills use full labels ("Body", "Energy", "Mood") not abbreviations — Journal is a reading surface; clarity over compactness.
  - `entry-notes` at `0.92rem` / `line-height: 1.65` — larger and airier than View Log's `0.82rem` for comfortable long-form reading.
  - No `padding-left` indent on notes (no activity badge to align under) — full-width makes prose more readable.
  - Check-in section separated by a `border-top` rule; shows all linked check-ins for each entry ordered newest-first.

### NAV-001 — Historical Date Navigation (2026-06-21)

- **Status:** ✅ Complete. All four date-filtered pages — View Log, Check-Ins, Trends, Balance — now have a compact anchor-date picker. The selected date becomes the end-boundary for the period window. Default anchor is today; future dates are blocked (via `max` attribute + clamp on change).
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. No schema or DTO changes. No new API endpoints — only optional `anchorDate` query params added to existing report endpoints.
- **Behavior:**
  - **Day/Today** = selected date only. **Week** = selected date + previous 6 days (7 total). **Month** = selected date + previous 29 days (30 total). Balance Year = unchanged (always Jan 1 of current year → today).
  - Balance Week/Month now use rolling windows (7 / 30 days ending on anchor) rather than the previous calendar-week / calendar-month starts.
  - Trends chart sparklines (dimension trend) remain anchored to today — they always show "last 8 weeks" regardless of anchor.
  - View Log `ReturnUrl` now includes `anchor=` so the date context is restored when returning from check-in add/edit flows.
  - View Log `DateDisplay` header updates to show the date range when Week or Month is selected.
- **Files changed:**
  - `Momentum.Application/Interfaces/IScoreService.cs` — added `DateOnly? anchorDate = null` to 4 interface methods
  - `Momentum.Application/Services/ScoreService.cs` — `GetDailyTotalsAsync`, `GetWeeklyTotalsAsync`, `GetMonthlyTotalsAsync`, `GetCategoryTotalsAsync` use anchor when provided; Balance week/month changed from calendar-based to rolling windows
  - `Momentum.API/Controllers/ReportsController.cs` — `[FromQuery] DateOnly? anchorDate = null` added to `GetDaily`, `GetWeekly`, `GetMonthly`, `GetBalance` actions
  - `Momentum.Client/Services/ReportsService.cs` — `GetDailyAsync`, `GetWeeklyAsync`, `GetMonthlyAsync`, `GetBalanceAsync` accept and pass `DateOnly? anchorDate`
  - `Momentum.Client/Pages/ActivityDetail.razor` + `.css` — anchor state, `[SupplyParameterFromQuery]` Anchor param, `GetDateRange()` uses anchor, `DateDisplay` shows range, `ReturnUrl` includes anchor, period-controls row + date-pill markup + CSS
  - `Momentum.Client/Pages/CheckIns.razor` + `.css` — anchor state, period-pill moved from header to new period-controls row, `Load()` uses anchor, `FilteredCheckIns` uses anchor, date-pill markup + CSS
  - `Momentum.Client/Pages/Reports.razor` + `.css` — anchor state, `LoadData()` passes anchor to all service calls, `CurrentPeriodTotal` uses anchor, date-pill in controls-row + CSS
  - `Momentum.Client/Pages/Balance.razor` + `.css` — anchor state, `GetPeriodFrom()` uses anchor for week/month, `LoadData()` passes anchor, `bal-header-controls` wrapper + date-pill markup + CSS
- **Design decisions:**
  - `_anchor` state is `private DateTime` in each page's `@code` — not a shared singleton/service, since each page has independent browsing context.
  - CSS is duplicated in each `.razor.css` file due to Blazor CSS isolation scoping (same approach as period-pill).
  - Balance Year period always remains current calendar year-to-date; anchor has no effect when Year is selected (spec: "Do not invent a new Year behavior without documenting it").
  - Check-Ins `Load()` uses `DateTime.Today` as anchor when `EditId` is set (edit-by-id mode) so the target check-in is guaranteed to be in the 30-day fetch window regardless of the current anchor.

### NAV-002 — Rolling-Window Semantics Verification (2026-06-21)

- **Status:** ✅ Complete — verification only, no code changes.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. No files modified.
- **Verified:**
  - `GetDateRange()` in `ActivityDetail.razor` uses rolling windows exclusively — Day = anchor only, Week = anchor − 6 days, Month = anchor − 29 days. No `DayOfWeek` or calendar-month-start logic anywhere.
  - Activity logs and standalone check-ins both receive the same `(from, to)` from `GetDateRange()`. Linked check-ins intentionally use the full unfiltered set (a check-in's date may differ from its parent log's date).
  - UTC boundaries are derived from `_anchor.Date.ToUniversalTime()` where `_anchor` is always a local `DateTime` (`DateTime.Today` or user-selected). No `DateTime.UtcNow.Date` in the date path.
  - `ReturnUrl` at `/log/detail?period={_period}&anchor={_anchorStr}&details=true` preserves all three context parameters.

### UX-003 — Standardize Trends Period Selector (2026-06-21)

- **Status:** ✅ Complete. The Trends page now uses the same period-pill dropdown as Balance and Check-Ins, replacing the segmented Daily / Weekly / Monthly tab buttons.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. Client-only change — no server, DTO, API, or service changes.
- **What shipped:**
  - `Momentum.Client/Pages/Reports.razor` — `.view-tabs` / `.tab-btn` block replaced with a `.period-pill` containing a `<select>` (options: Daily=0, Weekly=1, Monthly=2). Added `OnTabChanged(ChangeEventArgs e)` handler that delegates to the existing `SetTab(int idx)`. `_tabIndex` (int, 0/1/2) and all downstream logic (`PeriodLabel`, `AvgUnit`, `TopCardTitle`, `LoadData()`, `ComputeImprovementFromData()`) are entirely unchanged.
  - `Momentum.Client/Pages/Reports.razor.css` — removed `.view-tabs`, `.tab-btn`, `.tab-btn:hover`, `.tab-btn.active`. Added `.period-pill`, `.period-label`, `.period-select`, `.period-select option`, `.period-chevron` using the identical CSS as Balance and Check-Ins. Mobile `controls-row` column-stacking at ≤768px preserved.
  - `Docs/momentum-design-system.md` — §14 Trends controls description updated from "tabs" to "period dropdown"; §17 Toast corrected to reflect UX-002 changes (top placement, drop-down animation, wider surface); §19 Period Pill added as a documented cross-page reusable pattern (Balance, Check-Ins, Trends).
- **Design decisions:**
  - `_tabIndex` (int 0/1/2) was retained as-is — it drives all chart logic and is not exposed in the URL; changing its type would be unnecessary churn.
  - The period pill uses `@onchange` + `selected="@(_tabIndex == N)"` per-option (same pattern as CheckIns) rather than `@bind` because the change must also trigger `SetTab` → `LoadData()` asynchronously.
  - The period-pill pattern is now consistent across three pages; it is documented as a named pattern in the design system.

### UX-002 — Toast Placement and Visibility Improvements (2026-06-21)

- **Status:** ✅ Complete. Toast notifications repositioned to appear below the masthead and are visually distinct from page content.
- **Build/tests:** ✅ 0 errors; 54/54 tests pass. CSS-only change — no server, DTO, API, service, or Razor logic changed.
- **What shipped (`Momentum.Client/Components/ToastHost.razor.css`):**
  - **Position:** moved from `bottom: 24px / right: 24px` to `top: 64px / right: 24px` (60px topbar + 4px gap). Toasts no longer cover bottom-page actions (Save / Skip / Cancel).
  - **Stacking:** `flex-direction` changed from `column-reverse` to `column`; new toasts append below existing ones (stack downward).
  - **Width:** container now uses `width: min(420px, calc(100vw - 2rem))` so it is consistently sized on desktop without wrapping or causing horizontal scroll.
  - **Surface:** background changed from `--surface-2` → `--surface-3` (existing lighter token, `#1A3B66`) for better contrast against the dark page background.
  - **Shadow:** added `box-shadow: 0 4px 16px rgba(0,0,0,0.5), 0 1px 4px rgba(0,0,0,0.3)` to lift toasts off the page visually.
  - **Animation:** updated from `translateX(16px)` (slide from right) to `translateY(-12px)` (drop down from above), matching the new top placement.
  - **Mobile (≤540px):** `top: 64px; left: 12px; right: 12px; width: auto` — full-width below topbar, no bottom positioning.
- **No changes to:** `ToastHost.razor` (Razor markup unchanged), `MainLayout.razor` (toast host is `position: fixed` so DOM placement is irrelevant), `ToastService.cs` (API unchanged), timeout behavior.

### KI-009 + KI-015 + KI-016 Cleanup Cycle — COMPLETE & DEPLOYED (2026-06-05)

- **Status:** ✅ Complete — all three issues resolved, merged to `main`, deployed to production.
- **Commit:** `ff833da` ("Implement native toasts and remove MudBlazor")
- **Build/tests:** ✅ Clean build (0 warnings, 0 errors); 35/35 tests pass (2 new KI-015 regression tests added).
- **Published index.html verified:** fingerprinted Blazor script (`blazor.webassembly.{hash}.js`), no `_content/MudBlazor/` output.
- **Production smoke test:** ✅ Login, Log Activity (native toast appears), Trends, View Log — all pass. No Blazor error banner. No startup 404.

**KI-009 — Native Toast System + MudBlazor Removal:**
- `Momentum.Client/Services/ToastService.cs` — new Singleton; `Show(message, ToastType)` fires `Action<ToastMessage>`; 3 s (Success/Info) / 4.5 s (Error/Warning).
- `Momentum.Client/Components/ToastHost.razor` + `.css` — `Task.Delay` auto-dismiss, per-type left accent border (green/red/amber/sky), slide-in animation, bottom-right desktop / bottom full-width mobile (≤540px).
- `MainLayout.razor` — `<ToastHost />` inside `<Authorized>`.
- `Program.cs` — `AddMudServices()` removed; `AddSingleton<ToastService>()` added.
- `App.razor` — all four MudBlazor providers removed.
- `_Imports.razor` — `@using MudBlazor` removed.
- `index.html` — MudBlazor CSS/JS removed; `#[.{fingerprint}]` script placeholder preserved; `css/app.css` link added (suppresses Blazor default error banner in non-error conditions).
- `Momentum.Client.csproj` — MudBlazor NuGet removed.
- 4 pages (16 call sites) — `ISnackbar` / `Snackbar.Add(...)` replaced with `Toast` / `Toast.Show(...)`.

**KI-015 — Local-Day Daily Chart Fix:**
- `IScoreService` + `ScoreService` — `GetWeeklyComparisonAsync` and `GetDailyTotalsAsync` accept `int? localOffsetMinutes`; daily grouping uses `l.LoggedAt.AddMinutes(offset).Date`; UTC range query includes 1-day buffer.
- `ScoresController` + `ReportsController` — `localOffsetMinutes` forwarded from `[FromQuery]`.
- Client `ScoreService` + `ReportsService` — compute `TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes` and pass to API.
- `ScoreServiceTests` — 2 new regression tests for late-evening local-day bucketing.

**KI-016 — Blazor Fingerprint Script (Production 404):**
- Root cause identified: prior commit accidentally replaced `blazor.webassembly#[.{fingerprint}].js` with bare `blazor.webassembly.js`. Current implementation preserves the correct fingerprint placeholder.
- `css/app.css` restored so the `#blazor-error-ui` element is hidden by default (the Blazor error banner was visible as a persistent bottom strip in production).

### AUTH-001 Session Persistence — COMPLETE (2026-06-04)

- **Status:** ✅ Complete — near-term plan fully implemented.
- **Build/tests:** ✅ Clean build (0 warnings, 0 errors); 33/33 tests pass.
- **Design spec:** `Docs/session-persistence-design-spec.md`
- **What shipped:**
  - `Momentum.API/appsettings.json` — `Jwt:ExpiryMinutes` raised from `"60"` to `"10080"` (7 days). No code change required; `AuthService.GenerateToken()` already reads this value dynamically.
  - `Momentum.Client/Auth/JwtAuthStateProvider.cs` — `GetAuthenticationStateAsync` now calls `localStorage.removeItem("authToken")` before returning `Anonymous` when `BuildPrincipal` detects an expired token. Eliminates the stale-token gap where a dead token persisted in localStorage until the next 401.
  - `Momentum.Client/Pages/Login.razor` — Removed the inert "Keep me signed in" checkbox (`<label>` block and SVG checkmark) and the `_rememberMe` bool field from `@code`. The "Forgot password?" stub link remains.
  - `Momentum.Client/wwwroot/css/auth-pages.css` — `.auth-meta-row` updated to `justify-content: flex-end` (single remaining element). The `auth-check*` CSS rules are retained — they are used by `Register.razor` for the terms checkbox.
- **Not implemented:** refresh tokens. Long-term refresh-token plan remains documented in §6 of the design spec for future pre-PWA work.

### Check-In Feature — DESIGN/PLANNING DOCUMENTED (2026-06-04 — not implemented)

- **Status:** 📝 Planned — design documented only. **No application code, schema, migration, DTO, API, or UI has been implemented.**
- **Design spec:** `Docs/check-in-feature-design-spec.md`
- **Summary:** A planned new "Check-In" domain capturing user **state/outcomes** (Body / Energy / Mood, each `-5`…`+5`, `0` = baseline), complementing Activity Logs which capture **behaviors/inputs**. Planned `CheckIn` entity with user-editable `CheckedInAt` (analytics/display) + internal `CreatedAt` (audit), optional nullable `ActivityLogId` parent (one log → many check-ins), smart defaults (preload from most recent, else `0`), no notes in v1. Includes View Log "Notes"→"Details" toggle rename, a first-class Check-Ins history screen, a persistent "Check In" action button, future nav direction, and a mobile masthead concern (shorten "Manage Activities"→"Manage").
- **Deferred within this plan:** PWA / push-notification check-in reminders (Azure Function timer job sending push directly without waking the API), and Body/Energy/Mood reporting & correlation analytics. See design spec §16–§17.
- Roadmap updated with the Check-In feature, deferred PWA/push reminders, and future B/E/M reporting/correlation concept.
- Functional requirements and software specifications intentionally **not** updated — the feature is planned, not implemented.

### Rich Notes v1 — COMPLETE & DEPLOYED TO PRODUCTION (2026-06-03)

- **Status:** ✅ Complete — all three phases implemented, merged to `main`, deployed to production.
- **Commit:** `97093de` ("Implement Rich Notes v1")
- **Build/tests:** ✅ Clean build (0 warnings, 0 errors); 33/33 tests pass
- **Manual QA:** ✅ Passed (Add/Edit editor formatting, edit-mode load, bullet rendering, View Log Show Notes toggle, mobile)
- **What shipped:** rich text Notes (`contenteditable` + custom toolbar: bold/italic/underline/bullets) on Add/Edit Log Entry; server-side HTML sanitization with blank→NULL normalization (10,000-char limit); View Log "Notes" toggle (summary line, right-aligned, default OFF) rendering formatted notes beneath entries. No new tables, API, or navigation — built on the existing `ActivityLog.Notes` field.
- Detailed phase-by-phase history retained below.

### Rich Notes v1 — Design + Planning (2026-06-03 — on feature/rich-notes-v1)

**Reflection v1 (separate feature) abandoned before commit.**
- A separate Reflection table / entity / API / navigation was explored but deferred indefinitely.
- No Reflection code was ever written. No Reflection documentation exists on the current branch.

**Rich Notes v1 is the current direction.**
- Enhances the existing `ActivityLog.Notes` field with rich text formatting.
- No new tables, no new API endpoints, no new navigation items.
- Supports journaling via the existing Journaling activity + rich Notes field.

**Design specification created:** `Docs/rich-notes-v1-design-spec.md`
- Content stored as sanitized HTML in existing `nvarchar(max)` column
- Custom `contenteditable` + toolbar editor (no third-party libraries)
- Bold, italic, underline, bullet list for v1
- 240-char UI limit removed; DTO limit raised to 10,000; DB column unchanged
- Blank notes normalized to `NULL` server-side
- Paste strips external formatting to plain text
- View Log "Show Notes" toggle (hidden when no notes present; toggle OFF = today's behavior)
- Notes rendered as formatted HTML when toggle is ON
- Search deferred to future release
- 5 open technical questions (sanitization library, `execCommand` vs Range API, blank-detection logic, toolbar active-state detection, toggle placement on mobile)

**Implementation plan created:** `Docs/rich-notes-v1-implementation-plan.md`
- Files affected (server + client)
- DTO changes, sanitization helper, HtmlSanitizer NuGet recommendation
- `RichNotesEditor.razor` component design + JS interop (`richNotesEditor.js`)
- `ActivityDetail.razor` View Log toggle changes
- Blank normalization algorithm
- 5 unit/integration tests + full manual QA checklist
- 15-step sequenced implementation order

**Rich Notes v1 Phase 2 — Rich text editor UI complete (2026-06-03):**
- `Momentum.Client/Components/RichNotesEditor.razor` + `.css` created
- `Momentum.Client/wwwroot/js/richNotesEditor.js` created (init, getContent, format, focus helpers)
- `index.html` updated: `richNotesEditor.js` script tag added before Blazor runtime
- `_Imports.razor` updated: `@using Momentum.Client.Components` added
- `LogActivity.razor` updated: `<textarea>` replaced with `<RichNotesEditor @ref="_notesEditor" InitialValue="@_model.Notes" />`; `HandleSubmit` reads from editor via `GetContentAsync()` before building DTO; editor cleared via `ClearAsync()` after successful add-mode log; `ToggleQuickPick` made async to call `ClearAsync()` on deselect
- Sanitizer allowlist expanded to include `b` and `i` (browser `execCommand("bold")` / `execCommand("italic")` produce these tags)
- 2 new sanitizer tests for `<b>` and `<i>` preservation; all 31 tests pass

**Rich Notes v1 Phase 2 — defect fixes (2026-06-03):**
- **Bug 1 (bullets off-screen left):** `RichNotesEditor.razor.css` list/paragraph rules changed to use the `::deep` combinator. The `<ul>`/`<li>`/`<p>` nodes are inserted into the `contenteditable` by `execCommand`, so they never receive the component's CSS-isolation scope attribute; the scoped `.rne-editor ul` rule never matched them (same gotcha as KI-005). `::deep` drops the scope requirement and the 24px indent now applies.
- **Bug 2 (existing note blank in Edit):** The editor initialized its `contenteditable` from `InitialValue` on first render, but that first render happened at the first `await` in `OnInitializedAsync` — before `LoadEditEntryAsync` populated `_model.Notes`. Added a `_notesReady` gate (`@if (_notesReady)`) set to `true` only at the end of `OnInitializedAsync`, so the editor's first render (and init) happens after the note is loaded.
- **Bug 3 (text-then-bullet list not saved):** When text precedes a list, the browser's `execCommand` wraps the list in a `<div>` (`text<div><ul>…</ul></div>`). `HtmlSanitizer` defaulted to `KeepChildNodes = false`, which removed the disallowed `<div>` together with its children — dropping the list. Set `KeepChildNodes = true`: structural wrappers are unwrapped while allowed children (ul/li) survive. Script/style tags are still removed (no executable content survives; only inert text could remain, which is never rendered as markup).
- Sanitizer tests updated for `KeepChildNodes = true` semantics (script tag removed though inert text may remain; anchor unwrapped keeping text) and 2 regression tests added for div-wrapped and bare text-then-list. All 33 tests pass.

**Rich Notes v1 Phase 3 — View Log note display complete (2026-06-03):**
- `ActivityDetail.razor`: added a **Show Notes** toggle to the filter bar (chip style matching dimension filters). Toggle is rendered only when `HasDisplayedNotes` (at least one currently-filtered entry has a non-empty `Notes`); defaults OFF (`_showNotes = false`).
- When ON, the formatted note renders directly beneath each entry that has notes via `@((MarkupString)log.Notes!)` — no truncation, no "show more," no separate card.
- `.log-card` restructured into a column: existing flex row moved into `.log-card-row` (which keeps the click-to-edit handler and `cursor:pointer`); the note (`.log-note-body`) sits beneath it inside the same card. OFF state is visually identical to before — when `_showNotes` is false no note element renders.
- Note body styled as inline secondary detail (muted, indented under the text column, no border/panel). Child elements (`p`/`ul`/`ol`/`li`/`strong`/`b`/`em`/`i`/`u`) are styled via `::deep` because `MarkupString` content carries no CSS-isolation scope attribute (same pattern as RichNotesEditor Bug 1).
- Toggle visibility tracks the filtered set: changing period/dimension re-evaluates `HasDisplayedNotes`. `_showNotes` is sticky across filter changes (notes simply don't render when none are present).
- No DTO/API/schema changes — `ActivityLogDto.Notes` already carried the sanitized HTML. Build clean; 33/33 tests pass.
- **Rich Notes v1 feature complete** (Phases 1–3). Notes search remains a documented future enhancement (out of v1 scope).

**Rich Notes v1 Phase 3 — toggle placement refinement (2026-06-03):**
- Moved the notes toggle from the filter bar (where it wrapped to its own row on mobile) to the entry-count / score-summary line (`.detail-stats-row`), right-aligned via `margin-left: auto`.
- Relabeled "Show Notes" → "Notes"; kept the notebook icon; made the chip more compact (`0.72rem`, `4px 10px`).
- Improved accessibility: dynamic `aria-label`/`title` ("Show notes" / "Hide notes") in addition to `aria-pressed`.
- Visibility (only when displayed entries have notes), default OFF, and ON/OFF rendering behavior all unchanged. No editor, sanitization, or note-rendering changes. Build clean; 33/33 tests pass.

**Rich Notes v1 Phase 1 — Infrastructure complete (2026-06-03):**
- `HtmlSanitizer` NuGet (v9.0.892) added to `Momentum.Application`
- `CreateActivityLogDto` and `UpdateActivityLogDto` Notes limit raised from 1,000 → 10,000 characters
- UI 240-char `maxlength` and `0/240` counter removed from `LogActivity.razor`
- `ActivityLogService.SanitizeNotes()` implemented: allowlist sanitization (p/br/strong/em/u/ul/ol/li; no attributes) + 4-step blank normalization (sanitize → strip tags → decode entities → trim → NULL if empty)
- Wired into both `CreateAsync` and `UpdateAsync` before entity persistence
- 19 new sanitization tests added (`ActivityLogSanitizationTests.cs`); all 29 tests pass
- **Phase 2 (rich text editor + View Log toggle) not yet started.** Rich text editor component (`RichNotesEditor.razor`), JS interop, and View Log Show Notes toggle remain pending.

**Note on HtmlSanitizer v9 behavior:** `<a>` tags and their child text are both discarded (not just the tag). Documented in test. This is correct: the editor never produces `<a>` tags (paste strips to plain text); the security requirement (no href in storage) is met.

Work remains isolated on `feature/rich-notes-v1` — not merged to main.

### Phase 16: Update New-User Seeded Activity Library (2026-06-02 — complete)
- Seed list updated in `ActivitySeedService.cs` (applies to new registrations only; existing users unchanged)
- 14 → 13 activities: consolidated Hiking (Solo/With Others) → Hiking, Socializing with Friends → Socializing, Travel (Solo/With Others) → Travel, Reading (Nonfiction) → Reading/Learning
- Added: Alcohol / Drinking (−5 pts, Body + Mind)
- Points revised: Exercise/Gym +8→+15; most activities unified to +10; negatives −3/−6 → −5
- No migration created; no backfill; no schema changes

### Phase 15: DIM-001 — Align Persisted Dimension Names with Display Names (2026-06-02 — complete, deployed to production, verified)

- **EF Core migration `20260602180710_DIM001_RenameDimensions`** — updates the 5 `Dimensions` rows in-place using `UpdateData` by stable ID. `Down()` fully reverses to old names. No schema changes; no data loss; no relationship changes.
- **`AppDbContext.HasData`** updated: seed names now Body / Mind / Spirit / Connections / Responsibilities.
- **`DimensionDisplayHelper.cs` simplified**: `GetDisplayName()` now returns `dim.Name` directly (no lookup table needed). Mobile abbreviation table reduced to the two long names only: Connections→Con (Id=4), Responsibilities→Rsp (Id=5). The `_byName` legacy-name fallback dictionary removed entirely.
- **`ActivitySeedService.cs`** comment updated to reflect new dimension names.
- No API contracts changed. No DTO changes. No scoring, report, or date-handling changes. No CSS or markup changes required.
- `DimensionDisplayHelper` overloads for `CategoryDto` and `CategoryTotalDto` both preserved.

**Production deployment:**
- Commit: `b7ac3b7`
- Migration applied manually via idempotent SQL script (`DIM001_migration.sql`) to Azure SQL Serverless (West US 2)
- Production `Dimensions` table confirmed: Body / Mind / Spirit / Connections / Responsibilities with original colors and IDs
- `__EFMigrationsHistory` confirms `20260602180710_DIM001_RenameDimensions` present
- Smoke test passed: Login, Home, Add Entry, Manage Activities, View Log, Trends, Balance — desktop full names and mobile abbreviations (Con / Rsp) verified correct

### Phase 14: MOB-002 + UX-001 + UX-001A — Edit/Delete Interaction Standardization (2026-06-02 — complete, deployed to production)

**MOB-002 — Standardize Edit Screen Action Layout**
- Edit Activity modal footer refactored: Save + Cancel grouped left, delete control isolated right
- Replaced bare "DELETE ACTIVITY" text button with icon-only arm/confirm/cancel pattern (matches View Log row delete)
- Normal state: trash icon. Armed state: red check (confirm) + gray X (cancel)
- Mobile no longer requires extra scrolling to reach delete actions — footer stays on one row
- CSS: added `.act-btn`, `.act-btn.confirm`, `.act-btn.cancel-del` to `ManageActivities.razor.css`; removed `.btn-text-delete`; removed mobile `flex-direction: column` override

**UX-001 — Standardize Activity and Log Entry Edit/Delete Interactions**
- Removed redundant pencil/edit icon button from Manage Activities activity rows — row tap remains the edit action
- Added delete to Edit Log Entry screen: same trash → arm → confirm/cancel pattern as Edit Activity
- `LogActivity.razor`: added `_deleteArmed` state, `DeleteLogEntry()` method, action row split into `action-row-left` (Save + Cancel) and `action-row-right` (delete icon, edit mode only)
- Activities and Log Entries now share identical edit/delete UX

**UX-001A — Armed Delete Confirmation on Activity Rows**
- Manage Activities row-level trash icon no longer immediately triggers delete
- First click arms the row (`_pendingDeleteActivityId = a.Id`); armed state shows red check + gray X
- Red check invokes existing `DeleteActivity(a)` — all archive/cascade/conflict logic preserved
- Gray X cancels armed state. Only one row can be armed at a time
- Opening create or edit form clears any armed row state
- No CSS changes required — `.act-btn` styles already scoped in `ManageActivities.razor.css` from MOB-002

### Phase 13A: MOB-001A Dimension Rename Rollout — Home + Balance (2026-06-02 — complete)
- Added `CategoryTotalDto` overloads to `DimensionDisplayHelper` (Home and Balance pages use `CategoryTotalDto`, not `CategoryDto`)
- **Home page — Today's Momentum** category bar names now use new display names with mobile responsive spans
- **Home page — activity-row cat-tags** now use new display names with mobile responsive spans
- **Home page — This Week by Dimension** wkb-row names + wkb-stack tooltips use new display names
- **Balance page — dimension list** (`cat-name`) uses new display names with mobile responsive spans
- **Balance page — insight headline** "Your mind dimension is dominating…" uses new lowercased display name
- **Balance page — insight sub-text** recommendation text uses new display names (Connections, Responsibilities, etc.)
- No database, API, DTO, scoring, or calculation changes

### Phase 13: MOB-001 Dimension Rename + Mobile-Responsive Labels (2026-06-01 — complete)

- **Dimension rename:** All user-facing dimension names updated throughout the UI:
  - Physical → Body · Mental → Mind · Spiritual → Spirit · Social → Connections · Housekeeping → Responsibilities
- **Responsive abbreviations:** On mobile (≤540px), long names abbreviate to Body / Mind / Spirit / Con / Rsp. Desktop always shows full names. Implemented via `.dim-full` / `.dim-abbr` CSS spans toggled by a global media query in `momentum-theme.css`.
- **`DimensionDisplayHelper.cs`** — new static helper in `Momentum.Client/Services/`. Maps by stable dimension ID (primary) and stored name (fallback). Provides `GetDisplayName()` and `GetMobileLabel()`.
- **Accessibility:** Every chip and toggle button carries `title=` and/or `aria-label=` with the full display name. `.dim-abbr` spans carry `aria-hidden="true"` so screen readers always receive the full label.
- **View Log filter chips** now use the color-dot chip style (matching Trends page): `inline-flex` layout + 8px colored dot per dimension. "All" chip gets the gray dot + `.active.all` CSS class.
- **Pages updated:** LogActivity.razor, ActivityDetail.razor, ManageActivities.razor, Reports.razor.
- No database schema changes; no API contract changes; no scoring or data isolation changes.

### Phase 12: KI-013 UTC/Local Timezone Boundary Fix (2026-05-31 — complete)
- Fixed View Log, Home, Add Entry, and Balance date boundaries to use `DateTime.Today.ToUniversalTime()` (browser local midnight → UTC) instead of `DateTime.UtcNow.Date` (UTC midnight)
- Updated `ScoreService.GetSummaryAsync` to accept optional `todayStartUtc`/`weekStartUtc`/`monthStartUtc` params from the client; falls back to UTC.Now for backward compatibility
- Added boundary params to `ScoresController.GetSummary` and the client `ScoreService`; client now computes and passes local-day UTC equivalents on every summary request
- Added regression test: entry at 23:00 local (03:00 UTC next day) is correctly excluded from TodayTotal when local boundaries are supplied
- No database changes; repository query was already correct (`>= from && < to`)
- Known remaining limitation: Trends charts and weekly comparison still group by `l.LoggedAt.Date` (UTC date); entries within a few hours of UTC midnight may appear in the adjacent day's chart bucket — lower-impact, deferred

### Phase 11: Dimension UI Terminology + Per-Entry Dimension Control (2026-05-31 — complete)
- Renamed all user-facing "Category" / "Categories" text to "Dimension" / "Dimensions" across ManageActivities, ActivityDetail, Home, and Balance pages
- Added `DimensionIds: List<int>?` to `CreateActivityLogDto` and `UpdateActivityLogDto`
- Updated server-side `ActivityLogService.CreateAsync` to use client-supplied `DimensionIds` when provided, defaulting to activity's current dimensions otherwise
- Updated `ActivityLogService.UpdateAsync` to replace the log entry's dimension snapshot when `DimensionIds` is supplied; falls back to re-deriving from the new activity if only the activity changed; preserves existing snapshot otherwise
- Added dimension toggle selector UI to the Add Entry / Edit Log Entry page (`LogActivity.razor`) — preloads from selected activity on create, loads saved snapshot on edit, hint text distinguishes create vs. edit context
- No database schema changes; no new migrations

### Phase 10: v2 Dimension Model Migration (2026-05-29 — complete)
- Renamed `Category` → `Dimension` and `ActivityCategory` → `ActivityDimension` at all layers (entities, DTOs, repositories, services, API contracts, UI)
- Created `ActivityLogEntryDimensions` join table for point-in-time dimension snapshots per log entry
- Deleted legacy `Category.cs` and `ActivityCategory.cs` domain entities
- EF Core migration `20260529151638_V2_DimensionModel` applied to production MomentumDb
- Backfilled 79 `ActivityLogEntryDimensions` rows from existing `ActivityLogs × ActivityDimensions`
- All 6 production smoke tests passed
- User-facing "Category" terminology preserved; per-entry dimension overrides deferred to post-v2

### Phase 1–3: Foundation
- Multi-user architecture with JWT auth
- Five wellness categories (Physical, Mental, Spiritual, Social, Housekeeping) with seed data
- Activity library: create / edit / archive / delete with 409 conflict handling
- Activity logging with points, notes, date/time override
- Score service: today / week / month / year totals
- 14 boilerplate starter activities seeded on registration
- Azure SQL Serverless cold-start handling (retry loop with warm-up banner)

### Phase 4: UI Redesign (complete)
All pages converted from MudBlazor to custom HTML/CSS using design tokens from `momentum-theme.css`. No MudBlazor component remains in markup; `ISnackbar` (toast) is the sole MudBlazor service still injected.

| Page | Route | Status |
|---|---|---|
| Home (dashboard) | `/` | ✅ Custom |
| Add Entry | `/log` | ✅ Custom |
| View Log | `/log/detail` | ✅ Custom |
| Trends | `/reports` | ✅ Custom |
| Balance | `/reports/balance` | ✅ Custom |
| Manage Activities | `/activities` | ✅ Custom |
| Settings | `/settings` | ✅ Custom |
| Login | `/login` | ✅ Custom |
| Register | `/register` | ✅ Custom |

### Report restructuring (2026-05-27)
- **Trends page** — Category sparklines (8-week trend per category) added to bottom-left; top periods now include year in label (W21 2026 / May 2026); top days show full date (May 15, 2026).
- **Balance page** — Category Breakdown card removed (redundant with donut ring); Best & Worst Days is now full-width; date always visible at all breakpoints.
- **Home page** — Weekly Category Breakdown card added at bottom of dashboard (stacked proportion bar + category rows); loads `GetBalanceAsync("week")` in parallel with other home data.

### Bug fixes (2026-05-27)
- Mobile nav: Reports icon-only with no child items fixed. Guard changed from `@if (_collapsed)` to `@if (_collapsed && !_mobileOpen)` so mobile drawer always renders the full labeled template with Trends and Balance children.

---

## Known Issues & Technical Debt

| ID | Issue | Status |
|---|---|---|
| KI-009 | Replace MudBlazor Snackbar with native Momentum Toast system | **RESOLVED 2026-06-05** · commit `ff833da` |
| KI-010 | `Blazor-ApexCharts` NuGet leftover in `.csproj` | **RESOLVED 2026-06-04** · commit `6b4c29f` |
| KI-013 | Daily log uses wrong local day due to UTC/local timezone mismatch | **RESOLVED 2026-05-31** |
| KI-015 | Trends daily chart buckets use UTC date instead of local date | **RESOLVED 2026-06-05** · commit `ff833da` |
| KI-016 | Production Blazor bootstrap 404 after MudBlazor removal attempt | **RESOLVED 2026-06-05** · commit `ff833da` |

Full detail: `Docs/momentum-known-issues.md`

---

## Deployment Notes

- API and Blazor WASM are deployed to Azure.
- Azure SQL is on the Serverless tier — cold start can take 20–40 seconds after idle. A warm-up retry loop runs on startup (5 attempts × 3-second backoff) with a user-visible info banner.
- CORS is configured for the known client origin. Wildcard `*` is never used.
- JWT secrets and connection strings are in Azure App Service configuration — never in source.
- **v2 schema is live** as of 2026-05-29. The production database is on migration `20260529151638_V2_DimensionModel`. Pre-migration PITR restore point: `2026-05-29T19:00:51Z UTC`.

---

## Remaining / Future Work

| Item | Priority | Notes |
|---|---|---|
| AUTH-001 refresh tokens (long-term) | Low | Near-term done. Long-term: `RefreshToken` entity/table, `/api/auth/refresh` endpoint, rotation, revocation — implement before PWA/mobile work. See `Docs/session-persistence-design-spec.md` §6. |
| Check-In Phase 2 — API + DTOs | Medium | ✅ Complete (CHK-002 Phase 2, 2026-06-05). DTOs, repository, service, controller all implemented. 15 tests added. |
| Check-In Phase 3 — standalone form | Medium | ✅ Complete (CHK-002 Phase 3, 2026-06-05). `/check-in` page, client service, temporary nav item. |
| Check-In Phase 4 — post-activity flow | Medium | ⛔ Retired (CHK-006, 2026-07-07). Originally: Add Entry routed to Check-In with linked `ActivityLogId`; save or skip. Now: Add Entry routes directly to View Log / Today / Details ON; linking a Check-In is an explicit "+ Add Check-In" action from there. |
| Check-In Phase 5A — nav structure | Medium | ✅ Complete (CHK-002 Phase 5A, 2026-06-06). Persistent top "Check In" button, "Check Ins" history nav, `/check-ins` placeholder, mobile "Manage" title. |
| Check-In Phase 5B — history screen | Medium | ✅ Complete (CHK-002 Phase 5B, 2026-06-06). `/check-ins` list with inline edit + delete; `ActivityName` on DTO. |
| Check-In Phase 6A — View Log integration | Medium | ✅ Complete (CHK-002 Phase 6A, 2026-06-06). "Details" toggle shows/adds/edits/deletes linked check-ins in View Log. |
| Check-In Phase 6B — Edit Log Entry check-in list | Low | Not started. Show associated check-ins (+ add follow-up) on the Edit Log Entry screen (design spec §11) |
| Check-In reminders (PWA / push) | Low | Deferred long-term. Azure Function timer job sends push directly without waking the API (see design spec §16) |
| Body/Energy/Mood reporting & correlation | Low | Future — depends on Check-In data; activity-input → check-in-outcome analytics (see design spec §17) |
| Password change in Settings | Low | Planned but not implemented |
| Screen-reader chart descriptions | Low | SVG charts have `role="img"` + `aria-label` but no `<title>` child |
| Social login (Google/Apple) | Deferred | UI stubs exist on Login page; backend not implemented |

---

*Momentum Handoff — Updated 2026-07-07 (CHK-006 — retired automatic post-activity Check-In redirect; new logs route directly to View Log / Today / Details ON; 54/54 tests)*
