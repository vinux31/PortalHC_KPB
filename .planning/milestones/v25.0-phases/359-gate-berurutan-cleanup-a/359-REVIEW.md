---
phase: 359-gate-berurutan-cleanup-a
reviewed: 2026-06-10T00:00:00Z
depth: deep
files_reviewed: 11
files_reviewed_list:
  - Services/ProtonCompletionService.cs
  - Controllers/AssessmentAdminController.cs
  - Controllers/CoachMappingController.cs
  - Controllers/CDPController.cs
  - Models/CDPDashboardViewModel.cs
  - Models/HistoriProtonDetailViewModel.cs
  - Views/CDP/Shared/_CoacheeDashboardPartial.cshtml
  - Views/CDP/Shared/_CoachingProtonContentPartial.cshtml
  - Views/CDP/HistoriProtonDetail.cshtml
  - HcPortal.Tests/ProtonYearGateTests.cs
  - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
findings:
  critical: 0
  warning: 0
  info: 4
  total: 4
status: issues_found
---

# Phase 359: Code Review Report

**Reviewed:** 2026-06-10
**Depth:** deep
**Files Reviewed:** 11
**Status:** issues_found (4 Info, 0 Warning, 0 Critical)

## Summary

Reviewed the server-side Proton eligibility gate (CreateAssessment pre-pass + CoachMapping cross-year hard-block), the graduation gate (MarkMappingCompleted), and the CompetencyLevelGranted display cleanup across 4 controllers, 2 view-models, 3 views, and 2 test files. Cross-file analysis traced the gate predicate (`ProtonYearGate.IsAllowed` → `IsPrevYearPassedAsync` → `GetPassedYearsAsync`) and the two enforcement sites that consume it.

The implementation is solid against the priority risks:

- **Access control / no bypass (priority 1):** Every `uid` in the CreateAssessment gate is individually validated; `UserIds` are pre-verified against the DB (`userDictionary`, L1131-1160) before the gate, so there is no IDOR or unvalidated-id path. `eligibleUserIds` starts as a copy of `UserIds` but is fully *replaced* by `filtered` inside the Proton branch — no stale entries leak through. The empty-result guard (L1396) returns the view **before** `BeginTransactionAsync` (L1458), so a fully-skipped batch never opens a transaction. Skip counters (`gateSkippedNotHundred` / `gateSkippedPrevYear`) are mutually exclusive per-uid (each `continue`s after incrementing) and sum correctly.
- **Renewal side-door (priority 1):** Renewal exempts only the cross-year prereq (gate `a`, guarded by `!isRenewal`); the deliverable-100% check (gate `b`) runs unconditionally — renewal still passes the 100% gate exactly as required (D-07). Confirmed no side-door.
- **Cross-year hard-block (priority 2):** `CoachCoacheeMappingAssign` now returns `{ success=false, message=... }` with no `warning=true` and no `ConfirmProgressionWarning` escape — the soft-override path is gone. `isExemptFromCrossYear=false` is hardcoded per Phase 360 plan.
- **Graduation gate (priority 3):** `MarkMappingCompleted` is single-door behind `IsYearCompletedAsync` (`allApproved && hasFinalAssessment`) on the Tahun 3 assignment; transactional with proper rollback.
- **Null/scope safety (priority 4):** `protonTrackType`/`protonUrutan` captured correctly; `ProtonYearGate.IsAllowed` is null-safe on both args (6 unit tests cover null/empty/whitespace); no trailing-comma or dead-variable issues in the pruned object initializers (`finalAssessments`, `hasAssessment`/`fa` still consumed downstream).
- **Info leak (priority 5):** No `ex.Message` reaches TempData/Json in any new gate code; the gate-skip audit catch logs only to `_logger`.
- **View prune (priority 6):** No orphan bindings to `CompetencyLevelGranted` / `TrendLabels` / `TrendValues` / `CompetencyLevel` remain in any `.cshtml`/view-model. The dormant DB column (D-12) is intentionally untouched.

Verification performed: `dotnet build` = 0 warnings / 0 errors; `dotnet test --filter Category!=Integration` = 148/148 pass; `dotnet test --filter ProtonYearGate` = 7/7 pass (incl. real-SQL integration test).

