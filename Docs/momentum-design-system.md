# Momentum Design System

This document is the authoritative reference for UI implementation in the Momentum application. It covers design philosophy, visual tokens, component patterns, and rendering conventions. All new code and AI-generated code must follow this document.

---

## 1. Design Philosophy

Momentum is a **behavioral momentum system**, not a productivity dashboard. The UI should feel:

- **Calm and immersive** — dark, spacious, no visual clutter
- **Focused** — one clear action per surface; minimal competing elements
- **Reinforcing** — positive feedback without being garish
- **Forward-moving** — visual hierarchy always points toward the next action
- **Emotionally coherent** — color, weight, and spacing convey meaning, not just style

### Visual Language Principles

- Use dark blue layers (navy → surface → surface-2) to create depth without shadows
- Reserve the primary green (`--primary`) for interactive CTAs and positive scores only
- Use red (`--negative`) only for errors, destructive actions, and negative points
- Uppercase small-caps labels (kickers, field labels) create hierarchy without size
- Avoid gradients except for the primary CTA glow and brand logo
- Never hardcode colors — always use design tokens from `momentum-theme.css`

---

## 2. Color Tokens

All tokens are defined in `Momentum.Client/wwwroot/css/momentum-theme.css`.

### Background Scale

| Token | Value | Usage |
|---|---|---|
| `--bg` | `#050D1B` | Page background (body) |
| `--bg-2` | `#081427` | Topbar background |
| `--surface` | `#0E2240` | Card and modal backgrounds |
| `--surface-2` | `#143055` | Input fields, quick-pick tiles, activity rows |
| `--surface-3` | `#1A3B66` | Hover state for surface-2 elements |

### Border Scale

| Token | Value | Usage |
|---|---|---|
| `--border` | `#1F3D66` | Input borders, dropdown borders |
| `--border-soft` | `#15294A` | Card borders, dividers, grid lines |

### Brand / Interaction

| Token | Value | Usage |
|---|---|---|
| `--primary` | `#76E04A` | Primary CTA, active nav item, positive score text |
| `--primary-dim` | `#4FB02C` | Primary button hover state |
| `--primary-glow` | `#76E04A30` | Button shadow, focus ring, selected chip background |
| `--accent` | `#4FB8FF` | Month sparkline, secondary accent |
| `--negative` | `#FF6B6B` | Error states, negative points, destructive actions |
| `--positive` | `#76E04A` | Alias for `--primary` in score contexts |

### Text Scale

| Token | Value | Usage |
|---|---|---|
| `--text` | `#F0F4F8` | Primary body text, headings, input values |
| `--text-dim` | `#8BA0B8` | Secondary text, subtitles, timestamps |
| `--text-muted` | `#4A6080` | Labels, placeholders, disabled text, minor metadata |
| `--chart-label` | `#A8BED0` | SVG chart axis labels only (see §9 for why a separate token) |

### Category Colors

Never hardcode category colors in components. Always read `ColorHex` from `CategoryDto`.

| Dimension (display name) | Token | Value |
|---|---|---|
| Body (was Physical) | `--cat-physical` | `#76E04A` |
| Mind (was Mental) | `--cat-mental` | `#5BC8FF` |
| Spirit (was Spiritual) | `--cat-spiritual` | `#B894FF` |
| Connections (was Social) | `--cat-social` | `#F7B500` |
| Responsibilities (was Housekeeping) | `--cat-housekeeping` | `#FF9472` |
| All (filter default) | — | `#9E9E9E` |

CSS token names (`--cat-physical` etc.) are **not renamed** — they are internal style identifiers and do not need to track display name changes.

### Border Radius

| Token | Value | Usage |
|---|---|---|
| `--radius-sm` | `8px` | Inputs, buttons, chips, pills, small cards |
| `--radius` | `12px` | Standard cards |
| `--radius-lg` | `16px` | Large cards, auth card |

---

## 3. Typography

### Font Families

| Family | Usage |
|---|---|
| `Space Grotesk` (700) | All headings (h1–h6), large numeric scores, KPI values, brand name, modal titles |
| `DM Sans` (400/500/600/700) | All body text, labels, buttons, inputs, metadata |

Both fonts are loaded from Google Fonts in `index.html`.

### Type Scale

| Role | Size | Weight | Font | Color |
|---|---|---|---|---|
| Page title (h1) | 1.6–2.2rem | 700 | Space Grotesk | `--text` |
| Card title (h2) | 1.0–1.35rem | 700 | Space Grotesk | `--text` |
| Section title | 0.95rem | 600 | Space Grotesk | `--text` |
| KPI value | 1.8rem | 700 | Space Grotesk | `--text` |
| Body text | 0.875rem | 400–500 | DM Sans | `--text` / `--text-dim` |
| Field label (kicker) | 0.62–0.65rem | 700 | DM Sans | `--text-muted` |
| Metadata / timestamp | 0.72–0.78rem | 400–500 | DM Sans | `--text-muted` |
| Button text | 0.72–1.0rem | 700 | DM Sans | varies |
| Chart axis label | 9px | — | SVG text | `--chart-label` via inline style |

### Kicker Pattern

Used above page titles and card titles to provide eyebrow context:

```css
.kicker {
    display: block;
    font-size: 0.62–0.65rem;
    font-weight: 700;
    letter-spacing: 0.12–0.15em;
    color: var(--primary);  /* or --text-muted for less emphasis */
    text-transform: uppercase;
    margin-bottom: 6–8px;
}
```

Examples: `ACCOUNT`, `WELCOME BACK`, `START A STREAK`, `EDITING`, `LIBRARY`

---

## 4. Spacing Rhythm

Momentum uses a relaxed 4px base grid. Prefer multiples of 4 for all spacing.

### Common Spacing Values

| Context | Value |
|---|---|
| Between page sections / cards | 20–28px (gap) |
| Card padding (standard) | 20px |
| Card padding (large/auth) | 28–32px |
| Field group bottom margin | 20–22px |
| Label bottom margin | 6–8px |
| Button padding (primary CTA) | 11–14px vertical, 22–26px horizontal |
| Button padding (text action) | 4px vertical, 0 horizontal |
| Chip/pill padding | 2–4px vertical, 7–10px horizontal |

