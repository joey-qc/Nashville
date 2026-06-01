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

**Status: COMPLETE** — v2 Dimension Model migration deployed to production 2026-05-29.
Commit `79a81b5` on `feature/v2-dimension-model`. Migration `20260529151638_V2_DimensionModel`.
See `Docs/migration-snapshots/README.md` for validation results.

### What was completed (Phases 1–10)

- `Category` renamed to `Dimension` and `ActivityCategory` renamed to `ActivityDimension` at all layers: database schema, EF Core entities, repositories, services, DTOs, API controllers, and all client UI pages.
- `ActivityLogEntryDimensions` join table created and backfilled (79 rows from 54 existing log entries × their activity dimensions).
- Legacy `Category.cs` and `ActivityCategory.cs` domain entities deleted.
- All production validation checks passed; all 6 smoke tests passed.

### What was deferred (post-v2) — now complete (2026-05-31)

- **Per-entry dimension overrides at log time** ✅ — Users can now select which dimensions apply to a specific log entry when creating or editing. The selected dimensions are saved as the entry's `ActivityLogEntryDimensions` snapshot. Changing a log entry's dimensions does not affect the parent activity's defaults. `CreateActivityLogDto` and `UpdateActivityLogDto` accept an optional `DimensionIds: List<int>?`; when absent, defaults apply.
- **User-facing terminology change** ✅ — All user-facing "Category" / "Categories" labels across the UI are now "Dimension" / "Dimensions". The internal-to-UI alignment is complete.

### Architecture state (current)

- Activities are reusable templates with a set of `ActivityDimensions`.
- Each `ActivityLog` entry has its own `ActivityLogEntryDimensions` snapshot. The snapshot defaults to the activity's current dimensions but can be overridden by the user at log time or when editing an entry.
- Historical reports read from `ActivityLogEntryDimensions` — stable regardless of future changes to an activity's dimension configuration or to past edits of unrelated entries.

Future log entries may additionally support:
- contextual meaning and richer metadata

---

# Terminology Evolution

Current user-facing and internal terminology:
- **Dimension** ✅ (complete — both UI and data layer now use "Dimension")

Transition strategy:
- internal architecture first ✅ (complete — "Dimension" is the internal term at all layers since v2)
- user-facing terminology ✅ (complete — all UI labels updated 2026-05-31)

Rationale:
Momentum models multidimensional impact rather than mutually-exclusive categorization. "Dimension" is more psychologically resonant and better reflects the overlapping behavioral impact a single activity can have across multiple life areas.

---

# Future Enhancements

## Mobile UX

### Mobile-Friendly Dimension Labels

**Status:** Planned · **Priority:** Medium

Dimension names (Physical, Mental, Social, Spiritual, Housekeeping) consume significant horizontal space when displayed as chips, toggle buttons, filter pills, chart legends, or summary rows. On smaller mobile viewports this creates crowding across Add Entry, View Log, Balance, Trends, and Reports.

**Goal:** Investigate and implement a responsive dimension display strategy that preserves clarity and accessibility while reducing horizontal footprint on narrow screens.

**Approaches to evaluate:**

- Responsive label length: full name on desktop, abbreviated on mobile (e.g. PHY / MEN / SOC / SPI / HSK)
- Two-letter abbreviations
- Icon + abbreviated label combinations
- Tooltip or long-press expansion for full name

**Requirements:**

- No ambiguity between dimensions at any display size
- Accessible (screen reader receives full name regardless of visual label)
- Consistent across all screens that display dimension labels
- Does not affect stored data, API contracts, or DTO field names

---

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

## Brand Identity

### Momentum Logo and Application Icon

**Status:** Planned · **Priority:** Medium · **Dependencies:** None

Momentum currently uses the default Blazor favicon and no application logo. The product identity is now stable enough to warrant a dedicated visual brand.

**Goal:** Design and implement a Momentum logo and icon suite that reinforces the product's emotional character.

**Brand direction — reinforce:**
- momentum, forward motion, accumulation
- progress and growth over time
- consistency and positive reinforcement

**Brand direction — avoid:**
- generic task-manager or checklist imagery
- corporate dashboard / enterprise aesthetics
- static or trophy-style iconography

**Visual themes to explore:**
- momentum wave or arc
- stacked growth marks / accumulating path
- upward trajectory
- kinetic energy / motion concepts
- forward arrow with layered depth

**Deliverables:**
- Favicon (`favicon.ico` / `favicon.png`)
- Browser tab icon
- Application manifest icons (PWA)
- Login / Register page branding
- Header / navbar logo mark
- Mobile home screen icon assets
- Social / share image (future)

**Technical touch points:**
- `Momentum.Client/wwwroot/favicon.ico`
- `Momentum.Client/wwwroot/manifest.json` (icon entries)
- `Momentum.Client/wwwroot/index.html` (manifest + apple-touch-icon links)
- Login and Register page header markup
- Main layout navbar logo area

---

## Remaining MudBlazor / Legacy UI Work

All pages are now fully converted to custom HTML/CSS (Home, Add Entry, View Log, Trends, Balance, Manage Activities, Settings, Login, Register). No MudBlazor page components remain.

Remaining MudBlazor cleanup items:

### Shorten Toast Notification Duration

**Status:** Planned · **Priority:** Low

Current toast notifications stay visible slightly too long, making the UI feel less responsive and more visually cluttered than it should.

**Goal:** Reduce the default popup duration so toasts feel lighter and less intrusive, while remaining long enough to be readable on both mobile and desktop.

**Suggested defaults:**
- Success / info: ~2 seconds
- Warning / error: ~3–4 seconds

**Scope:**
- Locate the current `ISnackbar` / `MudSnackbar` configuration (likely in `Program.cs` via `AddMudServices()` options or at each `Snackbar.Add(...)` call site)
- Apply shorter durations centrally where possible rather than at each call site
- Verify the chosen durations on mobile (smaller read window, users glance at toasts)
- If the native `ToastService` (KI-009) is implemented first, bake these durations into its defaults instead

**Acceptance criteria:**
- Toasts dismiss faster than they do today
- Success toasts dismiss faster than error/warning toasts
- Duration is set in one place, not per call site
- No other UI changes are introduced

---

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