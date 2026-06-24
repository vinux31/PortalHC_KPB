# Phase 419 Plan 02 — Summary

**Plan:** 419-02 — PAG-04 export label Section (Excel band-header + PDF heading)
**Status:** ✅ Complete
**Commits:** `18f67b4e` (Task 1 Excel), `dee82bca` (Task 2 PDF + eager-load)
**Date:** 2026-06-24

## What was built

### Task 1 — Excel `AddDetailPerSoalSheet` Section-aware (`Helpers/ExcelExportHelper.cs`)
- Ordering kolom soal: `OrderBy(Section?.SectionNumber ?? int.MaxValue).ThenBy(Order).ThenBy(Id)` (canonical, mirror 416/417).
- Band-header merged per Section di atas grup kolom soal: `"Section {n}: {Nama}"` (Lainnya terakhir), bold + center + LightBlue.
- **Cabang baris (Pitfall 4 off-by-one):** `anySection` → band row 1, header row 2, data row 3, `FreezeRows(2)`. Tanpa Section → header row 1, data row 2, `FreezeRows(1)` = **legacy identik**.
- Kill-drift: `BuildAnswerCell`/`IsQuestionCorrect`/"Skor Total" VERBATIM. D-12: tanpa skor per-Section.

### Task 2 — PDF `GeneratePerPesertaPdf` + eager-load (`Controllers/AssessmentAdminController.cs`)
- Group soal `GroupBy(Section?.SectionNumber).OrderBy(key ?? max)`; heading `"Section {n}: {Nama}"` (QuestPDF `.Text().Bold().FontSize(12).Blue.Darken2`) sebelum tiap blok. Backward-compat: tanpa Section → suppress heading. `qNum` global 1..N lintas grup.
- Ordering `sessionQuestions` → canonical (SectionNumber, Order, Id).
- **Pitfall 1 fix:** `.Include(q => q.Section)` di KEDUA export load site (Excel `:5425`, PDF `:5673`) — tanpa ini `q.Section` null → band/heading semua "Lainnya" senyap.
- Kill-drift block (`IsQuestionCorrect`+`BuildAnswerCell`) + DoS guard 50 peserta utuh.

## Verifikasi
- `dotnet build HcPortal.csproj` exit 0.
- `ExportSectionLabelTests` **3/3 GREEN** (band-header, ordering, backward-compat).
- Kill-drift regresi (aggregator/answer-cell/IsQuestionCorrect) **30/30 GREEN**.
- Full xUnit suite: **690 passed, 2 failed** — 2 failed = `SectionEtWarningTests.CrossSiblingPool_Fires` + `GroupBySectionNumber_NotSectionId` (RED kontrak **Plan 04**). 0 regresi lain. Total 692.
- grep: `Include(q => q.Section)` 2 export site (7 total di file termasuk 5 pre-existing), PDF `GroupBy(sq => sq.Section`, label format `Section {`.

## Catatan
- `anySection` Excel = `sortedQuestions.Any(q => q.Section != null)`; PDF = `sessionQuestions.Any(sq => sq.Section != null)`.
- Band/heading = **label organisasi saja** (D-12, SREP-01 deferred).

## Next
Plan 04 (D-03 ET-warning re-spec lintas-sibling) — wave 3. Akan membuat 2 RED tersisa GREEN.
