---
phase: 396-import-excel-retire-bulkbackfill
plan: 02
subsystem: import-excel
tags: [excel, import, parser, closedxml, helper, validation, tdd-green]

# Dependency graph
requires:
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 01
    provides: "Failing 8-fact InjectExcelHelperTests (contract), InjectRequest.EssayTextRequired flag, InjectExcelUploadResult/InjectExcelPreviewRow DTOs"
  - phase: 395-mode-jawaban-input-asli-auto-generate
    provides: "InjectAnswerVM/InjectWorkerAnswersVM (parser OUTPUT shape, nested in InjectAssessmentViewModel), InjectQuestionSpec/InjectOptionSpec/InjectRowError"
provides:
  - "Helpers/InjectExcelHelper.cs — static EF-free GenerateTemplate (2-sheet template) + ParseMatrix (matrix -> List<InjectWorkerAnswersVM>) — Wave 0 suite GREEN"
  - "EssayTextRequired-scoped essay text-required rule (D-05 form-only) in PreflightValidateAsync"
  - "Locked template column-header strings + parser error-message strings (Plan 03/04 e2e assert targets)"
affects:
  - "396-03 (controller DownloadInjectTemplate calls GenerateTemplate + ExcelExportHelper.ToFileResult; UploadInjectExcel calls ParseMatrix; Excel commit sets EssayTextRequired=false)"
  - "396-04 (view Step5Method toggle + Excel panel)"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static EF-free helper (only System.*/ClosedXML.Excel/HcPortal.Models/HcPortal.ViewModels) — unit-testable without DB (analog AssessmentScoreAggregator)"
    - "ONE comparator OrderBy(Order).ThenBy(TempId) in BOTH generate and parse (kills Pitfall 1 silent ordering corruption)"
    - "Letter -> authored option order (A=Options[0]), never OrderBy(TempId)"
    - "Workbook open wrapped in try/catch -> friendly Bahasa-Indonesia error, never throws/500 (Security V5/V12)"
    - "Request-flag-scoped validation: EssayTextRequired guards form-only rule without forking PreflightValidateAsync"

key-files:
  created:
    - Helpers/InjectExcelHelper.cs
  modified:
    - Services/InjectAssessmentService.cs

key-decisions:
  - "Parser return type uses NESTED InjectAssessmentViewModel.InjectWorkerAnswersVM / .InjectAnswerVM (these VMs are nested classes, matching the test's `using HcPortal.ViewModels;` + var inference)"
  - "AnswerSheetName const ('Jawaban') referenced in both add (generate) and read (parse) so the sheet-name contract is single-sourced"
  - "Blank essay score cell detected via TryGetValue<double> false AND empty GetString() -> SkippedBlank (D-06 OMIT); un-parseable non-empty -> error"
  - "Comparator literal kept ONLY in the two code paths (doc-comment reworded) so it appears in BOTH methods and nowhere else"

patterns-established:
  - "Wave 0 RED -> GREEN: implementing the contracted helper compiles the whole test assembly and turns the 8-fact suite + the rest of the fast suite green in one step"

requirements-completed: []  # INJ-10 spans Plans 01-04; helper now exists but controller endpoints (03) + view toggle (04) not yet wired

# Metrics
duration: ~12min
completed: 2026-06-18
---

# Phase 396 Plan 02: Import Excel Wave 2 (GREEN) Summary

**Implemented the static EF-free `InjectExcelHelper` (2-sheet template generator + matrix parser) to turn the Plan 01 RED contract suite GREEN, and scoped the essay text-required rule to the form path (D-05) via `EssayTextRequired` so Excel essays with a score but no text are not rejected by the shared `PreflightValidateAsync`.**

## Performance

