# Momentum — Project Handoff & Status

This file tracks the current state of the project, what has been completed, and what remains. Update after every completed task.

---

## Current Project Status

**Phase:** UI Redesign — Complete  
**Build Status:** ✅ All projects build clean (0 warnings, 0 errors)  
**Last Updated:** 2026-05-27

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

---

## Completed Work

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

### Pre-v2-migration note on KI-013

KI-013 is an **active data accuracy bug** affecting all daily queries (Home dashboard, View Log Today, Trends daily chart, Balance best/worst days). It is independent of the v2 Dimension Model migration but should be fixed before or alongside the migration — the v2 schema work changes how `ActivityLogEntryDimensions` are queried, and fixing UTC/local boundaries in the same pass reduces the risk of the bug being baked into the new query patterns.

---

## Deployment Notes

- API and Blazor WASM are deployed to Azure.
- Azure SQL is on the Serverless tier — cold start can take 20–40 seconds after idle. A warm-up retry loop runs on startup (5 attempts × 3-second backoff) with a user-visible info banner.
- CORS is configured for the known client origin. Wildcard `*` is never used.
- JWT secrets and connection strings are in Azure Key Vault / App Service configuration — never in source.

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

*Momentum Handoff — Updated 2026-05-29*
