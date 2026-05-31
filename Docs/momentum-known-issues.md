# Momentum — Known Issues Log

Tracks bugs encountered during development, their root causes, and resolution status. Issues are assigned stable IDs (`KI-NNN`) and never removed — resolved issues are marked and preserved for future reference.

**Status values:** `RESOLVED` · `OPEN` · `DEFERRED` · `BY DESIGN`

---

## KI-001 — Logout 401 crash: CategoryService.LoadAsync throwing after SignOut

| Field | Value |
|---|---|
| **ID** | KI-001 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Services/CategoryService.cs`, `Home.razor` |
| **Severity** | High — unhandled exception visible in browser console; inconsistent logout UX |
| **Discovered** | During UI redesign phase |

### Symptoms
Clicking Sign Out produced a `System.Net.Http.HttpRequestException` in the browser console. The UI would sometimes freeze instead of redirecting to `/login`. Stack trace pointed to `CategoryService.LoadAsync`.

### Root Cause (multi-layer)
1. **Dead code:** `Home.OnInitializedAsync` called `CategoryService.GetAllAsync()` and assigned the result to a local variable `cats` that was never read. This call was the primary crash point.
2. **Unsafe HTTP client:** `GetFromJsonAsync<T>()` internally calls `EnsureSuccessStatusCode()`. After logout the JWT is cleared, so the next API call returns 401, causing `GetFromJsonAsync` to throw `HttpRequestException` instead of returning null.
3. **Missing auth guard:** `Home.OnInitializedAsync` had no early-exit check for authenticated state. During the async notification propagation window after logout, the page could fire `OnInitializedAsync` with a cleared token.

### Resolution
- Removed the dead `CategoryService.GetAllAsync()` call from `Home.OnInitializedAsync`.
- Added auth guard as the first action in `Home.OnInitializedAsync` — returns early if `!IsAuthenticated`.
- Converted all client service `GetFromJsonAsync` calls to `GetAsync` + explicit `IsSuccessStatusCode` check → return `null` / `[]` on failure. Affected: `CategoryService`, `ActivityService`, `ScoreService`, `ReportsService`, `ActivityLogService`.

---

## KI-002 — Setback total double-counted on Home page

| Field | Value |
|---|---|
| **ID** | KI-002 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor` |
| **Severity** | Medium — incorrect data shown to user on primary dashboard |
| **Discovered** | During Home page UI redesign |

### Symptoms
View Log showed a single negative entry worth −8 pts. Home dashboard showed Setbacks total as −16.

### Root Cause
`SetbacksTotal` was computed by summing over `_todayByCategory`, which is a per-category breakdown. An activity with multiple categories (e.g., Physical + Spiritual) produces two rows in that list, each carrying the full `PointsRecorded` value. Summing these rows counts the points once per category rather than once per log entry.

```csharp
// WRONG — double-counts multi-category activities
private int SetbacksTotal => _todayByCategory.Where(c => c.Total < 0).Sum(c => c.Total);
```

### Resolution
Changed `SetbacksTotal` to sum directly from `_todayLogs` (one entry per logged activity):

```csharp
// CORRECT — one contribution per log entry
private int SetbacksTotal => _todayLogs.Where(l => l.PointsRecorded < 0).Sum(l => l.PointsRecorded);
```

---

## KI-003 — Momentum ring shows full green gradient on negative-score days

