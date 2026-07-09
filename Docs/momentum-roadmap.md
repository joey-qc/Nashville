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

All pages have been converted to custom HTML/CSS. MudBlazor has been fully removed (KI-009, 2026-06-05) — `ToastService` + `ToastHost` are the native toast system. No MudBlazor references remain in the codebase.

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

Current user-facing, internal, and persisted terminology:
- **Dimension** ✅ (complete — both UI and data layer now use "Dimension")
- **Dimension names aligned end-to-end** ✅ (complete 2026-06-02 — DIM-001)

Transition strategy:
- internal architecture first ✅ (complete — "Dimension" is the internal term at all layers since v2)
- user-facing terminology ✅ (complete — all UI labels updated 2026-05-31)
- persisted names aligned ✅ (complete — `DIM001_RenameDimensions` migration updates DB rows to Body/Mind/Spirit/Connections/Responsibilities; `DimensionDisplayHelper` simplified to mobile-abbreviation-only helper)

Rationale:
Momentum models multidimensional impact rather than mutually-exclusive categorization. "Dimension" is more psychologically resonant and better reflects the overlapping behavioral impact a single activity can have across multiple life areas.

---

# Future Enhancements

## Authentication & Session

### AUTH-001 — Session Persistence Improvements

**Status:** ✅ Near-term complete (2026-06-04) · **Design spec:** `Docs/session-persistence-design-spec.md`

The current 60-minute JWT-only session causes daily re-authentication friction and conflicts with the low-friction capture philosophy. A "Keep me signed in" checkbox exists in the Login UI but is currently inert. This enhancement should be implemented before Check-In to avoid session expiry disrupting the post-activity Check-In flow.

**Near-term plan (AUTH-001):**
- Increase `Jwt:ExpiryMinutes` from 60 to **10080** (7 days) via `appsettings.json` and Azure App Service config override — zero schema or code changes required for the lifetime change itself.
- Fix stale-token cleanup in `JwtAuthStateProvider.BuildPrincipal`: remove the expired `authToken` from `localStorage` immediately when expiry is detected, rather than waiting for a 401.
- Resolve the inert "Keep me signed in" checkbox: either remove it from the Login UI or clearly document that it has no differential effect until refresh tokens are implemented. Do not leave it appearing functional when it does nothing.
- No new endpoints, no schema changes, no EF migrations.

**Long-term plan (pre-PWA/mobile):**
- Implement refresh tokens before any serious PWA/push notification/mobile-native work.
- Short-lived access token (~15 min) + longer-lived rotating refresh token (~30 days).
- `RefreshToken` entity/table with hashed token, user ID, created/expiry/revoked timestamps, and rotation chain tracking.
- `/api/auth/refresh` endpoint; token rotation on every refresh; replay detection (revoke entire family on reuse of a revoked token).
- `AuthMessageHandler` extended to retry once on 401 via silent refresh before logging the user out.
- "Keep me signed in" becomes real: short refresh-token TTL (1 day) vs. long (30 days) based on login-time choice.

---

## Check-Ins

### Check-In Feature (State / Outcome Capture)

**Status:** Planned (design documented) · **Priority:** Medium · **Design spec:** `Docs/check-in-feature-design-spec.md`

A planned new domain that captures the user's **current state / outcomes** (Body / Energy / Mood), complementing Activity Logs which capture **behaviors / inputs**. This directly supports the long-standing "Fast Capture + Reflective Analytics" philosophy: lightweight outcome snapshots that can later be correlated against behavioral inputs.