### Page Content Max-Widths

| Context | Max-Width |
|---|---|
| Home dashboard | 1200px |
| Log Activity | 680px |
| Manage Activities | unconstrained (full card list) |
| Settings | 640px |
| Auth pages (Login/Register) | 400px |
| Manage Activities modal | 580px |

---

## 5. Card Patterns

### Standard Card

```css
background: var(--surface);
border: 1px solid var(--border-soft);
border-radius: var(--radius);   /* 12px */
padding: 20px;
```

### Large Card (Settings, auth)

```css
border-radius: var(--radius-lg);  /* 16px or 20px */
padding: 28–32px;
```

### Auth Card

```css
border-radius: 20px;
padding: 32px 28px 24px;
box-shadow: 0 24px 80px rgba(0, 0, 0, 0.45);
```

### Card Header Row (`.card-head`)

```css
display: flex;
align-items: center;
justify-content: space-between;
margin-bottom: 16px;
flex-wrap: wrap;
gap: 8px;
```

Contains: `.card-label` (uppercase kicker, `--text-muted`) or `.card-title` (Space Grotesk, `--text`), plus an optional right-aligned action or legend.

### Card Divider

```html
<hr class="card-divider" />
```
```css
border: none;
border-top: 1px solid var(--border-soft);
margin: 0;
```

Used in Settings between card-top, card-body, and card-footer sections.

---

## 6. Button Hierarchy

Momentum uses three tiers of button emphasis.

### Tier 1 — Primary CTA

Green filled button. Used for the single dominant action on a form or page.

```css
padding: 11–14px 22–26px;
background: var(--primary);
color: #071a07;               /* dark green text — NOT white */
border: none;
border-radius: var(--radius-sm);
font-family: 'DM Sans', sans-serif;
font-size: 0.9–1.0rem;
font-weight: 700;
box-shadow: 0 0 16–22px rgba(118, 224, 74, 0.18–0.22),
            0 2–4px 6–12px rgba(0, 0, 0, 0.28–0.3);
cursor: pointer;
-webkit-appearance: none;
appearance: none;
```

Hover: `background: var(--primary-dim)` + increased glow shadow.  
Disabled: `opacity: 0.6; cursor: not-allowed; box-shadow: none`.

Primary CTAs always use the arrow icon (`→`) for navigation actions:
```html
<span>Sign in</span>
<svg viewBox="0 0 24 24" width="18" height="18" …><path d="M5 12h14M12 5l7 7-7 7"/></svg>
```

### Tier 2 — Secondary / Outline

Not currently used as a standalone pattern. Prefer primary or text actions.

### Tier 3 — Text Action (Cancel / Non-destructive)

Bare text button, no border or background.

```css
background: transparent;
border: none;
color: var(--text-muted);
font-size: 0.72rem;
font-weight: 700;
letter-spacing: 0.1em;
text-transform: uppercase;
padding: 4px 0;
cursor: pointer;
```

Example label: `CANCEL`

### Tier 4 — Destructive Text Action

Red text, same bare style as Tier 3.

```css
background: transparent;
border: none;
color: var(--negative);
font-size: 0.72rem;
font-weight: 700;
letter-spacing: 0.1em;
opacity: 0.6;       /* resting — draws less attention */
padding: 4px 0;
cursor: pointer;
```

Hover: `opacity: 1`.  
Example label: `DELETE ACTIVITY` (with small trash icon inline).

### Social / Ghost Buttons (auth pages only)

```css
background: var(--surface-2);
border: 1px solid var(--border);
border-radius: var(--radius-sm);
color: var(--text-dim);
font-size: 0.88rem;
font-weight: 500;
padding: 11px 16px;
```

---

## 7. Form Styling

### Standard Field Pattern

Used on all inner-app forms (Log Activity, Manage Activities, Settings):

```css
/* Label */
.field-label {
    display: block;
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    color: var(--text-muted);
    text-transform: uppercase;
    margin-bottom: 6–8px;
}

/* Input */
.field-input {
    width: 100%;
    padding: 10–12px;
    background: var(--surface-2);
    border: 1px solid var(--border);
    border-radius: var(--radius-sm);
    color: var(--text);
    font-family: 'DM Sans', sans-serif;
    font-size: 0.875–0.9rem;
    box-sizing: border-box;
    -webkit-appearance: none;
    appearance: none;
    transition: border-color 0.15s ease;
}

.field-input:focus {
    outline: none;
    border-color: var(--primary);
    box-shadow: 0 0 0 3px var(--primary-glow);  /* Settings uses this; Log does not */
}
```

Placeholder: `color: var(--text-muted)`.

### Auth Field Pattern (Login / Register)

Stacked label-inside-container: the label and input share one container div (`.auth-field`), so the label appears inside the input area rather than above it.

```html
<div class="auth-field">
    <label class="auth-field-label" for="field-id">EMAIL</label>
    <input id="field-id" type="email" class="auth-field-input" … />
</div>
```

```css
.auth-field {
    background: var(--surface-2);
    border: 1px solid var(--border);
    border-radius: var(--radius-sm);
    padding: 11px 14px 13px;
    margin-bottom: 10px;
    cursor: text;
    transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.auth-field:focus-within {
    border-color: var(--primary);
    box-shadow: 0 0 0 3px var(--primary-glow);
}

.auth-field-label {
    display: block;
    font-size: 0.6rem;
    font-weight: 700;
    letter-spacing: 0.12em;
    color: var(--text-muted);
    margin-bottom: 5px;
    pointer-events: none;
}

.auth-field-input {
    flex: 1;
    width: 100%;
    background: transparent;
    border: none;
    color: var(--text);
    font-size: 0.95rem;
    font-weight: 500;
    outline: none;
    padding: 0;
}
```

### Password Field with Eye Toggle

Wrap input + toggle in `.auth-field-inner` (flex row). The toggle button (`.pw-toggle`) is transparent with `--text-muted` icon color.

### Textarea

```css
background: var(--surface-2);
border: 1px solid var(--border);
border-radius: var(--radius-sm);
resize: vertical;
min-height: 80px;
```

### Points Spinner

