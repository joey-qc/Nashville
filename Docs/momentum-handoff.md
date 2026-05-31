# Momentum — Project Handoff & Status

This file tracks the current state of the project, what has been completed, and what remains. Update after every completed task.

---

## Current Project Status

**Phase:** Post-v2 — Dimension UI Terminology + Per-Entry Dimension Control  
**Build Status:** ✅ All projects build clean (0 warnings, 0 errors)  
**Last Updated:** 2026-05-31

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
| KI-009 | Replace MudBlazor Snackbar with native Momentum Toast system (`ToastHost` + `ToastService`) | Deferred |
| KI-010 | `Blazor-ApexCharts` NuGet leftover in `.csproj` | Open — safe to remove, no code uses it |
| KI-013 | Daily log uses wrong local day due to UTC/local timezone mismatch — entries after ~8 PM Eastern appear in next day's log; "Today" can appear empty before midnight | **Open — High** |

Full detail: `Docs/momentum-known-issues.md`

### Note on KI-013

KI-013 is an **active data accuracy bug** confirmed in production. It is independent of the v2 Dimension Model migration (which is now complete). The v2 migration did not introduce or worsen this bug.

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
| Custom toast component | Medium | Prerequisite for removing MudBlazor NuGet |
| Remove `Blazor-ApexCharts` NuGet | Low | Safe to remove now — no code references it |
| Password change in Settings | Low | Planned but not implemented |
| Screen-reader chart descriptions | Low | SVG charts have `role="img"` + `aria-label` but no `<title>` child |
| Social login (Google/Apple) | Deferred | UI stubs exist on Login page; backend not implemented |

---

*Momentum Handoff — Updated 2026-05-31 (Dimension UI terminology complete; per-entry dimension control live)*
