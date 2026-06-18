---
phase: 396-import-excel-retire-bulkbackfill
plan: 03
subsystem: import-excel
tags: [excel, import, controller, endpoint, upload, preview, integration-test]

# Dependency graph
requires:
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 02
    provides: "InjectExcelHelper.GenerateTemplate + ParseMatrix (static EF-free); EssayTextRequired-scoped essay text-required rule (D-05); locked template header + parser error strings"
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 01
    provides: "InjectExcelUploadResult/InjectExcelPreviewRow DTOs, InjectRequest.EssayTextRequired flag, InjectAssessmentViewModel.Step5Method flag"
  - phase: 395-mode-jawaban-input-asli-auto-generate
    provides: "InjectAssessmentController seams (userIdToNip, ParseQuestionVms, MapToInMemory, MapToRequest, ToAnswerSpec, PreviewInjectScore engine call); InjectAssessmentService.InjectBatchAsync (commit, atomic PreflightValidate)"
provides:
  - "Controllers/InjectAssessmentController.cs — POST DownloadInjectTemplate (2-sheet .xlsx) + POST UploadInjectExcel (parse -> atomic validate -> JSON: full error list OR batch preview + answersJson) + private BuildExcelPreviews + private MapVmQuestionsToSpec"
  - "HcPortal.Tests/InjectExcelImportTests.cs — integration (real SQL): Excel path == form path (preview==commit), essay text-optional (EssayTextRequired both directions), atomic rollback on any error"
affects:
  - "396-04 (view Step5Method toggle: download button POSTs to DownloadInjectTemplate; upload panel POSTs file to UploadInjectExcel; on Ok=true client puts AnswersJson into #AnswersJson and sets EssayTextRequired=false for the Excel commit; on Ok=false renders Errors list + SkippedBlankCount warning + Previews table)"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "POST (not GET) for template download — carries #QuestionsJson + UserIds (too large for query string) + antiforgery token"
    - "Factor-out shared projection (MapVmQuestionsToSpec) so template-gen, parse, preview, and commit use the IDENTICAL question set (Order = sequential index i, no behavior change)"
    - "Preview-batch reuses MapToInMemory + AssessmentScoreAggregator.Compute (same engine as commit) — NO new grading branch, NO cert#, NO SaveChanges -> preview == commit"
    - "Outer try/catch on upload endpoint -> friendly Json error, never a 500 leak (Security V5)"
    - "allowedNips built from vm.UserIds (picker) only (D-02) — controller never adds NIPs from the file"

key-files:
  created:
    - HcPortal.Tests/InjectExcelImportTests.cs
  modified:
    - Controllers/InjectAssessmentController.cs

key-decisions:
  - "DownloadInjectTemplate is [HttpPost] (not GET) so #QuestionsJson + UserIds + antiforgery ride in the form body; returns ExcelExportHelper.ToFileResult(wb, \"inject_template.xlsx\", this)"
  - "MapVmQuestionsToSpec factored out of MapToRequest (identical projection, Order = sequential index i) and reused by both Excel endpoints — kills drift between template-gen soal and commit soal"
  - "BuildExcelPreviews reverse-looks-up NIP from UserId via nipToUserId inverse dict; Answered = count of answers actually present, TotalQuestions = questions.Count"
  - "UploadInjectExcel wraps the whole body in try/catch -> friendly Json error (Security V5); file guards (null/empty + .xlsx/.xls whitelist) precede parse"

patterns-established:
  - "Controller seam for Excel path with NO commit branch: client feeds the returned AnswersJson into #AnswersJson and submits the existing 395 form -> MapToRequest -> InjectBatchAsync (Excel commit sets EssayTextRequired=false, D-05)"

requirements-completed: []  # INJ-10 spans Plans 01-04; controller endpoints now exist but view toggle (04) not yet wired

# Metrics
duration: ~7min
completed: 2026-06-18
---

# Phase 396 Plan 03: Import Excel Wave 3 (Controller Endpoints) Summary

