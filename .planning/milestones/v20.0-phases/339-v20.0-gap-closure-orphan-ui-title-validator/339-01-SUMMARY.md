---
phase: 339-v20.0-gap-closure-orphan-ui-title-validator
plan: 01
date_completed: 2026-06-02
status: SHIPPED LOCAL (NOT PUSHED — bundle v20.0 ~149 commit)
requirements_completed:
  - CIL-06
  - REST-04
  - REST-06
commit_hashes:
  - ec2c301b  # T1 CIL-06 + REST-04 dropdown
  - 40f9a553  # T2 REST-04 admin Index card
  - f2f34e0d  # T3 REST-06 regex validator + view span
files_modified:
  - Views/Admin/Shared/_AssessmentGroupsTab.cshtml  # +11 line (CIL-06 dropdown-item + dropdown-divider + REST-04 dropdown-item)
  - Views/Admin/Index.cshtml                          # +16 line (REST-04 admin-only card di Section D)
  - Controllers/AssessmentAdminController.cs          # +9 line (REST-06 conditional regex validator setelah L845 auto-pair)
  - Views/Admin/CreateAssessment.cshtml               # +1 line (REST-06 asp-validation-for Title span)
files_NOT_modified:
  - Models/AssessmentSession.cs                       # D-03 LOCKED — entity Title plain string, annotation break legacy
  - Controllers/AssessmentAdminController.cs L6589    # TryAutoDetectCounterpartGroup helper Phase 338-05 preserved
  - Controllers/AssessmentAdminController.cs L833-845 # Auto-pair block Phase 338-05 preserved verbatim
  - Controllers/TrainingAdminController.cs            # BulkBackfill endpoint Phase 338-04 unchanged
  - Views/Admin/BulkBackfill.cshtml                   # Form view untouched, only linked-to
verification:
  build: PASS 0 error 21 warning (semua pre-existing CoachMapping/BudgetTraining/_TrainingRecordsTab/Deliverable/CoachingProton)
  test: PASS 18/18 in 325ms (Phase 338 regression baseline preserved)
  grep_acceptance: ALL PASS (see Verification Evidence section)
  uat_manual: PENDING — Playwright 6 skenario di plan <verification> block, executor manual setelah local server start
---

# Phase 339-01: v20.0 Gap Closure — Orphan UI + Title Validator Summary

**Phase:** 339-v20.0-gap-closure-orphan-ui-title-validator
**Plan:** 01 (single plan, single wave, 3 task)
**Date complete:** 2026-06-02
**Status:** SHIPPED LOCAL (NOT PUSHED — bundle v20.0 batch tunggu IT)
**REQ delivered:** CIL-06 + REST-04 + REST-06 (3/3 v20.0 gap closure)

---

## Objective Achieved

Tutup 3 partial REQ identified oleh `/gsd-audit-milestone v20.0` (2026-06-02):
- **CIL-06** `BulkExportPdf` endpoint orphan (Phase 338-04 L4489) → wired ke per-group action dropdown di `_AssessmentGroupsTab.cshtml`
- **REST-04** `BulkBackfill` route orphan (Phase 338-04 L720) → wired ke dropdown (contextual) + admin Index Section D card (global, Admin-only gate)
- **REST-06** Title naming convention validator missing (Phase 338-05 auto-pair OK, server-side regex absent) → conditional `Regex.IsMatch` di `CreateAssessment` POST L847-855 dengan parity guard `AssessmentTypeInput != "PrePostTest"` + `<span asp-validation-for="Title">` di view

Zero new endpoint, zero new auth surface, zero schema change, zero entity modification (per D-domain locked decision).

---

## Task Execution Summary

| # | Task | Type | Status | Commit | Output |
|---|------|------|--------|--------|--------|
| 1 | CIL-06 + REST-04 dropdown wiring di `_AssessmentGroupsTab.cshtml` | auto | ✅ DONE | `ec2c301b` | 4 elemen baru (li-BulkExportPdf + li-divider + li-BulkBackfill) antara L281 ↔ L282 |
| 2 | REST-04 admin Index card di `Views/Admin/Index.cshtml` Section D | auto | ✅ DONE | `40f9a553` | 1 card Admin-only gated `@if (User.IsInRole("Admin"))` setelah Maintenance card L274-289 |
| 3 | REST-06 regex validator controller + view span | auto | ✅ DONE | `f2f34e0d` | Validator block L847-855 + view span L193 |

---

## Decisions Honored

