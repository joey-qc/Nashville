# Pre-Migration Snapshots — Momentum v2 Dimension Model

This folder holds query artifacts for the **v2 Dimension Model migration**
(`V2_DimensionModel` EF migration). The SQL files are safe to commit. CSV/JSON/XLSX
exports of actual table data are gitignored — store them locally only.

---

## Purpose

These snapshots exist for **validation and recovery confidence**, not as the
rollback mechanism. The authoritative rollback is the Azure SQL full backup taken
before the migration runs.

Use these snapshots to:
- Verify no `ActivityLog` rows were lost after migration
- Confirm the `ActivityLogEntryDimensions` backfill count matches expectations
- Spot-check that individual log entries received the correct dimension assignments

---

## Before You Migrate — Checklist

1. **Take Azure SQL backup** via the Azure Portal. Record the backup timestamp here:
   ```
   Azure backup timestamp: ___________________________
   ```

2. **Run `00_pre_migration_queries.sql`** against the production database in Azure
   Data Studio or SQL Server Management Studio.

3. **Export results to CSV** using your tool's export feature. Save files here:
   ```
   Docs/migration-snapshots/activities_snapshot.csv
   Docs/migration-snapshots/activity_categories_snapshot.csv
   Docs/migration-snapshots/activity_logs_snapshot.csv
   ```
   These files are gitignored and will not be committed.

4. **Record the row counts below** (copy from the query output):
   ```
   Activities row count         : 29
   ActivityCategories row count : 44
   ActivityLogs row count       : 54
   Expected backfill rows       : 79   (= ActivityLogs × ActivityCategories JOIN)
   Snapshot captured at         : 2026-05-29
   ```

5. **Sample 5 ActivityLog IDs** from the snapshot for post-migration spot-checking.
   Paste them into the placeholder in `00_pre_migration_queries.sql` Section 4.
   ```
   Sample log IDs: ___, ___, ___, ___, ___
   ```

---

## After Migration — Verification Steps

1. `SELECT COUNT(*) FROM ActivityLogs` must equal the pre-migration count above.
2. `SELECT COUNT(*) FROM ActivityLogEntryDimensions` must equal the expected backfill rows above.
3. Run the spot-check query in `00_pre_migration_queries.sql` Section 4 (with your
   sampled IDs substituted) and compare to the pre-migration snapshot.

---

## Files in This Folder

| File | Committed? | Description |
|---|---|---|
| `README.md` | ✅ Yes | This file |
| `00_pre_migration_queries.sql` | ✅ Yes | Ready-to-run SQL — no production data |
| `*.csv` | ❌ No (gitignored) | Actual table exports — may contain user data |
| `*.json` | ❌ No (gitignored) | Alternate export format |
| `*.xlsx` | ❌ No (gitignored) | Alternate export format |
