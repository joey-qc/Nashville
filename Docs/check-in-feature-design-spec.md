# Check-In Feature — Design Specification

**Status:** 🔨 IN PROGRESS — CHK-002 Phase 4 complete (post-activity flow); history screen, View Log integration, persistent action button, reporting not yet implemented
**Last Updated:** 2026-06-06

---

## 1. Feature Name

The feature is called **Check-In** (singular) / **Check-Ins** (plural). This is the canonical user-facing and internal name. It is used consistently across UI, navigation, data model, and documentation.

---

## 2. Product Purpose

Check-Ins capture the user's **current state / outcomes**, whereas Activity Logs capture **behaviors / inputs**.

| Concept | Captures | Question it answers |
|---|---|---|
| **Activity Log** | Behaviors / inputs | "What did I do?" |
| **Check-In** | State / outcomes | "How am I doing right now?" |

This separation lets Momentum eventually correlate behavioral inputs (activities logged) against state outcomes (how the user feels), without conflating the two into a single record.

A Check-In is intentionally lightweight: a fast snapshot of how the user is doing at a moment in time.

---

## 3. Check-In Metrics

Each Check-In records three metrics:

- **Body**
- **Energy**
- **Mood**

Each metric uses a **`-5` to `+5`** scale:

| Value | Meaning |
|---|---|
| `0` | Baseline / normal |
| Positive (`+1` … `+5`) | Better than baseline |
| Negative (`-1` … `-5`) | Worse than baseline |

The scale is relative to the user's own baseline, not an absolute clinical measure. `0` always means "normal for me."

---

## 4. Separate Model from Dimensions

Check-In metrics are a **distinct concept** from the existing Momentum **Dimensions**. The two must not be conflated or share a table.

| | Dimensions | Check-In Metrics |
|---|---|---|
| Examples | Body, Mind, Spirit, Connections, Responsibilities | Body, Energy, Mood |
| Describes | Activity **impact areas** (what an activity affects) | User **state** (how the user is doing) |
| Attached to | Activities and Activity Log entries | Check-Ins |
| Storage | `Dimensions` table (5 seeded rows) | Dedicated `CheckIn` entity fields |

**Important:** Do **not** reuse the existing `Dimensions` table for Check-In metrics. Although "Body" appears in both lists, the Dimension "Body" describes an activity's impact area, while the Check-In "Body" metric describes the user's current physical state. They are unrelated concepts that happen to share a label.

Check-In metrics (`BodyScore`, `EnergyScore`, `MoodScore`) are fixed columns on the `CheckIn` entity in v1 — not a normalized lookup/join model.

---

## 5. Data Model Direction

Planned entity: **`CheckIn`**

| Field | Type (planned) | Notes |
|---|---|---|
| `Id` | int (PK) | Primary key |
| `UserId` | FK → user | Owner; all queries scoped to current user (see CLAUDE.md §6.5) |
| `CheckedInAt` | DateTime | **User-editable effective timestamp.** Used for analytics and display. |
| `BodyScore` | int (`-5`…`+5`) | Body metric |
| `EnergyScore` | int (`-5`…`+5`) | Energy metric |
| `MoodScore` | int (`-5`…`+5`) | Mood metric |
| `ActivityLogId` | int? (nullable FK → `ActivityLog`) | Optional parent activity-log association |
| `CreatedAt` | DateTime | **Internal audit timestamp.** Audit/debugging only. |

### Timestamp semantics

- **`CheckedInAt`** — the effective moment the check-in represents. The user may edit this. **All analytics, grouping, sorting, and display use `CheckedInAt`.** This mirrors how `ActivityLog.LoggedAt` works for activities.
- **`CreatedAt`** — when the record was actually inserted. Internal only. **Never** used for analytics or display; used solely for audit/debugging.

### Conventions to follow (per CLAUDE.md)

- `UserId` is always extracted from JWT claims server-side — never trusted from the request body.
- Every repository query for Check-Ins must filter by `UserId`.
- The API returns a `CheckInDto`, never the `CheckIn` entity (CLAUDE.md §6.3).
- Migration will live in `Momentum.Infrastructure/Migrations/`.
- DTOs (`CheckInDto`, `CreateCheckInRequestDto`, `UpdateCheckInRequestDto`) will live in `Momentum.Shared`.

