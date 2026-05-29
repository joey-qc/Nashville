-- ============================================================
-- Momentum v2 Dimension Model — Pre-Migration Snapshot Queries
-- Run against production Azure SQL BEFORE applying V2_DimensionModel migration
-- Record results in Docs/migration-snapshots/README.md
-- ============================================================


-- ── SECTION 1: Row counts ──────────────────────────────────────────────────
-- Run all four. Copy the output numbers into the README checklist.

SELECT 'Activities'         AS [Table], COUNT(*) AS [RowCount] FROM Activities;
SELECT 'ActivityCategories' AS [Table], COUNT(*) AS [RowCount] FROM ActivityCategories;
SELECT 'ActivityLogs'       AS [Table], COUNT(*) AS [RowCount] FROM ActivityLogs;

-- Expected post-migration row count in ActivityLogEntryDimensions:
SELECT COUNT(*) AS [ExpectedBackfillRows]
FROM   ActivityLogs al
JOIN   ActivityCategories ac ON ac.ActivityId = al.ActivityId;


-- ── SECTION 2: Full table exports ─────────────────────────────────────────
-- Use Azure Data Studio / SSMS "Export to CSV" on each result set.
-- Save as: activities_snapshot.csv, activity_categories_snapshot.csv, activity_logs_snapshot.csv

SELECT
    Id,
    UserId,
    Name,
    Description,
    DefaultPoints,
    IsArchived,
    CreatedAt,
    UpdatedAt
FROM Activities
ORDER BY Id;

SELECT
    ActivityId,
    CategoryId
FROM ActivityCategories
ORDER BY ActivityId, CategoryId;

SELECT
    Id,
    UserId,
    ActivityId,
    LoggedAt,
    PointsRecorded,
    Notes,
    CreatedAt
FROM ActivityLogs
ORDER BY Id;


-- ── SECTION 3: Category assignments per activity (human-readable) ──────────
-- Useful for spot-checking that dimension assignments look correct.

SELECT
    a.Id          AS ActivityId,
    a.Name        AS ActivityName,
    c.Id          AS CategoryId,
    c.Name        AS CategoryName,
    c.ColorHex
FROM Activities a
JOIN ActivityCategories ac ON ac.ActivityId = a.Id
JOIN Categories          c  ON c.Id = ac.CategoryId
ORDER BY a.Name, c.Id;


-- ── SECTION 4: Spot-check template (fill in IDs before running post-migration) ──
-- 1. Sample 5 ActivityLog IDs from the Section 2 export.
-- 2. Paste them into the IN clause below.
-- 3. Save this output — use it post-migration to verify correct backfill.

SELECT
    al.Id            AS ActivityLogId,
    al.ActivityId,
    al.LoggedAt,
    al.PointsRecorded,
    ac.CategoryId    AS ExpectedDimensionId,
    c.Name           AS ExpectedDimensionName
FROM ActivityLogs al
JOIN ActivityCategories ac ON ac.ActivityId = al.ActivityId
JOIN Categories          c  ON c.Id = ac.CategoryId
WHERE al.Id IN (
    /* PASTE 5 SAMPLE ActivityLog IDs HERE — e.g.: 1, 5, 12, 23, 47 */
)
ORDER BY al.Id, ac.CategoryId;


-- ── SECTION 5: Post-migration verification queries ─────────────────────────
-- Run AFTER migration to confirm correctness.
-- (Keep here for reference — do not run pre-migration.)

/*
-- 5a. ActivityLogs row count must match pre-migration count exactly:
SELECT COUNT(*) AS [ActivityLogs_PostMigration] FROM ActivityLogs;

-- 5b. ActivityLogEntryDimensions count must match ExpectedBackfillRows:
SELECT COUNT(*) AS [ActivityLogEntryDimensions_PostMigration] FROM ActivityLogEntryDimensions;

-- 5c. Spot-check: verify 5 sampled log entries have correct dimensions:
SELECT
    al.Id            AS ActivityLogId,
    al.ActivityId,
    al.LoggedAt,
    led.DimensionId  AS ActualDimensionId,
    d.Name           AS ActualDimensionName
FROM ActivityLogs al
JOIN ActivityLogEntryDimensions led ON led.ActivityLogId = al.Id
JOIN Dimensions                  d  ON d.Id = led.DimensionId
WHERE al.Id IN (
    /* SAME 5 SAMPLE IDs FROM SECTION 4 */
)
ORDER BY al.Id, led.DimensionId;

-- Compare this output against the Section 4 pre-migration output.
-- ExpectedDimensionId (pre) must match ActualDimensionId (post) for each row.
*/
