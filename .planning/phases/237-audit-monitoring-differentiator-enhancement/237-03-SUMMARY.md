---
phase: 237-audit-monitoring-differentiator-enhancement
plan: "03"
subsystem: backend
tags: [asp.net-core, cdp, export, batch-approve, proton-coaching]

requires:
  - phase: 237-01
    provides: filter bug fixes sebagai fondasi
  - phase: 237-02
    provides: dashboard stats + bottleneck chart + workload indicator

provides:
  - ExportHistoriProton role-protected (Sr Supervisor/SH/HC/Admin) (MON-04)
  - ExportProgressExcel Coach role ditambahkan (MON-04)
  - ExportBottleneckReport: deliverable pending >30 hari (MON-04)
  - ExportCoachingTracking: tracking per coachee dengan filter cascade (MON-04)
  - ExportWorkloadSummary: beban kerja per coach (MON-04)
  - BatchHCApprove endpoint + UI checkbox multi-select (DIFF-02)

affects:
  - Browser verification (Task 3 checkpoint)

tech-stack:
  added: []
  patterns:
    - "BatchHCApproveRequest DTO + [FromBody] endpoint + race guard (Status==Submitted && HCApprovalStatus==Pending)"
    - "3 export baru menggunakan ExcelExportHelper.CreateSheet + ToFileResult pattern yang sama"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/CoachingProton.cshtml

key-decisions:
  - "ExportHistoriProton: tambah explicit Authorize role attr — sebelumnya hanya class-level auth"
  - "ExportProgressExcel: Coach ditambahkan ke role attr karena scope validation sudah ada di body"
  - "BatchHCApprove race guard: hanya proses Status==Submitted && HCApprovalStatus==Pending"
  - "Batch approve JS: @if(isHC) dihapus dari scripts section (Razor parsing issue) — null check di JS sudah cukup"

requirements-completed: [MON-04, DIFF-02]

duration: 20min
completed: 2026-03-23
---

# Phase 237 Plan 03: Export Audit + Batch HC Approve Summary

**Export actions diaudit untuk role protection, 3 export baru ditambah untuk monitoring, dan HC bisa batch approve deliverable Pending HC Review sekaligus.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-03-23T05:20:00Z
- **Completed:** 2026-03-23T05:46:00Z
- **Tasks:** 2 (Task 3 = checkpoint, belum selesai)
- **Files modified:** 2

## Accomplishments

- MON-04 Role attr fixes: ExportHistoriProton sekarang punya explicit `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]`; ExportProgressExcel sekarang include Coach role
- MON-04 ExportBottleneckReport: query deliverable Submitted >30 hari, sort by hari pending desc, Excel dengan kolom Coachee/NIP/Bagian/Coach/Deliverable/SubKompetensi/Kompetensi/TanggalSubmit/HariPending
- MON-04 ExportCoachingTracking: export tracking dengan filter cascade (coacheeId, bagian, unit, trackType, tahun), role-scoped per user level
- MON-04 ExportWorkloadSummary: grouping per coach, hitung coachee aktif + deliverable pending Spv review + pending HC review
- DIFF-02 BatchHCApprove: endpoint HTTP POST dengan race guard, audit log, return JSON `{success, approvedCount, skippedCount}`
- DIFF-02 UI: checkbox column di header (selectAll) + per baris item HC pending, Approve Selected button, modal konfirmasi Bootstrap, AJAX POST

## Task Commits

1. **Task 1: Audit export role attrs + 3 export baru (MON-04)** - `4925cf84` (feat)
2. **Task 2: Batch HC Approve endpoint + UI (DIFF-02)** - `5000f32c` (feat)

## Files Created/Modified

- `Controllers/CDPController.cs` — BatchHCApproveRequest DTO, BatchHCApprove endpoint, ExportBottleneckReport, ExportCoachingTracking, ExportWorkloadSummary, role attr fixes di ExportHistoriProton + ExportProgressExcel
- `Views/CDP/CoachingProton.cshtml` — checkbox column di thead, per-row batch-check, batchApproveBtn dengan selectedCount, modal konfirmasi, JS selectAll + AJAX

## Decisions Made

- ExportHistoriProton memerlukan explicit role attr karena class-level auth saja tidak cukup membatasi akses
- Coach ditambah ke ExportProgressExcel karena scope validation sudah ada di body (coacheeId check via mapping)
- Batch approve race guard: hanya item masih `Status=="Submitted" && HCApprovalStatus=="Pending"` yang diproses — konsisten dengan button individual HC review di view
- JS `@if(isHC)` di dalam scripts section dihapus (Razor parser error: `function(cb)` diparse sebagai tipe C#) — JS run unconditionally dengan null checks cukup karena elemen tidak ada jika bukan HC

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Property name mismatch: Nama vs NamaDeliverable/NamaSubKompetensi/NamaKompetensi**
- **Found during:** Task 1 build verification
- **Issue:** ExportBottleneckReport dan ExportCoachingTracking menggunakan `.Nama` yang tidak ada di ProtonDeliverable/ProtonSubKompetensi/ProtonKompetensi
- **Fix:** Ubah ke `.NamaDeliverable`, `.NamaSubKompetensi`, `.NamaKompetensi` sesuai model aktual
- **Files modified:** Controllers/CDPController.cs
- **Commit:** 4925cf84 (sudah di-fix sebelum commit)

**2. [Rule 1 - Bug] @if(isHC) Razor parsing error dalam scripts section**
- **Found during:** Task 2 build verification
- **Issue:** `@if (isHC)` di dalam `@section Scripts` menyebabkan Razor mengparse `function(cb)` sebagai C# — error CS0246 tipe tidak ditemukan
- **Fix:** Hapus wrapper `@if(isHC)` dari scripts section, JS berjalan unconditionally dengan null checks pada getElementById
- **Files modified:** Views/CDP/CoachingProton.cshtml
- **Commit:** 5000f32c (sudah di-fix sebelum commit)

## Issues Encountered

None beyond auto-fixed deviations.

## Checkpoint Pending

Task 3 (Browser Verification) belum selesai — menunggu konfirmasi user di browser.

## User Setup Required

None - tidak ada external service configuration.

## Next Phase Readiness

- Task 3 checkpoint: user perlu verifikasi browser semua fitur Phase 237
- Build bersih tanpa error

---
*Phase: 237-audit-monitoring-differentiator-enhancement*
*Completed: 2026-03-23*
