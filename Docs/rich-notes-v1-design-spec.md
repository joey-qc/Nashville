# Rich Notes v1 — Design Specification

**Branch:** `feature/rich-notes-v1`  
**Status:** Design Final · Implementation Planning  
**Last Updated:** 2026-06-03

---

## 1. Product Purpose and Direction

Rich Notes v1 enhances the existing `ActivityLog.Notes` field with rich text formatting capabilities. It replaces the current plain `<textarea>` with a lightweight custom editor on the Add/Edit Log Entry screen and adds a toggleable formatted-note display to the View Log screen.

### Why Rich Notes instead of a separate Reflection feature

Rich Notes v1 is the current implementation direction because it:
- Uses the existing `ActivityLog.Notes` field and `ActivityLog` model — no new tables, no new API, no new navigation
- Keeps notes tied to logged activities, which is consistent with how users already think about logging behavior
- Avoids introducing a parallel journaling domain
- Supports journaling naturally: a user logs the Journaling activity and writes their journal entry in the Notes field

Reflection as a separate top-level feature (separate table, page, entity, navigation) is **deferred**. It may be revisited in a future release.

---

## 2. Scope

Rich Notes v1 applies to **all Activity Log entries**. Any activity can have rich notes — Exercise, Reading, Meditation, Travel, Journaling, or any user-created activity. There are no special rules or behaviors based on activity type.

---

## 3. Storage

### Field

`ActivityLog.Notes` — existing field, no schema change.

### Format

Notes are stored as **sanitized HTML**. The `contenteditable` editor produces HTML natively; storing HTML avoids a conversion round-trip on every load and edit. Content is sanitized server-side before persistence (see §6).

### Database column

`nvarchar(max)` — no migration needed. The column is already unconstrained.

### Blank note normalization

- Empty string, whitespace-only, or HTML-whitespace-only content (e.g. `<p><br></p>`) must be normalized to `NULL` before persistence.
- `NULL` is the canonical "no notes" value.
- The API must never store an empty string or whitespace-only string.

---

## 4. Length Limit Changes

| Layer | Current | Rich Notes v1 |
|---|---|---|
| UI `maxlength` attribute | 240 | **Removed** |
| UI `0/240` counter hint | Present | **Removed** |
| `CreateActivityLogDto` `[MaxLength]` | 1,000 | **10,000** |
| `UpdateActivityLogDto` `[MaxLength]` | 1,000 | **10,000** |
| Domain entity (`ActivityLog.Notes`) | None | No change |
| Database (`nvarchar(max)`) | None | No change |

---

## 5. Add / Edit Log Entry — Editor UX

### Placement

The Notes editor replaces the current `<textarea>` in its existing position on the form — same visual location, same field label (`NOTES`).

### Visibility

The editor is **always visible** on both Add Entry and Edit Log Entry. There is no show/hide toggle or expand behavior on the entry form.

### Placeholder

When the editor is empty, it shows:

```
Add optional notes about this activity...
```

- Placeholder is UI-only — never saved, never sent to the API.
- Placeholder disappears when the user focuses and begins typing.

### Formatting toolbar

A custom lightweight toolbar sits above the editor area. Always visible (not hidden behind a "more" button). Contains exactly four controls in v1:

| Control | Format | HTML output |
|---|---|---|
| **B** | Bold | `<strong>` |
| *I* | Italic | `<em>` |
| U̲ | Underline | `<u>` |
| • | Bullet list | `<ul><li>` |

Toolbar behavior:
- Applies formatting to selected text.
- Toggling a format button while text is selected applies or removes that format.
- Continued typing after toggling a format keeps the format active until toggled off.
- Toolbar buttons visually indicate active state when the cursor is inside formatted text.

### Editor behavior

| Behavior | Decision |
|---|---|
| **Enter key** | Creates a new paragraph (`<p>`) |
| **Bullet list** | Created via toolbar button only (not auto-detected from `-` or `*`) |
| **Paste** | Allowed; external formatting stripped; only plain text retained |
| **Paste source** | User reformats with toolbar after paste |

---

## 6. Sanitization

Notes HTML is **sanitized server-side before persistence**. Client content is never trusted directly.

### Allowlist (v1)

Only the following tags are preserved after sanitization. All other tags and attributes are stripped:

| Tag | Purpose |
|---|---|
| `<p>` | Paragraph |
| `<br>` | Line break |
| `<strong>` | Bold |
| `<em>` | Italic |
| `<u>` | Underline |
| `<ul>` | Unordered list |
| `<ol>` | Ordered list (future-safe) |
| `<li>` | List item |

Not allowed in v1:
- `<a>` (hyperlinks)
- `<img>` (images)
- `style` attributes
- `class` attributes
- `onclick` or any event attributes
- Any other tags not in the allowlist

### Implementation approach

**Decided:** `Ganss.Xss.HtmlSanitizer` NuGet package, applied server-side in `ActivityLogService` before the entity is written to the database. See §3 of the implementation plan for the full algorithm.

---

## 7. View Log — Show Notes Toggle

### Toggle presence

- The Show Notes toggle is **only visible when at least one currently displayed log entry contains notes** (non-null `Notes` value).
- If no displayed entries contain notes, the toggle is hidden entirely.
- Filtering (by period or dimension) may cause the toggle to appear or disappear as the displayed set changes.

### Default state

Toggle is **OFF** by default. View Log loads exactly as it does today with no visual changes.

### Toggle OFF state

- Log entries look exactly as they do today.
- No note icon, no badge, no extra indicator on individual entries.
- No visual difference from current behavior.

### Toggle ON state

- The full formatted note renders **directly beneath each log entry** that has a note.
- Content is rendered as sanitized HTML — paragraphs, bold, italic, underline, and bullet lists display correctly.
- Entries without notes show no additional content.

### Note rendering rules

| Rule | Decision |
|---|---|
| **Truncation** | None — full note always displayed |
| **"Show More"** | Not used |
| **Container** | No separate card, no bordered panel, no extra click required |
| **HTML rendering** | Sanitized HTML rendered directly; not raw text |
| **Whitespace** | Paragraphs and bullets preserved |

---

## 8. Search

Search over Notes content is **not included in Rich Notes v1 MVP.**

Documented as a future enhancement:
- Full-text search over note content
- Optionally filter entries that contain notes
- Strip HTML to plain text for search indexing
- Add SQL full-text index if needed for performance

---

## 9. MVP Boundary

### In scope for Rich Notes v1

- Replace plain `<textarea>` with `contenteditable` + custom toolbar on Add/Edit Log Entry
- Bold, italic, underline, bullet list formatting
- Sanitized HTML storage (server-side)
- Remove 240-char UI limit; increase DTO limit to 10,000
- Blank-note normalization to `NULL`
- Paste stripping (plain text only)
- Show Notes toggle on View Log
- Toggle visibility conditional on entries with notes
- Formatted HTML rendering in View Log when toggle is ON
- Placeholder text in editor when empty

### Out of scope for v1

- Notes search / filtering
- Hyperlinks
- Images
- Tables
- Colored text
- Numbered lists (sanitizer allows `<ol>` defensively but toolbar doesn't create them)
- Separate Reflection table / page / entity / API
- Top-level Reflection navigation
- Note export
- Note length indicator / counter

---

## 10. Open Technical Questions

Product decisions are fully resolved. TQ-1 and TQ-3 are now resolved. The following implementation questions remain open.

### Resolved

| # | Question | Decision |
|---|---|---|
| ~~TQ-1~~ | ~~HTML sanitization library~~ | **`Ganss.Xss.HtmlSanitizer` NuGet, server-side.** Allowlist: `p`, `br`, `strong`, `em`, `u`, `ul`, `ol`, `li`. No attributes. No links. No images. No inline styles. |
| ~~TQ-3~~ | ~~Blank-note normalization logic~~ | **Server-side, after sanitization:** (1) sanitize HTML, (2) strip tags, (3) decode HTML entities, (4) trim whitespace — if result is empty, store `NULL`. Applies to both create and update. |

### Open

| # | Question | Notes |
|---|---|---|
| TQ-2 | **`execCommand` vs. Range API** | `document.execCommand` is deprecated but has near-universal support and is the pragmatic choice for v1; `Selection`/`Range` API is the long-term alternative |
| TQ-4 | **Toolbar active-state detection** | `document.queryCommandState` (deprecated but works) vs. inspecting cursor position in the DOM for format detection |
| TQ-5 | **View Log toggle placement** | Exact position within the existing View Log filter bar; how it interacts with the period pill and dimension filter chips at narrow viewport widths |

---

*Rich Notes v1 Design Specification — created 2026-06-03 on `feature/rich-notes-v1`*  
*Status: Design Final · Implementation Planning in progress*
