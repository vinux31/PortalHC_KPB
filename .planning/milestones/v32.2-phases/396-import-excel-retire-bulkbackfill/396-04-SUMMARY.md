---
phase: 396-import-excel-retire-bulkbackfill
plan: 04
subsystem: ui
tags: [excel, import, view, ui, toggle, preview, e2e, playwright, razor]

# Dependency graph
requires:
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 03
    provides: "POST DownloadInjectTemplate (2-sheet .xlsx) + POST UploadInjectExcel (JSON InjectExcelUploadResult: Ok/Errors/AnswersJson/Previews/SkippedBlankCount); BuildExcelPreviews preview==commit; MapVmQuestionsToSpec shared projection"
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 01
    provides: "InjectExcelUploadResult/InjectExcelPreviewRow DTOs, InjectRequest.EssayTextRequired flag, InjectAssessmentViewModel.Step5Method flag"
  - phase: 395-mode-jawaban-input-asli-auto-generate
    provides: "InjectAssessment.cshtml Step-5 sub-components + #AnswersJson/#QuestionsJson serialize listener + #btnInject -> InjectBatchAsync commit path; .textContent XSS-safe render; #step5DefaultMode radio analog"
provides:
  - "Views/Admin/InjectAssessment.cshtml — Step-5 N1 room-level Form/Excel toggle (mutually exclusive), N2 Excel panel (Download Template + file picker + Upload & Pratinjau), N3 batch preview table (skor/lulus/terjawab, no cert#), N4 full atomic error list + blank-cell warn; client wires #AnswersJson from excelAnswersCache when method=excel"
  - "Controllers/InjectAssessmentController.cs — Excel commit sets req.EssayTextRequired=false when vm.Step5Method=excel (D-05)"
  - "tests/e2e/inject-excel-396.spec.ts — Playwright (serial, snapshot/restore): toggle mutual-exclusivity + real template download + upload-success->preview->commit (anti silent-grade-0) + invalid upload full error list 0-write (atomic) + blank-cell warn-but-allow"
affects:
  - "396-05 (retire BulkBackfill — INJ-11; the Excel inject path is now the single entry-point, BulkBackfill can be hard-removed)"
  - "398 (Test + UAT 'seakan online' — full lifecycle incl. Excel inject path now end-to-end usable)"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Programmatic hidden-<form> POST for template download (document.createElement('form') + data-download-url from @Url.Action) carrying #QuestionsJson + UserIds + antiforgery — a real browser download (NOT fetch-blob), so Playwright observes a download event"
    - "Room-level mutually-exclusive method toggle (name=step5Method) wraps the 395 form path in #step5FormPath and the Excel path in #step5ExcelPanel; toggling adds/removes d-none on the whole unit (no form-state clearing — HC can switch back)"
    - "Excel commit gate: submit listener fills #AnswersJson from window.injExcelAnswersCache ONLY after a 0-error upload; invalid upload sets the cache to '[]' so commit produces nothing (T-396-10 — preview is the gate, D-08)"
    - "e2e Excel authoring with exceljs (devDep) builds a FRESH upload .xlsx rather than round-tripping the ClosedXML-generated template (exceljs.readFile incompatible with ClosedXML output); parser reads sheet 'Jawaban' by name + column position so a fresh-but-equivalent file is accepted"

key-files:
  created:
    - tests/e2e/inject-excel-396.spec.ts
  modified:
    - Views/Admin/InjectAssessment.cshtml
    - Controllers/InjectAssessmentController.cs

key-decisions:
  - "Template download = hidden-form POST (not fetch-blob): document.createElement('form') + data-download-url=@Url.Action(DownloadInjectTemplate) + serialized #QuestionsJson + UserIds + antiforgery -> .submit() triggers a real .xlsx download (Playwright download event asserts this)"
  - "All dynamic preview/error text rendered via .textContent (never innerHTML) — carry 395, XSS-safe (T-396-09)"
  - "EssayTextRequired=false applied ONLY when vm.Step5Method=excel; the form path keeps the default true (D-05 essay text-optional scoped to Excel only)"
  - "e2e builds upload .xlsx fresh via exceljs instead of editing the downloaded ClosedXML template (lib incompatibility) — equivalent file, documented in the spec header"

patterns-established:
  - "Excel UI path reuses the existing #btnInject -> #AnswersJson -> MapToRequest -> InjectBatchAsync commit (byte-identical to form/online path); the toggle only changes WHAT fills #AnswersJson, never the commit branch"

requirements-completed: []  # INJ-10 spans Plans 01-04; this plan completes the UI surface but, per the 01-03 convention, final INJ-10 close is left to phase verification (398)

# Metrics
duration: ~25min (impl) + checkpoint UAT (orchestrator-driven)
completed: 2026-06-18
---

# Phase 396 Plan 04: Import Excel UI (Step-5 Toggle + Panel + Preview + Errors) Summary