```html
<div class="pts-spinner">
    <button class="pts-btn">−</button>
    <div class="pts-value-wrap pos|neg|''">
        <span class="pts-num">+5</span>
        <span class="pts-sub">PER ENTRY</span>
    </div>
    <button class="pts-btn">+</button>
</div>
```

- Positive value: `.pos` → `color: var(--positive)`
- Negative value: `.neg` → `color: var(--negative)`
- Zero: `--text-muted`
- Sub-label "PER ENTRY" is `0.52rem`, `opacity: 0.45`

### Bounded Score Stepper (Check-In)

A constrained variant of the Points Spinner used by the Check-In form for the Body / Energy / Mood metrics. Identical visual language, but the value is **clamped to a fixed range (−5…+5)** and the +/− buttons **disable at the bounds**.

```html
<div class="score-stepper">
    <button class="score-btn" disabled>−</button>   <!-- disabled at -5 -->
    <span class="score-value pos|neg|''">+3</span>
    <button class="score-btn">+</button>
</div>
```

- Layout mirrors `.pts-spinner` (56px buttons, centered value, dividers via `--border-soft`).
- Value color: `.pos` → `var(--primary)`, `.neg` → `var(--negative)`, zero → `var(--text-muted)`.
- `.score-btn:disabled` → `opacity: 0.3; cursor: not-allowed` — the only behavioral difference from the unbounded Points Spinner.
- Clamping is enforced in code (`Math.Clamp(value, -5, 5)`); the disabled state is a visual reinforcement, not the sole guard.
- Scoped to `CheckIn.razor.css` (not global) — follows the per-page CSS-isolation convention.

### Custom Checkbox (auth / terms)

Native checkbox is visually hidden (`opacity: 0; width: 1px`). A sibling `.auth-check-box` is styled via the adjacent sibling CSS selector:

```css
.auth-check:checked + .auth-check-box {
    background: var(--primary);
    border-color: var(--primary);
    color: #071a07;  /* checkmark color */
}
```

---

## 8. Action Row Conventions

### Standard Action Row

The action row sits at the bottom of a form card or modal. Primary CTA on the left; Cancel as text action to its right.

```html
<div class="action-row">
    <button type="submit" class="btn-submit" disabled="@busy">Log activity</button>
    <button type="button" class="btn-text-cancel">CANCEL</button>
</div>
```

```css
.action-row {
    display: flex;
    align-items: center;
    gap: 16px;
    margin-top: 6px;
}
```

The primary button is **not** full-width; it takes only the width of its content plus padding.

### Modal Footer (with Destructive Action)

When a modal has both save/cancel and a potential delete action, use a `space-between` footer layout with the icon-only arm/confirm pattern (see §9):

```html
<div class="modal-footer">
    <div class="modal-footer-left">
        <button type="submit" class="btn-save">Save changes</button>
        <button type="button" class="btn-text-cancel">CANCEL</button>
    </div>
    @if (isEditing)
    {
        <div class="modal-footer-right">
            @if (_deleteArmed)
            {
                <button class="act-btn confirm" title="Confirm delete" @onclick="ConfirmDelete"><!-- ✓ --></button>
                <button class="act-btn cancel-del" title="Cancel delete" @onclick="@(() => _deleteArmed = false)"><!-- ✕ --></button>
            }
            else
            {
                <button class="act-btn delete" title="Delete activity" @onclick="@(() => _deleteArmed = true)"><!-- 🗑 --></button>
            }
        </div>
    }
</div>
```

```css
.modal-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-top: 20px;
    border-top: 1px solid var(--border-soft);
    gap: 12px;
    flex-wrap: wrap;
}

.modal-footer-left  { display: flex; align-items: center; gap: 16px; }
.modal-footer-right { display: flex; align-items: center; gap: 4px; }
```

The delete control sits at the far right, visually separated. On mobile the row stays intact — no column-stacking needed because the icon is compact enough to coexist with Save + Cancel on a single line.

---

## 9. Destructive Action Conventions

1. **Never show a destructive button unless a user-initiated action has created a context** (user has clicked Edit, selected a row, etc.)
2. Delete uses an **icon-only two-step arm/confirm pattern** — not a visible text button. This is the single consistent destructive-action UX across the application.
3. For activities with log history, a 409 response triggers a **confirmation dialog** with two options:
   - **Hide from future logging** (archive — preserves data)
   - **Delete this activity and all history** (cascade — permanent, clearly warned)
4. The cascade option should use `var(--negative)` or be visually marked as irreversible
5. Never auto-proceed after a destructive action — always require explicit user confirmation

### Icon-Only Delete Arm Pattern

Used on: View Log entry rows, Edit Activity modal footer, Edit Log Entry action row, Manage Activities activity rows.

**Normal state** — trash icon only, muted color:
```html
<button class="act-btn delete" title="Delete" @onclick="@(() => _armed = true)">
    <!-- trash SVG, width/height 15 -->
</button>
```

**Armed state** — red confirm check + gray cancel X:
```html
<button class="act-btn confirm" title="Confirm delete" @onclick="ConfirmDelete">
    <!-- checkmark SVG -->
</button>
<button class="act-btn cancel-del" title="Cancel" @onclick="@(() => _armed = false)">
    <!-- X SVG -->
</button>
```

```css
.act-btn          { width:30px; height:30px; border-radius:var(--radius-sm); border:none; background:transparent; }
.act-btn.delete   { color:var(--text-muted); }
.act-btn.delete:hover { background:rgba(255,107,107,0.12); color:var(--negative); }
.act-btn.confirm  { color:var(--negative); background:rgba(255,107,107,0.12); }
.act-btn.cancel-del { color:var(--text-muted); }
```

The `.act-btn` styles are **scoped per component** (ActivityDetail.razor.css, ManageActivities.razor.css) — not global — to avoid class-name collisions across Blazor's CSS isolation scope.

---

## 10. Modal Patterns

### Trigger

Modals in Manage Activities open when a user taps a row or the "+ New Activity" button. They are rendered in-page with a backdrop overlay.

### Modal Card

```css
.modal-card {
    background: var(--surface);
    border-radius: var(--radius-lg);
    padding: 28px;
    max-width: 580px;
    width: 100%;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.5);
}
```

### Modal Header

