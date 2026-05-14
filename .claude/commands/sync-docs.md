# /sync-docs — Keep Momentum Documentation Current

After completing any set of code changes, review and update the three Momentum documentation files to reflect what was actually built. Run this command at the end of every work session or after any non-trivial change.

## Files to Review and Update

1. **`CLAUDE.md`** (project root) — Architecture decisions, technology rules, naming conventions, business rules, seed data. Update when:
   - A technology version changes (e.g., .NET, MudBlazor, ApexCharts)
   - A new architectural pattern or rule is established
   - A business rule changes (deletion logic, seeding logic, auth behavior)
   - A new service, layer, or caching pattern is introduced
   - Naming conventions change

2. **`Docs/momentum-software-specifications.md`** — Technical architecture, data models, API endpoints, project structure. Update when:
   - A new entity, DTO, or enum is added, changed, or removed
   - API endpoints are added, renamed, or their query parameters change
   - Project structure changes (new project, renamed project)
   - Dependencies change (new NuGet packages, removed packages)
   - Authentication or authorization approach changes

3. **`Docs/momentum-functional-requirements.md`** — User-facing behavior descriptions. Update when:
   - UI behavior changes (e.g., checkboxes → chips, new sort options)
   - New screens or features are added
   - Existing feature behavior changes
   - Seed data values change

## How to Sync

1. Read the three files above.
2. Compare their content against what was just built — check entities, DTOs, endpoints, UI behavior, and rules.
3. Edit only the sections that are stale. Do not rewrite sections that are still accurate.
4. Update the version line at the bottom of each file you change.
5. Keep edits minimal and precise — document what exists, not what might exist.

## What NOT to Document

- Implementation details already clear from the code (method bodies, EF configuration details)
- Git history or which PR introduced a change
- Temporary states or work-in-progress
- Hypothetical future features (those belong in Section 13 of the spec)