- **Duration:** ~12 min
- **Completed:** 2026-06-18
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments
- `InjectExcelHelper.ParseMatrix` translates Excel cells into the exact same `InjectWorkerAnswersVM` shape the form produces (`Mode="manual"`) — preview & commit reuse the 393/395 path with NO new branch and NO grading logic in the parser.
- Locked D-04 stable ordering: ONE comparator `OrderBy(q => q.Order).ThenBy(q => q.TempId)` in BOTH generate and parse; letter mapping is authored order (A=Options[0]), proven by the deliberately out-of-order option TempIds in the test (A.TempId=99 > B.TempId=10).
- D-06 blank-omit (`SkippedBlank++`, never an empty MC/MA spec that would reject-all the batch), D-02/D-09 per-row/per-cell validation (NIP-not-in-picker, invalid letter, essay-score-out-of-range), and Security V5/V12 (workbook open in try/catch -> friendly error, never throws/500).
- D-05: scoped the essay text-required rule behind `req.EssayTextRequired` (default `true` = Form 395 byte-identical; Plan 03 Excel commit will set `false`).

## Task Commits

Each task committed atomically:

1. **Task 1: Scope essay text-required rule to form path (D-05)** — `42f47b45` (fix)
2. **Task 2: Implement InjectExcelHelper.GenerateTemplate + ParseMatrix (Wave 0 GREEN)** — `b515156c` (feat)

## Files Created/Modified
- `Helpers/InjectExcelHelper.cs` (created, 282 lines) — static class `InjectExcelHelper` in `namespace HcPortal.Helpers`; `GenerateTemplate` + `ParseMatrix` + private `LetterToIndex`/`IndexToLetter`. EF-free.
- `Services/InjectAssessmentService.cs` (modified, +1 line + comment) — wrapped the essay text-required check with `req.EssayTextRequired &&` (D-05 form-only).

## Contracted Strings (source of truth for Plan 03/04 e2e assertions)

### GenerateTemplate — Sheet "Jawaban" header layout
- Col 1 = `NIP`, Col 2 = `Nama`, then per question (in `OrderBy(Order).ThenBy(TempId)` order, 1-based display index `idx`):
  - **Essay** -> TWO columns: `Soal {idx} Skor (0..{ScoreValue})` then `Soal {idx} Teks (opsional)`
  - **MultipleAnswer** -> ONE column: `Soal {idx} (MA huruf, pisah koma)`
  - **MultipleChoice (else)** -> ONE column: `Soal {idx} (MC 1 huruf)`
- Header bold + fill `#16A34A` white font; row 1 frozen. Data rows start at row 2 (one per worker; answer cells blank).

### GenerateTemplate — Sheet "Legenda" header layout
- `No`, `Teks Soal`, `Tipe`, `Skor Maks`, `Opsi (huruf = teks)`; bold + light-gray header.
- Non-essay options string: `A={text}; B={text}; ...` (authored order). Essay -> `(jawaban teks/skor)`.

### ParseMatrix — error message strings (Bahasa Indonesia)
- File rusak: `Gagal membaca file Excel. Pastikan file .xlsx valid dan tidak rusak.`
- NIP not in picker (D-02): `Baris {row}: NIP {nip} tidak ada di daftar pekerja terpilih.`
- Essay score un-parseable: `Baris {row}, kolom Soal {idx} (Essay): skor tidak valid.`
- Essay score out of range (D-09): `Baris {row}, kolom Soal {idx} (Essay): skor {score} melebihi maksimum {ScoreValue}.`
- Invalid option letter (D-09): `Baris {row}, kolom Soal {idx}: opsi '{letter}' tidak valid (hanya A..{maxLetter}).`

### Behaviors locked by tests
- Round-trip cell -> correct TempId; A=Options[0] (not OrderBy(TempId)); MA comma-list `A,C` -> 2 TempIds (authored order); essay score parsed, text optional (text col at scoreCol+1); blank cell omitted with `SkippedBlank>=1`; per-row/per-cell errors for NIP-not-in-picker / invalid letter / essay-out-of-range.

