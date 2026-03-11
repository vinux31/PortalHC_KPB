# CDP-04 Audit Report — CDP Dashboard Role-Scoped Metrics

**Phase:** 156-planidp-cdp-dashboard-audit
**Plan:** 156-02
**Audited files:**
- `Controllers/CDPController.cs` (lines 237–580)
- `Views/CDP/Dashboard.cshtml`
- `Views/CDP/Shared/_CoacheeDashboardPartial.cshtml`
- `Views/CDP/Shared/_CoachingProtonPartial.cshtml`
- `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml`
- `Models/UserRoles.cs`

---

## Summary

**Overall result: PASS** — No critical bugs or security vulnerabilities found. Three cosmetic/edge-case findings documented below. Phase 87-02 fixes confirmed still in place.

**Total findings: 3**
- Bug (critical): 0
- Security: 0
- Edge case: 2
- Cosmetic / code smell: 1

---

## Section 1: Dashboard Metrics (CDP-04)

### 1.1 BuildCoacheeSubModelAsync (lines 297–333)

**Metric accuracy:**

| Metric | Source | Verdict |
|--------|--------|---------|
| `TotalDeliverables` | `progresses.Count` (all ProtonDeliverableProgress for coachee) | PASS |
| `ApprovedDeliverables` | `progresses.Count(p => p.Status == "Approved")` | PASS |
| `ActiveDeliverables` | `progresses.Count(p => p.Status == "Pending")` | PASS — Phase 87-02 fix confirmed: "Pending" used (not non-existent "Active" status) |
| `CompetencyLevelGranted` | Most recent `ProtonFinalAssessment` ordered by `CreatedAt` desc | PASS |
| `CurrentStatus` | `"Completed"` if finalAssessment exists, else `"In Progress"` | PASS |

**Phase 87-02 fix verification:** Line 322 comment explicitly documents `"Pending"` for in-progress deliverables — fix is still in place.

**Edge case — coachee with no active assignment (line 305):** Returns empty `CoacheeDashboardSubModel` with all zero counts. View renders correctly with 0/0 deliverables, 0% progress bar, "Pending" status. No crash. PASS.

**Edge case — coachee with active assignment but deleted track (line 311):** Returns early with empty model. Safe. PASS.

**Edge case — no ProtonFinalAssessment:** `finalAssessment` is null → `CompetencyLevelGranted` is null → view renders "Pending" badge. PASS (line 42 of _CoacheeDashboardPartial.cshtml uses null-conditional: `Model.CompetencyLevelGranted.HasValue`).

**Divide-by-zero check (line 4 of _CoacheeDashboardPartial.cshtml):**
```razor
var progressPercent = Model.TotalDeliverables > 0
    ? (int)((double)Model.ApprovedDeliverables / Model.TotalDeliverables * 100)
    : 0;
```
Guard present. PASS.

---

### 1.2 BuildProtonProgressSubModelAsync (lines 339–572)

**Role branching:**

| Role(s) | Level | Branch | Scoping |
|---------|-------|--------|---------|
| HC, Admin | 1–2 | Full access | All active ProtonTrackAssignments |
| Direktur, VP, Manager | 3 | Full access (`HasFullAccess` = level ≤ 3) | Same as HC/Admin |
| SectionHead, SrSupervisor | 4 | Section access | Active assignments where user.Section matches |
| Coach, Supervisor | 5 | Coach branch | Active assignments for mapped coachees only |
| Coachee | 6 | Not reached (excluded at line 248) | N/A |

**Phase 87-02 fix verification:** `IsActive` filter present at lines 349, 360, 376 — active-only filters confirmed.

**HC with no coachees:** `scopedCoacheeIds` = empty list → `coacheeUsers` = empty → all stats zero → `CoacheeRows` = empty list → view shows "No coachees found in your scope." alert (line 201 of _CoachingProtonContentPartial.cshtml). PASS.

**Coach with no active mappings:** `mappedCoacheeIds` = empty → `scopedCoacheeIds` = empty → same empty-state path. PASS.

**Trend chart with no completed assessments:** `scopedCompletedAssessments.Any()` = false → no chart rendered → info alert shown (line 72 of _CoachingProtonContentPartial.cshtml). PASS.

**Doughnut chart with no deliverable data:** `Model.StatusData.Any(d => d > 0)` = false → info alert shown (line 93). PASS.

---

## Section 2: Role Scoping

### 2.1 Dashboard() (lines 237–260)

Role is resolved at line 243 via `GetRolesAsync` (not from claims cache) — reads from DB, always current. PASS.

`isLiteralCoachee` check at line 248 short-circuits before ProtonProgress branch — Coachee never sees team data in Dashboard(). PASS.

**Finding F-1 (cosmetic): Direktur/VP/Manager not explicitly documented in coach scoping**

- **Severity:** Cosmetic
- **File:line:** CDPController.cs:347
- **Detail:** Direktur/VP/Manager (level 3) falls into `HC/Admin` branch via `HasFullAccess(level ≤ 3)`. These roles do see all-sections data, which is correct for executives. The comment at line 337 says "HC/Admin=all" but the code implements "HC/Admin/Direktur/VP/Manager=all". No data leakage, but comment is misleading.
- **Fix:** Update the comment to accurately reflect all full-access roles.
- **Status:** Auto-fixed (Rule 1 — comment is part of code correctness documentation).

