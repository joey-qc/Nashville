# Momentum — Functional Requirements Document

## 1. Overview

**Momentum** is a personal wellness tracking web application. Users log daily activities, each assigned a custom point value (positive for beneficial, negative for detrimental), across five life categories. The app provides scoring summaries, trend reporting, and data management tools to help users incentivize and track progress toward their personal wellness goals.

---

## 2. User Accounts & Authentication

### 2.1 Registration
- New users can create an account with an email address, password, and display name.
- Upon successful registration, a starter set of boilerplate activities is automatically added to the user's activity library with default point values.
- Registration is open to allow multiple users (multi-user architecture).

### 2.2 Login
- Users log in with email and password.
- Upon successful login, the user is directed to the **Score Summary (Landing Page)**.

### 2.3 Logout
- Users can log out from any screen via a navigation option.

---

## 3. Global Navigation

- A persistent navigation menu (sidebar or top bar) is accessible from all authenticated screens.
- Navigation items (left nav):
  - **Home** (Score Summary / Landing Page)
  - **Add Entry** (also accessible via the persistent "+ Add Entry" button)
  - **View Log** (Activity Detail View — browsable history by period)
  - **Check Ins** (Check-In history — see §11; route `/check-ins`)
  - **Reports** (Reporting & Analytics — contains Trends and Balance sub-pages)
  - **Manage Activities** (Data Management)
  - **Settings**
- **Persistent top action buttons** are visible on every authenticated screen in the top bar, side by side:
  - **+ Add Entry** — navigates to the Add Entry screen (`/log`).
  - **Check In** — navigates to the standalone Check-In form (`/check-in`).
- On mobile, the page title **"Manage Activities" shortens to "Manage"** so the two top action buttons fit on one row without wrapping.

---

## 4. Dimension Definitions & Color Coding

Activities belong to one or more of the following five wellness dimensions. Dimension colors are consistent across all screens, charts, lists, chips, and UI elements throughout the application.

| Dimension | Color | Mobile Label |
|---|---|---|
| Body | Bright Green | Body |
| Mind | Sky Blue | Mind |
| Spirit | Soft Purple | Spirit |
| Connections | Amber | Con |
| Responsibilities | Salmon | Rsp |
| All (used in reporting filters) | Medium Gray | All |

**Responsive display:** On mobile (≤540px), dimension chips and labels use the abbreviated form (Con, Rsp). Desktop and tablet always show the full name. Full name is always preserved in `title`, `aria-label`, or equivalent for accessibility.

**Stored names:** Database persisted names now match display names (Body/Mind/Spirit/Connections/Responsibilities), aligned by migration `DIM001_RenameDimensions`. `DimensionDisplayHelper` in `Momentum.Client/Services/` no longer needs to translate names — it only provides mobile abbreviations for the two longer names (Connections→Con, Responsibilities→Rsp).

---

## 5. Score Summary — Landing Page (Home)

This is the first screen a user sees after successful login.

### 5.1 Personalized Welcome
- Displays a personalized greeting: **"Welcome back, [User's Name]!"**

### 5.2 Score Totals
- Displays three score totals prominently:
  - **Today's Points**
  - **This Week's Points**
  - **This Month's Points**
- Each score total is a **clickable link** that navigates to the **Activity Detail View** for that time period (see Section 7).

### 5.3 Weekly Comparison Bar Chart
- Displays a bar chart comparing cumulative points earned up to today's day of the week versus the same cumulative day last week.
- Example: If today is Tuesday, the chart compares total points earned Monday–Tuesday this week vs. Monday–Tuesday last week.
- This gives immediate visual feedback on whether the user is ahead of or behind last week's pace.

### 5.4 Weekly Dimension Breakdown
- At the bottom of the Home dashboard, a card shows this week's activity broken down by wellness dimension.
- A full-width stacked proportional bar visualizes each dimension's relative share of the week's points using dimension colors.
- Below the bar, a row per dimension shows: color dot · dimension name · point total · percentage · horizontal bar.
- Only dimensions with at least one positive point entry for the current week are displayed.
- Dimensions appear in canonical order: Body → Mind → Spirit → Connections → Responsibilities.

---

## 6. Log Activity Screen

Accessible via the persistent "+" button from any screen.

### 6.1 Quick Pick List
- Displays the user's most frequently logged activities as quick-select buttons or tiles (top 5–10 by frequency).
- Tapping a quick pick pre-selects that activity and populates the form fields with its default values.

### 6.2 Activity Selection
- Below the quick pick list, a **searchable autocomplete dropdown** allows the user to search and select any activity from their full library.
- As the user types (e.g., "hi"), matching activities filter and display (e.g., "Hiking (Solo)", "Hiking (With Others)").