Key planned characteristics (full detail in the design spec):
- Three metrics — **Body, Energy, Mood** — each on a `-5`…`+5` scale (`0` = baseline).
- A **separate model from Dimensions** — Check-In metrics describe user state, not activity impact areas; they do not reuse the `Dimensions` table.
- New `CheckIn` entity with a user-editable `CheckedInAt` (used for analytics/display) and an internal `CreatedAt` (audit only).
- **Optional ActivityLog association** — a check-in can be standalone (e.g. morning) or linked to one activity log (e.g. post-Exercise). One activity log can have many check-ins.
- **Post-activity flow — retired (CHK-006, 2026-07-07):** an earlier iteration automatically navigated to the Check-In form after saving a new activity log. This automatic redirect has been retired — new logs now go straight to View Log / Today / Details ON, and a Check-In can still be linked to the entry from there via "+ Add Check-In." See `Docs/check-in-feature-design-spec.md` §7.
- **Smart defaults** — preload from the most recent check-in, or default all scores to `0` if none exists.
- **No check-in notes in v1** — capture stays lightweight.
- **View Log integration** — rename the existing "Notes" toggle to **"Details"**; when ON, show both notes and linked check-ins.
- **First-class Check-Ins history screen** — period-based, similar to View Log; shows all check-ins; linked check-ins display contextual text like `After: Exercise / Gym`, standalone ones show no extra label.
- **Persistent "Check In" action button** on authenticated pages (alongside Add Entry); creation flows stay on persistent buttons, not in the nav.
- **Future nav direction:** Home · View Log · Check-Ins · Trends · Balance · (future) Body/Energy/Mood reporting · Manage · Settings.
- **Mobile masthead concern:** when both Add Entry and Check In buttons are present, shorten "Manage Activities" → "Manage" at the mobile breakpoint to prevent header wrapping.

### Check-In Reminders via PWA / Push Notifications

**Status:** Deferred (long-term) · **Priority:** Low · **Design spec:** `Docs/check-in-feature-design-spec.md` §16

Long-term reminder delivery for check-ins, explicitly **not** part of the near-term Check-In implementation. Planned architecture:
- PWA installable app experience.
- Browser push notifications for check-in reminders.
- An **Azure Function timer job** queries notification recipients and sends push notifications **directly**, without requiring the main API to stay awake (accommodates the Azure SQL Serverless cold-start / idle model).
- The API wakes only when the user opens the app or responds to a notification.

### Body / Energy / Mood Reporting & Correlation

**Status:** Future concept · **Priority:** Low · **Design spec:** `Docs/check-in-feature-design-spec.md` §17

Reporting and analytics enabled once Check-In data exists — correlating behavioral inputs against state outcomes:
- Body / Energy / Mood over time (trend lines).
- Average mood after specific activities.
- Energy changes after morning exercise.
- Next-day Body score after resistance training.
- Effects of meals / alcohol / meditation / socializing on subsequent check-in metrics.
- General activity-input → check-in-outcome correlation analysis.

A dedicated Body/Energy/Mood reporting page is anticipated in the future nav structure.

---

## Mobile UX

### Mobile-Friendly Dimension Labels

**Status:** ✅ Complete (2026-06-01 — MOB-001) · **Priority:** Medium

Dimension names were simultaneously renamed (Physical→Body, Mental→Mind, Spiritual→Spirit, Social→Connections, Housekeeping→Responsibilities) and given mobile-responsive display via `DimensionDisplayHelper`.

**Implemented approach:** Responsive label spans (`.dim-full` / `.dim-abbr`) toggled by a global CSS media query at ≤540px. Both spans exist in the DOM; CSS hides the one not appropriate for the viewport. Desktop shows full names; mobile shows Body / Mind / Spirit / Con / Rsp.

**Accessibility:** `title=`, `aria-label=`, or both on every chip/button. `.dim-abbr` spans carry `aria-hidden="true"`. Screen readers always receive the full display name.

**Scope covered:** Add Entry (LogActivity), Edit Log Entry (LogActivity), New/Edit Activity (ManageActivities), View Log filter chips, View Log entry-level dimension metadata, Trends filter chips, Trends sparkline rows.

**No stored data changes:** Database still holds Physical/Mental/Spiritual/Social/Housekeeping. `DimensionDisplayHelper` maps client-side. API contracts unchanged.

---

## UX Consistency

### Standardize Edit Screen Action Layout

**Status:** ✅ Complete (2026-06-02 — MOB-002)

Edit screens (Edit Activity, Edit Log Entry) use a consistent action row layout: Save + Cancel left-aligned, icon-only delete control right-aligned. The "DELETE ACTIVITY" bare text button was replaced with the arm/confirm/cancel trash icon pattern to match View Log row deletion. Mobile no longer requires scrolling to reach delete controls.

---

