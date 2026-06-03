# Rich Notes v1 — Implementation Plan

**Branch:** `feature/rich-notes-v1`  
**Status:** Planning · Pre-implementation  
**Last Updated:** 2026-06-03  
**Design spec:** `Docs/rich-notes-v1-design-spec.md`

No implementation code has been written yet.

---

## 1. Files Likely Affected

### Server-side

| File | Change |
|---|---|
| `Momentum.Shared/CreateActivityLogDto.cs` | `[MaxLength(1000)]` → `[MaxLength(10000)]` on `Notes` |
| `Momentum.Shared/UpdateActivityLogDto.cs` | `[MaxLength(1000)]` → `[MaxLength(10000)]` on `Notes` |
| `Momentum.Infrastructure/Services/ActivityLogService.cs` | Add sanitization + blank-note normalization before `SaveChanges` in `CreateAsync` and `UpdateAsync` |
| `Momentum.Infrastructure/Momentum.Infrastructure.csproj` | Add HtmlSanitizer NuGet reference (if Option A chosen — see §3) |

### Client-side

| File | Change |
|---|---|
| `Momentum.Client/Pages/LogActivity.razor` | Replace `<textarea>` Notes block with `<RichNotesEditor>` component |
| `Momentum.Client/Pages/ActivityDetail.razor` | Add Show Notes toggle + conditional HTML rendering in log entry rows |
| `Momentum.Client/Pages/ActivityDetail.razor.css` | Styles for note rendering and toggle |
| `Momentum.Client/Components/RichNotesEditor.razor` | **New** — `contenteditable` editor + toolbar |
| `Momentum.Client/Components/RichNotesEditor.razor.css` | **New** — scoped styles for editor and toolbar |
| `Momentum.Client/wwwroot/js/richNotesEditor.js` | **New** — JS interop helpers (get/set content, format commands) |
| `Momentum.Client/wwwroot/index.html` | Add `<script>` reference for `richNotesEditor.js` |

### No changes needed

| File | Why |
|---|---|
| `Momentum.Domain/Entities/ActivityLog.cs` | `Notes` is already `string?` with no constraint |
| Any EF Core migration | Database column is already `nvarchar(max)` |
| `Momentum.API/Controllers/ActivityLogsController.cs` | Existing endpoints unchanged; sanitization happens in the service layer |
| Scoring / reporting services | Notes field is never read by score or report logic |

---

## 2. DTO Validation Changes

Two files in `Momentum.Shared`:

**`CreateActivityLogDto.cs`**
```csharp
// Before
[MaxLength(1000)]
public string? Notes { get; set; }

// After
[MaxLength(10000)]
public string? Notes { get; set; }
```

**`UpdateActivityLogDto.cs`**
```csharp
// Before
[MaxLength(1000)]
public string? Notes { get; set; }

// After
[MaxLength(10000)]
public string? Notes { get; set; }
```

No other DTO changes required.

---

## 3. Notes Sanitization Approach

Sanitization runs **server-side** in `ActivityLogService`, applied to `dto.Notes` before any entity is created or updated. Client content is never trusted directly.

### Option A — HtmlSanitizer NuGet (recommended)

Add `Ganss.Xss.HtmlSanitizer` NuGet to `Momentum.Infrastructure`.

```csharp
// In ActivityLogService.CreateAsync and UpdateAsync:
private static string? SanitizeNotes(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;

    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedTags.Clear();
    sanitizer.AllowedTags.UnionWith(new[] { "p", "br", "strong", "em", "u", "ul", "ol", "li" });
    sanitizer.AllowedAttributes.Clear();  // no attributes permitted

    var sanitized = sanitizer.Sanitize(raw);

    // Normalize: if stripping HTML leaves only whitespace, treat as null
    return string.IsNullOrWhiteSpace(StripTags(sanitized)) ? null : sanitized;
}
```

HtmlSanitizer is well-maintained, actively used in production .NET apps, and has a clean allowlist API.

### Option B — Custom tag stripping (simpler but fragile)

Regex or manual parsing to remove disallowed tags. Not recommended — HTML parsing with regex is notoriously incomplete and XSS-prone.

**Recommendation: Option A.** The library is purpose-built for exactly this use case.

---

## 4. Rich Text Editor Component Design

### `RichNotesEditor.razor`

A self-contained Blazor component wrapping a `contenteditable` div and a custom toolbar strip.

**Parameters:**

```csharp
[Parameter] public string? Value { get; set; }           // initial HTML content
[Parameter] public string Placeholder { get; set; } = "Add optional notes about this activity...";
[Parameter] public EventCallback<string?> ValueChanged { get; set; }  // two-way binding
```

**Markup structure:**

```html
<div class="rne-wrap">
    <div class="rne-toolbar">
        <button type="button" class="rne-btn" id="rne-bold"      title="Bold">B</button>
        <button type="button" class="rne-btn" id="rne-italic"    title="Italic">I</button>
        <button type="button" class="rne-btn" id="rne-underline" title="Underline">U</button>
        <button type="button" class="rne-btn" id="rne-list"      title="Bullet list">•</button>
    </div>
    <div class="rne-editor" contenteditable="true" id="@_editorId"
         data-placeholder="@Placeholder">
    </div>
</div>
```

