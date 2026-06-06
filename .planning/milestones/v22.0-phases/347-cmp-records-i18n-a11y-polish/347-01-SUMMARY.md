---
phase: 347-cmp-records-i18n-a11y-polish
plan: 01
subsystem: CMP/Records views (i18n)
tags: [i18n, razor, polish]
requires: [Phase 345 null-badge, Phase 346 REC-01..09]
provides: [Bahasa Indonesia badge/header/label/filter di 3 view CMP/Records]
affects: [Views/CMP/Records.cshtml, Views/CMP/RecordsWorkerDetail.cshtml, Views/CMP/RecordsTeam.cshtml]
tech-stack:
  added: []
  patterns: ["@OrgLabels.GetLabel(0) untuk label organisasi (Phase 343 global inject)"]
key-files:
  created: []
  modified:
    - Views/CMP/Records.cshtml
    - Views/CMP/RecordsWorkerDetail.cshtml
    - Views/CMP/RecordsTeam.cshtml
key-decisions:
  - "POL-01: hanya teks tampil badge case true/false yang diubah; class bg-success/bg-danger + switch-key status + null-case @item.Status (Menunggu Penilaian) verbatim"
  - "POL-03 Section: pakai @OrgLabels.GetLabel(0) (bukan hardcode) — konsisten Phase 343"
  - "POL-10 label tombol (Lihat Hasil/Sertifikat): verify-only, sudah konsisten — no-op edit"
requirements-completed: [POL-01, POL-02, POL-03, POL-04, POL-05, POL-10]
duration: 8 min
completed: 2026-06-04
---

# Phase 347 Plan 01: CMP/Records i18n Bahasa Indonesia Summary

Swap teks Inggris → Bahasa Indonesia pada 3 view CMP/Records (badge, header, label info worker, opsi filter, subtitle) — pure i18n, zero behavior/logic/query change.

**Duration:** ~8 min | **Tasks:** 3 | **Files:** 3 modified | **Commits:** 3 (feat 347-01)

## What Was Built

### Task 1 — Records.cshtml (My Records)
- Badge `Passed`→`Lulus`, `Failed`→`Tidak Lulus` (case `IsPassed==true/false` saja).
- Header kolom `Score`→`Nilai`.
- **Tidak disentuh:** class `bg-success`/`bg-danger`; switch-key `"Passed" => "bg-success"` (data status, bukan teks tampil); null-case `@item.Status` = "Menunggu Penilaian" amber (Phase 345).

### Task 2 — RecordsWorkerDetail.cshtml
- Badge `Lulus`/`Tidak Lulus` (true/false); `PendingGrading` null-case utuh.
- Subtitle → "Lihat detail rekam jejak penilaian dan pelatihan anggota tim."
- Label info worker `Position`→`Jabatan`; `Section`→`@OrgLabels.GetLabel(0)`.

### Task 3 — RecordsWorkerDetail + RecordsTeam (filter options)
- WorkerDetail: opsi default `All Categories/Sub Categories/Types` → `Semua Kategori/Semua Sub Kategori/Semua Tipe` (markup + JS rebuild `subSelect.innerHTML`).
- RecordsTeam: header `Position`→`Jabatan`; opsi `All Categories/Sub Categories` → `Semua Kategori/Semua Sub Kategori` (markup + JS rebuild).
- POL-10 label tombol (Lihat Hasil/Sertifikat): verifikasi — sudah konsisten, no-op.

## Verification (grep gate, all PASS)

| Check | Expected | Result |
|-------|----------|--------|
| Records present (Lulus/Tidak Lulus/Nilai) | 3 | 3 ✓ |
| Records EN-gone (Passed/Failed span) | 0 | 0 ✓ |
| Records switch-key intact | 1 | 1 ✓ |
| WorkerDetail present (Lulus/Tidak Lulus/Jabatan/subtitle) | 4 | 4 ✓ |
| WorkerDetail EN-gone (Passed/Failed/Position/Section/for team member) | 0 | 0 ✓ |
| WD+Team All* EN-gone | 0 | 0 ✓ |
| Team Jabatan header | ≥1 | 1 ✓ |
| PendingGrading non-regression (R+W) | 2 | 2 ✓ |

Build gate (`dotnet build`) + Playwright visual = plan 347-04.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Next Phase Readiness

Ready for 347-02 (a11y attributes). Wave 2 menyentuh file yang sama — line numbers bergeser, re-grep sebelum edit.
