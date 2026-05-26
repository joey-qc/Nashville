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
- Navigation items:
  - **Home** (Score Summary / Landing Page)
  - **Add Entry** (also accessible via the persistent plus button)
  - **View Log** (Activity Detail View — browsable history by period)
  - **Reports** (Reporting & Analytics — contains Trends and Balance sub-pages)
  - **Manage Activities** (Data Management)
  - **Settings**
- A **persistent "+" (Add Entry) button** is visible on every authenticated screen, allowing the user to quickly navigate to the Add Entry screen at any time.

---

## 4. Category Definitions & Color Coding

Activities belong to one or more of the following five categories. Category colors are consistent across all screens, charts, lists, checkboxes, and UI elements throughout the application.

| Category | Color |
|---|---|
| Physical | Bright Green |
| Mental | Sky Blue |
| Spiritual | Soft Purple |
| Social | Amber |
| Housekeeping | Salmon |
| All (used in reporting filters) | Medium Gray |

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
| Date | Defaults to today's date; user can change it |
| Time | Defaults to current time; user can change it |
| Points | Pre-populated with the activity's default point value; user can adjust up or down using a **spinner control** |
| Notes | Optional free-text field for additional context |

### 6.4 Submission
- User submits the form to log the activity.
- Upon successful submission, the user is directed to the **Post-Log Confirmation Screen** (see Section 6.5).

### 6.5 Post-Log Confirmation Screen
- Confirms the activity was successfully logged.
- Displays a summary of what has been logged today, including:
  - List of activities logged so far today
  - Today's running point total
  - This week's running point total
  - This month's running point total
- The persistent "+" button remains available to log another activity.

---

## 7. Activity Detail View

Accessible by clicking on a score total (Today, This Week, This Month) from the Landing Page.

### 7.1 Activity List
- Displays all activities logged within the selected time period (day, week, or month).
- Activities are listed in **chronological order**, earliest at the top, most recent at the bottom.
- Each entry displays:
  - Activity name
  - Category (displayed as a color-coded chip or badge matching the category color)
  - Points earned (the adjusted value at time of logging, not the default)
  - Date and time logged
  - Notes (if any)

### 7.2 Editing a Log Entry
- Each activity entry is **clickable/editable**.
- Clicking an entry opens an edit form pre-populated with the logged values.
- The user can modify the activity, date, time, points, and notes.
- Changes are saved and reflected immediately in score totals.

### 7.3 Deleting a Log Entry
- Users can delete a log entry from the edit form.
- A confirmation prompt is shown before deletion.
- Deletion permanently removes the log entry and updates all score totals accordingly.

---

## 8. Reports — Reporting & Analytics Screen

A single consolidated reporting screen with configurable filters.

### 8.1 Time Aggregation Selector
The user selects one of the following aggregation levels:
- **Day** — shows daily totals for the past 30 days (or since first entry if less than 30 days)
- **Week** — shows weekly totals for the past 52 weeks (or since first entry if less than 52 weeks)
- **Month** — shows monthly totals for the past 12 months (or since first entry if less than 12 months)

### 8.2 Category Filter
The user can filter the chart by category:
- Physical
- Mental
- Spiritual
- Social
- Housekeeping
- **All** (default — shows combined totals in medium gray)

When a specific category is selected, only points from activities in that category are shown, using that category's color.

### 8.3 Chart Display
- A **line chart** or **bar chart** displays point totals over time based on the selected aggregation and category filter.
- X-axis: time periods (days, weeks, or months)
- Y-axis: point totals
- Chart updates dynamically when the user changes the aggregation or category filter.

### 8.4 Balance — Category Breakdown Report

A dedicated sub-page under Reports showing how the user's points are distributed across the five wellness categories.

- **Period selector:** Week, Month, or Year
- **Chart:** Pie (or donut) chart showing each category's share of total points for the selected period
- **Data labels:** Category name, total points, and percentage of overall total
- Category colors match the global color scheme (Physical=green, Mental=blue, etc.)
- Data is scoped to the logged-in user and filtered to the selected time period

### 8.5 Data Scope
- Data shown is scoped to the logged-in user only.
- If the user has been active for less than the maximum period, only available data is shown.

---

## 9. Manage Activities — Data Management Screen

A dedicated section for managing the user's activity library.

### 9.1 Activity List View
- Displays all active activities in the user's library.
- Each activity shows:
  - Activity name
  - Categories (color-coded checkboxes or chips)
  - Default point value
  - Active/archived status
- Activities can be sorted by clicking column headers (name, categories, default points) and searched by name.

### 9.2 Add New Activity
- A form to create a new activity with the following fields:

| Field | Description |
|---|---|
| Name | Text field (e.g., "Hiking (Solo)") |
| Description | Optional multi-line text field describing the activity |
| Categories | Multi-select color-coded chips, one per category; at least one must be selected |
| Default Point Value | Integer input; can be positive (beneficial) or negative (detrimental) |

### 9.3 Edit Existing Activity
- Clicking an activity opens an edit form pre-populated with existing values.
- All fields (name, description, categories, point value) are editable.
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

| Activity | Categories | Default Points |
|---|---|---|
| Exercise / Gym | Physical | +8 |
| Hiking (Solo) | Physical | +8 |
| Hiking (With Others) | Physical, Social | +9 |
| Meditation | Mental, Spiritual | +7 |
| Reading (Nonfiction) | Mental | +5 |
| Journaling | Mental, Spiritual | +6 |
| Cooking a Healthy Meal | Physical, Housekeeping | +5 |
| Cleaning / Organizing | Housekeeping | +4 |
| Socializing with Friends | Social | +6 |
| Calling Family | Social | +5 |
| Travel (Solo) | Mental, Spiritual | +7 |
| Travel (With Others) | Mental, Spiritual, Social | +9 |
| Watching Excessive TV | Mental | -3 |
| Skipping Sleep | Physical, Mental | -6 |

---

## 11. Settings Screen

### 11.1 Profile
- **Display Name** — editable text field; used for the personalized welcome greeting throughout the app
- **Email Address** — read-only; displayed for reference but cannot be changed after registration
- **Password** — *(planned for future release)* change password with current password confirmation

### 11.2 Appearance
- The application uses **permanent dark mode**. There is no theme toggle — light mode is not available.

---

## 12. General UX Requirements

- The application is **responsive** and usable on desktop and tablet browsers.
- **Color coding** for categories is consistent across all screens — charts, activity lists, category chips, checkboxes, and form elements all use the same color per category.
- **Friendly error messages** are shown to users when something goes wrong — no technical stack traces visible to the user.
- All data is **scoped to the logged-in user** — no user can see another user's activities, logs, or scores.
- The application supports **multiple user accounts** — architecture is multi-user from the ground up.

---

*Momentum — Functional Requirements Document*
*Version 1.3 — Updated navigation label to "Add Entry"; updated category color names to new palette; replaced theme toggle with permanent dark mode note*