### 6.3 Form Fields
| Field | Description |
|---|---|
| Activity | Selected from quick pick or autocomplete dropdown (required) |
| Description | Read-only; shown only when the selected activity has a description |
| Dimensions | Pre-selected from the chosen activity's default dimensions; user can add/remove dimensions for this specific entry before saving |
| Date | Defaults to today's date; user can change it |
| Time | Defaults to current time; user can change it |
| Points | Pre-populated with the activity's default point value; user can adjust up or down using a **spinner control** |
| Notes | Optional rich text field for personal context, observations, or journaling. Supports bold, italic, underline, and bullet lists. Stored as sanitized HTML. No character limit shown in UI; API validates up to 10,000 characters. |

### 6.4 Submission
- User submits the form to log the activity.
- A success confirmation (toast) confirms the activity was logged.
- **Upon successfully logging a *new* activity, the user is taken to the Check-In form** (post-activity Check-In flow — see Section 11.2) with that activity pre-associated. The user may save a linked check-in or skip; either way they then land on the **Home / Score Summary** screen (see Section 5).
- **Editing an existing log entry** does not trigger the Check-In flow — the user returns to the View Log screen.

### 6.5 Score Summary After Logging
- After completing (or skipping) the post-activity check-in, the user lands on the **Home / Score Summary** screen, which shows today's, this week's, and this month's running point totals and the day's logged activities (see Section 5).
- The persistent "+" button remains available to log another activity.

---

## 7. Activity Detail View

Accessible by clicking on a score total (Today, This Week, This Month) from the Landing Page.

### 7.1 Activity List
- Displays all activities logged within the selected time period (day, week, or month).
- Activities are listed in **chronological order**, earliest at the top, most recent at the bottom.
- Each entry displays:
  - Activity name
  - Dimensions (displayed as color-coded dots and labels matching the entry's saved dimension snapshot)
  - Points earned (the adjusted value at time of logging, not the default)
  - Date and time logged

#### Details toggle
- A **Details** toggle appears on the summary line whenever there is at least one displayed entry. (Renamed from "Notes" in CHK-002 Phase 6A; it now reveals notes **and** linked check-ins.)
- The toggle defaults **OFF**. When OFF, entries appear compact — no notes, check-ins, or extra rows.
- When **ON**, each entry shows a details section containing:
  - its full formatted note (bold, italic, underline, paragraphs, bullet lists), if present;
  - its **linked check-ins**, if any — each showing the check-in's local date/time and Body / Energy / Mood scores;
  - a **"+ Add Check-In"** action that starts a check-in linked to that log entry.
- **Editing a linked check-in:** clicking a check-in row opens it for editing on the Check-Ins screen.
- **Deleting a linked check-in:** a trash → confirm/cancel control deletes only that check-in; the log entry itself is unaffected.
- **Returning to context:** adding or editing a check-in from View Log returns the user to the **same View Log view** (same period, Details still expanded) after Save, Skip, or Cancel.
- Entries with no note and no check-ins still show the "+ Add Check-In" action when Details is ON, and are otherwise unchanged.

### 7.2 Editing a Log Entry
- Each activity entry is **clickable/editable**.
- Clicking an entry opens an edit form pre-populated with the logged values.
- The user can modify the activity, date, time, points, notes, and **dimensions**.
- When editing, the dimension selector loads the entry's saved dimension snapshot (not the activity's current defaults).
- Dimension changes apply only to that specific log entry — the parent activity's default dimensions are not affected.
- Changes are saved and reflected immediately in score totals.

### 7.3 Deleting a Log Entry
- Users can delete a log entry from the edit form.
- A confirmation prompt is shown before deletion.
- Deletion permanently removes the log entry and updates all score totals accordingly.

---

## 8. Reports — Reporting & Analytics

The Reports section contains two distinct sub-pages accessible via the Reports nav group: **Trends** and **Balance**. Each page focuses on a specific analytical perspective.

---

### 8.1 Trends Page (`/reports`)

Focused on time-series patterns: how scores move day-over-day, week-over-week, and month-over-month.

#### 8.1.1 Time Aggregation Selector
The user selects one of the following aggregation levels:
- **Daily** — daily totals for the past 14 days
- **Weekly** — weekly totals for the past 8 weeks
- **Monthly** — monthly totals for the past 6 months

#### 8.1.2 Dimension Filter
The user can filter the chart by dimension (Body, Mind, Spirit, Connections, Responsibilities, or **All**). When a specific dimension is selected, only points from activities in that dimension are shown, using that dimension's color.

#### 8.1.3 Bar Chart
- Stacked bar chart displaying point totals over time for the selected aggregation and category filter.
- X-axis: time period labels (day, week number, or month abbreviation).
- Y-axis: point totals with gridlines at 25% intervals.
- Value labels displayed inside bars (white) if tall enough, above (muted) if short.
- Period improvement pill (↑ or ↓ percentage vs. previous period) shown in the header.