```html
<div class="modal-header">
    <div class="modal-title-group">
        <span class="modal-kicker">EDITING</span>  <!-- or LIBRARY for new -->
        <h2 class="modal-title">Activity Name</h2>
    </div>
    <button class="modal-close-btn" type="button" aria-label="Close">✕</button>
</div>
```

```css
.modal-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    margin-bottom: 24px;
    gap: 12px;
}

.modal-kicker {
    font-size: 0.6rem;
    font-weight: 700;
    letter-spacing: 0.15em;
    color: var(--primary);
}

.modal-title {
    font-family: 'Space Grotesk', sans-serif;
    font-size: 1.15rem;
    font-weight: 700;
    color: var(--text);
    margin: 0;
}
```

### Side-by-Side Form Layout (within modal)

When Default Points and Categories should appear on the same row:

```css
.form-split-row {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 20px;
    margin-bottom: 20px;
    align-items: start;
}
```

On mobile (≤660px): collapses to `grid-template-columns: 1fr`.

---

## 11. Dimension Display Names & Responsive Labels

### Display Name Mapping

Since migration `DIM001_RenameDimensions`, persisted database names match user-facing display names exactly. No client-side name translation is needed.

| ID | Persisted Name (DB) | Display Name | Mobile Label (≤540px) |
|---|---|---|---|
| 1 | Body | Body | Body |
| 2 | Mind | Mind | Mind |
| 3 | Spirit | Spirit | Spirit |
| 4 | Connections | Connections | Con |
| 5 | Responsibilities | Responsibilities | Rsp |

`DimensionDisplayHelper` in `Momentum.Client/Services/` is simplified post-DIM-001:
- `GetDisplayName(dim)` returns `dim.Name` directly — no lookup needed.
- `GetMobileLabel(dim)` applies abbreviations only to IDs 4 and 5 (the two long names); all others return `dim.Name`.

### Responsive Label Markup Pattern

All dimension chips and labels use this two-span pattern so CSS can swap full vs. abbreviated text without JavaScript:

```html
<span class="dim-full">Connections</span>
<span class="dim-abbr" aria-hidden="true">Con</span>
```

Global CSS in `momentum-theme.css`:
- Desktop (>540px): `.dim-full` visible, `.dim-abbr` hidden (`display:none`)
- Mobile (≤540px): `.dim-full` hidden, `.dim-abbr` visible

Accessibility: the parent element (`<button>`, `<span>`, etc.) always carries `title=` and/or `aria-label=` with the full display name. `.dim-abbr` uses `aria-hidden="true"` so screen readers always receive the complete name.

---

## 12. Category Chip Styling

### Filter Chips (View Log, Trends)

All dimension filter chip rows (`.cat-chips`, `.cat-filter`) use the color-dot style — an 8px colored dot before the label:

```html
<!-- "All" chip -->
<button class="cat-chip @(active ? "active all" : "")">
    <span class="chip-dot" style="background:#9E9E9E"></span>All
</button>

<!-- Dimension chip -->
<button class="cat-chip @(active ? "active" : "")"
        style="@(active ? $"background:{cat.ColorHex}26;border-color:{cat.ColorHex};color:{cat.ColorHex}" : "")"
        title="@DimensionDisplayHelper.GetDisplayName(cat)">
    <span class="chip-dot" style="background:@cat.ColorHex"></span>
    <span class="dim-full">@DimensionDisplayHelper.GetDisplayName(cat)</span>
    <span class="dim-abbr" aria-hidden="true">@DimensionDisplayHelper.GetMobileLabel(cat)</span>
</button>
```

`.cat-chip` must be `display: inline-flex; align-items: center; gap: 6px` to align the dot and label. `.chip-dot` is an 8px circle (`border-radius: 50%`). Active state uses inline styles (colored background + border) rather than a CSS class, because the color is dynamic per dimension.

Used in Manage Activities modal for multi-select category toggles.

### Selected State (solid fill)

```html
<button class="cat-toggle-btn cat-selected"
        style="border-color:{cat.ColorHex};background:{cat.ColorHex};color:rgba(0,0,0,0.78)">
    <span class="cat-toggle-dot" style="background:rgba(0,0,0,0.22)"></span>
    @cat.Name
</button>
```

### Unselected State (colored border)

```html
<button class="cat-toggle-btn"
        style="border-color:{cat.ColorHex}55;color:{cat.ColorHex}CC">
    <span class="cat-toggle-dot" style="background:{cat.ColorHex}"></span>
    @cat.Name
</button>
```

```css
.cat-toggle-btn {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 6px 12px;
    border-radius: 20px;
    border: 1.5px solid;
    font-family: 'DM Sans', sans-serif;
    font-size: 0.78rem;
    font-weight: 600;
    cursor: pointer;
    background: transparent;
    transition: all 0.15s ease;
}
```

The color is always **driven by inline styles from `cat.ColorHex`**, not hardcoded CSS classes.

### Display Chips (read-only, on log entries / activity rows)

```html
<span class="cat-tag" style="background:{cat.ColorHex}1A;color:{cat.ColorHex}">
    @cat.Name
</span>
```

```css
.cat-tag {
    font-size: 0.65rem;
    font-weight: 600;
    padding: 2px 7px;
    border-radius: 10px;
    white-space: nowrap;
}
```

The `1A` suffix (hex opacity 10%) on the background provides the subtle tinted fill.

---

## 12. SVG and Chart Rendering Conventions

### Critical Blazor Constraint

**`<text>` is a reserved Razor pseudo-element.** Placing `<text x="..." y="...">` in a Razor component causes build error RZ1023. SVG text nodes must be rendered via `@((MarkupString)...)`:

```razor
@((MarkupString)$"<text x=\"{x}\" y=\"{y}\" style=\"font-size:9px;fill:var(--chart-label)\">{value}</text>")
```

### CSS Isolation and MarkupString

Blazor's CSS isolation adds a scoped attribute (e.g., `b-a1b2c3d4`) to elements rendered by the component. `MarkupString` bypasses the Blazor renderer — injected elements never receive the scoped attribute, so **scoped CSS class rules do not match them**.

**Always apply `fill`, `font-size`, and other SVG text styles via inline `style` attribute in the MarkupString string, not via CSS class selectors.**