**Built the only UI surface of INJ-10 on `/Admin/InjectAssessment` Step-5: a room-level mutually-exclusive Form/Excel method toggle, the Excel panel (Download Template via hidden-form POST, file picker, Upload & Pratinjau via fetch FormData), the mandatory batch preview table (skor/lulus/soal-terjawab — no cert#), and the full atomic validation-error list + blank-cell warn — all Bahasa Indonesia, .textContent-rendered (XSS-safe), wiring `#AnswersJson` from the Excel parse so the existing `#btnInject` commits byte-identical, with the Excel commit setting `EssayTextRequired=false` (D-05). Locked by a 5-scenario Playwright suite and approved 5/5 by live-browser UAT.**

## Performance

- **Duration:** ~25 min implementation (Tasks 1-2) + human-verify checkpoint (orchestrator-driven Playwright MCP UAT)
- **Tasks:** 2 auto (impl + e2e) + 1 checkpoint (human-verify, APPROVED)
- **Files modified:** 2 (view + controller); 1 created (e2e spec) + SEED_JOURNAL touched

## Accomplishments
- **N1 toggle** — room-level `name="step5Method"` radio (Isi via Form default / Import Excel); choosing Excel hides the whole `#step5FormPath` (395 form roster) and shows `#step5ExcelPanel` (mutually exclusive, D-03); hidden bound `#Step5Method` posts the choice to the server.
- **N2 panel** — Download Template (hidden-form POST carrying `#QuestionsJson` + UserIds + antiforgery → real `.xlsx` download), file picker (`.xlsx/.xls`), Upload & Pratinjau (fetch FormData → `UploadInjectExcel`).
- **N3 preview** — batch table (NIP/Nama, Skor Final %, Lulus/Tidak Lulus badge, Soal Terjawab) rendered from `Previews` via `.textContent`; **no certificate number** shown (preview-only gate, D-08).
- **N4 errors + blank warn** — invalid upload renders the **FULL** `Errors` list (not stop-at-first) in a red `.alert-danger`, hides the preview, and resets `injExcelAnswersCache` to `'[]'` (atomic — nothing commits, D-09); blank cell shows a warn-but-allow notice (D-06).
- **Controller D-05** — `req.EssayTextRequired = false` only when `vm.Step5Method == "excel"`; the form path keeps default `true`.
- **e2e** — 5-scenario Playwright spec (serial, DB snapshot/restore per CLAUDE.md Seed Workflow) covering toggle mutual-exclusivity, real template download, upload→preview→commit (anti silent-grade-0 at `/CMP/Results`), invalid upload full error list with 0-write, and blank-cell warn-but-allow.

## Task Commits

Each task was committed atomically (by the prior executor before the checkpoint):

1. **Task 1: Step-5 Excel UI (N1 toggle + N2 panel + N3 preview + N4 errors) + client JS + commit wiring** — `041f6e22` (feat) — `Views/Admin/InjectAssessment.cshtml` (+296), `Controllers/InjectAssessmentController.cs` (+5)
2. **Task 2: Playwright e2e inject-excel-396** — `63a1affa` (test) — `tests/e2e/inject-excel-396.spec.ts` (+343), `docs/SEED_JOURNAL.md` (+3)

**Task 3: Human-verify checkpoint** — verify-only (APPROVED, see below); no commit.

**Plan metadata:** this commit (docs: complete 396-04 plan — SUMMARY + STATE + ROADMAP).

## Files Created/Modified
- `Views/Admin/InjectAssessment.cshtml` — Step-5 N1 toggle + `#step5FormPath` wrapper + `#step5ExcelPanel` (download/upload/preview/errors) + client JS (toggle show/hide, hidden-form download POST, fetch upload + render via `.textContent`, `injExcelAnswersCache` gate, submit-listener branch for `#AnswersJson`).
- `Controllers/InjectAssessmentController.cs` — Excel commit sets `req.EssayTextRequired = false` (D-05) before the BLOCKING auto-gen guard; everything else (BLOCKING guard, `InjectBatchAsync`, TempData) unchanged.
- `tests/e2e/inject-excel-396.spec.ts` — 5 scenarios, `mode: 'serial'`, `--workers=1`, exceljs upload-file authoring, dbSnapshot BACKUP/RESTORE, SEED_JOURNAL cleaned.

## Decisions Made
- **Template download mechanism = hidden-form POST (not fetch-blob).** A programmatic `<form method=post action=DownloadInjectTemplate>` carrying `#QuestionsJson` + UserIds + the antiforgery token is submitted, producing a real browser file download (Playwright asserts the `download` event + `.xlsx` filename). Chosen over fetch-blob because it carries the antiforgery token cleanly and matches a native download.
- **e2e .xlsx authoring = build fresh with exceljs**, not edit the ClosedXML-generated template (exceljs.readFile is incompatible with ClosedXML output). The parser reads sheet `"Jawaban"` by name + column position, so a fresh equivalent file is accepted; documented in the spec header.
- **EssayTextRequired=false scoped to Excel only** — form path keeps default `true` (D-05).
- INJ-10 **not** marked complete here — consistent with Plans 01-03 which deliberately left INJ-10 open until the surface is fully in place; final close deferred to phase verification (398) per REQUIREMENTS.md traceability.

## Deviations from Plan

None — plan executed exactly as written (toggle mutual-exclusivity, download/upload/preview/error panel, `#AnswersJson` from Excel cache, `EssayTextRequired=false` for Excel commit, e2e + checkpoint). 0 migration.

## Verification

- **Build:** `dotnet build HcPortal.csproj` 0 error.
- **Fast suite:** 389/389 GREEN (no regression).
- **Playwright e2e (inject-excel-396):** GREEN from the MAIN working tree (AD-off) — 5 scenarios; orchestrator recorded 6/6 green (5 tests + setup). Build/run from main tree required (no Razor runtime compilation — view embedded at build).
- **0 migration** — view + 5-line controller scope + e2e only; no `Migrations/`/`Data/` diff.

## Human-Verify Checkpoint — APPROVED (5/5)

Live-browser UAT (orchestrator-driven Playwright MCP @ localhost:5277, AD-off, admin@pertamina.com). DB snapshot/restore per CLAUDE.md Seed Workflow; 0 leftover sessions; SEED_JOURNAL marked cleaned.

1. **N1 toggle Form↔Excel mutually-exclusive — PASS.** Form path hidden, Excel panel shown, Bahasa Indonesia text correct; toggles back cleanly.
2. **N2 Download Template — PASS.** Real `inject_template.xlsx` download; 2 sheets confirmed — `"Jawaban"` (matrix NIP|Nama|kolom-per-soal, prefilled one row per selected worker) + `"Legenda"` (No|Teks Soal|Tipe|Skor Maks|Opsi huruf=teks).
3. **N3 upload valid → preview → commit — PASS.** Preview Rino 100% Lulus, 2/2, **no cert# in preview**. After commit: DB session #173 Score=100, IsPassed=1, IsManualEntry=1, Status=Completed; 2 PackageUserResponses; AuditLog ManualInject=1; NomorSertifikat KPB/005/VI/2026 auto-generated; `/CMP/Results/173` rendered per-soal "Tinjauan Jawaban" (Soal1 MC Benar, Soal2 Essay Benar) + `/CMP/Certificate/173` downloadable. **Essay scored WITHOUT text accepted = D-05 confirmed.** Anti silent-grade-0 confirmed (score 100, not 0).
4. **D-09 invalid upload (MC "E", essay score 99>max, foreign NIP) — PASS.** Red alert "Perbaiki kesalahan berikut, lalu unggah ulang:" with the **FULL 3-item list** (not stop-at-first); preview hidden; client cache `"[]"`; DB 0 sessions for that title (atomic rollback).
5. **D-06 blank cell (MC empty + Essay 10) — PASS.** Warn "1 jawaban kosong di Excel — soal terkait dihitung 0. Periksa pratinjau sebelum commit." + preview Rino 50% Tidak Lulus 1/2 (warn-but-allow).

## Known Minor Items (non-blocking)

**[Cosmetic — do NOT fix in this plan] Legenda "Tipe" shows internal enum, not the UI label.** The template `"Legenda"` sheet's `"Tipe"` column shows the internal enum value `"MultipleChoice"` for a question whose UI label is `"Single Answer"` (LBL-02). HC sees `"MultipleChoice"` in the legend instead of `"Single Answer"`. Reference-only text (legend, not a data cell) — candidate for a 1-line polish (map enum→UI label in `InjectExcelHelper.GenerateTemplate` Legenda). Recommend bundling into Phase 398 or a backlog item; not blocking 396.

## Issues Encountered
None during planned work. (exceljs/ClosedXML round-trip incompatibility was anticipated in the plan and handled by building the upload file fresh — documented above.)

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- **Plan 396-05 ready:** the Excel inject path is now the single end-to-end entry-point for batch inject, so BulkBackfill (`TrainingAdminController.cs:787/836` + `Views/Admin/BulkBackfill.cshtml` + Section D links) can be hard-removed in 396-05 (INJ-11) without leaving HC without a tool.
- **0 migration** for this plan (and the whole phase so far). Handoff unchanged: branch main, notify IT migration=FALSE; ❌ no Dev/Prod edits.
- **INJ-10 UI surface complete**; final REQ close left to phase verification (398) per the 01-03 convention.

## Self-Check: PASSED
- FOUND: Views/Admin/InjectAssessment.cshtml
- FOUND: Controllers/InjectAssessmentController.cs
- FOUND: tests/e2e/inject-excel-396.spec.ts
- FOUND commit: 041f6e22 (feat 396-04 Step-5 Import Excel UI + commit wiring)
- FOUND commit: 63a1affa (test 396-04 Playwright e2e inject-excel-396)

---
*Phase: 396-import-excel-retire-bulkbackfill*
*Completed: 2026-06-18*