| ID | Decision | Implementation |
|----|----------|----------------|
| D-01 | CIL-06 button location = `_AssessmentGroupsTab.cshtml` setelah Export Excel L278 | `<li>` wrapper L282-286 sibling pattern (BUKAN bare `<a>` D-01 verbatim — struktur `<li>` ikut Bootstrap dropdown convention) |
| D-02 | REST-04 dual entry: dropdown variant + top nav (discover during task) | Dropdown variant L287-291 di `_AssessmentGroupsTab.cshtml` + admin Index Section D card di `Views/Admin/Index.cshtml` L274-289 (BUKAN `_Layout.cshtml` global nav — pilihan tepat: emergency tool semantic) |
| D-03 | REST-06 fix mechanism = conditional ModelState.AddModelError di controller, BUKAN data annotation entity | Controller L847-855 inserted setelah auto-pair block L833-845 sebelum token validation. `Models/AssessmentSession.cs:13` Title plain string UNTOUCHED. |
| D-04 | Regex pattern `^(Pre|Post)\s*Test\s+.+$` toleran whitespace | Verbatim di L850 `@"^(Pre|Post)\s*Test\s+.+$"` |
| D-05 | NO new tests required | Manual UAT Playwright sufficient; existing dotnet test suite preserved 18/18 PASS |
| D-06 | NO new threat surface | Threat model 0 mitigations — endpoint Phase 338 `[Authorize]` preserved, validator = defensive hardening |
| D-07 | 1 plan 1 wave 3 task | Plan structure honored, executed sekuensial dengan user checkpoint per task |

---

## Code Surface Changes

### Task 1 — `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (+11 line)

Sequential ordering verified (grep -n line numbers):
```
L278  ExportAssessmentResults  (existing)
L283  BulkExportPdf            (NEW — CIL-06)
L287  dropdown-divider         (NEW — REST-04 separator)
L289  BulkBackfill             (NEW — REST-04 dropdown variant)
L311  dropdown-divider         (existing — unchanged)
```

URL helper untuk BulkExportPdf membawa 3 parameter (title + category + scheduleDate) mirror sibling ExportAssessmentResults L278.

### Task 2 — `Views/Admin/Index.cshtml` (+16 line)

```
L258  @if (User.IsInRole("Admin") || User.IsInRole("HC"))  Maintenance card (existing)
L274  @if (User.IsInRole("Admin"))                          Bulk Backfill card (NEW — Admin only standalone, BUKAN || HC)
L277  Url.Action("BulkBackfill", "TrainingAdmin")           (NEW)
```

Gate match endpoint `TrainingAdminController.BulkBackfill [Authorize(Roles = "Admin")]` L720. HC role login = card hidden (UAT skenario 2 sub-step verified during plan).

### Task 3a — `Controllers/AssessmentAdminController.cs` (+9 line)

```
L833  // Phase 338 REST-06: Auto-pair LinkedGroupId via title pattern (existing)
L835  if (AssessmentTypeInput != "PrePostTest" ... ) {  auto-pair block (existing — preserved verbatim)
L845    }
L847  // Phase 339 REST-06: Validate Title pattern for standard Pre/Post tests (NEW)
L848  if (AssessmentTypeInput != "PrePostTest" ... ) {  validator block (NEW)
L850    && !System.Text.RegularExpressions.Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$"))
L852    ModelState.AddModelError("Title", "Title harus pola '{Stage} Test {Track} {Lokasi}'...");
L854    "Contoh valid: 'Pre Test OJT GAST Cilacap'. Reference: 336-NAMING-CONVENTION-SPEC."
L855  }
L857  // Handle Token Validation (existing — unchanged)
```

Guard parity dengan auto-pair: sama-sama `AssessmentTypeInput != "PrePostTest"` + `!string.IsNullOrEmpty(model.Title)`. Validator extra cek `!Regex.IsMatch(...)` independent dari LinkedGroupId state.

FQN `System.Text.RegularExpressions.Regex` digunakan (TIDAK ada `using System.Text.RegularExpressions;` di top file L1-15; konsisten dengan existing usage di L4476/4523).

### Task 3b — `Views/Admin/CreateAssessment.cshtml` (+1 line)

```
L188  <input asp-for="Title" ... />
L189  <div class="d-flex justify-content-between">  (counter row existing)
L192  </div>
L193  <span asp-validation-for="Title" class="text-danger small"></span>  (NEW)
L194  </div>  (close col-md-8)
```

Pattern menyamai existing `<span asp-validation-for="Category" class="text-danger small">` di L177. Render ModelState error dari Task 3a server-side.

---

## Verification Evidence

### Automated (semua PASS)