**Lifecycle:**
- `OnAfterRenderAsync(firstRender)`: call `JS.InvokeVoidAsync("richNotesEditor.init", _editorId, Value)` to hydrate content
- On blur or parent-triggered save: call `JS.InvokeAsync<string>("richNotesEditor.getContent", _editorId)` to extract HTML before posting

### `richNotesEditor.js`

Small JS file with three helpers:

```javascript
window.richNotesEditor = {
    init: (id, html) => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = html || '';
    },

    getContent: (id) => {
        const el = document.getElementById(id);
        return el ? el.innerHTML : '';
    },

    format: (command) => {
        document.execCommand(command, false, null);
    }
};
```

`execCommand` for `bold`, `italic`, `underline`, `insertUnorderedList` covers all v1 toolbar commands. It is deprecated but has universal browser support and is the pragmatic choice for v1 (TQ-2 in design spec).

### Paste stripping

The `contenteditable` div needs a paste event listener to strip incoming formatting:

```javascript
// Added inside init()
el.addEventListener('paste', (e) => {
    e.preventDefault();
    const text = e.clipboardData.getData('text/plain');
    document.execCommand('insertText', false, text);
});
```

This intercepts paste, discards the rich HTML clipboard data, and inserts only plain text.

### Toolbar active state

On `keyup` and `mouseup` inside the editor:
```javascript
document.querySelectorAll('.rne-btn').forEach(btn => btn.classList.remove('active'));
if (document.queryCommandState('bold'))          document.getElementById('rne-bold').classList.add('active');
if (document.queryCommandState('italic'))        document.getElementById('rne-italic').classList.add('active');
if (document.queryCommandState('underline'))     document.getElementById('rne-underline').classList.add('active');
// List detection via selection ancestor check (queryCommandState is unreliable for lists)
```

`queryCommandState` for active-state detection is deprecated alongside `execCommand` (TQ-4), but has the same broad support and is appropriate for v1.

### Placeholder

CSS-based placeholder using `[data-placeholder]` attribute and `:empty::before` pseudo-element:

```css
.rne-editor:empty::before {
    content: attr(data-placeholder);
    color: var(--text-muted);
    pointer-events: none;
}
```

This avoids the placeholder ever entering the DOM as actual content — it is purely presentational.

---

## 5. Add / Edit Log Entry Changes

### In `LogActivity.razor`

Replace the current Notes `<textarea>` block:

```razor
<!-- BEFORE -->
<div class="field-group">
    <div class="field-label-row">
        <label class="field-label">NOTES</label>
        <span class="field-hint">Optional · @(_model.Notes?.Length ?? 0)/240</span>
    </div>
    <textarea class="field-textarea" maxlength="240" rows="3"
              placeholder="How did it feel? Anything to remember?"
              value="@_model.Notes"
              @oninput="@(e => _model.Notes = e.Value?.ToString())"></textarea>
</div>

<!-- AFTER -->
<div class="field-group">
    <label class="field-label">NOTES</label>
    <RichNotesEditor @bind-Value="_model.Notes" />
</div>
```

The `LogFormModel.Notes` property type remains `string?`. Before submitting, the component reads the editor content via JS interop, stores it in `_model.Notes`, and the existing submit logic sends it in the DTO as before.

---

## 6. View Log Toggle Changes

### In `ActivityDetail.razor`

**State additions:**

```csharp
private bool _showNotes;
private bool HasAnyNotes => FilteredLogs.Any(l => !string.IsNullOrWhiteSpace(l.Notes));
```

**Toggle markup (added to filter-bar):**

```razor
@if (HasAnyNotes)
{
    <button type="button"
            class="notes-toggle @(_showNotes ? "active" : "")"
            @onclick="@(() => _showNotes = !_showNotes)">
        <!-- notebook SVG icon -->
        Notes
    </button>
}
```

**Note rendering (added inside each `.log-card`, below `.log-info`):**

```razor
@if (_showNotes && !string.IsNullOrWhiteSpace(log.Notes))
{
    <div class="log-note-body">
        @((MarkupString)log.Notes)
    </div>
}
```

`(MarkupString)` renders the sanitized HTML as real DOM. Since content is sanitized before storage, this is safe to render directly.

**CSS (in `ActivityDetail.razor.css`):**

```css
.notes-toggle { /* same chip style as cat-chip */ }
.notes-toggle.active { /* same active chip style */ }
.log-note-body {
    padding: 6px 0 2px;
    font-size: 0.82rem;
    color: var(--text-dim);
    line-height: 1.5;
}
.log-note-body p  { margin: 0 0 6px; }
.log-note-body ul { margin: 0 0 6px; padding-left: 18px; }
.log-note-body li { margin-bottom: 2px; }
```

---

## 7. Rendering Sanitized HTML Safely

