# Check-In Feature — Design Specification

**Status:** 📝 PLANNED — not implemented (design only)
**Last Updated:** 2026-06-04

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

This document is **design/planning only.** No application code, schema, migration, DTO, API, or UI for Check-Ins has been implemented. Functional requirements and software specifications will be updated when the feature is implemented, not before.

---

*Check-In Feature Design Specification — created 2026-06-04*
*Status: 📝 PLANNED — design only, not implemented*