```razor
<!-- CORRECT -->
@((MarkupString)$"<text ... style=\"fill:var(--chart-label);font-size:9px\">{tv}</text>")

<!-- WRONG — CSS class will not apply -->
@((MarkupString)$"<text class=\"chart-y-label\" ...>{tv}</text>")
```

### Bar Chart Geometry

Standard bar chart SVG geometry constants:

```csharp
const double ChL = 40;   // left x of chart area
const double ChT = 15;   // top y of bars
const double ChB = 170;  // bottom y of bars
const double ChH = 155;  // usable chart height (ChB - ChT)
const double ChW = 450;  // chart width (490 - ChL)
const double BarW = 14;  // individual bar pixel width
```

ViewBox: `"0 0 500 200"`. Y-axis labels at `x=34`, x-axis day labels at `y=193`.

### Bar Colors

- Current week bars: `fill="var(--primary)"` (green)
- Last week / comparison bars: `fill="var(--text-muted)"` (gray)
- Negative-value bars: `fill="var(--negative)"` (red) — use `.setback-fill`

### Gridlines

```razor
<line x1="@ChL" y1="@y" x2="490" y2="@y"
      stroke="var(--border-soft)" stroke-width="1"/>
```

### Ring Chart (Today's Momentum)

Full-circle ring using `stroke-dasharray` (conceptually). Implemented as a plain `<circle>` with `stroke`:

- Positive or zero day: `stroke="url(#ringGrad)"` (green gradient), `stroke-opacity="1"`
- Negative day: `stroke="var(--negative)"`, `stroke-opacity="0.4"`

Score text color:
- Positive: `fill: var(--text)` (via `.ring-score-text`)
- Negative: `fill: var(--negative)` (via `.ring-score-neg`)

### Sparklines

```css
.spark-line {
    fill: none;
    stroke-width: 1.5;
    stroke-linecap: round;
    stroke-linejoin: round;
}

.week-spark  { stroke: var(--primary); }
.month-spark { stroke: var(--accent); }
```

### Category Bar Fill

Progress bars in the momentum card read color from `CategoryTotalDto.ColorHex`. Never hardcode category colors in SVG.

---

## 13. Responsive Behavior

### Breakpoints

| Breakpoint | Applies To |
|---|---|
| ≤900px | Report bottom-row collapses from two columns to single column (Balance page) |
| ≤768px | Home dashboard grid collapses; sidebar switches to overlay drawer; report bottom-row collapses (Trend page) |
| ≤660px | Manage Activities modal footer stacks; form split row collapses |
| ≤540px | Log Activity side-by-side fields collapse; quick-pick goes single column |
| ≤480px | Home KPI cards stack vertically; momentum body stacks ring above bars; auth cards get reduced padding; Best & worst day date suffix rendered at slightly smaller font (never hidden) |

### Home Dashboard

- **Desktop**: Two-column grids (`hero-row`: `1fr 300px`; `split-row`: `1fr 380px`)
- **≤768px**: Both collapse to single column; KPI cards become `1fr 1fr` grid side-by-side
- **≤480px**: KPI cards collapse to single column; momentum body stacks vertically

### Auth Pages

Auth pages render inside `.auth-shell` (full-height flex column, centered), which is provided by `MainLayout.razor` when the user is unauthenticated. The auth page itself (`.auth-page`) is `max-width: 400px`, centering brand + card + footer.

### Modals

Modals are in-page overlays with `max-width: 580px`. On mobile (≤660px), the modal footer stacks vertically (`flex-direction: column; align-items: stretch`).

### Top Bar (`.topbar`)

The authenticated top bar is a sticky flex row: `[hamburger] [title] [actions]`.

**Persistent action buttons.** Top-level creation flows live as persistent buttons in the top bar (right-aligned), **not** as left-nav items. They sit side by side inside a `.topbar-actions` flex container (`gap: 8px; flex-shrink: 0`) and share one style class, `.topbar-cta` (green `--primary` fill, `#071a07` text, `--radius-sm`, `white-space: nowrap`):

```html
<div class="topbar-actions">
    <a href="/log" class="topbar-cta">+ Add Entry</a>
    <a href="/check-in" class="topbar-cta">Check In</a>
</div>
```

- Both buttons appear on every authenticated page (they persist across navigation).
- Add new persistent creation actions here rather than in the left nav. The left nav is for **destinations** (Home, View Log, Check Ins, Reports, Manage, Settings), not creation.

**Responsive page title.** The title renders two spans — `.title-full` (default) and `.title-short` (hidden by default) — toggled at the **≤767px** breakpoint, mirroring the dimension-label pattern (§11). This lets a long title shorten on mobile (e.g. "Manage Activities" → "Manage") so the action buttons never wrap. The title also carries `min-width: 0` + `overflow: hidden; text-overflow: ellipsis` so it truncates rather than pushing the buttons off-row. On mobile the top bar tightens (`gap`, `.topbar-cta` padding/font) to fit both buttons.

```razor
<span class="topbar-title">
    <span class="title-full">@PageTitle</span>
    <span class="title-short">@PageTitleShort</span>
</span>
```

`PageTitleShort` equals `PageTitle` except where a shorter mobile form is needed (currently only `/activities` → "Manage").

### Sidebar

- Desktop: Fixed 280px sidebar; collapses to 64px icon-only mode via the `collapsed` CSS class
- Mobile (≤767px): Sidebar slides off-screen; `.hamburger` button shows it as an overlay drawer with `.mobile-overlay` backdrop

#### Sidebar State Model

The hamburger button toggles **two boolean flags** simultaneously via `ToggleSidebar()`:

| Flag | Controls |
|---|---|
| `_collapsed` | Desktop: sidebar shrinks to 64px icon rail |
| `_mobileOpen` | Mobile: sidebar slides in as an overlay drawer |

These are toggled together because the same button serves both viewports — CSS media queries decide which behavior is actually visible at the current width.

**Critical rule — Razor conditional rendering in the nav:**  
Never use `@if (_collapsed)` alone to decide which nav item template to render. The `_collapsed` flag is `true` whenever the mobile drawer is open (because both flags toggle together). Using `_collapsed` alone as the template guard causes the collapsed (icon-only) template to render even when the mobile drawer is showing the full expanded nav.