`(MarkupString)` in Blazor renders raw HTML into the DOM. This is safe **if and only if** the content was sanitized before storage. Since sanitization runs server-side on every create and update, content stored in `ActivityLog.Notes` can be trusted to contain only the allowlisted tags.

**Pattern:**
```razor
@((MarkupString)log.Notes)
```

**Never** render user-supplied `Notes` as `MarkupString` without sanitization having been applied. The correct trust boundary is:
- Sanitize at write time (service layer)
- Render freely at read time (MarkupString is fine)

---

## 8. Blank-Note Normalization

Blank normalization must happen **server-side in the service layer**, regardless of what the client sends.

```csharp
// In ActivityLogService.SanitizeNotes:
if (string.IsNullOrWhiteSpace(raw)) return null;
var sanitized = sanitizer.Sanitize(raw);
// Strip all tags and check if any text content remains
var textOnly = System.Text.RegularExpressions.Regex.Replace(sanitized, "<[^>]+>", "");
return string.IsNullOrWhiteSpace(textOnly) ? null : sanitized;
```

This catches:
- `null` input
- `""` (empty string)
- `"   "` (whitespace-only)
- `"<p></p>"` or `"<p><br></p>"` (editor empty state that produces HTML whitespace)

---

## 9. Testing Plan

### Unit / integration tests

| Test | Description |
|---|---|
| `ActivityLogService_Notes_SanitizesDisallowedTags` | `<script>` and `<a>` are stripped; `<strong>` is preserved |
| `ActivityLogService_Notes_NormalizesBlankToNull` | Empty string → `null`; whitespace → `null`; `<p><br></p>` → `null` |
| `ActivityLogService_Notes_PreservesAllowedFormatting` | `<strong>`, `<em>`, `<u>`, `<ul><li>` survive sanitization |
| `ActivityLogService_Notes_AcceptsUpTo10000Chars` | 10,000-char note succeeds; 10,001-char note fails DTO validation |
| `ActivityLogService_Create_StoresNullWhenNoNotes` | Log created with no notes → `Notes` is `null` in DB |

### Manual verification checklist (before merge to main)

- [ ] Type formatted text in Add Entry editor; save; reopen in Edit — formatting preserved
- [ ] Bold, italic, underline, bullet list all render correctly in View Log with toggle ON
- [ ] Paste rich text from external source — only plain text retained
- [ ] Paste plain text — pasted correctly
- [ ] Empty notes field on save — `Notes` stored as `NULL` (verify via dev DB query)
- [ ] `<script>alert(1)</script>` in notes via direct API call — stripped before storage
- [ ] View Log toggle hidden when no entries have notes
- [ ] View Log toggle visible when at least one entry has notes
- [ ] Toggle OFF: entries look identical to current state
- [ ] Toggle ON: notes render below entries with correct formatting
- [ ] Mobile: editor toolbar usable at 375px
- [ ] Mobile: rendered notes readable in View Log at 375px

---

## 10. Documentation Updates Required After Implementation

When implementation is complete, update:

| Document | Update |
|---|---|
| `Docs/momentum-handoff.md` | Add completed phase entry; update build/test status |
| `Docs/momentum-functional-requirements.md` | §7.1 update: entries now show formatted notes when toggle is ON; §6.3 update: Notes field now supports rich formatting |
| `Docs/momentum-design-system.md` | Document `RichNotesEditor` component pattern; notes rendering in View Log |
| `Docs/momentum-known-issues.md` | Close any related issue if applicable |

---

## 11. Suggested Implementation Sequence

Work is isolated on `feature/rich-notes-v1`. Build and test after each step.

| Step | Work |
|---|---|
| 1 | Update `CreateActivityLogDto` and `UpdateActivityLogDto`: `MaxLength(1000)` → `MaxLength(10000)` |
| 2 | Add `Ganss.Xss.HtmlSanitizer` NuGet to `Momentum.Infrastructure` |
| 3 | Implement `SanitizeNotes()` helper in `ActivityLogService`; wire into `CreateAsync` and `UpdateAsync` |
| 4 | Write unit tests for sanitization and blank normalization (step 3) |
| 5 | Create `richNotesEditor.js` with `init`, `getContent`, `format` helpers; wire paste stripping |
| 6 | Add `<script>` reference to `index.html` |
| 7 | Build `RichNotesEditor.razor` + `RichNotesEditor.razor.css` |
| 8 | Replace `<textarea>` in `LogActivity.razor` with `<RichNotesEditor>` |
| 9 | Verify Add Entry and Edit Log Entry end-to-end: create → save → reopen → edit → save |
| 10 | Add `_showNotes` state + `HasAnyNotes` computed to `ActivityDetail.razor` |
| 11 | Add Notes toggle to View Log filter bar |
| 12 | Add `log-note-body` rendering to log cards + CSS |
| 13 | Verify View Log: toggle hidden/shown correctly; toggle ON/OFF renders correctly |
| 14 | Full manual QA checklist |
| 15 | Merge PR to `main` |

---

*Rich Notes v1 Implementation Plan — created 2026-06-03 on `feature/rich-notes-v1`*  
*Status: Planning · No implementation code written*
