---
phase: 63-data-source-fix
plan: 01
subsystem: CDPController
tags: [backend, proton-progress, ajax, role-based-access, data-source-fix]
dependency_graph:
  requires: []
  provides: [CDPController.ProtonProgress, CDPController.GetCoacheeDeliverables]
  affects: [Views/CDP/ProtonProgress.cshtml (Plan 02)]
tech_stack:
  added: []
  patterns: [EF Include chain, ResponseCache no-store, role-based access control, anonymous JSON objects]
key_files:
  created: []
  modified:
    - Controllers/CDPController.cs
decisions:
  - Progress() action disabled (redirects to Index) instead of redirecting to ProtonProgress — per CONTEXT.md cut-over strategy
  - Coach coachee list ordered by ProtonTrack.Urutan then FullName — consistent with CONTEXT.md coachee list ordering decision
  - Approval status derived from ProtonDeliverableProgress.Status (Approved/Rejected/Submitted/Active) — Claude's discretion per CONTEXT.md
  - GetCoacheeDeliverables returns error JSON for unauthorized rather than HTTP 403 — prevents information leakage via status codes
metrics:
  duration_seconds: 164
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_modified: 1
requirements_satisfied: [DATA-01, DATA-02, DATA-03, DATA-04]
---

# Phase 63 Plan 01: CDPController Backend — ProtonProgress and GetCoacheeDeliverables Summary

CDPController updated with ProtonDeliverableProgress-based ProtonProgress GET action, real CoachCoacheeMapping-driven coachee list, and AJAX GetCoacheeDeliverables JSON endpoint replacing the IdpItems-based legacy Progress action.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Create ProtonProgress GET action with role-based coachee loading and summary stats | cd15f71 | Done |
| 2 | Create GetCoacheeDeliverables JSON endpoint for AJAX coachee switching | aa33f8a | Done |

## What Was Built

### Task 1 — ProtonProgress GET Action (cd15f71)

Added `[ResponseCache(Duration=0, NoStore=true)] public async Task<IActionResult> ProtonProgress(string? coacheeId = null)` to CDPController:

- **Role-based coachee list:**
  - Level 6 (Coachee): `targetCoacheeId = user.Id`, no dropdown
  - Level 5 (Coach): queries `CoachCoacheeMappings` for real coachee list, ordered by `ProtonTrack.Urutan` then `FullName`
  - Level 4 (SrSpv/SectionHead): queries users where `u.Section == user.Section && u.RoleLevel == 6`
  - Level 1-2 (HC/Admin): queries all users where `RoleLevel == 6`

- **ProtonDeliverableProgress query** with full `Include().ThenInclude()` chain (ProtonDeliverable → ProtonSubKompetensi → ProtonKompetensi), ordered by Urutan fields

- **TrackingItem mapping** from ProtonDeliverableProgress (not IdpItems):
  - `EvidenceStatus`: "Uploaded" if `EvidencePath != null`, else "Pending"
  - `ApprovalSrSpv/SectionHead`: derived from `Status` (Approved/Rejected/Submitted → Not Started)
  - `ApprovalHC`: "Approved" if `HCApprovalStatus == "Reviewed"`, else "Pending"

- **Summary stats (DATA-03):**
  - `progressPercent = (int)(sum(Approved=1.0, Submitted=0.5) / total * 100)`
  - `pendingActions = count(Status == "Active" || Status == "Rejected")`
  - `pendingApprovals = count(Status == "Submitted")`

- **Track label** from `ProtonTrackAssignments` — e.g., "Panelman Tahun 2"

- **Error messages**: `ViewBag.NoTrackMessage` when no track assignment; `ViewBag.NoProgressMessage` when track exists but no progress records

- **Old Progress() disabled**: `public IActionResult Progress() => RedirectToAction("Index")`

### Task 2 — GetCoacheeDeliverables JSON Endpoint (aa33f8a)

Added `[HttpGet][ResponseCache(Duration=0, NoStore=true)] public async Task<IActionResult> GetCoacheeDeliverables(string coacheeId)`:

- **Access control validation (Pitfall 4 prevention):**
  - Level 6: silently redirects `coacheeId` to `user.Id`
  - Level 5: `CoachCoacheeMappings.AnyAsync(...)` — returns `{error: "unauthorized"}` JSON on failure
  - Level 4: validates `coacheeUser.Section == user.Section` — returns `{error: "unauthorized"}` JSON on failure
  - Level 1-2: allow all

- **Same query + mapping** as ProtonProgress but returns anonymous camelCase JSON objects

- **JSON response structure:**
  ```json
  {
    "items": [...],
    "stats": { "progressPercent": 0, "pendingActions": 0, "pendingApprovals": 0 },
    "trackLabel": "Panelman Tahun 2",
    "coacheeName": "Ahmad Fauzi",
    "noTrack": false,
    "noProgress": false
  }
  ```

## Verification Results

- Build: 0 errors, 31 warnings (all pre-existing, none from new code)
- `ProtonProgress` at line 1413 with `[ResponseCache(Duration=0, NoStore=true)]` at line 1412
- `GetCoacheeDeliverables` at line 1579 with `[ResponseCache(Duration=0, NoStore=true)]` at line 1578
- `Progress()` at line 1575 returns `RedirectToAction("Index")`
- No `IdpItems` reference in new actions
- `CoachCoacheeMappings` queried for Coach role (no mock data)
- Stats formula: Approved=1.0, Submitted=0.5, others=0.0

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- `Controllers/CDPController.cs`: FOUND (modified)
- commit `cd15f71`: FOUND (Task 1)
- commit `aa33f8a`: FOUND (Task 2)
- Build: 0 errors