**Correct guard for icon-rail templates:**
```razor
@if (_collapsed && !_mobileOpen)
{
    <!-- Desktop icon-rail only: icon, no label, no children -->
}
else
{
    <!-- Full nav (desktop expanded OR mobile open drawer): icon + label + children -->
}
```

This ensures that when the mobile drawer is open (`_mobileOpen=true`), the full labeled template always renders regardless of the `_collapsed` state.

#### CSS Restoration vs. DOM Existence

CSS `!important` overrides in the mobile media query can **restore visibility** of hidden elements, but they **cannot create elements that were never rendered**. If Razor chooses the icon-only branch, `<span class="nav-label">` does not exist in the DOM — no CSS can make it appear. Always ensure the correct Razor template branch renders first; then use CSS to fine-tune appearance.

#### Reports Group Submenu

The Reports nav entry uses a collapsible pattern (toggle button + child links):
- The toggle button carries `aria-expanded="true/false"` and `aria-controls="reports-submenu"`.
- Child links (`Trends`, `Balance`) are wrapped in `<div id="reports-submenu" style="display:contents">` for accessible association without layout disruption.
- `_reportsExpanded` defaults to `true` and is initialized to `true` when the page path starts with `/reports`.
- On mobile: Reports is expanded (full group) by default because the mobile drawer always uses the full-template branch.

---

## 14. Report Page Layouts

Each report sub-page uses a consistent two-zone bottom-row layout: a wide `1fr` left card and a fixed-width right card, separated by a 20px gap.

### Trends Page (`/reports`)

```
┌─────────────────────────────────────────────────────┐
│ Header: title · period label · improvement pill      │
│ Stats line: total · avg · best period                │
├─────────────────────────────────────────────────────┤
│ Controls: Period dropdown (Daily / Weekly / Monthly) │
│           Category filter chips                      │
├─────────────────────────────────────────────────────┤
│ Bar chart card (full width, stacked by category)     │
├──────────────────────────────┬──────────────────────┤
│ Category trend               │ Top days / periods   │
│ (sparklines, last 8 weeks)   │ (300px fixed)        │
│ 1fr                          │                      │
└──────────────────────────────┴──────────────────────┘
```

**Bottom-row grid:** `grid-template-columns: 1fr 300px`  
**Top periods date format:** `MMM d, yyyy` (daily only — e.g., "May 15, 2026"); weekly/monthly use short labels.  
**Top periods grid:** `grid-template-columns: 24px auto 1fr 40px` — label column auto-sizes to date text width; bar takes remaining `1fr`.

### Balance Page (`/reports/balance`)

```
┌─────────────────────────────────────────────────────┐
│ Header: kicker · title · subtitle · period selector  │
├─────────────────────────────────────────────────────┤
│ Main card: donut chart + category list               │
│ (full width, period-based distribution)              │
├─────────────────────────────────────────────────────┤
│ Insight callout (dominant category coaching)         │
├─────────────────────────────────────────────────────┤
│ Best & worst days (full width)                       │
│ top 2 best days + 1 worst day for the period         │
└─────────────────────────────────────────────────────┘
```

**Layout:** All cards are full-width stacked — no side-by-side bottom row on this page.  
**Best & worst day format:** `DayName · MM/dd/yyyy` — date is an inline `<span class="bwd-date-inline">` with lighter weight and muted color. The date is **always visible** at all breakpoints. At ≤480px, font-size shrinks to `0.72rem` but the element is never `display: none`.

### Home Dashboard (`/`)

```
┌─────────────────────────────────────────────────────┐
│ Header: date · welcome · subtitle                    │
├──────────────────────────────────┬──────────────────┤
│ Today's Momentum card            │ KPI stack        │
│ (ring + category bars)           │ (This Week       │
│ 1fr                              │  This Month)     │
│                                  │ 300px            │
├──────────────────────────────┬───┴──────────────────┤
│ Bar chart                    │ Today's activity     │
│ (This wk vs last wk)         │ (list, up to 5)      │
│ 1fr                          │ 380px                │
├─────────────────────────────────────────────────────┤
│ Weekly category breakdown (full width)               │
│ stacked bar + category rows · This Week only         │
└─────────────────────────────────────────────────────┘
```

**Grid rows:** `hero-row`: `1fr 300px`; `split-row`: `1fr 380px`; breakdown card is full-width in page flow.  
**Weekly breakdown CSS prefix:** `wkb-` (`.wkb-card`, `.wkb-stack`, `.wkb-seg`, `.wkb-row`, etc.)  
**Breakdown data:** `ReportsService.GetBalanceAsync("week")` — fixed to current week, positive categories only, canonical order.  
**Responsive:** At ≤768px the bar track column hides (`grid-template-columns: 10px 1fr 44px 110px`); at ≤480px the bar track is `display: none` and columns collapse to `10px 1fr 44px`.

### Section Placement Rationale

| Section | Page | Rationale |
|---|---|---|
| Category trend (sparklines) | Trends | Time-series data belongs with time-series analysis |
| Top days / Top periods | Trends | Highest-scoring periods are a trend-analysis concept |
| Category breakdown (stacked bar) | Home (§5.4) | Weekly snapshot belongs on the dashboard for at-a-glance use |
| Best & worst days | Balance | Day-level scoring within a period is a balance insight |
| Donut + category list | Balance | Proportional distribution across the selected period |

### Date Format Conventions for Report Pages

| Context | Format | Example |
|---|---|---|
| Top days (Trend page) | `MMM d, yyyy` | May 15, 2026 |
| Top weeks (Trend page) | `W{n} yyyy` | W21 2026 |
| Top months (Trend page) | `MMM yyyy` | May 2026 |
| Best/worst day name (Balance) | `dddd` | Tuesday |
| Best/worst day date (Balance) | `MM/dd/yyyy` | 05/26/2026 |
| Combined best/worst display | `{DayName} · {Date}` | Tuesday · 05/26/2026 |

---

## 15. Auth Page Patterns

Auth pages (Login, Register) share styles from `wwwroot/css/auth-pages.css` — a **global** (non-scoped) stylesheet loaded in `index.html`.

### Structure