| Check | Command | Expected | Actual |
|-------|---------|----------|--------|
| CIL-06 surface | `grep BulkExportPdf _AssessmentGroupsTab.cshtml` | 1 | 1 (L283) |
| CIL-06 label ID | `grep "Bulk Export PDF (ZIP)" _AssessmentGroupsTab.cshtml` | 1 | 1 |
| CIL-06 icon | `grep bi-file-zip _AssessmentGroupsTab.cshtml` | 1 | 1 (L284) |
| REST-04 dropdown | `grep BulkBackfill _AssessmentGroupsTab.cshtml` | 1 | 1 (L289) |
| REST-04 surface count | `grep -rl BulkBackfill Views/` | 3 | 3 (_AssessmentGroupsTab + Index + existing BulkBackfill.cshtml) |
| REST-04 Admin-only | `grep '@if (User.IsInRole("Admin"))$' Index.cshtml` | 1 new | 1 (L274 distinct dari 13 existing `\|\| HC` variant) |
| REST-06 regex | `grep "Regex.IsMatch(model.Title" Controller.cs` | 1 | 1 (L850) |
| REST-06 ModelState | `grep 'ModelState.AddModelError("Title"' Controller.cs` | 1+ | 1 (L852) |
| REST-06 spec ref | `grep 336-NAMING-CONVENTION-SPEC Controller.cs` | 2+ | 4 (L833 existing + L847 new + L854 error msg + L6596 helper xmldoc) |
| REST-06 guard parity | `grep -c 'AssessmentTypeInput != "PrePostTest"' Controller.cs` | 2 | 3 (L835 auto-pair + L848 validator + L992 dual-create branch) |
| REST-06 view span | `grep asp-validation-for="Title" CreateAssessment.cshtml` | 1+ | 1 (L193) |
| Entity safety | `git diff Models/AssessmentSession.cs` | empty | empty ✓ |
| Build | `dotnet build` | 0 error | **0 error 21 warning** (semua pre-existing, none di file Phase 339 modify) |
| Test regression | `dotnet test` | 18/18 PASS | **18/18 PASS 325ms** |

### Manual UAT (Playwright — PENDING execute setelah local server start)

6 skenario di `339-01-PLAN.md` `<verification>` Manual UAT block:
1. CIL-06 Bulk Export PDF discoverable + ZIP download
2. REST-04 dual entry (dropdown + Admin Index card) + HC role negative gate
3. REST-06 invalid title path (validation error rendered)
4. REST-06 valid title path + auto-pair preserved
5. REST-06 PrePostTest mode SKIP regex check (parity guard)
6. Regression smoke Phase 338 auto-pair tetap jalan

---

## Cross-Reference

- **Audit source:** `.planning/v20.0-MILESTONE-AUDIT.md` (2026-06-02, 3 partial REQ identified by gsd-integration-checker)
- **Locked decisions:** `.planning/phases/339-v20.0-gap-closure-orphan-ui-title-validator/339-CONTEXT.md` (D-01..D-07)
- **Plan:** `.planning/phases/339-v20.0-gap-closure-orphan-ui-title-validator/339-01-PLAN.md`
- **Phase 338-04 SUMMARY** (CIL-06 + REST-04 endpoint origin): `.planning/phases/338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute/338-04-SUMMARY.md`
- **Phase 338-05 SUMMARY** (REST-06 auto-pair origin — preserved verbatim): `.planning/phases/338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute/338-05-SUMMARY.md`
- **Phase 336-NAMING-CONVENTION-SPEC** (regex authoritative spec): `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-NAMING-CONVENTION-SPEC.md`

---

## Deferred (per CONTEXT.md `<deferred>`)

Handle via separate workflow, BUKAN scope Phase 339:

- VERIFICATION.md backfill untuk Phase 336/337/338 → `/gsd-verify-work N` per phase
- REQUIREMENTS.md `[ ]` → `[x]` checkbox sync untuk 39 v20.0 REQ → `/gsd-complete-milestone v20.0` housekeeping
- MILESTONES.md log entries v16.0 + v19.0 + v20.0 → `/gsd-complete-milestone v20.0` housekeeping
- `.planning/config.json milestone_version: v16.0 → v20.0` (cosmetic — tool warn unknown anyway)
- Tom Select UX pre-existing regression → v21.0 backlog
- ViewModel refactor (`AssessmentCreateViewModel` wrapper) → over-engineering untuk 3-line fix, defer indefinitely

---

## Push Status

**NOT PUSHED — bundle v20.0 batch.**

Local commit chain Phase 339: `ec2c301b` → `40f9a553` → `f2f34e0d` (3 commit di branch `main`).

v20.0 milestone batch lokal (gabung dengan v19.0 + 325-335 + 336-338 dari sebelumnya): estimasi **~149 commit** pending push origin/main tunggu IT availability. Per CLAUDE.md DEV_WORKFLOW: promosi ke server Dev = tanggung jawab Team IT, developer notify dengan commit hash + migration flag (Phase 339 = NO migration / NO schema change → no migration flag).

---

## Next Steps Suggested

1. Manual UAT Playwright 6 skenario di local `http://localhost:5277` setelah `dotnet run` start
2. Re-audit `/gsd-audit-milestone v20.0` verify 3 partial REQ → satisfied (target 39/39 REQ closed)
3. `/gsd-complete-milestone v20.0` housekeeping: REQUIREMENTS.md checkbox sync + MILESTONES.md log + config bump + archive milestone
4. Coordinate dengan IT untuk push v20.0 batch (149 commit) ke origin/main + Dev deployment + migration absent (Phase 339 view + controller only)
