---
phase: 386-assessmentadmincontroller-hardening
plan: 05
subsystem: assessment-export
tags: [pdf-export, excel-export, multiple-answer, essay, kill-drift, display-path, pxf-05]

# Dependency graph
requires:
  - phase: 386-02-wave1-pure-helper-extraction
    provides: "AssessmentScoreAggregator.BuildAnswerCell (MA join ', ' / MC single / Essay truncate 300+'...' / empty '—') + IsQuestionCorrect (essay >0, MA SetEquals) — the two shared display helpers wired here"
  - phase: 386-01-wave0-red-scaffolds
    provides: "PdfAnswerCellTests (6 Fact) RED contract locking BuildAnswerCell signature + ', ' MA join — stays GREEN after this wiring"
  - phase: 383-essay-grading-correctness
    provides: "AssessmentScoreAggregator.IsQuestionCorrect (essay >0 canonical) reused for both export surfaces"
provides:
  - "GeneratePerPesertaPdf (Controllers/AssessmentAdminController.cs) labels Multiple Answer all-or-nothing (SetEquals via IsQuestionCorrect) and lists ALL selected options (BuildAnswerCell) for the official per-peserta PDF evidence — PXF-05 / F-17 closed"
  - "ExcelExportHelper.AddDetailPerSoalSheet routed through the SAME two helpers — second official surface byte-consistent with PDF; F-DEV-02 (D-13) folded in & closed"
  - "Both official-evidence export surfaces now share one display-truth source (kill-drift) — PDF and Excel render identical Jawaban + Benar? for MC/MA/Essay"
