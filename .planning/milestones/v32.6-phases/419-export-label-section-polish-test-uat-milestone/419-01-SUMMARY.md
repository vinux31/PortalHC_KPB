# Phase 419 Plan 01 — Summary

**Plan:** 419-01 — Wave-0 test scaffolding (RED)
**Status:** ✅ Complete
**Commits:** `ff64a342` (Task 1 xUnit RED), `ef5dea9d` (Task 2 e2e skeleton + drop D-02)
**Date:** 2026-06-24

## What was built

### Task 1 — xUnit RED (kontrak Wave 2)
- `HcPortal.Tests/ExportSectionLabelTests.cs` (PAG-04) — `IClassFixture<SectionFixture>`, drive REAL `ExcelExportHelper.AddDetailPerSoalSheet`:
  - RED: `BandHeader_RendersSectionLabelRow`, `BandHeader_OrdersBySectionNumberThenOrder` (band-header merged "Section {n}: {Nama}" + reorder per (SectionNumber, Order) belum ada).
  - GREEN: `NoSection_BackwardCompat` (assessment tanpa Section → tak ada band, output legacy).
- `HcPortal.Tests/SectionEtWarningTests.cs` (D-03) — drive REAL `AssessmentAdminController.ManagePackageQuestions` GET (12-dep ctor, semua null!/no-op kecuali `_context`):
  - RED: `CrossSiblingPool_Fires`, `GroupBySectionNumber_NotSectionId` (predikat lama per-paket tak fire).
  - GREEN: `FullCoverage_NoWarning_NonBlocking` (semantik non-blocking dikunci).
- Helper seed lokal: `SeedWorkerAsync`-equiv (AssessmentSession.UserId = FK → wajib seed `ApplicationUser` dulu; bug awal di-fix), `AddPackageWithEtAsync` (ET per soal).

### Task 2 — e2e skeleton
- 4 `tests/e2e/*-419.spec.ts` (`test.fixme`, `mode: serial`) untuk D-04.1–4 — ter-discover Playwright `--list` (5 tests/5 files OK). Import `{ test }` saja; helper di komentar untuk Plan 05.

## DEVIASI (penting) — D-02 / Plan 03 DI-DROP → backlog 999.16

**Temuan saat eksekusi:** `Models/InjectAssessmentDtos.cs` `InjectQuestionSpec` TIDAK punya `SectionId` → paket buatan `InjectAssessmentService.InjectBatchAsync` SELALU all-Lainnya. Satu-satunya surface LinkPrePost = jalur inject (Phase 397). Dengan keputusan user **skip-on-all-Lainnya**, guard D-02 menjadi **no-op/tak teramati** → tak ada RED bermakna, Plan 03 = kode mati defensif.

**Keputusan user (2026-06-24):** drop D-02 → backlog **999.16** (defensive guard, promote bila ada surface LinkPrePost non-inject). Konsekuensi yang sudah diselaraskan:
- `LinkPrePostSectionGuardTests.cs` TIDAK dibuat (dibatalkan dari Plan 01).
- `419-03-PLAN.md` dihapus (`git rm`).
- ROADMAP 419 plans: 5→4; CONTEXT D-02 ditandai DROPPED; backlog 999.16 dicatat.
- Plan 05 `depends_on` [01,02,04]; D-04.3 e2e di-reframe ke KOHERENSI (bukan blok).
- SEC-06 sync audit TETAP di Plan 05.

## Verifikasi
- `dotnet build HcPortal.Tests` exit 0 (RED via assertion, bukan compile-error).
- Full xUnit suite: **688 passed, 4 failed** (4 failed = tepat RED kontrak: BandHeader×2 + ET ×2). **0 regresi 415-418**. Total 692.
- Playwright `--list` 4 spec: OK (ter-discover, no import/syntax error).

## Kontrak untuk Wave 2+ (agar RED → GREEN)
- **Plan 02 (PAG-04):** `AddDetailPerSoalSheet` reorder kolom per (SectionNumber ?? max, Order, Id) + band-header merged "Section {n}: {Nama}" ("Lainnya" terakhir); `GeneratePerPesertaPdf` heading antar-blok; `.Include(q => q.Section)` di KEDUA export load site (Pitfall 1). Backward-compat: tanpa Section → tak ada band.
- **Plan 04 (D-03):** `ManagePackageQuestions` muat sibling packages (same `AssessmentSessionId`), pool ET by SectionNumber lintas-sibling, K=min(count Section antar sibling), fire bila DistinctEt > K. NON-BLOCKING.

## Next
Plan 02 (PAG-04 export label) — wave 2.