#### 8.1.4 Dimension Trend
A sparkline panel showing each dimension's weekly point trend over the last 8 weeks. Displayed alongside the Top days/periods card.

- One row per dimension (Body → Mind → Spirit → Connections → Responsibilities)
- Each row shows: color dot · dimension name · sparkline area chart · 8-week total

#### 8.1.5 Top Days / Top Periods
The five highest-scoring periods for the selected aggregation level.
- **Daily view ("Top days"):** Date displayed as `MMM d, yyyy` (e.g., "May 15, 2026") for unambiguous context.
- **Weekly view ("Top periods"):** ISO week number with year (e.g., "W21 2026").
- **Monthly view ("Top periods"):** Month abbreviation with year (e.g., "May 2026").
- Progress bar showing score relative to the top scorer.

---

### 8.2 Balance Page (`/reports/balance`)

Focused on dimension distribution: how balanced the user's activity is across the five wellness dimensions for a chosen period.

#### 8.2.1 Period Selector
Dropdown (presented as a styled pill): **This Week**, **This Month**, **This Year**.

#### 8.2.2 Donut Chart + Dimension List
- Donut ring chart showing each dimension's proportional share of total points for the selected period.
- Dimension list alongside: color dot · dimension name · point total · percentage · horizontal bar.

#### 8.2.3 Insight Callout
A coaching callout highlighting which dimension is dominating and suggesting which underrepresented dimensions to add to bring the balance closer to even.

#### 8.2.4 Best & Worst Days
The two best-scoring days and the single worst-scoring day within the selected period.
- Each row shows: best/worst badge · day name · date (`MM/dd/yyyy`) · top activity · point total.
- Format example: **Tuesday · 05/26/2026**
- Date is shown as subtle secondary text alongside the day name and is **always visible** — it is never hidden at any screen size or breakpoint (may be rendered at a slightly smaller font size on very small phones).

---

### 8.3 Data Scope
- Data shown is scoped to the logged-in user only.
- If the user has been active for less than the maximum period, only available data is shown.

---

## 9. Manage Activities — Data Management Screen

A dedicated section for managing the user's activity library.

### 9.1 Activity List View
- Displays all active activities in the user's library.
- Each activity shows:
  - Activity name
  - Dimensions (color-coded chips)
  - Default point value
  - Active/archived status
- Activities can be sorted by clicking column headers (name, dimensions, default points) and searched by name.

### 9.2 Add New Activity
- A form to create a new activity with the following fields:

| Field | Description |
|---|---|
| Name | Text field (e.g., "Hiking (Solo)") |
| Description | Optional multi-line text field describing the activity |
| Dimensions | Multi-select color-coded chips, one per dimension; at least one must be selected |
| Default Point Value | Integer input; can be positive (beneficial) or negative (detrimental) |

### 9.3 Edit Existing Activity
- Clicking an activity opens an edit form pre-populated with existing values.
- All fields (name, description, dimensions, point value) are editable.
- Changes apply to future log entries only; historical logs retain their recorded point values.

### 9.4 Delete / Archive Activity
When a user attempts to delete an activity, the system checks for existing log entries associated with that activity:

**If no log entries exist:**
- The activity is permanently deleted with no further prompts.

**If log entries exist:**
- The user is presented with two options:
  - **Hide from future logging** — The activity is archived (soft-deleted). It no longer appears in the activity picker or quick pick list but all historical log entries and their points are preserved.
  - **Delete this activity and all associated history** — The activity and all log entries linked to it are permanently deleted. Score totals are recalculated accordingly. A clear warning is shown before this action is confirmed.

---

## 10. Boilerplate Starter Activities

When a new user registers, the following starter activities are automatically added to their library with default point values. Users can edit, archive, or delete these at any time.

| Activity | Dimensions | Default Points |
|---|---|---|
| Exercise / Gym | Body | +15 |
| Hiking | Body | +10 |
| Meditation | Mind, Spirit | +10 |
| Reading/Learning | Mind | +10 |
| Journaling | Mind, Spirit | +10 |
| Cooking a Healthy Meal | Body, Responsibilities | +10 |
| Cleaning / Organizing | Responsibilities | +5 |
| Socializing | Connections | +10 |
| Calling Family | Connections | +5 |
| Travel | Mind, Spirit | +10 |
| Watching Excessive TV | Mind | -5 |
| Skipping Sleep | Body, Mind | -5 |
| Alcohol / Drinking | Body, Mind | -5 |

---

## 11. Check-In Screen

The Check-In screen (`/check-in`) captures the user's **current state** (how they feel), as distinct from Activity Logs which capture behaviors. Check-Ins are intentionally lightweight.