---

## 6. Optional ActivityLog Association

A Check-In may be **standalone** or **linked to exactly one ActivityLog**.

Relationship cardinality:

- One `ActivityLog` can have **zero, one, or many** Check-Ins.
- One `CheckIn` can have **zero or one** parent `ActivityLog`.

This is a nullable one-to-many: `ActivityLog (1) ──< CheckIn (0..*)`, with the `CheckIn.ActivityLogId` foreign key nullable.

This supports both:

- **Standalone check-ins** — e.g. first thing in the morning, with no associated activity.
- **Post-activity check-ins** — e.g. recording Body / Energy / Mood right after an Exercise log entry.

A single activity log can accumulate multiple check-ins over time (e.g. immediately after exercise, and again a few hours later).

---

## 7. Post-Activity Flow

Saving a new Activity Log entry **automatically navigates to the Check-In form** with the newly created `ActivityLogId` pre-populated.

The intended flow is:

```
Save Activity Log → Check-In form opens → user saves or cancels
```

From the Check-In form the user can:

- **Save** — creates a Check-In associated with that ActivityLog via `ActivityLogId`.
- **Cancel / skip** — dismisses the form without creating a Check-In record.

The post-activity Check-In is the **expected next step** after activity logging, not an incidentally offered option. Creating the Check-In record itself remains optional because the user can always cancel.

---

## 8. Default Values

When opening the Check-In form:

- **If the user has a previous Check-In:** preload `Body` / `Energy` / `Mood` from the **most recent** Check-In (by `CheckedInAt`).
- **If no previous Check-In exists:** default all three scores to `0`.

Rationale: preloading the last recorded state lets the user adjust **relative to where they were** rather than re-entering everything from scratch. State tends to drift incrementally, so the previous values are usually the closest starting point.

---

## 9. Notes

**Check-In notes are NOT included in v1.** Check-In capture is intentionally lightweight — three quick sliders/scores and save.

(Rich notes already exist on Activity Logs via Rich Notes v1; Check-Ins deliberately stay minimal.)

---

## 10. View Log Integration

The existing View Log **"Notes" toggle is renamed to "Details."**

### Details toggle OFF

- Activity rows remain compact — identical to today's default view.

### Details toggle ON

- Show **Notes** if present (existing Rich Notes rendering).
- Show **linked Check-Ins** if present (any Check-In whose `ActivityLogId` matches the entry).

The toggle thus expands a single control's meaning from "notes" to "all supplementary detail for the entry" (notes + check-ins).

---

## 11. Edit Log Entry Integration

The Edit Log Entry screen should **eventually** show:

- A **compact list of associated Check-Ins** for that activity log entry.
- An option to **add a follow-up Check-In** (routes to the Check-In form with this `ActivityLogId` pre-populated, per §7).

Full edit/delete management of individual check-ins from this screen can be **phased** — the initial increment may be read-only display + "add follow-up," with inline edit/delete added later.

---

## 12. Check-Ins History Screen

Plan a **first-class "Check-Ins" screen**, structurally similar to View Log, showing Check-Ins for a selected period.

- Shows **all** Check-Ins regardless of whether they are linked to an ActivityLog.
- Ordered/grouped by `CheckedInAt` (consistent with View Log period model).

### Contextual labeling