**Added the two controller seams wiring the Excel path — POST `DownloadInjectTemplate` (returns the 2-sheet .xlsx built from authored questions + picker workers) and POST `UploadInjectExcel` (parse -> atomic validate -> JSON `InjectExcelUploadResult` with the full error list OR a per-NIP batch preview + answersJson) — reusing `ParseQuestionVms`/`MapToInMemory`/`AssessmentScoreAggregator.Compute` so preview == commit with no new grading branch, and locked it with a 3-fact real-SQL integration suite proving Excel-parsed workers grade identically to form workers.**

## Performance

- **Duration:** ~7 min
- **Completed:** 2026-06-18
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Endpoint Contract (source of truth for Plan 04 view JS)

### POST `/Admin/DownloadInjectTemplate` (verb: POST, returns FileContentResult)
- **Attributes:** `[HttpPost] [Authorize(Roles="Admin, HC")] [ValidateAntiForgeryToken]`
- **Body:** `InjectAssessmentViewModel` (carries `QuestionsJson` + `UserIds` + antiforgery token from the wizard form).
- **Behavior:** `MapVmQuestionsToSpec(ParseQuestionVms(vm))` -> `InjectExcelHelper.GenerateTemplate(questions, workers)` -> `ExcelExportHelper.ToFileResult(wb, "inject_template.xlsx", this)`. Workers = picker users with non-empty NIP (NIP + FullName).
- **Guards:** 0 soal -> `TempData["Error"]` + re-render wizard; 0 picker worker ber-NIP -> `TempData["Error"]` + re-render. 0 DB write (read-only Users query).
- **Filename:** `inject_template.xlsx`.

### POST `/Admin/UploadInjectExcel` (verb: POST, returns `Json(InjectExcelUploadResult)`)
- **Attributes:** `[HttpPost] [Authorize(Roles="Admin, HC")] [ValidateAntiForgeryToken] [RequestFormLimits(MultipartBodyLengthLimit = 10*1024*1024)]`
- **Body:** `IFormFile? excel` + `InjectAssessmentViewModel vm` (UserIds + QuestionsJson + antiforgery).
- **JSON response shape (`InjectExcelUploadResult`):**
  - `Ok` (bool) — `false` on any error (atomic, no write), `true` on success.
  - `Errors` (`List<InjectRowError>{ Nip, Message }`) — full list (file-level + per-row/per-cell from ParseMatrix). Bahasa Indonesia.
  - `AnswersJson` (string) — serialized `List<InjectWorkerAnswersVM>` (Mode="manual"); the view puts this into `#AnswersJson` (only when `Ok=true`).
  - `Previews` (`List<InjectExcelPreviewRow>{ Nip, Name, Percentage, IsPassed, Answered, TotalQuestions }`) — per-NIP dry-run, engine-computed, NO cert#.
  - `SkippedBlankCount` (int) — blank cells omitted (D-06); render as a warn-but-allow notice.
- **Guards (in order):** null/empty file -> "File Excel wajib diunggah."; extension not `.xlsx`/`.xls` -> "Format file harus .xlsx atau .xls."; 0 soal -> error; 0 picker worker ber-NIP -> error; ParseMatrix errors -> `Ok=false` + full `Errors` (no write); else `Ok=true` + Previews + AnswersJson. Outer try/catch -> "Gagal memproses file Excel..." (never a 500).

### How the client commits the Excel result (Plan 04)
1. On `Ok=true`, set `document.getElementById('AnswersJson').value = result.AnswersJson`.
2. The Excel path commit MUST use `EssayTextRequired=false` (D-05) — Plan 04 wires this via the view's hidden field / `Step5Method` handling so the existing `MapToRequest` -> `InjectBatchAsync` accepts essays scored without text.
3. Submitting the same wizard form (`#btnInject`) reuses the existing 395 commit path — NO new commit branch added here.