The 4 findings below are all Info-level (cleanliness / consistency), none blocking.

## Info

### IN-01: Dead client-side warning-override path after cross-year hard-block

**File:** `Views/Admin/CoachCoacheeMapping.cshtml:779-796`
**Issue:** The cross-year block in `CoachCoacheeMappingAssign` was changed from a soft warning (`{ success=false, warning=true, ... }`) to a hard block (`{ success=false, message=... }`). The server no longer emits `warning=true`, so the JS branch `else if (data.warning) { if (confirm(...)) { payload.ConfirmProgressionWarning = true; <re-POST> } }` is now unreachable. The hard-block message falls through to the final `else { alert(data.message); }`, which is the correct behavior — but the re-POST block and `payload.ConfirmProgressionWarning = true` are dead code. The DTO field `CoachAssignRequest.ConfirmProgressionWarning` (`Controllers/CoachMappingController.cs:1794`) is likewise now vestigial (only referenced in a comment server-side).
**Fix:** Prune the now-unreachable `else if (data.warning)` branch in the view and remove the dead `ConfirmProgressionWarning` request field once Phase 360 confirms no other consumer. No behavior change — the hard-block already surfaces via the `else` alert. (Defer if leaving the field is intentional scaffolding for Phase 360; if so, add a comment in the DTO saying so.)

### IN-02: Cross-year gate silently allows when previous track is missing (data-integrity edge)

**File:** `Controllers/AssessmentAdminController.cs:1345-1348`, `Controllers/CoachMappingController.cs:506-510`
**Issue:** Both enforcement sites only apply the cross-year prereq when the previous track exists. In CreateAssessment, `prevTahunKe` is resolved via `Urutan-1` lookup; if no sibling track is found, `prevTahunKe = null` → `IsPrevYearPassedAsync(..., null)` returns `true` (Tahun-1 semantics), so a Tahun-2/3 assignment whose Tahun-1 sibling is misconfigured/missing would pass cross-year unchallenged. CoachMapping mirrors this (only blocks when `prevTrack != null`). This is consistent across both paths and only triggers on a misconfigured track set, but it is a silent gate weakening rather than a fail-closed.
**Fix:** Acceptable as-is for a well-configured track catalog. If hardening is desired later, log a warning when `protonUrutan > 1` but no prev track resolves, so admins notice the misconfiguration instead of getting a silent pass.

### IN-03: Pre-Post Test mode bypasses the Proton eligibility gate

**File:** `Controllers/AssessmentAdminController.cs:1210-1333`
**Issue:** The Pre-Post Test branch (`isPrePostMode`) creates sessions and `return RedirectToAction(...)` at L1322 — i.e. before the Phase 359 gate at L1336. If a `Category == "Assessment Proton"` request were ever submitted in pre-post mode, neither the 100% deliverable gate nor the cross-year gate would run. In practice Proton assessments are always standard mode (Tahun 3 = interview-only, Tahun 1/2 = single exam), so this path is not expected to carry Proton traffic, and the gate's placement after the pre-post return is by design.
**Fix:** No action required if Proton + pre-post is structurally impossible. For defense-in-depth, consider an early guard rejecting `Category == "Assessment Proton" && isPrePostMode` so the gate can never be skipped via that mode.

### IN-04: End-user guide docs still describe the removed "Competency Level" concept

**File:** `wwwroot/documents/guides/Panduan-Lengkap-Coaching-Proton.html:448,481,526`, `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html:841`
**Issue:** The CompetencyLevelGranted UI was removed from the coachee dashboard, progress table, histori timeline, and trend chart (now showing "Lulus / Belum Lulus" status instead). The static user guides still describe "Competency Level (skala 1-5)" and "Penetapan Level Kompetensi" as a visible feature, which no longer matches the UI and may confuse users.
**Fix:** Update the guide HTML to reflect the new "Status Proton: Lulus / Belum Lulus" model (or remove the Competency Level sections). Out of strict code-prune scope for this phase; track as a docs follow-up.

---

_Reviewed: 2026-06-10_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: deep_