---

### 2.2 FilterCoachingProton() (lines 265–280)

Server-side role override confirmed at lines 274–276:
```csharp
if (UserRoles.HasSectionAccess(roleLevel)) { section = user.Section; }
else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }
```

**Attempted attack scenario — Coach sends `bagian=OtherSection`:**
The parameter is named `section` (not `bagian`). Even if a Coach sends `section=OtherSection`, line 276 overwrites it with `user.Section` before the query. Server enforces scope regardless of client input. PASS — no data leakage.

**Finding F-2 (edge case): Coachee can call FilterCoachingProton**

- **Severity:** Edge case (not security — no data leakage)
- **File:line:** CDPController.cs:265–280
- **Detail:** `FilterCoachingProton` has no role restriction (class-level `[Authorize]` only requires authentication). A Coachee (level 6) calling this endpoint hits `IsCoachingRole(6)` → true → section/unit set from user's profile → `BuildProtonProgressSubModelAsync` runs Coach branch → `CoachCoacheeMappings` queried with coachee's Id as CoachId → returns empty (coachees have no coach mappings) → empty ProtonProgressSubModel returned. No data leakage, but the endpoint is accessible to Coachees unnecessarily.
- **Suggested fix:** Add `[Authorize(Roles = "HC,Admin,Direktur,VP,Manager,SectionHead,SrSupervisor,Coach,Supervisor")]` or return 403 for Coachee role. Alternatively, check `isLiteralCoachee` at the top of the action and return a 403.
- **Status:** Not auto-fixed (cosmetic boundary — no actual data leakage, no crash, no current functional impact). Noted for future hardening.

---

### 2.3 GetCascadeOptions() (lines 285–292)

Returns units for a section from `OrganizationStructure.GetUnitsForSection`. No role restriction — but this is appropriate since it only returns organizational structure data (not coachee data). PASS.

**Cascade behavior:** `section=null/empty` → returns empty units list. Non-empty section → returns units. Correct. PASS.

---

## Section 3: AJAX Filtering

**Section filter (full-access roles only):** Line 391–392 adds in-memory section filter only when `HasFullAccess(roleLevel)` — restricted roles do not get this additional filter (their scope is already locked by scoped coachee ID list). PASS.

**Unit filter:** Line 393 applies to any role after scope is established. Since Coach/SectionHead already have a locked unit/section scope, the in-memory filter is redundant but harmless. PASS.

**Category/Track filter:** Lines 440–443 filter `assignments` list (not the base scoped list). PASS.

**displayCoacheeIds filtering (lines 462–465):** When category/track filter active, only shows coachees with matching assignments. Correct. PASS.

**`_lastScopeLabel` field (line 576):** Private instance field used to pass scope label from `BuildProtonProgressSubModelAsync` back to `Dashboard()`. Since ASP.NET Core creates a new controller instance per request (`AddControllersWithViews` = transient), there is no thread-safety concern between different users' requests. However, if `Dashboard()` called `BuildProtonProgressSubModelAsync` multiple times in a single request, the field could be overwritten. Currently only called once. PASS.

---

## Section 4: Edge Cases

**Finding F-3 (edge case): ProtonDeliverableProgress not filtered to active assignment only in BuildCoacheeSubModelAsync**

- **Severity:** Edge case
- **File:line:** CDPController.cs:315–317
- **Detail:** `BuildCoacheeSubModelAsync` queries progresses by `CoacheeId` only (not by active `ProtonTrackAssignmentId`). If a coachee had a prior inactive assignment with deliverables, those deliverables would be included in the counts.
  ```csharp
  var progresses = await _context.ProtonDeliverableProgresses
      .Where(p => p.CoacheeId == userId)
      .ToListAsync();
  ```
  The supervisor view (`BuildProtonProgressSubModelAsync`) correctly filters by active assignment IDs (line 408). The coachee view does not.
- **Suggested fix:** Join on the active assignment to get the correct assignment ID, then filter progresses by that ID — matching the supervisor view's approach.
- **Status:** Auto-fixed (Rule 1 — metrics mismatch between coachee and supervisor views for coachees with historical inactive assignments).

---

## Fixes Applied

### Fix 1: Update scope comment in BuildProtonProgressSubModelAsync

**File:** `Controllers/CDPController.cs`
**Change:** Updated comment at line 337 to include Direktur/VP/Manager in full-access roles.

### Fix 2: Scope ProtonDeliverableProgress to active assignment in BuildCoacheeSubModelAsync

**File:** `Controllers/CDPController.cs`
**Change:** Filter progresses by active assignment ID (matching supervisor view logic) to prevent stale deliverables from inactive assignments inflating coachee counts.

---

## Phase 87-02 Regression Verification

| Fix | Location | Status |
|-----|----------|--------|
| ActiveDeliverables uses "Pending" not "Active" | CDPController.cs:322 | CONFIRMED in place |
| IsActive filter on ProtonTrackAssignments | CDPController.cs:349,360,375 | CONFIRMED in place |
| Role-scoped AJAX override | CDPController.cs:274–276 | CONFIRMED in place |

---

*Audit completed: 2026-03-12*
*Auditor: Claude (execute-phase)*