## Accomplishments
- Two public endpoints added after `PreviewInjectScore`; `DownloadInjectTemplate` calls `GenerateTemplate` + `ToFileResult`, `UploadInjectExcel` calls `ParseMatrix` + the preview engine. `grep -c InjectExcelHelper.(GenerateTemplate|ParseMatrix)` == 2.
- `MapVmQuestionsToSpec` factored out of `MapToRequest` (identical projection, `Order = i`) so the soal used for template generation, parsing, preview, and commit are guaranteed the same set.
- `BuildExcelPreviews` reuses `ToAnswerSpec` + `MapToInMemory` + `AssessmentScoreAggregator.Compute` — the exact engine the commit path uses; no `CertNumberHelper`, no `SaveChanges` -> preview == commit by construction.
- Security posture mirrors 394/395: RBAC `Admin, HC` (T-396-05), `[ValidateAntiForgeryToken]` on both POSTs (T-396-06), 10MB `RequestFormLimits` + `.xlsx`/`.xls` whitelist + try/catch friendly error (T-396-02/V5), `allowedNips` = picker-only (T-396-01).
- Integration suite (3 facts, real SQL) proves the core 396 guarantee: an Excel-parsed `InjectWorkerAnswersVM` set is structurally identical to the form set and commits to the same DB `Score` the Aggregator predicts (preview == commit).

## Task Commits

Each task committed atomically (with hooks):

1. **Task 1: Add DownloadInjectTemplate + UploadInjectExcel endpoints** — `6a8eaaad` (feat)
2. **Task 2: Integration — Excel path == form path + atomic rollback + essay text-optional** — `cdcbf0f3` (test)

## Files Created/Modified
- `Controllers/InjectAssessmentController.cs` (modified, +174 / -16) — added `DownloadInjectTemplate` + `UploadInjectExcel` public actions + private `BuildExcelPreviews` + private `MapVmQuestionsToSpec` (factored out of `MapToRequest`). No existing 395 action touched (behavior of `MapToRequest` unchanged — same projection, now via the shared helper).
- `HcPortal.Tests/InjectExcelImportTests.cs` (created, 322 lines) — `[Trait("Category","Integration")]`, `IClassFixture<InjectAssessmentFixture>`; 3 facts (preview==commit, essay text-optional both directions, atomic rollback). Local `QuestionColumn` accounts for the essay double-column (score + text) layout.

## Verification Results
- `dotnet build HcPortal.csproj` -> **0 Error** (24 pre-existing warnings in unrelated view/controller files, out of scope — same baseline as Plan 02).
- `dotnet test --filter "Category!=Integration"` -> **389/389 PASSED** (no regression to existing controller/helper tests).
- `dotnet test --filter "Category=Integration&FullyQualifiedName~InjectExcelImport"` -> **3/3 PASSED** with SQLEXPRESS (preview==commit + atomic rollback + essay text-optional).

### Acceptance grep
- `grep -c "DownloadInjectTemplate"` == 3 (>= 1); `grep -c "UploadInjectExcel"` == 3 (>= 1).
- `grep -c "RequestFormLimits"` == 1 (>= 1); `grep -c "\.xlsx\|\.xls"` == 5 (>= 1).
- `grep -c "AssessmentScoreAggregator.Compute"` == 4 (>= 1); `grep -c "InjectExcelHelper.ParseMatrix\|InjectExcelHelper.GenerateTemplate"` == 2 (== 2).
- `grep -c 'Authorize(Roles = "Admin, HC")'` == 5 (both new endpoints + 3 existing); `grep -c "ValidateAntiForgeryToken"` == 4 (both new POSTs + existing).
- Test file: `grep -c "Category.*Integration"` == 2 (>= 1); `grep -c "\[Fact\]"` == 3 (== 3); `grep -c "InjectBatchAsync\|Rejected\|ParseMatrix"` == 12 (>= 3).

## Build / Migration Impact
- **0 migration** — added two controller actions + two private helpers + an integration test file; no DbSet/entity/EF config changed. `git diff --name-only` shows no changes under `Migrations/` or `Data/`. `git status` shows no new migration files.