- If a Check-In **has a parent ActivityLog**, show contextual text such as:

  ```
  After: Exercise / Gym
  ```

  (i.e. `After: {activity name}` derived from the linked ActivityLog's activity.)

- If a Check-In **has no parent ActivityLog**, show **no extra label** (do not display "Standalone" or any equivalent badge). The absence of an "After:" line is itself the indicator.

---

## 13. Navigation

Do **not** add "Add Entry" or "Check In" items to the nav menu **if persistent top-level action buttons already exist** for those creation flows (see §14). Creation flows live on persistent action buttons, not in the nav.

### Future navigation direction

The intended nav structure (creation actions excluded):

1. Home
2. View Log
3. **Check-Ins**
4. Trends
5. Balance
6. *(future)* Body / Energy / Mood reporting page
7. Manage
8. Settings

---

## 14. Persistent "Check In" Button

Plan a **persistent "Check In" action button** available on authenticated pages, analogous to the current persistent **Add Entry** button.

- Provides one-tap access to the Check-In form from anywhere in the authenticated app.
- Exact visual treatment is **undecided** (placement, styling, pairing with Add Entry to be determined during design).

---

## 15. Mobile Masthead Concern

Document a future mobile UI need:

- When **both** "Add Entry" and "Check In" persistent buttons are present, page header actions risk **wrapping** at the mobile breakpoint.
- Mitigation to plan: **shorten "Manage Activities" to "Manage"** at the mobile breakpoint so page header actions fit on one row without wrapping.

This aligns with the existing mobile-responsive label approach (`DimensionDisplayHelper` / responsive label spans) already used elsewhere.

---

## 16. Deferred: PWA / Push Notification Reminders

PWA installability and push notification reminders are **long-term / deferred** — **not** part of the near-term Check-In implementation.

### Future architecture concept

- **PWA** installable app experience.
- **Browser push notifications** for check-in reminders.
- An **Azure Function timer job** queries notification recipients and sends push notifications **directly**, without requiring the main API to stay awake.
- The **API wakes only** when the user opens the app / responds to a notification.

This design specifically accommodates the Azure SQL Serverless cold-start / idle behavior already documented in the handoff — reminders do not depend on the API being warm.

---

## 17. Future Reporting / Correlation

Document future reporting ideas enabled by Check-In data (input → outcome correlation):

- **Body / Energy / Mood over time** (trend lines).
- **Average mood after specific activities.**
- **Energy changes after morning exercise.**
- **Next-day Body score after resistance training.**
- **Effects of meals / alcohol / meditation / socializing** on subsequent check-in metrics.
- General **activity inputs → check-in outcomes** correlation analysis.

These depend on the input/outcome separation established in §2 (Activity Logs as inputs, Check-Ins as outcomes) and on `CheckedInAt` being the analytics timestamp (§5).

---

## 18. Implementation Status

### CHK-002 Phase 1 — Entity + Migration (COMPLETE 2026-06-05)

Migration: `20260605193819_CHK001_AddCheckIn`

| File | Change |
|---|---|
| `Momentum.Domain/Entities/CheckIn.cs` | New entity — `Id`, `UserId`, `CheckedInAt`, `BodyScore`, `EnergyScore`, `MoodScore`, `ActivityLogId?`, `CreatedAt`, `ActivityLog?` nav |
| `Momentum.Domain/Entities/ActivityLog.cs` | Added `ICollection<CheckIn> CheckIns` reverse navigation |
| `Momentum.Infrastructure/Data/AppDbContext.cs` | `DbSet<CheckIn> CheckIns`; FK config (`ActivityLog → SetNull`); indexes on `UserId` and `CheckedInAt` |
| `Momentum.Infrastructure/Migrations/20260605193819_CHK001_AddCheckIn.cs` | Creates `CheckIns` table with correct columns, FK, and three indexes |

Schema produced:
- `CheckIns` table: `Id` (PK, identity), `UserId` (nvarchar 450), `CheckedInAt` (datetime2), `BodyScore` (int), `EnergyScore` (int), `MoodScore` (int), `ActivityLogId` (int, nullable FK → `ActivityLogs.Id`, SetNull), `CreatedAt` (datetime2)
- Indexes: `IX_CheckIns_UserId`, `IX_CheckIns_CheckedInAt`, `IX_CheckIns_ActivityLogId`
- `Down()` drops the table cleanly

**Not yet implemented (Phase 3):** client service, UI form, Check-Ins history screen, View Log "Details" toggle, persistent "Check In" button, navigation, reporting.

### CHK-002 Phase 2 — API + DTOs + Repository/Service (COMPLETE 2026-06-05)

Build: ✅ 0 errors · Tests: ✅ 50/50 (15 new)

**DTOs added (`Momentum.Shared`):**

| File | Purpose |
|---|---|
| `CheckInDto.cs` | Response DTO — all fields including `CreatedAt` (audit only) |
| `CreateCheckInRequestDto.cs` | Create request — `CheckedInAt?` optional, scores with `[Range(-5,5)]`, `ActivityLogId?` optional |
| `UpdateCheckInRequestDto.cs` | Update request — `CheckedInAt` required, scores with `[Range(-5,5)]`, `ActivityLogId?` optional |

**Repository (`Momentum.Application/Interfaces` + `Momentum.Infrastructure/Repositories`):**

| File | Purpose |
|---|---|
| `ICheckInRepository.cs` | `GetByIdAsync`, `GetByDateRangeAsync`, `AddAsync`, `SaveChangesAsync`, `DeleteAsync` |
| `CheckInRepository.cs` | EF Core implementation; all queries scoped by `UserId` |

**Service (`Momentum.Application/Interfaces` + `Momentum.Application/Services`):**

| File | Purpose |
|---|---|
| `ICheckInService.cs` | CRUD interface; `ArgumentException` documented for invalid scores / bad ActivityLogId |
| `CheckInService.cs` | Validates scores in `[-5, 5]`; validates ActivityLogId ownership via `IActivityLogRepository.GetByIdAsync(id, userId)`; maps entities ↔ DTOs; `CreatedAt` set server-side on create only |

**API (`Momentum.API`):**

| File | Purpose |
|---|---|
| `Controllers/CheckInsController.cs` | `GET /api/checkins`, `GET /api/checkins/{id}`, `POST /api/checkins`, `PUT /api/checkins/{id}`, `DELETE /api/checkins/{id}`; `[Authorize]`; UserId from JWT claims; `ArgumentException` → 400 |
| `Program.cs` | `ICheckInRepository` + `ICheckInService` registered as Scoped |

**Tests (`Momentum.Tests/CheckInServiceTests.cs`) — 15 tests:**
- Create with valid scores succeeds
- Create with valid ActivityLogId links successfully
- Score out of range (6 parametrized cases) → `ArgumentException`
- ActivityLogId belonging to another user → `ArgumentException`
- ActivityLogId not found → `ArgumentException`
- Date range query calls repo with correct UserId
- Update belonging to current user succeeds
- Update belonging to other user returns null (repo returns null; SaveChanges not called)
- Delete belonging to current user returns true
- Delete belonging to other user returns false

### CHK-002 Phase 3 — Standalone Check-In Form (COMPLETE 2026-06-05)

Build: ✅ 0 errors · Tests: ✅ 50/50 (unchanged — no new server tests; client UI is manual-QA per project convention)

**Client service (`Momentum.Client/Services`):**

| File | Purpose |
|---|---|
| `CheckInService.cs` | `CreateAsync` (POST `/api/checkins`, returns null on failure) and `GetMostRecentAsync` (wide-range GET, takes first by `CheckedInAt` desc). Tags `CheckedInAt`/`CreatedAt` as UTC after deserialization. Registered Scoped in `Program.cs`. |

**Page (`Momentum.Client/Pages`):**

| File | Purpose |
|---|---|
| `CheckIn.razor` | Route `/check-in`, `[Authorize]`. Date/Time fields (default now, Today/Now chips). Three bounded −5…+5 steppers (Body/Energy/Mood) sharing one DRY descriptor loop. Preloads scores from most recent check-in on init; 0/0/0 if none. Save posts a standalone check-in (`ActivityLogId = null`), shows `ToastService` success/error. |
| `CheckIn.razor.css` | Scoped styles using design tokens; new `.score-stepper` bounded-stepper pattern; mobile breakpoint at ≤540px. |

**Navigation (`Momentum.Client/Layout/MainLayout.razor`):**
- Temporary **Check In** nav item added after View Log (documented as interim; persistent action button per §14 comes later).
- `PageTitle` switch maps `/check-in` → "Check In".

**Save behavior (decision):** After a successful save the user **stays on the page**, the entered scores are **retained** (the natural starting point for a follow-up check-in), and the **timestamp resets to now**. Chosen as the simplest behavior that also supports rapid repeat check-ins.

**Score range enforcement:** Steppers clamp to `[-5, 5]` client-side and disable at bounds; DTO `[Range(-5,5)]` + server `CheckInService` validation remain the authoritative guards (Phase 2).

**Not yet implemented (after Phase 3):** post-activity flow (§7 — delivered in Phase 4 below), Check-Ins history screen (§12), View Log "Details" integration (§10), Edit Log Entry check-in list (§11), persistent "Check In" action button (§14), reporting/correlation (§17).

**Manual QA checklist (to run against the live app):**
- Open `/check-in`; defaults load from most recent check-in, else 0/0/0.
- Save a valid check-in → success toast; timestamp resets to now, scores retained.
- Steppers cannot exceed −5…+5 (buttons disable at bounds).
- No browser console errors.
- Mobile layout (≤540px): date/time stack, steppers full-width and usable.

### CHK-002 Phase 4 — Post-Activity Check-In Flow (COMPLETE 2026-06-06)

Build: ✅ 0 errors · Tests: ✅ 50/50 (unchanged — no schema/API change; client flow is manual-QA per project convention)

Implements the §7 flow: `Save Activity Log → Check-In form opens → user saves or cancels`.

**Add Entry → Check-In hand-off (`Momentum.Client/Pages/LogActivity.razor`):**
- On successful **new** log creation, navigates to `/check-in?activityLogId={newId}&from={activityName}` (the `from` value is display-only context). The previous behavior (reset form, stay on Add Entry) is replaced for the create path. **Edit path is unchanged** — it still returns to View Log.

**Check-In page changes (`Momentum.Client/Pages/CheckIn.razor` + `.css`):**
- New query params: `[SupplyParameterFromQuery] int? ActivityLogId` and `[SupplyParameterFromQuery(Name="from")] string? FromActivity` (display only).
- `IsLinked => ActivityLogId.HasValue`.
- When linked, a context chip shows `After: {FromActivity}` (or a generic line if the name is absent).
- Save passes `ActivityLogId` through to `CreateCheckInRequestDto` — ownership is validated server-side by the existing Phase 2 `CheckInService` (`ArgumentException` → 400 if the log isn't the user's).
- **Linked mode** adds a **SKIP** text button; both Save and Skip then navigate to Home (`/`).
- **Standalone mode** (`/check-in` with no query) is **unchanged**: Save keeps the user on the page, retains scores, resets timestamp.

**Save / skip behavior (decision):**
- Linked Save → creates a check-in with the populated `ActivityLogId`, success toast, navigate to Home.
- Linked Skip → **no check-in created**, navigate to Home.
- Standalone Save → unchanged (stay on page).

**No schema, API, repository, or service changes** — Phase 4 is entirely client-side routing + the page's optional-link handling. Linked check-ins are ordinary records; `ActivityLogId` SetNull-on-delete behavior (Phase 1) is untouched.

**Manual QA checklist (Phase 4):**
- Save a new activity → Check-In form opens automatically with an `After: {activity}` context chip.
- Save the linked check-in → Dev DB row has `ActivityLogId` populated; lands on Home.
- Save another activity → **Skip** → no check-in row created; lands on Home.
- Open `/check-in` directly → standalone flow unchanged (no skip button, stays on page after save).
- No browser console errors.

### CHK-002 Phase 5A — Check-In Navigation Structure (COMPLETE 2026-06-06)

Build: ✅ 0 errors · Tests: ✅ 50/50 (unchanged — layout/routing only; manual-QA per project convention)

Separates Check-In **creation** (top action button) from Check-In **history** (left-nav). Implements §13 (Navigation), §14 (Persistent Check In button), and §15 (Mobile masthead concern).

**Navigation (`Momentum.Client/Layout/MainLayout.razor` + `.css`):**
- **Removed** the temporary left-nav "Check In" link that pointed to `/check-in` (added in Phase 3).
- **Added** a left-nav "Check Ins" link → `/check-ins` (history; active when path starts with `/check-ins`).
- **Added** a persistent top action button "Check In" → `/check-in`, beside "+ Add Entry", inside a new `.topbar-actions` flex container. Both use the same `.topbar-cta` style. Visible on all authenticated pages (persists like Add Entry).
- `PageTitle` switch: `/check-ins` case added **before** `/check-in` (since `/check-ins` also starts with `/check-in`).
- **Mobile masthead (§15):** `PageTitleShort` returns "Manage" for `/activities`; the topbar renders `.title-full` (desktop) + `.title-short` (mobile ≤767px) spans toggled by CSS. Top bar tightened on mobile (smaller gap + button padding; title truncates via `min-width:0`/ellipsis) so "+ Add Entry" and "Check In" never wrap.

**Placeholder page (`Momentum.Client/Pages/CheckIns.razor` + `.css`):**
- `/check-ins`, `[Authorize]`. Simple "history coming soon" placeholder — the full history list is deferred to a later phase.

**Unchanged:** the `/check-in` form behavior (Phase 3) and the post-activity flow (`/check-in?activityLogId=…`, Phase 4) are untouched — only the entry points moved.

**Manual QA checklist (Phase 5A):**
- Left-nav no longer has "Check In" → `/check-in`; it has "Check Ins" → `/check-ins`.
- Top bar shows "+ Add Entry" and "Check In" on authenticated pages; Check In opens `/check-in`, Add Entry opens `/log`.
- Post-activity flow still opens `/check-in?activityLogId=…`.
- Manage Activities title shows "Manage" on mobile; top row does not wrap.
- No browser console errors.

### CHK-002 Phase 5B — Check-Ins History Screen (COMPLETE 2026-06-06)

Build: ✅ 0 errors · Tests: ✅ 52/52 (2 new server tests for `ActivityName` projection)

Replaces the `/check-ins` placeholder with a usable history list supporting inline edit and delete.

**DTO / server (`Momentum.Shared`, `Momentum.Application`, `Momentum.Infrastructure`):**
- `CheckInDto` gains **`ActivityName`** (string?, display-only) — the linked activity's name, or null for standalone.
- `CheckInRepository.GetByIdAsync` / `GetByDateRangeAsync` now `.Include(c => c.ActivityLog).ThenInclude(l => l.Activity)` so the name is available.
- `CheckInService.Map` populates `ActivityName = c.ActivityLog?.Activity?.Name`.
- No new endpoints — reuses GET (date-range), PUT, DELETE from Phase 2.

**Client service (`Momentum.Client/Services/CheckInService.cs`):**
- Added `GetAllAsync()` (wide date-range, newest first, empty list on failure), `UpdateAsync(id, dto)`, `DeleteAsync(id)`. `GetMostRecentAsync` now delegates to `GetAllAsync`.

**Page (`Momentum.Client/Pages/CheckIns.razor` + `.css`):**
- Lists check-ins **newest first** (the repo already orders by `CheckedInAt` desc).
- Each row shows timestamp, **Body / Energy / Mood** score pills (colored pos/neg/zero), and `After: {ActivityName}` only when linked (no label for standalone).
- **Inline edit:** an edit (pencil) button swaps the row for a form with date/time + three bounded −5…+5 steppers. Save / Cancel. The existing `ActivityLogId` is preserved and sent back unchanged (link is not editable here); `CreatedAt` is never shown.
- **Delete:** trash → confirm(✓)/cancel(✕) pattern, identical to View Log / Manage Activities. Deleting a check-in never affects any ActivityLog.
- Empty state when the user has no check-ins.

**Unchanged:** the `/check-in` form, the post-activity flow, and the persistent top "Check In" button.

**Out of scope (not added):** charts, reporting, search, pagination, filtering, analytics.

**Manual QA checklist (Phase 5B):**
- Open `/check-ins` → newest-first list.
- Standalone check-ins show no "After:" label; linked ones show `After: {activity}`.
- Edit a check-in → values update and persist.
- Delete a check-in → it disappears; delete requires explicit confirm.
- "+ Add Entry" / "Check In" top buttons still work; no console errors; mobile layout usable.

#### Phase 5B time-display fix (2026-06-06)

QA found check-in times displayed in UTC (e.g. a 10:52 AM EDT check-in showed as 2:52 PM) and the list appeared mis-ordered. Root cause: EF Core returns `CheckedInAt`/`CreatedAt` as `DateTimeKind.Unspecified`; the API's `UtcDateTimeConverter.Write` then ran `ToUniversalTime()` on them, which on a **non-UTC host (the dev machine)** re-shifted the already-UTC value by the host offset (+4h). Production (UTC host) was unaffected, so it only surfaced in local QA.

Fix (UTC stored throughout; no schema change):
- `CheckInService.Map` now marks `CheckedInAt` and `CreatedAt` as `DateTimeKind.Utc` (`DateTime.SpecifyKind`) so the serializer emits a correct `Z` without re-converting against the server timezone.
- Client `CheckInService.GetAllAsync` sorts newest-first by `CheckedInAt` (after UTC-tagging) so display order tracks the true instant regardless of source order.
- Client display (`ToLocalTime()`) and edit-field round-trip (`SpecifyKind(Local).ToUniversalTime()`) were already correct and are unchanged.
- 3 server tests added: UTC-kind mapping without clock shift, `ActivityName` projection (linked / standalone), and pass-through ordering.

### CHK-002 Phase 6A — View Log Details Integration (COMPLETE 2026-06-06)

Build: ✅ 0 errors · Tests: ✅ 54/54 (no server change → no new server tests; client UI is manual-QA per project convention)

Surfaces linked check-ins inside View Log's expandable details and lets the user add/edit/delete them in context. Implements §10 (View Log integration).

**No server/DTO/API change** — reuses the existing user-scoped `GET /api/checkins` (date-range), `DELETE`, and `CheckInDto.ActivityLogId`. View Log groups check-ins by `ActivityLogId` on the client.

**View Log (`Momentum.Client/Pages/ActivityDetail.razor` + `.css`):**
- Toggle renamed **"Notes" → "Details"** (`.details-toggle`); now shown whenever there is ≥1 displayed entry (previously only when notes existed); default OFF.
- On load, injects `CheckInService` and builds `_checkInsByLog` = `GetAllAsync()` grouped by `ActivityLogId` (standalone check-ins excluded).
- When **Details ON**, each entry's section shows: the note (if present), then each linked check-in as a row (local time + Body/Energy/Mood values), then a dashed **"+ Add Check-In"** button.
- **Edit:** clicking a check-in row navigates to `/check-ins?editId={id}` (the history page opens that row's inline editor).
- **Delete:** per-row trash → confirm/cancel (shared `.act-btn` pattern), calls `CheckInService.DeleteAsync`, reloads. Deletes only the check-in — never the ActivityLog.
- **Add:** "+ Add Check-In" navigates to `/check-in?activityLogId={logId}&from={activityName}` (same target as the post-activity flow, so the saved check-in links correctly).

**History page (`CheckIns.razor`):** added `?editId={id}` — after load, auto-opens that check-in's inline editor (reuses existing `StartEdit`).

**Unchanged:** post-activity flow; Edit Log Entry save flow does **not** trigger check-in creation; standalone `/check-in`; create/edit time handling.

**Out of scope (not added):** reporting, charts, analytics, filtering, search.

**Polish (pre-commit, 2026-06-06):**
- Linked check-in **timestamps are normal-weight / secondary** (`--text-dim`) so the Body/Energy/Mood scores read as primary.
- **Return-context (generic `returnUrl` pattern):** when add/edit is launched from View Log, the launcher passes `returnUrl` (an encoded URL that also carries `period` and `details=true`). `CheckIn.razor` (save/skip) and `CheckIns.razor` (edit save/cancel) navigate to `returnUrl` when present, so the user returns to the **same View Log context with Details still expanded**. When `returnUrl` is absent, existing behavior is unchanged (standalone `/check-in` stays on page; post-activity → Home; history-page edit stays on the list). View Log reads `details=true` to restore the expanded state.

**Manual QA checklist (Phase 6A):**
- View Log shows a **Details** toggle (not "Notes").
- Details ON shows notes (when present), linked check-ins under the entry, and "+ Add Check-In".
- Linked check-in time shows in local time; clicking it opens edit; editing persists.
- Delete requires confirmation and removes only the check-in (ActivityLog remains).
- "+ Add Check-In" opens `/check-in` with `activityLogId` populated; saving links it.
- Post-activity flow still works; entries without notes/check-ins behave cleanly; no console errors; mobile usable.

---

*Check-In Feature Design Specification — created 2026-06-04*
*Status: 🔨 IN PROGRESS — Phase 6A complete (View Log Details integration); Edit Log Entry check-in list, reporting not started*
