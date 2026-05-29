Project Nashville. 
The Momentum solution
My first attempt at uding Claude to generate a full n-tier solution. 

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


Next Prompts:
- When I try to add an entry after 9pm, the log is empty as though I have started a new day 3 hours early. 
- The view log page
    - When fiter: "Day" is selected, make the date picker control so that I can customize the day of activities shown
    - When "Week", "Month" or "Year" selected... group by report. (date picker control hidden)