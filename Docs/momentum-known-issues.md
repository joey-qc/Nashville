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

## KI-009 — MudBlazor ISnackbar dependency not yet replaced

| Field | Value |
|---|---|
| **ID** | KI-009 |
| **Status** | DEFERRED |
| **Area** | Multiple pages, `_Imports.razor`, `Program.cs`, MudBlazor NuGet |
| **Severity** | Low — functional, but blocks full MudBlazor removal |
| **Target** | After custom global toast component is implemented |

### Description
All pages and components have been converted from MudBlazor to custom HTML/CSS (see CLAUDE.md §3.1). The only remaining MudBlazor dependency is `ISnackbar` / `MudSnackbar`, used for toast-style success and error notifications throughout the application.

Until a custom global toast component is implemented, `ISnackbar` remains the approved notification mechanism. This is intentional and documented as "BY DESIGN (temporary)" in CLAUDE.md.

### Resolution Path
1. Implement a lightweight custom toast component (e.g., `<ToastHost>` in `MainLayout.razor` with a `ToastService` singleton).
2. Replace all `ISnackbar.Add(...)` calls with `ToastService.Show(...)` equivalents.
3. Remove `builder.Services.AddMudServices()` from `Program.cs`.
4. Remove the `Blazor-MudBlazor` NuGet package.
5. Update CLAUDE.md §3.1 to reflect full removal.

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

## KI-011 — Custom global toast: prerequisite for full MudBlazor removal

| Field | Value |
|---|---|
| **ID** | KI-011 |
| **Status** | DEFERRED |
| **Area** | New component required: `Momentum.Client/Components/ToastHost.razor` |
| **Severity** | Low — enhancement / cleanup |
| **Blocked by** | KI-009 (same resolution path) |

### Description
A custom `ToastHost` / `ToastService` needs to be built before MudBlazor can be fully removed. This is tracked as a distinct issue from KI-009 because it requires design and implementation work, not just a replacement call.

### Design Notes
- `ToastService` should be a `Singleton` or `Scoped` service with `Show(message, type)` API.
- Toast types: `Success`, `Error`, `Warning`, `Info` — styled with Momentum color tokens.
- `ToastHost` renders as an overlay in `MainLayout.razor`, outside the main content area.
- Auto-dismiss after ~4 seconds; swipe-to-dismiss on mobile.
- Stacks multiple toasts if called in quick succession.

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

*Momentum — Known Issues Log*  
*Last updated: 2026-05-27*