## Decisions Made
- `DownloadInjectTemplate` is `[HttpPost]` (not GET) so the wizard can pass `#QuestionsJson` + `UserIds` (too large/structured for a query string) along with the antiforgery token; the file is still returned as a normal `FileContentResult` download.
- Factored `MapVmQuestionsToSpec` out of `MapToRequest` rather than duplicating the projection in the Excel endpoints — single source for the soal set across template-gen, parse, preview, and commit (kills any drift; `MapToRequest` behavior unchanged, `Order = i` preserved).
- `BuildExcelPreviews` reuses the existing private `MapToInMemory` + `ToAnswerSpec` (made reachable from the new helper) so there is exactly one grading engine path (the Aggregator) for both preview and commit.

## Deviations from Plan

None — both tasks executed exactly as written. The plan's `DownloadInjectTemplate` sketch already used `[HttpPost]` (the `// GET` in the prose heading was a label; the code block and acceptance criteria specify POST + antiforgery, which is what was implemented). `MapVmQuestionsToSpec` was factored out and `MapToRequest` rewired to call it, exactly as the plan's note instructed (no behavior change).

## Issues Encountered
None — Task 1 built clean on the first compile; Task 2 integration suite passed 3/3 on the first run against SQLEXPRESS.

## User Setup Required
None — no external service configuration. 0 migration; no IT migration notification for this plan. (Phase-wide handoff still: notify IT migration=FALSE when the milestone bundle is pushed; not this plan's responsibility.)

## Threat Surface
No new security surface introduced beyond the plan's `<threat_model>`. The two endpoints are the controller gate for untrusted Excel bytes + picker payload:
- T-396-05 (Access Control): both endpoints `[Authorize(Roles="Admin, HC")]`.
- T-396-06 (CSRF): both POSTs `[ValidateAntiForgeryToken]`.
- T-396-02 (DoS / file upload): `[RequestFormLimits(10MB)]` + `.xlsx`/`.xls` whitelist + ParseMatrix try/catch + outer try/catch -> friendly Json, never 500; stream read in-memory, not saved to disk.
- T-396-01 (Tampering / NIP outside picker): `allowedNips` = `vm.UserIds` (picker) only; ParseMatrix rejects others to the error list; the controller never adds NIPs from the file.
- T-396-07 (Cert reserved during preview): preview uses `Aggregator.Compute` only — no `CertNumberHelper`, no `SaveChanges`; cert number reserved only at commit (InjectBatchAsync).
- T-396-08 (Audit): commit reuses `InjectBatchAsync` (AuditLog "ManualInject" per session, inherited from 393) — no separate Excel audit needed.

No `## Threat Flags` — nothing introduced outside the threat register.

## Known Stubs
None — both endpoints are fully wired to live data (Users query for NIP/Name, InjectExcelHelper for gen/parse, Aggregator for preview). The view that consumes `InjectExcelUploadResult` (download button, upload panel, preview table, error list) is Plan 04 scope, documented above as the client contract.

## Next Phase Readiness
- **Plan 04 ready:** the view JS contract is fully specified (Endpoint Contract section). Download button -> POST `DownloadInjectTemplate`; upload panel -> POST `UploadInjectExcel` (multipart); on `Ok=true` feed `AnswersJson` into `#AnswersJson` and ensure the Excel commit sends `EssayTextRequired=false`; on `Ok=false` render `Errors` + `SkippedBlankCount` + (when present) `Previews`. The `Step5Method` VM flag (Plan 01) toggles form vs Excel.
- **No blockers.**

## Self-Check: PASSED

- FOUND: Controllers/InjectAssessmentController.cs
- FOUND: HcPortal.Tests/InjectExcelImportTests.cs
- FOUND: .planning/phases/396-import-excel-retire-bulkbackfill/396-03-SUMMARY.md
- FOUND commit: 6a8eaaad (Task 1 feat)
- FOUND commit: cdcbf0f3 (Task 2 test)

---
*Phase: 396-import-excel-retire-bulkbackfill*
*Completed: 2026-06-18*
