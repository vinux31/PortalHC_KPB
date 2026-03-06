# Quick Task 19: Move Search & Filter Bar inside My Records tab

## What Changed
- Moved the Search & Filter Bar (search input + year dropdown + reset button) from above the tab navigation into the `#pane-myrecords` tab pane in `Views/CMP/Records.cshtml`

## Why
- The filter bar was positioned above the tabs, making it visible on both "My Records" and "Team View" tabs
- Since the filter only operates on My Records data, it should only appear in that tab
- Team View has its own separate filter controls in `RecordsTeam.cshtml`

## Files Modified
- `Views/CMP/Records.cshtml` — moved Search & Filter Bar div inside My Records tab pane
