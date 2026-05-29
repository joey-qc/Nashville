# Momentum Roadmap

## Vision

Momentum is evolving beyond a traditional habit tracker into a personal behavioral momentum system.

The long-term vision is to provide:
- fast, low-friction behavioral capture
- multidimensional life analysis
- momentum trend visibility
- balance awareness
- reflective analytics
- emotionally reinforcing UX

Momentum is intended to function as a lightweight personal operating system for intentional living.

---

# Core Product Philosophy

## Fast Capture + Reflective Analytics

Momentum intentionally separates:
- rapid behavioral logging
- deeper behavioral reflection

### Logging UX Principles
Logging should remain:
- fast
- low-friction
- emotionally lightweight
- mobile-friendly
- action-oriented

### Analytics UX Principles
Analytics may become:
- introspective
- nuanced
- insight-rich
- interpretive
- behaviorally meaningful

The app should avoid requiring excessive reflection during data entry.

---

# Current Architectural Direction

## UI Direction

Momentum is moving away from MudBlazor toward:
- custom Razor markup
- custom HTML/CSS
- shared design tokens
- inline SVG graphics/charts
- responsive mobile-first layouts

All pages have been converted to custom HTML/CSS. The only remaining MudBlazor dependency is `ISnackbar` / `MudSnackbar` for toast notifications, retained intentionally until a native Momentum toast system is implemented (see KI-009).

---

# Current Technical Stack

- Blazor WebAssembly
- ASP.NET Core API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Azure Hosting
- GitHub Actions CI/CD

---

# Current UX Identity

The Momentum UI is intended to feel:
- calm
- focused
- reinforcing
- forward-moving
- immersive
- emotionally coherent

The design language emphasizes:
- momentum
- accumulation
- progress
- behavioral reinforcement
- clarity over clutter

---

# Known Architectural Evolution

## Activity Template vs Behavioral Event

**Status: IN PROGRESS** — v2 Dimension Model migration underway on branch `feature/v2-dimension-model`.
See `Docs/migration-snapshots/` for pre-migration artifacts and the implementation plan.

### Phase 1 milestone: ActivityLogEntryDimension
The first concrete step is materializing per-log-entry dimension assignments into a dedicated
`ActivityLogEntryDimensions` table. This decouples historical report data from the activity's
current dimension configuration — changing an activity's dimensions will no longer retroactively
alter past reports.

Simultaneously, "Category" is renamed to "Dimension" at every layer (entities, DTOs, services,
API endpoints, UI) to align with the long-term terminology goal.

### Original intent (preserved)

Current model:
- Activities define default meaning
- Log entries inherit metadata from activities

Target model:
- Activities become reusable templates with default dimensions
- Individual log entries become richer behavioral events with their own persisted dimensions

Future log entries may additionally support:
- per-entry dimension overrides at log time (UI — deferred to post-v2)
- contextual meaning and richer metadata

This transition impacts:
- database schema (new join table; table renames)
- EF Core entities and migrations
- DTOs and API contracts
- all reporting calculations (ScoreService, ReportsService)
- all UI pages

This is a major architectural milestone — implement on the dedicated feature branch only.

---

# Terminology Evolution

Current user-facing terminology:
- Category

Potential future internal terminology:
- Dimension
- Aspect
- Vector
- Domain

Rationale:
Momentum models multidimensional impact rather than mutually-exclusive categorization.

Transition strategy:
- internal architecture first
- user-facing terminology later

Potential future UX benefits:
- less task-manager language
- more holistic/self-development framing
- better support for overlapping behavioral impact
- more psychologically resonant terminology

---

# Future Enhancements

## Logging UX

### Pinned Favorites
Allow users to pin commonly-used activities for rapid access.

### Time-of-Day Smart Picks
Suggest activities based on:
- time of day
- usage patterns
- recency
- behavioral history

### Recent Activities
Surface recently-used activities for fast repeat logging.

### Progressive Log Detail
Allow optional deeper behavioral annotation while preserving low-friction logging.

---

## Analytics & Reporting

### Richer Trend Analysis
Expand reporting around:
- momentum trends
- consistency
- dimension balance
- streaks
- momentum drift

### Balance Targets
Allow users to define desired balance ratios between dimensions.

Example:
- 40% Mental
- 25% Physical
- 15% Social
- 10% Spiritual
- 10% Housekeeping

### Behavioral Insights
Generate observations such as:
- neglected dimensions
- over-dominant dimensions
- behavioral drift
- momentum stagnation
- recovery patterns

---