### 11.1 Standalone Check-In Form (CHK-002 Phase 3 — implemented)
- Reachable from the persistent **Check In** top action button (`/check-in`).
- Records three metrics — **Body**, **Energy**, **Mood** — each on a **−5 to +5** scale where **0 = baseline / normal**.
- A **date and time** field captures when the check-in applies; defaults to now and is user-editable (with **Today** / **Now** shortcuts).
- **Smart defaults:** when the form opens, the three scores preload from the user's most recent Check-In; if the user has no prior Check-In, all three default to 0.
- Scores are adjusted with bounded +/− steppers and cannot exceed the −5…+5 range.
- **No notes field** in this version — capture stays minimal.
- On save, a success toast confirms the check-in. The entered scores are retained (the natural starting point for the next check-in) and the timestamp resets to now; the user remains on the page.
- Saved Check-Ins from this screen are **standalone** (not linked to any activity log).

### 11.2 Post-Activity Check-In Flow (CHK-002 Phase 4 — implemented)
- After a user successfully logs a **new** activity, the Check-In form opens automatically with that activity pre-associated.
- The form shows an **"After: {activity}"** context label so the user knows the check-in will link to what they just logged.
- The form behaves the same as the standalone form (same metrics, scale, date/time, and smart defaults), with two differences:
  - A **Skip** option is available. Skipping creates **no** Check-In.
  - After **Save** or **Skip**, the user is taken to the **Home / Score Summary** screen.
- A saved check-in from this flow is **linked** to the activity log entry; a skipped one creates nothing.

### 11.3 Check-Ins History (CHK-002 Phase 5B — implemented)
- A **Check Ins** left-nav item routes to the Check-In history screen (`/check-ins`).
- Lists the user's check-ins **newest first** (by the actual check-in instant), each showing the date/time — in the user's **local browser time** — and the **Body**, **Energy**, and **Mood** scores.
- A check-in linked to an activity shows **"After: {activity name}"**; standalone check-ins show no extra label.
- **Edit:** the user can edit a check-in's date/time and Body/Energy/Mood scores inline. The activity link cannot be changed from this screen.
- **Delete:** the user can delete any of their check-ins via a trash → confirm/cancel control. Deleting a check-in does **not** delete the associated activity log.
- An empty state is shown when the user has no check-ins.

### 11.4 View Log Details Integration (CHK-002 Phase 6A — implemented)
- The View Log **Details** toggle (§7.1) surfaces each entry's linked check-ins and a "+ Add Check-In" action, in addition to notes.
- Adding a check-in from a log entry links it to that entry; editing/deleting is available inline. See §7.1 for behavior.

### 11.5 Not yet implemented
- Edit Log Entry screen: associated check-in list with "add follow-up".
- Body/Energy/Mood reporting and correlation.

---

## 12. Settings Screen

### 12.1 Profile
- **Display Name** — editable text field; used for the personalized welcome greeting throughout the app
- **Email Address** — read-only; displayed for reference but cannot be changed after registration
- **Password** — *(planned for future release)* change password with current password confirmation

### 12.2 Appearance
- The application uses **permanent dark mode**. There is no theme toggle — light mode is not available.

---

## 13. General UX Requirements

- The application is **responsive** and usable on desktop and tablet browsers.
- **Color coding** for dimensions is consistent across all screens — charts, activity lists, dimension chips, and form elements all use the same color per dimension.
- **Friendly error messages** are shown to users when something goes wrong — no technical stack traces visible to the user.
- All data is **scoped to the logged-in user** — no user can see another user's activities, logs, or scores.
- The application supports **multiple user accounts** — architecture is multi-user from the ground up.

---

*Momentum — Functional Requirements Document*
*Version 1.11 — CHK-002 Phase 4: §6.4/§6.5 updated for the post-activity Check-In flow (new logs route to the Check-In form, then Home); §11.2 adds the post-activity flow with Save/Skip*
*Version 1.12 — CHK-002 Phase 5A: §3 nav restructured (persistent top "Check In" button + "Check Ins" history nav, mobile "Manage" title); §11.1 entry point updated; §11.3 Check-Ins history placeholder added*
*Version 1.13 — CHK-002 Phase 5B: §11.3 Check-Ins history screen implemented (newest-first list, "After: {activity}" for linked, inline edit, trash→confirm delete)*
*Version 1.14 — CHK-002 Phase 5B fix: §11.3 clarifies check-in times display in the user's local browser time, ordered by the true check-in instant*
*Version 1.15 — CHK-002 Phase 6A: §7.1 "Show Notes" toggle renamed to "Details" (now reveals notes + linked check-ins + "+ Add Check-In", with inline edit/delete); §11.4 View Log integration marked implemented*
*Version 1.16 — CHK-002 Phase 6A polish: §7.1 add/edit check-in from View Log returns to the same View Log context (period + Details expanded) after save/skip/cancel*