```
.auth-page
├── .auth-brand (logo wrap + brand name + DAILY WELLNESS)
├── .auth-card
│   ├── .auth-card-header (eyebrow + title + subtitle)
│   ├── .auth-alert (error or info banner — conditional)
│   ├── <form>
│   │   ├── .auth-field (stacked label + input)
│   │   ├── .auth-meta-row (checkbox + forgot password link)
│   │   └── .auth-submit (full-width green CTA)
│   ├── .auth-divider (OR CONTINUE WITH — Login only)
│   ├── .auth-social-row (Google/Apple — Login only, disabled)
│   └── .auth-bottom-nav (No account? Create one)
└── <footer class="auth-footer">
```

### Eyebrow Labels

| Page | Eyebrow | Title |
|---|---|---|
| Login | `WELCOME BACK` | `Sign in` |
| Register | `START A STREAK` | `Create account` |

### Alert Banners

```css
.auth-alert-error {
    background: rgba(255, 107, 107, 0.1);
    border: 1px solid rgba(255, 107, 107, 0.3);
    color: #FF9B9B;
}

.auth-alert-info {  /* cold-start banner */
    background: rgba(91, 200, 255, 0.1);
    border: 1px solid rgba(91, 200, 255, 0.25);
    color: var(--text-dim);
}
```

---

## 16. Accessibility Expectations

- All `<button>` and `<input>` elements must have meaningful text content or `aria-label`
- Eye-toggle buttons: `aria-label="Show password"` / `aria-label="Hide password"` (dynamic)
- Close buttons: `aria-label="Close"`
- Decorative SVG icons: `aria-hidden="true"`
- Charts: currently no screen-reader text; future work should add `<title>` inside SVG
- Headings (`h1`–`h6`) have `outline: none` in global CSS — they are not interactive and must not receive visible focus rings
- **Do not** globally suppress focus outlines — interactive elements (buttons, inputs, links) must retain their browser focus ring for keyboard navigation
- Color is never the **only** indicator of state (points always show `+`/`-` prefix in addition to color)

---

## 17. Toast Notifications

Toast notifications use the native **`ToastService`** (KI-009 resolved 2026-06-05 — MudBlazor fully removed).

### Usage

Inject `ToastService` and call `Show`:

```csharp
@inject ToastService Toast

Toast.Show("Settings saved.", ToastType.Success);
Toast.Show("Failed to save settings.", ToastType.Error);
Toast.Show("Please select an activity.", ToastType.Warning);
```

`ToastType` values: `Success`, `Error`, `Warning`, `Info`.

Duration defaults (set in one place in `ToastService.Show`): 3 s for Success/Info; 4.5 s for Error/Warning.

### Visual Design

`ToastHost.razor` renders a fixed-position overlay (`z-index: 9999`):

- **Desktop:** top-right, `top: 64px; right: 24px` (60px topbar + 4px gap), `flex-direction: column` (new toasts append below existing), `width: min(420px, calc(100vw - 2rem))`.
- **Mobile (≤540px):** `top: 64px; left: 12px; right: 12px; width: auto` — full-width below topbar.
- Background: `var(--surface-3)`, border: `1px solid var(--border)`, radius: `var(--radius-sm)`, `box-shadow: 0 4px 16px rgba(0,0,0,0.5), 0 1px 4px rgba(0,0,0,0.3)`.
- Left accent border (4px) by type:
  - `Success` → `var(--primary)` (green)
  - `Error` → `var(--negative)` (red)
  - `Warning` → `var(--cat-social)` (amber)
  - `Info` → `var(--accent)` (sky blue)
- Entry animation: drop down from above (`translateY(-12px)` → 0), 200ms ease-out. *(UX-002: was slide-right; changed to match top placement.)*
- Manual dismiss: ✕ button per toast.

### Files

| File | Purpose |
|---|---|
| `Momentum.Client/Services/ToastService.cs` | Singleton — fires `Action<ToastMessage>` event |
| `Momentum.Client/Components/ToastHost.razor` | Component — subscribes to event, `Task.Delay` auto-dismiss, renders stack |
| `Momentum.Client/Components/ToastHost.razor.css` | Scoped styles — overlay, card, accent borders, animation, mobile breakpoint |

`ToastHost` is registered in `MainLayout.razor` inside `<Authorized>`. Do not add it to individual pages.

---

## 18. RichNotesEditor Component

Used on the Add/Edit Log Entry form in place of the plain `<textarea>` for the Notes field.

### Structure

```html
<div class="rne-wrap">         <!-- outer wrapper; receives focus-within border highlight -->
    <div class="rne-toolbar">  <!-- button strip: Bold · Italic · Underline · Bullet list -->
        <button class="rne-btn" @onmousedown:preventDefault @onclick="...">...</button>
    </div>
    <div id="@_id" class="rne-editor" contenteditable="true"
         data-placeholder="Add optional notes about this activity..."
         role="textbox" aria-multiline="true">
    </div>
</div>
```

### Key behaviors

- **Toolbar buttons** use `@onmousedown:preventDefault` so the `contenteditable` div never loses focus when a format button is clicked. Format commands run via `document.execCommand()` (JS interop).
- **Paste** is intercepted in JS: strips incoming HTML, inserts plain text only via `document.execCommand('insertText')`.
- **Placeholder** rendered via CSS `::before` on `.rne-editor:empty`.
- **Submit** — the parent form reads HTML via `RichNotesEditor.GetContentAsync()` before building the DTO. Content is never pushed to the parent on every keystroke.
- **Reset** — parent calls `RichNotesEditor.ClearAsync()` after successful add-mode submission or activity deselect.
- **Edit mode** — `InitialValue` is set at render time; `OnAfterRenderAsync(firstRender)` hydrates the div via JS. `ShouldRender()` returns `false` after first render to prevent Blazor from overwriting user edits.

### CSS tokens used

- Border/background: `var(--surface-2)`, `var(--border)`, `var(--border-soft)`, `var(--primary-glow)`
- Text: `var(--text)`, `var(--text-muted)`
- Hover: `var(--surface-3)`

### Styling contenteditable content requires `::deep`

