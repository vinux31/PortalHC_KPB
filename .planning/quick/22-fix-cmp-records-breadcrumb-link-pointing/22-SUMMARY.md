# Quick Task 22: Fix CMP Records breadcrumb link

## What Changed
- `Views/CMP/Records.cshtml` line 26: `href="/CMP"` → `@Url.Action("Index", "CMP")`
- `Views/CMP/RecordsWorkerDetail.cshtml` line 25: same fix

## Root Cause
Hardcoded `/CMP` path doesn't resolve to CMP/Index. Other CMP views already use `@Url.Action("Index", "CMP")` correctly.

## Commit
`3cb34b6` — fix: CMP Records breadcrumb link pointing to error page