### Standardize Activity and Log Entry Edit/Delete Interactions

**Status:** ✅ Complete (2026-06-02 — UX-001 + UX-001A)

- Removed the redundant pencil edit icon from Manage Activities activity rows. Row tap is the single edit affordance, matching View Log behavior.
- Added delete controls to the Edit Log Entry screen. Activities and Log Entries now share identical edit/delete UX: trash icon → arm → red check to confirm / gray X to cancel.
- Manage Activities row-level trash icon now uses the arm/confirm/cancel pattern (UX-001A). Previously it immediately triggered the delete flow; now it requires a confirmation step identical to View Log row deletion.
- The arm/confirm/cancel pattern is now the single, consistent destructive-action UX across all three deletion surfaces: View Log rows, Edit Activity, Edit Log Entry, and Manage Activities rows.

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
**Status:** ✅ Delivered by Rich Notes v1 (2026-06-03 — commit `97093de`, deployed to production).

Rich Notes v1 added optional rich text annotation to the existing `ActivityLog.Notes` field (bold/italic/underline/bullets, sanitized HTML) with a View Log "Notes" toggle — deeper behavioral annotation without adding friction to fast logging. A separate Reflection domain (table/page/API) was explored and deferred in favor of this lower-footprint approach. See `Docs/rich-notes-v1-design-spec.md`.

Future extension (not in v1): notes full-text search/filtering.

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

### AI Integration

**AI-001 (delivered 2026-07-09):** A minimal **read-only** AI integration API now exists — `GET /api/ai/today`, returning an AI-safe snapshot (date, total points, entry count, and per-entry activity name/points/dimension names/timestamp) of the configured AI user's activity logs for the current local day. Authenticated via a shared `X-Momentum-AI-Key` header rather than JWT, since it's a single server-to-server integration, not a per-end-user endpoint. Notes/journal text, IDs, and user profile data are never exposed. See `Docs/momentum-software-specifications.md` §4.5/§7.2 for the full contract.

This is the foundation the **Behavioral Insights** direction above depends on — v1 deliberately stops at read-only data access for a single configured user. Future direction, not yet started:
- Additional read-only query endpoints (date ranges, weekly/monthly trends, Check-In Body/Energy/Mood data).
- Actual AI-generated observations/insights consuming this data (external to the API itself in v1).
- Multi-user AI access (v1 supports exactly one configured user via `Ai:UserEmail`) if ever needed.
- Any write capability — explicitly out of scope for the foreseeable future given the sensitivity of write access to personal wellness data.

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

## MudBlazor / Legacy UI Work — COMPLETE ✅

All pages are fully converted to custom HTML/CSS. MudBlazor has been fully removed (KI-009, 2026-06-05).

- ✅ All pages converted to custom HTML/CSS
- ✅ Native `ToastService` + `ToastHost` replaces `ISnackbar` / `MudSnackbar` — 3 s Success/Info, 4.5 s Error/Warning, set in one place
- ✅ MudBlazor NuGet removed
- ✅ Blazor-ApexCharts NuGet removed (KI-010, 2026-06-04)
## Planned View Log Enhancements

### Dynamic Period Navigation

**Status:** ✅ Anchor-date picker delivered (NAV-001, 2026-06-21) · Aggregation/grouping still planned

The View Log screen will evolve from a static filter model into a navigable time-based activity browser.

**NAV-001 (delivered):** All four date-filtered pages (View Log, Check-Ins, Trends, Balance) now have a compact anchor-date picker (date pill beside the period pill). Selecting a date sets the end boundary for the selected period window: Day = that date only; Week = selected date + previous 6 days; Month = selected date + previous 29 days. Future dates are blocked. The period pill label remains Day/Week/Month; the date picker provides the historical navigation capability.

The originally-planned aggregation views (grouped Week/Month/Year with activity-level rollups, previous/next navigation arrows, and a Year period for View Log) remain on the roadmap below.

---

### Day View

Current filter label:
- Today → Day

Behavior:
- Show selected date beside the Day filter. ✅ (NAV-001)
- Clicking the displayed date opens a calendar/date picker. ✅ (NAV-001)
- User can navigate to any available log date. ✅ (NAV-001)

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