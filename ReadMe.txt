Project Nashville.
The Momentum solution
My first attempt at using Claude to generate a full n-tier solution.

to start locally:
Start the API in Terminal 1: dotnet run --project Momentum.API
Start the WASM app in Terminal 2: dotnet run --project Momentum.Client

- or -

dotnet watch --project Momentum.API
dotnet watch --project Momentum.Client

----------------

To commit and (optionally) push
git add .
git commit -m "Moved a few reports around and fixed mobile nav"
git push

----------------

v2 Schema is live (2026-05-29)
Migration: 20260529151638_V2_DimensionModel
- Categories → Dimensions
- ActivityCategories → ActivityDimensions
- ActivityLogEntryDimensions table added (point-in-time snapshot per log entry)
- Commit: 79a81b5

----------------

Known issues (next up):
- KI-013: Daily log uses wrong local day due to UTC/local timezone mismatch.
  When I try to add an entry after 9pm, the log is empty as though I have started a new day 3 hours early.
  Balance page and View Log show different totals for the same day.

- The View Log page (planned):
    - When filter: "Day" is selected, make the date picker control so that I can customize the day of activities shown
    - When "Week", "Month" or "Year" selected... group by report. (date picker control hidden)
