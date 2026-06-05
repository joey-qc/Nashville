# Check-In Feature — Design Specification

**Status:** 🔨 IN PROGRESS — CHK-002 Phase 3 complete (standalone Check-In form); post-activity flow, history screen, View Log integration, reporting not yet implemented
**Last Updated:** 2026-06-05

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

**Not yet implemented (later phases):** post-activity flow (§7), Check-Ins history screen (§12), View Log "Details" integration (§10), Edit Log Entry check-in list (§11), persistent "Check In" action button (§14), reporting/correlation (§17).

**Manual QA checklist (to run against the live app):**
- Open `/check-in`; defaults load from most recent check-in, else 0/0/0.
- Save a valid check-in → success toast; timestamp resets to now, scores retained.
- Steppers cannot exceed −5…+5 (buttons disable at bounds).
- No browser console errors.
- Mobile layout (≤540px): date/time stack, steppers full-width and usable.

---

*Check-In Feature Design Specification — created 2026-06-04*
*Status: 🔨 IN PROGRESS — Phase 3 complete (standalone form); post-activity flow, history, View Log integration, reporting not started*
