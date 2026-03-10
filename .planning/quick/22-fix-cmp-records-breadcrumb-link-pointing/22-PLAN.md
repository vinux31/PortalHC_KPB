# Quick Task 22: Fix CMP Records breadcrumb link

## Task
Fix breadcrumb "CMP" link on Records page pointing to error page instead of CMP Index.

## Root Cause
`Views/CMP/Records.cshtml` line 26 uses hardcoded `href="/CMP"` instead of `@Url.Action("Index", "CMP")`.
Same issue exists in `Views/CMP/RecordsWorkerDetail.cshtml` line 25.

## Tasks

### Task 1: Fix breadcrumb links in CMP Records views
- **files:** `Views/CMP/Records.cshtml`, `Views/CMP/RecordsWorkerDetail.cshtml`
- **action:** Replace `href="/CMP"` with `href="@Url.Action("Index", "CMP")"` in breadcrumb
- **verify:** Build succeeds, breadcrumb navigates to CMP Index
- **done:** Both views use Url.Action for CMP breadcrumb link