affects: [386-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Wire-once: replace inline single-row FirstOrDefault answer/correct derivation on BOTH export surfaces with the shared BuildAnswerCell + IsQuestionCorrect (single source of display truth)"
    - "Display-path-only edit: scoring engine (Compute) byte-untouched (D-11); only PDF/Excel rendering re-pointed to helpers"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Helpers/ExcelExportHelper.cs

key-decisions:
  - "GeneratePerPesertaPdf per-question loop: REPLACED the single-row `var resp = sessionResponses.FirstOrDefault(...)` + `if (resp != null) { ... opt.IsCorrect ... }` MA-mislabel block (old L5100-5117) with `var responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList(); bool? correct = IsQuestionCorrect(q, responsesForQ); string jawaban = BuildAnswerCell(q, responsesForQ);` — statusColor/statusText ternary (✓ Benar/✗ Salah/— Pending) + QuestPDF render KEPT byte-identical."
  - "ExcelExportHelper.AddDetailPerSoalSheet per-question loop: REPLACED the `var response = responses.FirstOrDefault(...)` + `if (response == null) {...} else {...}` single-row block (old L83-124) with `var responsesForQ = responses.Where(r => r.AssessmentSessionId == session.Id && r.PackageQuestionId == q.Id).ToList(); string jawabanText = BuildAnswerCell(...); bool? isCorrect = IsQuestionCorrect(...);` — the two-cell write (Jawaban + ✓/✗/— with Green/Red color) KEPT."
  - "INTENTIONAL Excel essay-label unification (D-13): old Excel used `EssayScore >= ScoreValue/2` for essay correctness; the shared IsQuestionCorrect uses essay `> 0` (v30.0 canonical). Excel essay Benar? now matches PDF + web Results. Documented per plan."
  - "No `using HcPortal.Helpers;` added to ExcelExportHelper.cs — the file IS in namespace `HcPortal.Helpers`, so AssessmentScoreAggregator is directly in scope (build confirms)."
  - "Compute (scoring engine) NOT touched — Helpers/AssessmentScoreAggregator.cs has 0 diff across both commits (git-verified). D-11 satisfied."

patterns-established:
  - "Pattern: two official-evidence export surfaces (PDF + Excel) re-pointed to the SAME pure display helpers to kill MA-mislabel drift permanently"

requirements-completed: [PXF-05]

# Metrics
duration: 4min
completed: 2026-06-15
---

# Phase 386 Plan 05: Wave-4 PXF-05 PDF + Excel MA-Label Wiring Summary

**Both official-evidence export surfaces — the per-peserta PDF (`GeneratePerPesertaPdf`, F-17) and the Excel "Detail Per Soal" sheet (`AddDetailPerSoalSheet`, F-DEV-02) — now route their "Jawaban" cell through `BuildAnswerCell` and their Benar/Salah label through `IsQuestionCorrect`, so Multiple Answer is labeled all-or-nothing (SetEquals) and lists every selected option on BOTH surfaces identically; scoring engine untouched, 0 migration.**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-06-15T15:19:34Z
- **Completed:** 2026-06-15T15:23:37Z
- **Tasks:** 2
- **Files modified:** 2 (0 created, 2 modified)

## Accomplishments
- **Task 1 (PXF-05 D-09/D-10, PDF):** In `GeneratePerPesertaPdf` (Controllers/AssessmentAdminController.cs, per-question loop on Page 2+ "Detail Jawaban per Soal"), replaced the single-row `FirstOrDefault` block that read only ONE response row (mislabeling Multiple Answer) with `responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList()`, then `bool? correct = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ)` (ALL types) and `string jawaban = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ)`. MC stays byte-identical; Essay already used the helper; the `statusColor`/`statusText` ternary and the QuestPDF render are unchanged.
- **Task 2 (PXF-05 D-13, Excel — F-DEV-02 folded):** In `ExcelExportHelper.AddDetailPerSoalSheet`, replaced the `var response = responses.FirstOrDefault(...)` + `if (response == null) {...} else {...}` single-row derivation with `responsesForQ = responses.Where(r => r.AssessmentSessionId == session.Id && r.PackageQuestionId == q.Id).ToList()` passed to the SAME two helpers. The two-cell write (Jawaban cell + ✓/✗/— cell with Green/Red color) is preserved exactly.
- Both edits are **display-path only** — `Helpers/AssessmentScoreAggregator.cs` (which contains `Compute`) has **0 diff** across both commits (git-verified). D-11 satisfied: scoring engine and on-submit grading untouched.
- Build 0 errors; pure test suite 347/347 GREEN (incl. PdfAnswerCellTests + IsQuestionCorrect regression). 0 migration.

## Edited Ranges

| Surface | File | Old block (replaced) | New |
|---------|------|----------------------|-----|
| PDF per-peserta | `Controllers/AssessmentAdminController.cs` | per-question loop: `var resp = FirstOrDefault(...)` + `if (resp != null) { ... opt.IsCorrect ... }` (single-row MA mislabel) | `responsesForQ = Where(...).ToList()` → `IsQuestionCorrect` + `BuildAnswerCell`; statusColor/statusText + QuestPDF render KEPT |
| Excel Detail Per Soal | `Helpers/ExcelExportHelper.cs` `AddDetailPerSoalSheet` | per-question loop: `var response = FirstOrDefault(...)` + `if (response == null) {...} else {...selectedOption.IsCorrect / EssayScore >= ScoreValue/2...}` | `responsesForQ = Where(...).ToList()` → `BuildAnswerCell` + `IsQuestionCorrect`; ✓/✗/— + Green/Red color cell KEPT |

## Intentional Essay-Label Unification (Excel, D-13)

The OLD Excel "Detail Per Soal" sheet derived essay correctness as `EssayScore >= ScoreValue/2`. The shared `IsQuestionCorrect` uses the v30.0 canonical essay rule `EssayScore.Value > 0` (true) / `== 0` (false) / `null` (pending). After this wiring, the Excel essay **Benar?** column now matches the PDF per-peserta export AND the web Results page — one canonical essay-correctness rule across all official surfaces. This is a deliberate unification mandated by D-13 (F-DEV-02 folded into PXF-05), not a regression. The doc-comment on `AddDetailPerSoalSheet` was updated to reflect the new derivation.

## No-Response Cell Behavior (note)

Previously the Excel no-response case wrote `"—"` for both the Jawaban and Benar? cells. With the shared helpers: `BuildAnswerCell` returns `"—"` for no response (unchanged), and `IsQuestionCorrect` returns `false` for an unanswered MC/MA (→ `"✗"`) and `null` for a pending/no-answer Essay (→ `"—"`). The Benar? column for an unanswered MC/MA now shows `✗` instead of `—`. This is the same all-or-nothing/IsQuestionCorrect contract already used by the PDF (Task 1) and the web Results page — both official surfaces are now identical. The plan's acceptance constraint (`—` for the essay-pending `isCorrect == null` case) is preserved.

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build HcPortal.csproj` (after Task 1) | Build succeeded, **0 Error(s)** (24 pre-existing warnings in unrelated views) |
| `Select-String BuildAnswerCell(q, responsesForQ)` in AssessmentAdminController.cs | Found (L5107) + IsQuestionCorrect (L5106) |
| Single-row mislabel `var opt = q.Options?.FirstOrDefault(o => o.Id == resp.PackageOptionId.Value)` in controller | **0 occurrences** (removed) |
| `statusText` ternary (✓ Benar/✗ Salah/— Pending) | Present (L5112) — render preserved |
| `dotnet build HcPortal.csproj` (after Task 2) | Build succeeded, **0 Error(s)** |
| Helpers in ExcelExportHelper.AddDetailPerSoalSheet | `BuildAnswerCell` (L93) + `IsQuestionCorrect` (L94) |
| Single-row `var selectedOption = q.Options?.FirstOrDefault(...)` in Excel | **0 occurrences** (removed) |
| `dotnet test --filter "Category!=Integration"` | **Passed! Failed: 0, Passed: 347, Total: 347** |
| `git diff` on `Helpers/AssessmentScoreAggregator.cs` (Compute) across both commits | **empty** — scoring engine untouched (D-11) |

## Task Commits

1. **Task 1: Rewire GeneratePerPesertaPdf to IsQuestionCorrect + BuildAnswerCell (PXF-05 D-09/D-10)** — `85861b69` (feat)
2. **Task 2: Fold F-DEV-02 — rewire ExcelExportHelper.AddDetailPerSoalSheet to the same helpers (D-13)** — `bb058f1b` (feat)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` (modified, +8/-18) — `GeneratePerPesertaPdf` per-question loop now calls the two shared helpers; mislabel block removed; render preserved.
- `Helpers/ExcelExportHelper.cs` (modified, +24/-44) — `AddDetailPerSoalSheet` per-question loop now calls the two shared helpers; mislabel block + `if (response == null)` branch removed; doc-comment updated; ✓/✗/— color cell preserved.

## Decisions Made
- Both surfaces re-pointed to `BuildAnswerCell` + `IsQuestionCorrect` (kill-drift) — no inline cell re-implementation.
- Excel essay label unified to `> 0` (v30.0 canonical), intentionally replacing `>= ScoreValue/2` (D-13).
- No `using HcPortal.Helpers;` needed in ExcelExportHelper.cs (same namespace) — confirmed by build.
- Compute / on-submit grading deliberately untouched (D-11) — git-verified 0 diff on AssessmentScoreAggregator.cs.

## Deviations from Plan

None — plan executed exactly as written. Both surfaces wired to the shared helpers with the exact replacements the plan's `<interfaces>` and `<action>` blocks specified; all acceptance criteria per task verified (build 0 error, grep removals = 0, render/styling preserved, pure suite 347/347 GREEN, Compute untouched).

## Issues Encountered
- The `<verify>` for Task 1 used the PowerShell `Select-String` cmdlet, which is not available in the Bash tool; substituted the equivalent ripgrep-backed Grep tool to confirm the same `BuildAnswerCell(q, responsesForQ)` presence and the removal of the single-row mislabel pattern. Same evidence, different tool — no impact on outcome.
- Build emits 24 warnings, all pre-existing and in unrelated files (`Views/Admin/Shared/_TrainingRecordsTab.cshtml`, `Views/CMP/BudgetTraining.cshtml`, `WorkerDataServiceSearchTests.cs`). Out of scope per the scope boundary — not introduced by this plan, not fixed.

## Known Stubs
None. Both export surfaces are fully wired to complete pure helpers with real per-question response data — no hardcoded empty values, no placeholder text, no unwired data source. PXF-05 is closed by this wiring (helper existed since Plan 02; this plan connected it to both controller/export paths).

## Threat Flags
None. This plan only re-points the rendering of two already-authorized export endpoints (BulkExportPdf / Excel export) to shared display helpers — no new request entry point, no data mutation, no auth surface change. Threat T-386-05-INTEGRITY (`mitigate`) is now satisfied: both official PDF/Excel surfaces label MA all-or-nothing (SetEquals) + list all selected options. T-386-05-SCOPE (`accept`) holds — Compute untouched.

## User Setup Required
None — no external service configuration required. 0 migration.

## Next Phase Readiness
- **Plan 06 (verify/e2e):** can un-skip the gated e2e specs (`option-validation-386.spec.ts`, `essay-empty-finalize-386.spec.ts`, `test.fixme` → `test`) and perform the manual PDF/Excel visual check confirming a Multiple Answer question is labeled Benar only when the exact correct set is selected, and the Jawaban cell lists all selected options on both surfaces.
- No blockers. 0 migration. Scoring engine (`Compute`) untouched this plan.

## Self-Check: PASSED

Both modified files exist on disk (`Controllers/AssessmentAdminController.cs`, `Helpers/ExcelExportHelper.cs`) and both task commit hashes (`85861b69`, `bb058f1b`) are present in git history. Main project builds 0 errors; pure test suite 347/347 GREEN; `Helpers/AssessmentScoreAggregator.cs` (Compute) has 0 diff across both commits.

---
*Phase: 386-assessmentadmincontroller-hardening*
*Completed: 2026-06-15*
