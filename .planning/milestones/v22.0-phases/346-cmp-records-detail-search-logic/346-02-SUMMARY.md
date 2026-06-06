---
phase: 346-cmp-records-detail-search-logic
plan: 02
subsystem: CMP/Records (Worker Detail drill-down)
tags: [view, razor, authz-synergy, modal]
requires: []
provides: ["Worker Detail Lihat Hasil button", "modal Kategori/SubKategori"]
affects: ["Views/CMP/RecordsWorkerDetail.cshtml"]
tech-stack:
  added: []
  patterns: ["un-gated action button (gate on RecordType not GenerateCertificate)"]
key-files:
  created: []
  modified: ["Views/CMP/RecordsWorkerDetail.cshtml"]
key-decisions:
  - "Lihat Hasil un-gated dari GenerateCertificate (gate = RecordType+AssessmentSessionId) — setiap assessment submitted punya Results"
  - "returnUrl pakai browser back-button, TIDAK ubah signature action Results (out of scope, hindari ripple semua caller)"
requirements-completed: [REC-03, REC-05]
duration: 6 min
completed: 2026-06-04
---

# Phase 346 Plan 02: Worker Detail — Lihat Hasil + Modal Kategori/SubKategori Summary

Halaman Worker Detail `/CMP/RecordsWorkerDetail`: (REC-03) tombol `Lihat Hasil`→`/CMP/Results` di tiap row Assessment Online (un-gated dari `GenerateCertificate`), dan (REC-05) modal training existing diperkaya row Kategori + SubKategori. Keduanya view-only — data sudah ada di kolom tabel.

**Tasks:** 2 | **Files:** 1 | **Commits:** 2 (`bcf86cb7` Task 1, `b894fa0f` Task 2)

## What was built

- **Task 1** (`bcf86cb7`): cabang assessment di action column diubah dari `else if (GenerateCertificate && AssessmentSessionId.HasValue)` (cuma render Certificate) → `else if (RecordType=="Assessment Online" && AssessmentSessionId.HasValue)` membungkus `d-flex gap-1`: `Lihat Hasil` (asp-action Results, `bi-bar-chart-line`) **selalu** muncul + `Sertifikat` (asp-action Certificate, `bi-award`) tetap kondisional `@if (item.GenerateCertificate)`. Fallback `—` dipertahankan.
- **Task 2** (`b894fa0f`): modal `<dl>` +2 row (`mdKategori`/`mdSubKategori`) setelah Nomor Sertifikat; Detail button +`data-kategori`/`data-subcategory`; JS handler +2 assignment `btn.dataset.kategori/subcategory`.

## Verification

- `dotnet build` → Build succeeded, 0 Error.
- grep: `asp-action="Results"`+`Lihat Hasil`+`bi-bar-chart-line` ✓ · `asp-action="Certificate"`+`bi-award` masih ada (gated) ✓ · `id="mdKategori"`/`id="mdSubKategori"` ✓ · `data-kategori`/`data-subcategory` pada button ✓ · JS set keduanya dari dataset ✓.
- `data-subcategory` muncul 2× = button baru (1) + atribut row client-side category-filter pre-existing (1) — bukan duplikasi salah.

## Deviations from Plan

None — plan executed exactly as written.

**returnUrl decision (per plan):** action `Results` TIDAK ditambah param `returnUrl` (akan ubah signature + semua caller). Andalkan browser back-button (breadcrumb back-link existing). Sesuai instruksi plan Task 1.

## Self-Check: PASSED

- file modified exists ✓ · 2 commits present (`git log --grep="346-02"`) ✓ · acceptance_criteria re-run PASS ✓ · build green ✓.

## Notes

- **Sinergi REC-04 (346-03):** tombol `Lihat Hasil` baru akan benar-benar ter-authorize untuk atasan L3/L4 SETELAH 346-03 melonggarkan authz `Results` (saat ini `Results` masih `owner||Admin||HC` → L3/L4 atasan Forbid). AUTHZ-01 side-fix (tombol Sertifikat dead L3/L4) juga beres via 346-03.
- UAT visual (klik Lihat Hasil → Results untuk L3 & L4-section; L4 beda section Forbid) ditunda ke 346-06 (depends REC-04).
- Ready for 346-03 (Wave 1, CMPController.cs authz — independent file).