| Field | Value |
|---|---|
| **ID** | KI-003 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor`, `Home.razor.css` |
| **Severity** | Medium — misleading positive visual feedback on bad days |
| **Discovered** | During Home page UI redesign |

### Symptoms
When today's total score was negative (e.g., −8), the circular progress ring on the Home page still rendered as a full green gradient, visually suggesting a positive day.

### Root Cause
The ring SVG `stroke` was hardcoded to `url(#ringGrad)` (the green gradient) with no conditional state for negative totals.

### Resolution
Added `IsNegativeDay` computed property. Ring conditionally renders:
- **Positive day:** full green gradient stroke, opacity 1.
- **Negative day:** `var(--negative)` red stroke, opacity 0.4 (muted to indicate a "dim" ring, not a full-danger warning).

Score text also conditionally applies `.ring-score-neg` CSS class (red fill) on negative days.

```razor
<circle class="ring-full" cx="70" cy="70" r="52"
    stroke="@(IsNegativeDay ? "var(--negative)" : "url(#ringGrad)")"
    stroke-opacity="@(IsNegativeDay ? "0.4" : "1")"/>
```

---

## KI-004 — Build error RZ1023: SVG `<text>` element with attributes invalid in Razor

| Field | Value |
|---|---|
| **ID** | KI-004 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor` |
| **Severity** | Critical — build failure; app would not compile |
| **Discovered** | During chart label rendering work |

### Symptoms
```
error RZ1023: "<text>" and "</text>" tags cannot contain attributes.
```
Build failed on two lines that rendered SVG `<text>` elements with `x`, `y`, and `text-anchor` attributes.

### Root Cause
The Blazor/Razor parser treats the HTML tag name `<text>` as a reserved pseudo-element used to output literal markup inside code blocks (e.g., `@{ <text>hello</text> }`). When `<text>` appears with attributes in a Razor `.razor` file, the parser raises RZ1023 because it tries to interpret it as the Blazor syntax construct, not as an SVG element.

### Resolution
Converted all SVG `<text>` elements to `@((MarkupString)$"<text ...>...</text>")` inline literals. This renders the SVG element as a raw string, bypassing Razor's parser entirely.

> **Gotcha downstream → see KI-005:** MarkupString-rendered elements are not processed by Blazor's renderer, so they never receive the scoped CSS isolation attribute (`b-xxxxxxxx`). CSS rules in `.razor.css` files do not apply to them.

---

## KI-005 — SVG chart axis labels invisible / wrong color due to CSS isolation bypass

| Field | Value |
|---|---|
| **ID** | KI-005 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor`, `Home.razor.css`, `momentum-theme.css` |
| **Severity** | Medium — chart labels invisible or incorrectly colored |
| **Discovered** | After fixing KI-004 |

### Symptoms
After the RZ1023 fix (KI-004), chart axis labels rendered but appeared extremely dark (nearly black against the dark chart background) despite CSS rules in `Home.razor.css` specifying lighter fill colors.

### Root Cause
Blazor scoped CSS (`.razor.css` files) works by adding a unique attribute (`b-xxxxxxxx`) to every DOM element the Blazor renderer touches. CSS selectors in the scoped file are compiled to include that attribute. However, elements rendered via `@((MarkupString)...)` bypass the Blazor renderer — they are injected as raw HTML and never receive the scope attribute. Scoped CSS selectors therefore never match them. SVG `<text>` elements fell back to the SVG default fill: black.

Changing the color token in `Home.razor.css` had no effect because the selector never matched.

### Resolution
Two-part fix:
1. Added a dedicated `--chart-label: #A8BED0` token to `momentum-theme.css` (global, not scoped — so it applies everywhere including inside MarkupString elements).
2. Applied the color as an **inline `style` attribute** directly on the MarkupString SVG text elements, not via a CSS class selector. Inline styles are part of the raw HTML string, so they apply regardless of CSS isolation.

```razor
@((MarkupString)$"<text x=\"{x}\" y=\"{y}\" text-anchor=\"middle\" style=\"font-size:9px;fill:var(--chart-label)\">{label}</text>")
```

**Rule established:** Any SVG element rendered via `MarkupString` must use inline `style` attributes for all visual properties — never rely on scoped CSS class selectors to style them.

---

## KI-006 — Mobile dashboard clips horizontally instead of stacking

| Field | Value |
|---|---|
| **ID** | KI-006 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor.css` |
| **Severity** | Medium — app unusable on mobile without horizontal scroll |
| **Discovered** | During Home page UI redesign |

### Symptoms
On narrow viewports (phones, small tablets), the Home page dashboard cards overflowed horizontally. The two-column grid remained side-by-side at all screen widths, requiring horizontal scrolling.

### Root Cause
The `.hero-row` and `.split-row` grid layouts used fixed `grid-template-columns` values (e.g., `1fr 300px`) with no responsive breakpoints. The sidebar column could not shrink below its minimum content width, forcing overflow.

### Resolution
Added `@media (max-width: 768px)` and `@media (max-width: 480px)` breakpoints to `Home.razor.css`:
- 768px: both grid rows collapse to `grid-template-columns: 1fr`; KPI cards switch to a 2-column sub-grid.
- 480px: KPI cards stack to 1 column; momentum body ring + bars stack vertically; category tags hidden to keep activity rows compact.

---

## KI-007 — White focus ring appearing around page headings

| Field | Value |
|---|---|
| **ID** | KI-007 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/wwwroot/css/momentum-theme.css` |
| **Severity** | Low — cosmetic; confusing visual artifact |
| **Discovered** | During Login page UI redesign |

### Symptoms
A white browser focus outline appeared around `<h1>` and other heading elements (e.g., "Sign in") after page load or navigation. The headings are not interactive and should never show a focus ring.

### Root Cause
Some browsers (particularly Chromium-based) apply a default focus outline to heading elements that receive programmatic focus (e.g., when Blazor navigates to a page and focuses the first heading for accessibility). The default browser outline style is white/blue and highly visible on the dark Momentum background.

### Resolution
Added `outline: none` to the global heading rule in `momentum-theme.css`:

```css
h1, h2, h3, h4, h5, h6 {
  font-family: 'Space Grotesk', sans-serif;
  outline: none; /* headings are not interactive; suppress browser focus ring */
}
```

---

## KI-008 — Dead code: unused CategoryService call in Home.OnInitializedAsync

| Field | Value |
|---|---|
| **ID** | KI-008 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Pages/Home.razor` |
| **Severity** | Low — no visible symptom, but contributed to KI-001 logout crash |
| **Discovered** | During KI-001 investigation |

### Symptoms
None visible to user. Found during code review when diagnosing KI-001.

### Root Cause
During an earlier refactor, `Home.OnInitializedAsync` retained a call to `CategoryService.GetAllAsync()` that assigned to a local `cats` variable. The Home page never used that variable — categories were already embedded in each `ActivityLogDto.Categories` returned by the log API. The call was dead code that survived the refactor.

### Resolution
Removed the `var cats = await CategoryService.GetAllAsync();` line. No functional change required — the home page already had all category data it needed from the log DTOs.

---

## KI-009 — Replace MudBlazor Snackbar with native Momentum Toast system

| Field | Value |
|---|---|
| **ID** | KI-009 |
| **Status** | DEFERRED |
| **Area** | UI Infrastructure — `Momentum.Client/Components/`, `MainLayout.razor`, `Program.cs`, MudBlazor NuGet |
| **Severity** | Low — fully functional today; MudBlazor snackbar works correctly; this is a clean-up and design-consistency initiative |
| **Target** | Prioritize when MudBlazor NuGet is being removed (see KI-010) |

### Description

All pages and components have been converted from MudBlazor to custom HTML/CSS (CLAUDE.md §3.1). The **one remaining MudBlazor dependency** is `ISnackbar` / `MudSnackbar`, used for toast-style notifications (success, error, warning) throughout the application. Until a native Momentum toast system exists, `ISnackbar` is the approved temporary mechanism.

This issue tracks the full initiative: designing and building the native toast system, migrating all call sites, and removing MudBlazor entirely from the project.

**Why this matters beyond cleanup:**
- MudSnackbar uses its own visual language (colors, radius, shadows) that does not align with Momentum's design tokens.
- The MudBlazor NuGet package adds ~300 kB to the WASM payload; removal meaningfully improves cold-start time.
- A native `ToastService` can be better typed, easier to test, and can be extended (e.g., persistent toasts, action buttons) without MudBlazor constraints.

### Current State

`ISnackbar` is injected in the following contexts (not exhaustive — all pages with form submission or destructive actions):
- `LogActivity.razor` — success on log submission
- `ActivityDetail.razor` — success on edit / delete log entry
- `Activities.razor` — success/error/conflict on activity CRUD
- `Settings.razor` — success/error on profile save

`AddMudServices()` is registered in `Program.cs`. `MudSnackbarProvider` is rendered in `MainLayout.razor`.

### Architectural Design (target state)

#### `ToastService` (singleton)
```csharp
// Momentum.Client/Services/ToastService.cs
public class ToastService
{
    public event Action<ToastMessage>? OnShow;
    public void Show(string message, ToastType type = ToastType.Success, int durationMs = 4000)
        => OnShow?.Invoke(new ToastMessage(message, type, durationMs));
}

public record ToastMessage(string Message, ToastType Type, int DurationMs);
public enum ToastType { Success, Error, Warning, Info }
```

#### `ToastHost` component
- Registered in `MainLayout.razor` (outside the main content area, inside the auth shell).
- Subscribes to `ToastService.OnShow` via `StateHasChanged`.
- Renders a fixed-position overlay (`bottom-right`, `z-index` above sidebar).
- Manages a queue of active toasts; each auto-dismisses after `DurationMs`.
- Supports swipe-to-dismiss on mobile (touch events).
- Stacks gracefully: newest toast appears at bottom, pushes older ones up.

#### Visual design
- Uses `var(--surface-2)` card background + `1px solid var(--border)` border.
- Left accent border (4px) matches toast type:
  - `Success` → `var(--primary)` (green)
  - `Error` → `var(--negative)` (red)
  - `Warning` → `var(--cat-social)` (amber)
  - `Info` → `var(--cat-mental)` (sky blue)
- Entry animation: slide in from right (`translateX`) with `opacity` fade — 200ms ease-out.
- Exit animation: fade out with slight upward translate — 180ms ease-in.
- Mobile: toasts appear at top-center (full-width minus 24px margin) instead of bottom-right.

### Migration Steps

1. Implement `ToastService` in `Momentum.Client/Services/` and register as `Singleton` in `Program.cs`.
2. Build `ToastHost.razor` + `ToastHost.razor.css` in `Momentum.Client/Components/`.
3. Add `<ToastHost />` to `MainLayout.razor`; remove `<MudSnackbarProvider />`.
4. Replace all `ISnackbar.Add(...)` call sites with `ToastService.Show(...)`.
5. Remove `[Inject] ISnackbar Snackbar` from all pages.
6. Remove `builder.Services.AddMudServices()` from `Program.cs`.
7. Remove `<PackageReference Include="MudBlazor" .../>` from `Momentum.Client.csproj`.
8. Update `CLAUDE.md §3.1` and `§3.2` to remove the `ISnackbar` exception note.
9. Update this issue to RESOLVED; update KI-010 if ApexCharts removal is also complete.

---

## KI-010 — ApexCharts.Blazor NuGet package still referenced

| Field | Value |
|---|---|
| **ID** | KI-010 |
| **Status** | OPEN |
| **Area** | `Momentum.Client.csproj` |
| **Severity** | Low — unused package bloats build output; no runtime impact |

### Description
The `Blazor-ApexCharts` NuGet package is referenced in `Momentum.Client.csproj` but is no longer used anywhere in the codebase. All charts are rendered as custom inline SVG (see CLAUDE.md §3.2). The package was retained as a leftover during the charting migration.

### Resolution Path
Remove `<PackageReference Include="Blazor-ApexCharts" .../>` from `Momentum.Client.csproj`. Verify no remaining `@using ApexCharts` directives exist in any `.razor` file before removing.

> **Prerequisite check:** `grep -r "@using ApexCharts" --include="*.razor"` should return no results before removing the package.

---

## KI-011 — ~~Custom global toast: prerequisite for full MudBlazor removal~~

| Field | Value |
|---|---|
| **ID** | KI-011 |
| **Status** | RETIRED — consolidated into KI-009 |
| **Consolidated** | 2026-05-28 |

This issue was originally tracked separately from KI-009 to distinguish "replace the call sites" (KI-009) from "build the toast component" (KI-011). In practice they are the same initiative and cannot be resolved independently. All content from KI-011 has been merged into the KI-009 description. **KI-011 is retired; do not reuse this ID.**

---

---

## KI-012 — Mobile nav drawer: Reports shows icon-only; Trends and Balance missing

| Field | Value |
|---|---|
| **ID** | KI-012 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Client/Layout/MainLayout.razor`, `MainLayout.razor.css` |
| **Severity** | High — primary navigation unusable for Reports section on mobile |
| **Discovered** | Mobile browser testing |

### Symptoms
When opening the mobile drawer via the hamburger button, the Reports nav entry rendered as an icon with no label. The Trends and Balance child items were completely absent.

### Root Cause
`ToggleSidebar()` toggles both `_collapsed` and `_mobileOpen` simultaneously (the same button serves both desktop collapse and mobile drawer open). On mobile, when the user taps the hamburger from the default state:
- `_collapsed = false → true`
- `_mobileOpen = false → true`

The Reports nav section used `@if (_collapsed)` to select between two Razor templates:
- **`true` branch:** icon-only `<a>` element (no `<span class="nav-label">`, no chevron, no child links)
- **`false` branch:** full button+label+chevron+children group

Because `_collapsed = true` whenever the mobile drawer is open, Razor always chose the icon-only branch on mobile. CSS `!important` overrides in the mobile media query can restore the *visibility* of elements that exist in the DOM but were hidden — they **cannot create elements that were never rendered**. The label and children never existed in the DOM, so no CSS rule could show them.

### Resolution
Changed the Razor conditional guard from `@if (_collapsed)` to `@if (_collapsed && !_mobileOpen)`:

```razor
@if (_collapsed && !_mobileOpen)
{
    <!-- Desktop icon-rail only -->
    <a href="/reports" class="nav-item ..."><svg .../></a>
}
else
{
    <!-- Full nav: desktop expanded OR mobile open drawer -->
    <button ... aria-expanded="@(_reportsExpanded ? "true" : "false")" aria-controls="reports-submenu">
        <svg .../><span class="nav-label">Reports</span><svg class="nav-chevron..."/>
    </button>
    <div id="reports-submenu">
        @if (_reportsExpanded) { <!-- Trends + Balance --> }
    </div>
}
```

This ensures the full labeled template renders whenever the mobile drawer is open, regardless of `_collapsed` state. The `display: contents` CSS rule on `#reports-submenu` keeps the wrapper layout-neutral.

Also added `aria-expanded` and `aria-controls` to the Reports toggle button for keyboard accessibility.

**Pattern established:** Any Razor conditional that renders different nav item templates based on collapsed state must use `@if (_collapsed && !_mobileOpen)`, not `@if (_collapsed)`. See momentum-design-system.md §13 Sidebar for the authoritative rule.

---

## KI-013 — Daily log uses wrong local day due to UTC/local timezone mismatch

| Field | Value |
|---|---|
| **ID** | KI-013 |
| **Status** | RESOLVED |
| **Area** | Date/Time Handling — `Momentum.Client/Pages/`, `Momentum.Application/Services/ScoreService.cs`, `Momentum.Infrastructure/Repositories/` |
| **Severity** | High — entries appear on the wrong day; daily scoring, View Log, and Home dashboard all show incorrect data |
| **Discovered** | Live use testing, Eastern timezone |
| **Resolved** | 2026-05-31 |

### Symptoms

Two distinct but related manifestations of the same underlying UTC/local boundary mismatch:

**Symptom A — Early rollover (View Log Today appears empty before midnight)**
- "Today" in View Log and the Home dashboard appears empty after approximately 9 PM local Eastern time, even when entries were logged earlier the same evening.
- App behaves as though the local calendar day has already ended before the clock reaches midnight.

**Symptom B — Prior-evening entries appear in the next day's log**
- Entries logged the previous night appear in the *next* local day's View Log "Today" screen.
- Observed example: "Programming" and "Salad" entries logged at **8:23 PM Eastern** on a given night appeared in the following day's "Today" view.

### Production Reproduction (confirmed 2026-05-29)

- **Balance page** reported Friday total: **+1**
- **View Log "Today"** reported Friday total: **+26**
- Cause: Prior-evening entries from Thursday night were included in Friday's "Today" view (UTC boundary mismatch).

### Root Cause

`ActivityLog.LoggedAt` is stored in UTC (correct). The bug was in how day boundaries were computed for filtering.

Multiple locations used `DateTime.UtcNow.Date` as the start of "today":

- `ActivityDetail.razor` `GetDateRange()` — all three periods (day/week/month) used UTC date arithmetic
- `Home.razor` `OnInitializedAsync` — `todayUtc = DateTime.UtcNow.Date` for today's log fetch
- `LogActivity.razor` `OnInitializedAsync` — same pattern for "today so far" count
- `Balance.razor` `GetPeriodFrom()` — UTC date for week/month/year period starts
- `ScoreService.GetSummaryAsync` (server) — `todayStart = DateTime.UtcNow.Date` for TodayTotal and WeekTotal filtering

For Eastern time (UTC-4 in summer / UTC-5 in winter), UTC midnight is 8–9 PM of the *previous* local evening. This produced the bidirectional mismatch: entries logged after ~8 PM Eastern were UTC-dated to the next day (Symptom B) and "today" filters excluded entries logged that same evening (Symptom A).

`Balance.razor` `DayScores` was **already correct** — it used `DateOnly.FromDateTime(l.LoggedAt.ToLocalTime())` for per-day grouping.

The `ActivityLogRepository.GetByDateRangeAsync` query is also correct — it uses `>= from && < to` — so no changes were needed in the repository or EF layer.

### Resolution

**Client-side (Blazor WASM):** `DateTime.Today` in the browser returns the user's local midnight as `DateTimeKind.Local`. Calling `.ToUniversalTime()` converts to the correct UTC equivalent. All client-side day boundary computations now use this pattern instead of `DateTime.UtcNow.Date`.

```csharp
// Before (wrong — UTC midnight used as day boundary)
var todayUtc = DateTime.UtcNow.Date;
LogService.GetByDateRangeAsync(todayUtc, todayUtc.AddDays(1));

// After (correct — local midnight converted to UTC)
var localToday = DateTime.Today; // browser local midnight
LogService.GetByDateRangeAsync(localToday.ToUniversalTime(), localToday.AddDays(1).ToUniversalTime());
```

**Server-side (Score Summary):** The API server has no timezone context. `ScoresController.GetSummary` now accepts optional `todayStartUtc`, `weekStartUtc`, and `monthStartUtc` query parameters. The client computes these from local date arithmetic and passes them on every summary request. The server uses them when present; falls back to UTC.Now boundaries when called without params (backward-compatible).

```csharp
// Client ScoreService now sends:
// GET /api/scores/summary?todayStartUtc=2026-05-31T04:00:00Z&weekStartUtc=...&monthStartUtc=...

// Server ScoreService now accepts (optional, falls back to UTC.Now if absent):
public async Task<ScoreSummaryDto> GetSummaryAsync(string userId,
    DateTime? todayStartUtc = null, DateTime? weekStartUtc = null, DateTime? monthStartUtc = null)
```

### Files Changed

| File | Change |
|---|---|
| `Momentum.Client/Pages/ActivityDetail.razor` | `GetDateRange()` uses `DateTime.Today.ToUniversalTime()` for all periods |
| `Momentum.Client/Pages/Balance.razor` | `GetPeriodFrom()` uses local date arithmetic for week/month/year starts |
| `Momentum.Client/Pages/Home.razor` | Day log fetch uses `DateTime.Today.ToUniversalTime()` boundaries |
| `Momentum.Client/Pages/LogActivity.razor` | Same fix for "today so far" log count |
| `Momentum.Client/Services/ScoreService.cs` | Computes and passes `todayStartUtc`, `weekStartUtc`, `monthStartUtc` on summary request |
| `Momentum.Application/Interfaces/IScoreService.cs` | Added optional boundary params to `GetSummaryAsync` |
| `Momentum.Application/Services/ScoreService.cs` | Uses client-supplied boundaries when provided |
| `Momentum.API/Controllers/ScoresController.cs` | Accepts `[FromQuery]` boundary params, forwards to service |
| `Momentum.Tests/ScoreServiceTests.cs` | Added regression test: entry at 23:00 local (03:00 UTC next day) excluded from local today |

### Known Remaining Limitation

`ScoreService.GetWeeklyComparisonAsync` and `GetTotalsAsync` (Trends charts) still group entries by `l.LoggedAt.Date` (UTC date). Entries logged within a few hours of UTC midnight may be bucketed into the adjacent day in trend charts. This affects the weekly comparison bar chart and Trends page charts, but only for entries near UTC midnight. These are lower-impact compared to the primary View Log / Score Summary symptoms and are deferred to a future fix.

---

## KI-014 — UpdateAsync: first save fails when adding a new dimension to an existing log entry

| Field | Value |
|---|---|
| **ID** | KI-014 |
| **Status** | RESOLVED |
| **Area** | `Momentum.Application/Services/ActivityLogService.cs` |
| **Severity** | High — editing log-entry dimensions failed on first attempt; second attempt succeeded |
| **Discovered** | Local testing of per-entry dimension control feature (2026-05-31) |

### Symptoms

When editing a log entry and adding a dimension that the entry did not previously have:

- Clicking Save showed the generic "Failed to update log entry. Please try again." toast.
- Clicking Save a second time (without any further changes) succeeded.
- The parent activity's dimensions were unaffected in both cases.

### Root Cause

`UpdateAsync` was calling `Map(log)` directly after `SaveChangesAsync()` using the in-memory `ActivityLog` entity. `Map()` accesses `led.Dimension.Id` / `.Name` / `.ColorHex` on each `ActivityLogEntryDimension` in the snapshot.

When a new dimension is added to the snapshot (e.g., Social/DimensionId=4 added to an entry that only had Mental/DimensionId=2), the new `ActivityLogEntryDimension` is created as:
```csharp
new ActivityLogEntryDimension { DimensionId = 4 }
```
The `Dimension` navigation property on this object is `null`. EF Core can only fix up navigation properties after `SaveChanges` by matching FK values against entities **already loaded into the current DbContext scope**. Because the original `GetByIdAsync` call eagerly loaded only the `Dimension` entities referenced by the log's *pre-existing* snapshot, `Dimension{Id=4}` (Social) was not in the identity map. EF fixup could not populate `led.Dimension`, so `Map()` threw a `NullReferenceException`.

The controller's `try/catch` caught this, logged a Serilog error, and returned `500`. The client service received a non-success response and returned `null`, triggering the error toast.

**Why the second save succeeded:** `SaveChangesAsync()` committed the dimension changes to the DB before the exception was thrown. On the second save, `GetByIdAsync` reloaded the log, this time eagerly loading both Mental and Social dimension entities. Both were in the identity map, EF fixup worked, and `Map()` succeeded.

A secondary issue was also present: the `Clear()` + re-add pattern re-added `ActivityLogEntryDimension` rows for dimension IDs that were already in the snapshot, creating a risk of EF identity-map PK conflicts on those rows.

### Resolution

Two changes to `ActivityLogService.UpdateAsync`:

1. **Re-fetch after `SaveChangesAsync()`** — matches the pattern already used by `CreateAsync`. The re-fetch runs `GetByIdAsync` with the full `ThenInclude(led => led.Dimension)` chain, ensuring all navigation properties are populated before `Map()` is called.

2. **Diff-based dimension update** — instead of `Clear()` + re-add-all, the update now only removes dimensions being dropped and only adds dimensions that are truly new. Dimensions staying in the snapshot are left untouched in EF's change tracker, avoiding any identity-map conflict on shared PK values.

```csharp
// Before (Clear + re-add everything — caused NullRef on Dimension nav for newly added IDs)
log.LogEntryDimensions.Clear();
foreach (var dimId in dto.DimensionIds)
    log.LogEntryDimensions.Add(new ActivityLogEntryDimension { DimensionId = dimId });
await logRepo.SaveChangesAsync();
return Map(log);  // ← NullReferenceException if any new DimensionId wasn't loaded into DbContext

// After (diff update + re-fetch)
var newIds      = dto.DimensionIds.ToHashSet();
var existingIds = log.LogEntryDimensions.Select(led => led.DimensionId).ToHashSet();
foreach (var led in log.LogEntryDimensions.Where(led => !newIds.Contains(led.DimensionId)).ToList())
    log.LogEntryDimensions.Remove(led);
foreach (var dimId in newIds.Where(id => !existingIds.Contains(id)))
    log.LogEntryDimensions.Add(new ActivityLogEntryDimension { DimensionId = dimId });
await logRepo.SaveChangesAsync();
var updated = await logRepo.GetByIdAsync(log.Id, userId);  // ← re-fetch with full includes
return Map(updated!);
```

Same diff-based pattern applied to the activity-change fallback path.

---

## KI-015 — Trends daily chart buckets use UTC date instead of local date

| Field | Value |
|---|---|
| **ID** | KI-015 |
| **Status** | OPEN · DEFERRED |
| **Area** | `Momentum.Application/Services/ScoreService.cs` — `GetWeeklyComparisonAsync`, `GetTotalsAsync` |
| **Severity** | Low–Medium — daily chart bucket labeling/grouping can be off by one day for entries logged near local midnight; primary View Log and Score Summary symptoms from KI-013 are not affected |
| **Discovered** | During KI-013 resolution (2026-05-31) |

### Context

KI-013 fixed the primary UTC/local boundary bug across View Log, Score Summary, Home Today totals, and Balance period starts. Two server-side methods were deferred from that fix:

- `ScoreService.GetWeeklyComparisonAsync` — groups entries by day for the weekly comparison bar chart
- `ScoreService.GetTotalsAsync` — groups entries into daily, weekly, or monthly buckets for the Trends page charts

### Symptom

Both methods use `l.LoggedAt.Date` for grouping, where `l.LoggedAt` is stored as UTC:

```csharp
// GetWeeklyComparisonAsync
.Where(l => l.LoggedAt.Date == thisWeekStart.AddDays(i))

// GetTotalsAsync — daily bucket grouping
.GroupBy(l => l.LoggedAt.Date)
```

Because `LoggedAt` is UTC, `.Date` returns the UTC calendar date. For users in negative UTC offsets (Eastern, Central, Mountain, Pacific), entries logged in the late evening local time have a UTC timestamp that crosses into the following UTC date:

- Local: 2026-05-31 23:00 EDT
- Stored UTC: 2026-06-01 03:00Z
- `l.LoggedAt.Date` = **June 1** (UTC)
- Correct local date: **May 31** (EDT)

Such entries appear in the wrong day bucket in Trends daily charts and the Home weekly comparison chart.

### Impact

- **Trends page daily chart** — an entry logged at 11 PM local appears in the next day's bar
- **Home weekly comparison chart** — same late-evening entry may appear in the adjacent day's comparison column
- **Weekly Trends** and **Monthly Trends** — not significantly affected; the offset only matters at day boundaries, which have less weight at week/month granularity
- **View Log, Score Summary, Balance** — not affected; those were fixed by KI-013

### Proposed Fix

Local-day bucketing on the server requires knowing the user's UTC offset (or full timezone). Options:

1. **Client supplies offset as a query parameter** — simplest: client sends `?localOffsetMinutes=-240` (e.g. EDT); server applies the offset when computing `.Date` on each `LoggedAt`:
   ```csharp
   var localDate = l.LoggedAt.AddMinutes(offsetMinutes).Date;
   .GroupBy(l => l.LoggedAt.AddMinutes(offsetMinutes).Date)
   ```
2. **Client supplies explicit daily bucket boundaries** — client pre-computes the UTC start/end for each local day and passes them; server groups by matching bucket. More complex but handles DST correctly.
3. **Client-side grouping** — fetch raw log entries and group locally in the browser. Feasible for small date ranges; not practical for Trends which spans months.

Option 1 is the most pragmatic starting point. DST transitions would cause up to one hour of bucket misalignment on two dates per year; this is an acceptable limitation unless a full IANA timezone approach is added later.

### Acceptance Criteria

- Entries are bucketed under the correct local calendar date in Trends daily charts and the weekly comparison chart.
- An entry at 23:00 local (next UTC day) appears in the same day's bar as an entry at 08:00 local.
- Daily chart bucket edges use inclusive start / exclusive end UTC boundaries derived from local midnight.
- Existing KI-013 fixes (View Log, Score Summary, Balance) remain intact.
- Regression test: entry at 23:00 local time is grouped into the correct local day bucket, not the following UTC day bucket.

---

*Momentum — Known Issues Log*  
*Last updated: 2026-05-31 (KI-013 resolved; KI-015 opened)*
