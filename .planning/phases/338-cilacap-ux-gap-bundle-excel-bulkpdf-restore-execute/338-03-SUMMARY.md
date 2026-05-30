---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
plan: 03
subsystem: excel-export
tags: [excel, aggregate, closedxml, additive]

requires:
  - phase: 320
    provides: SpiderChartRenderer + pre-loaded data pattern di ExportAssessmentResults
provides:
  - CIL-05 Excel +2 aggregate sheet ADDITIVE (Detail Per Soal grid + Elemen Teknis matrix)
  - Per-peserta sheets EXISTING preserve (no breaking change tool external)

affects: [338-04 REST-04 — restore Cilacap can reference aggregate sheet format]

tech-stack:
  added: []
  patterns:
    - "ClosedXML helper static method reusable: AddDetailPerSoalSheet + AddElemenTeknisSheet"
    - "Reuse pre-loaded data L4250-4266 (NO new DB query)"
    - "Sheet ordering ADDITIVE: Summary(1) → DetailPerSoal(2) → ElemenTeknis(3) → per-peserta(4+)"
    - "Score percentage = CorrectCount/QuestionCount*100 (matches SpiderChartRenderer formula)"
    - "PackageQuestion sort by Order (stable key per AssessmentPackage.cs L38 comment)"

key-files:
  created: []
  modified:
    - Helpers/ExcelExportHelper.cs
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "Field name corrections vs plan: PackageOptionId (BUKAN SelectedOptionId), TextAnswer (BUKAN AnswerText), CorrectCount/QuestionCount (BUKAN Score decimal)"
  - "Sort by Order field BUKAN Id (Phase 308 comment line 38)"
  - "Sheet position 2+3 setelah Summary (BUKAN 1+2) — preserve existing Summary position 1"
  - "Cell Benar?: ✓ hijau / ✗ merah / — abu derive dari PackageOption.IsCorrect (MC) atau EssayScore>=ScoreValue/2 (Essay)"
  - "ADDITIVE only — per-peserta sheets EXISTING L4296+ tidak diubah (T-338-01 mitigation)"

patterns-established:
  - "Aggregate sheets vs per-peserta detail: HC scan/pivot view + drill-down view coexist di workbook"

requirements-completed: [CIL-05]

duration: ~25min
completed: 2026-05-30
---

# Phase 338-03: CIL-05 Excel +2 Aggregate Sheet Summary

**CIL-05 HIGH PRIORITY auto-Playwright UAT 1/1 PASS. 2 commit lokal. xlsx structure verified via JSZip.**

## Performance

- **Duration:** ~25 min (2 helper + 1 integration + 1 UAT)
- **Completed:** 2026-05-30
- **Files modified:** 2
- **Build status:** PASS 0 error

## Accomplishments

- 2 new ClosedXML static helpers di ExcelExportHelper.cs reusable untuk Export endpoint future
- ExportAssessmentResults workbook contains 4 sheet (was 2):
  - Sheet 1: Summary (existing)
  - Sheet 2: Detail Per Soal (NEW aggregate) — grid per-peserta-per-soal
  - Sheet 3: Elemen Teknis (NEW aggregate) — matrix peserta x elemen
  - Sheet 4+: Per-peserta sheets (existing preserve)
- HC dapat scan/pivot semua peserta dalam 1 view tanpa klik-klik N sheet
- Per-peserta drill-down sheets preserved untuk detail individual
- Reuse pre-loaded data — no new DB query, no performance regression

## Task Commits

1. **T1+T2-338-03: 2 helper functions** — `6624f12c` (feat)
2. **T3-338-03: Integrate di ExportAssessmentResults** — `f280f7a2` (feat)

## Files Modified

- `Helpers/ExcelExportHelper.cs` (+157 LOC) — `AddDetailPerSoalSheet` + `AddElemenTeknisSheet` static methods
- `Controllers/AssessmentAdminController.cs` (+7 LOC) — 2 helper call insert L4266+ setelah pre-load `allResponses`/`allEtScores`/`allQuestions`, sebelum PNG pre-compute L4268

## UAT Verification (Auto-Playwright + JSZip xlsx parse)

**Test target:** GET /Admin/ExportAssessmentResults?title=UAT%20v14%20Standard&category=OJT&scheduleDate=2026-04-10

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| CIL-05 | ✅ PASS | xlsx response 200 + content-type openxml-spreadsheet. JSZip parse workbook.xml: 4 sheets in correct order: `<x:sheet name="Summary" sheetId="1"/> <x:sheet name="Detail Per Soal" sheetId="2"/> <x:sheet name="Elemen Teknis" sheetId="3"/> <x:sheet name="123456_Iwan" sheetId="4"/>`. Sheet 3 (Elemen Teknis) header verified: `Nama \| NIP \| Bulk UAT \| HSSE \| Pengetahuan Umum \| Proses Kilang \| Avg`. Row 2 data: `Iwan \| 123456 \| 55.6 \| 100 \| 100 \| 0 \| 63.9` — Avg arithmetic verified ((55.6+100+100+0)/4 = 63.9 ✓). Sheet 2 (Detail Per Soal) dimension A1:AB2 (28 columns = 3 fixed + 12 soal × 2 + 1 Skor Total). |

**Coverage:** 1/1 REQ browser-Playwright xlsx structure + content verified.

## Threats

| Threat ID | Status |
|-----------|--------|
| T-338-01 Excel format breaking change | mitigated (ADDITIVE — per-peserta sheets existing tidak diubah) |
| T-338-03 N+1 query | accept (reuse pre-loaded data, no new query) |
| Cell injection via TextAnswer | mitigated (truncate 200 char + ClosedXML escape) |

## Seed Workflow

- No temp seed (UAT pakai existing UAT v14 Standard session 2026-04-10)
- DB baseline preserved

## Lessons & Surprises

- **Plan model field assumption WRONG**: PackageUserResponse uses `PackageOptionId` (not `SelectedOptionId`), `TextAnswer` (not `AnswerText`), no `IsCorrect` field (derive dari PackageOption.IsCorrect). SessionElemenTeknisScore uses `CorrectCount` + `QuestionCount` int (not raw `Score` decimal).
- Score percentage formula consistent dengan SpiderChartRenderer L4280 — keep math identical across views.
- PackageQuestion `Order` field comment L38 explicitly call out "stable sort key" — use ini bukan Id.
- ClosedXML default uses sharedStrings table dengan inline `t="s"` cell reference, not inline `<is>` — JSZip parse butuh extract `<t>` dari sharedStrings.xml not from sheet xml directly.
- 4 elemen teknis distinct di UAT v14 Standard session: Bulk UAT, HSSE, Pengetahuan Umum, Proses Kilang.

## Next

- Wave 4 Plan 338-04 (REST-04 BulkBackfillAssessment + CIL-06 BulkExportPdf ZIP)