The `<p>`, `<ul>`, `<li>` elements inside the editor are inserted by the browser via `document.execCommand`, not rendered by Blazor. They therefore do **not** receive the component's CSS-isolation scope attribute (`b-xxxxxxxx`), so an ordinary scoped selector like `.rne-editor ul` never matches them — the global/UA list style wins and bullets render outside the box. Use the `::deep` combinator so the descendant part of the selector drops the scope requirement:

```css
.rne-editor ::deep ul { padding-left: 24px; list-style-position: outside; }
.rne-editor ::deep li { margin-bottom: 2px; }
```

This is the same CSS-isolation gotcha documented for `MarkupString` SVG in §12 / KI-005 — any DOM not rendered by the Blazor renderer must be styled via `::deep` or a global stylesheet.

### View Log details display (read-only notes + linked check-ins)

The View Log page (`ActivityDetail.razor`) shows an expandable **details** section per entry when the **Details** toggle is ON. The section holds: the formatted note (if present), the entry's linked check-ins (if any), and a "+ Add Check-In" action.

- **Toggle** (`.details-toggle`) is a compact chip labeled **Details** with a notebook icon, placed on the entry-count / score-summary line (`.detail-stats-row`) and **right-aligned** via `margin-left: auto`. Same chip language as the dimension filter chips (`--border`, `--text-dim`; active = `--primary-glow` bg + `--primary` border/text), `0.72rem`, `4px 10px`. Rendered whenever there is at least one displayed entry; defaults OFF. Accessibility: dynamic `aria-label`/`title` ("Show details" / "Hide details") and `aria-pressed`. *(Renamed from the former "Notes" toggle in CHK-002 Phase 6A; the concept now covers notes + check-ins.)*
- **Card structure:** `.log-card` is a flex **column**. The clickable row (`.log-card-row`, holding badge · info · time/points · delete, with the edit `@onclick` and `cursor:pointer`) is the first child; the details block is an optional second child (sibling of the row, so its clicks do not trigger the row's edit handler). When the toggle is OFF nothing extra renders, so the card is visually identical to before.
- **Note body** (`.log-note-body`) is inline secondary detail — muted (`--text-dim`), smaller font, indented under the text column, **no border/background/panel**. Rendered via `@((MarkupString)log.Notes)`. `::deep` is required for its child elements (`p`, `ul`, `ol`, `li`, `strong`, `b`, `em`, `i`, `u`) since `MarkupString` content has no scope attribute.
- **Linked check-ins** (`.log-checkins`) are indented to match the note body. Each `.ci-row` has a clickable `.ci-main` and a `.ci-del` trash → confirm/cancel control reusing the shared `.act-btn` destructive pattern (§9). Inside `.ci-main`, the **timestamp (`.ci-time`) is normal-weight / secondary** (`--text-dim`) and the Body/Energy/Mood values are the primary information; score values use `.ci-val.pos`/`.neg` for green/red. The row navigates to `/check-ins?editId={id}` to edit; a dashed `.ci-add` button navigates to `/check-in?activityLogId={logId}&from={name}`. Both launches also carry a `returnUrl` so the flow returns to the View Log context (see software spec §8.2b).

```css
.log-card        { display: flex; flex-direction: column; }
.log-card-row    { display: flex; align-items: center; gap: 12px; cursor: pointer; }
.log-note-body   { margin-top: 10px; padding-left: 50px; color: var(--text-dim); font-size: 0.82rem; }
.log-checkins    { margin-top: 10px; padding-left: 50px; display: flex; flex-direction: column; gap: 6px; }
```

---

## 19. Period Pill — Time-Range Selector

A rounded pill control containing a `PERIOD` label, a native `<select>`, and a chevron SVG. Used on Balance, Check-Ins, and Trends to switch the time range driving all data on the page.

### Markup pattern

```html
<div class="period-pill">
    <span class="period-label">Period</span>
    <select class="period-select" @onchange="OnPeriodChanged">
        <option value="..." selected="@(...)">…</option>
        …
    </select>
    <svg class="period-chevron" viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
        <path d="M7 10l5 5 5-5z" fill="currentColor"/>
    </svg>
</div>
```

Use `@onchange` (not `@bind`) so the handler can call async data-fetch logic after updating the selection state. Bind the `selected` attribute on each `<option>` to the current state so re-renders keep the dropdown in sync.

### CSS (must be duplicated per page — Blazor CSS isolation scopes by component)

```css
.period-pill {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 8px 14px 8px 16px;
    background: var(--surface-2); border: 1px solid var(--border);
    border-radius: 20px; flex-shrink: 0; cursor: pointer;
    transition: border-color 0.15s ease;
}
.period-pill:focus-within { border-color: var(--primary); }
.period-label { font-size: 0.68rem; font-weight: 700; letter-spacing: 0.08em; color: var(--text-muted); text-transform: uppercase; white-space: nowrap; pointer-events: none; }
.period-select { background: transparent; border: none; color: var(--text); font-family: 'DM Sans', sans-serif; font-size: 0.88rem; font-weight: 600; cursor: pointer; outline: none; -webkit-appearance: none; appearance: none; padding: 0; margin: 0; }
.period-select option { background: var(--surface-2); color: var(--text); }
.period-chevron { color: var(--text-muted); flex-shrink: 0; pointer-events: none; }
```

### Notes

- `pointer-events: none` on `.period-label` and `.period-chevron` ensures clicks always reach the invisible `<select>` sitting between them.
- `:focus-within` provides keyboard-navigation feedback (green border) without JS.
- CSS is **not global** — each page must include these rules in its own `.razor.css` file.

### Pages using this pattern

| Page | File | Options | Default |
|---|---|---|---|
| Balance | `Reports.Balance.razor.css` | This Week · This Month · This Year | This Week |
| Check-Ins | `CheckIns.razor.css` | Day · Week · Month | Day |
| Trends | `Reports.razor.css` | Daily · Weekly · Monthly | Daily |

---

*Momentum Design System — v1.12*
*UX-002: §17 Toast — updated to top placement (top: 64px), wider container (min(420px, …)), surface-3 background, box-shadow, drop-down animation (translateY)*
*UX-003: §14 Trends controls — "tabs" → "period dropdown"; §19 added — Period Pill documented as a cross-page reusable pattern*