## Personalization & Settings

Future settings may evolve into a personal operating model configuration system.

Potential settings domains:
- scoring philosophy
- dimension weighting
- preferred balance targets
- reminder windows
- notification tuning
- emotional tone preferences
- daily reset hour
- recovery behavior
- weekly/monthly calibration

---

## Notification System

Potential future features:
- push notifications
- reminder scheduling
- nudges
- recovery prompts
- momentum encouragement

Potential future delivery:
- PWA push notifications
- mobile notifications

---

## Recovery & Re-engagement UX

Future UX should eventually address:
- stagnation
- burnout
- avoidance spirals
- disengagement
- behavioral recovery

Momentum should eventually support:
- graceful recovery
- resets
- re-engagement
- non-punitive behavioral support

---

# Known Risks

## Overcomplication Risk

The current logging flow is intentionally lightweight.

Future enhancements must preserve:
- speed
- clarity
- low cognitive load
- low-friction capture

Advanced features should remain:
- optional
- progressively disclosed
- non-intrusive

---

## Timezone Complexity

The application currently uses:
- UTC storage
- local time conversion

Future risk areas:
- DST transitions
- timezone consistency
- user timezone preferences
- reporting boundaries

---

## Chart Responsiveness

Known issue:
- chart/layout responsiveness during resize/orientation changes

Potential causes:
- SVG resizing
- viewport recalculation
- render timing

---

# Future Documentation Possibilities

Potential future addition:
- Architecture Decision Records (ADR)

Example structure:

/Docs/ADR
    ADR-001-move-away-from-mudblazor.md
    ADR-002-log-entry-dimension-overrides.md
    ADR-003-svg-charting-approach.md

Not required yet, but likely valuable later.

---

# Product Positioning

Momentum is not intended to become:
- a generic productivity app
- an enterprise dashboard
- a traditional task manager

Momentum is intended to become:
- a behavioral momentum system
- a multidimensional life tracking platform
- a reflective personal operating system
- an emotionally reinforcing behavioral UX experience

## Remaining MudBlazor / Legacy UI Work

All pages are now fully converted to custom HTML/CSS (Home, Add Entry, View Log, Trends, Balance, Manage Activities, Settings, Login, Register). No MudBlazor page components remain.

Remaining MudBlazor cleanup items:

- **Native Momentum toast system** — implement `ToastHost` + `ToastService` to replace `ISnackbar`; enables full MudBlazor removal (see KI-009)
- **Remove `ISnackbar` calls** — replace all `ISnackbar.Add(...)` usages once custom toast is live
- **Remove MudBlazor NuGet package** — after all `ISnackbar` references are eliminated
- **Remove ApexCharts NuGet package** — unused leftover from charting migration (see KI-010)

## Planned View Log Enhancements

Status: Planned

### Dynamic Period Navigation

The View Log screen will evolve from a static filter model into a navigable time-based activity browser.

---

### Day View

Current filter label:
- Today

Planned label:
- Day

Behavior:
- Show selected date beside the Day filter.
- Clicking the displayed date opens a calendar/date picker.
- User can navigate to any available log date.

Display mode:
- Raw activity log entries for the selected calendar day.

If filter is not Day:
- Hide date picker/calendar UI.

---

### Week View

Filter label:
- Week

Behavior:
- Display selected week and year.
- Clicking the displayed period allows selecting:
  - week number (1–52)
  - year
- Earliest selectable year:
  - 2026
- If only one year of data exists:
  - year may display as read-only

Aggregation behavior:
- Group entries by:
  - week
  - year
  - activity

Displayed points:
- SUM of grouped entries

Purpose:
- Weekly behavioral pattern analysis.

---

### Month View

Filter label:
- Month

Behavior:
- Display selected month and year.
- Clicking the displayed period allows selecting:
  - month
  - year

Aggregation behavior:
- Group entries by:
  - month
  - year
  - activity

Displayed points:
- SUM of grouped entries

Purpose:
- Monthly trend analysis and habit visibility.

---

### Year View

Filter label:
- Year

Behavior:
- Display selected year.
- Clicking the displayed year allows selecting a year.

Aggregation behavior:
- Group entries by:
  - year
  - activity

Displayed points:
- SUM of grouped entries

Purpose:
- Long-term activity trend visibility.

---

### Future UX Considerations

Potential future enhancements:
- previous/next period navigation arrows
- quick jump to current period
- empty-period handling
- expandable grouped rows
- drill-down from Week/Month/Year into Day detail
- sticky period header
- charts/mini visualizations