## Verification Results
- `dotnet build HcPortal.csproj` -> **0 Error** (24 pre-existing warnings in unrelated view/controller files, out of scope).
- `dotnet test --filter "FullyQualifiedName~InjectExcelHelperTests"` -> **8/8 PASSED** (all Wave 0 facts GREEN).
- `dotnet test --filter "Category!=Integration"` -> **389/389 PASSED** (381 prior baseline + 8 new InjectExcelHelper facts; no regression — the 395 form-path tests pass because `EssayTextRequired` defaults to `true`).

### Acceptance grep
- `grep -c "public static class InjectExcelHelper"` == 1.
- `grep -c "ApplicationDbContext\|DbContext\|_context"` == 0 (EF-free).
- `grep -c "OrderBy(q => q.Order).ThenBy(q => q.TempId)"` == 2 (one per code path; doc-comment reworded to keep it exactly 2).
- `grep -c '"Jawaban"' / AnswerSheetName` references span both add + read.
- `grep -c "req.EssayTextRequired"` == 1; MC/MA/range checks unchanged (`grep -c "wajib tepat 1 jawaban\|wajib minimal 1 jawaban\|di luar rentang 0"` == 3).

## Build / Migration Impact
- **0 migration** — added a static helper + a one-line guard; no DbSet/entity/EF config changed, so the EF model snapshot is unchanged by construction. `git status` shows no new migration files. (The 395 SUMMARY already documented the `dotnet ef` CLI cosmetic-rewrite probe is unreliable in this repo; not run since nothing schema-relevant changed.)

## Decisions Made
- Parser return uses the NESTED VM types (`InjectAssessmentViewModel.InjectWorkerAnswersVM` / `.InjectAnswerVM`) — the only correct interpretation, since those classes are nested and the test relies on `var` inference via `using HcPortal.ViewModels;`.
- `AnswerSheetName`/`LegendSheetName` consts single-source the sheet-name contract used by both methods (and by the test's `wb.Worksheet("Jawaban")`).
- Reworded the helper's XML-doc to remove the literal comparator string so the comparator appears in exactly the two code paths (acceptance `== 2`) — semantics unchanged (doc still describes "urut menaik Order lalu TempId").

## Deviations from Plan

None — both tasks executed exactly as written. The only adjustment was a cosmetic doc-comment rewording (removing a duplicated comparator literal from the XML-doc) to satisfy the acceptance grep `== 2`; no functional change, fully within the plan's contract.

## Issues Encountered
None — Wave 0 suite went RED -> GREEN cleanly; full fast suite restored to green in the same step.

## User Setup Required
None — no external service configuration. 0 migration; no IT migration notification for this plan.

## Threat Surface
No new security surface introduced beyond the plan's `<threat_model>`. The parser is the first validation gate for untrusted Excel bytes: `allowedNips` (picker set) rejection (T-396-01), try/catch friendly error (T-396-02), blank-omit never empty-spec (T-396-03), and D-05 EssayTextRequired scoping (T-396-04) are all implemented. Size limit + extension whitelist remain the controller's responsibility (Plan 03).

## Next Phase Readiness
- **Plan 03 ready:** `GenerateTemplate` returns a bare `XLWorkbook` (controller GET wraps with `ExcelExportHelper.ToFileResult`); `ParseMatrix` returns `(Workers, Errors, SkippedBlank)` ready to feed `MapToInMemory` -> `AssessmentScoreAggregator.Compute` for preview and `#AnswersJson` -> `InjectBatchAsync` for commit (Excel commit sets `EssayTextRequired=false`).
- **No blockers.**

## Self-Check: PASSED

- FOUND: Helpers/InjectExcelHelper.cs
- FOUND: Services/InjectAssessmentService.cs
- FOUND: .planning/phases/396-import-excel-retire-bulkbackfill/396-02-SUMMARY.md
- FOUND commit: 42f47b45 (Task 1 fix)
- FOUND commit: b515156c (Task 2 feat)

---
*Phase: 396-import-excel-retire-bulkbackfill*
*Completed: 2026-06-18*
