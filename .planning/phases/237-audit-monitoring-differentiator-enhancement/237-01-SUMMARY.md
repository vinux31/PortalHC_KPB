---
phase: 237-audit-monitoring-differentiator-enhancement
plan: 01
subsystem: ui
tags: [cdp, coaching-proton, proton-data, monitoring, override, filter-cascade, audit-trail]

requires:
  - phase: 236-audit-completion
    provides: AuditLogService di-inject ke CDPController, IsCompleted/CompletedAt tracking
  - phase: 235-audit-execution-flow
    provides: CoachingProton tracking page dengan filter cascade dan pagination group-boundary

provides:
  - CoachingProton dropdown coachee yang konsisten dengan filter tahun aktif
  - OverrideSave dengan illegal transition validation (Approved ke Pending diblokir)
  - HCApprovalStatus auto-reset saat status override bukan Approved
  - Audit trail OverrideSave terverifikasi lengkap

affects: [237-02, 237-03, plan-03]

tech-stack:
  added: []
  patterns:
    - "STEP 4b: filter scopedCoacheeIds berdasarkan tahun sebelum build coachee dropdown"
    - "illegalTransitions Dictionary<string, HashSet<string>> untuk validasi transisi status"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Controllers/ProtonDataController.cs

key-decisions:
  - "Tahun filter bug dikonfirmasi dan difix: scopedCoacheeIds sekarang difilter by tahun sebelum dropdown coachee dibangun"
  - "Illegal transition: hanya Approved ke Pending yang diblokir; Approved ke Rejected diizinkan (untuk undo approval salah)"
  - "HCApprovalStatus di-reset ke Pending otomatis jika NewStatus bukan Approved (data consistency)"
  - "Audit trail OverrideSave sudah ada dan lengkap di L1437 — tidak perlu perubahan"

patterns-established:
  - "illegalTransitions pattern: mudah diperluas — tambah key baru ke Dictionary untuk aturan transisi tambahan"
  - "Filter cascade pattern: Bagian → Unit → (Track untuk Coach) → Tahun → Coachee sekarang sepenuhnya konsisten"

requirements-completed: [MON-02, MON-03]

duration: 35min
completed: 2026-03-23
---

# Phase 237 Plan 01: Audit CoachingProton Tracking & Override Flow Summary

**Tahun filter bug fix di CoachingProton dropdown coachee + illegal transition validation Approved-ke-Pending di OverrideSave, dengan audit trail terverifikasi lengkap**

## Performance

- **Duration:** 35 min
- **Started:** 2026-03-23T05:15:00Z
- **Completed:** 2026-03-23T05:50:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- MON-02: Tahun filter bug difix — dropdown coachee sekarang hanya menampilkan coachee yang punya assignment di tahun yang dipilih
- MON-02: Filter cascade audit dikonfirmasi benar (Bagian → Unit → Track → Tahun → Coachee), pagination group-boundary sudah ada sejak Phase 161/235
- MON-02: Role-based column visibility dikonfirmasi — kolom SrSpv/SH/HC tampil read-only untuk semua role (by-design); tombol aksi hanya tampil untuk role yang berwenang
- MON-03: OverrideSave memblokir transisi Approved ke Pending dengan pesan error JSON yang informatif
- MON-03: HCApprovalStatus auto-reset ke Pending jika override ke non-Approved status (data consistency fix)
- MON-03: Audit trail `_auditLog.LogAsync` di OverrideSave dikonfirmasi sudah ada dan lengkap (oldStatus → newStatus + alasan)

## Task Commits

1. **Task 1: Audit CoachingProton tracking (MON-02)** - `7b63fd1e` (fix)
2. **Task 2: Fix OverrideSave transition validation (MON-03)** - `9812347f` (fix)

## Files Created/Modified

- `Controllers/CDPController.cs` — Tambah STEP 4b: filter scopedCoacheeIds berdasarkan tahun aktif sebelum build coachee dropdown
- `Controllers/ProtonDataController.cs` — Tambah illegal transition validation + HCApprovalStatus consistency check di OverrideSave

## Decisions Made

- **Tahun filter scope**: Filter tahun di-apply ke `scopedCoacheeIds` via join ke `ProtonTrackAssignments.ProtonTrack.TahunKe` — konsisten dengan cara filter track bekerja
- **Illegal transition scope**: Hanya blokir Approved → Pending. Approved → Rejected diizinkan karena admin mungkin perlu undo approval yang salah. Ini sesuai recommendation dari research
- **HCApprovalStatus reset**: Jika NewStatus bukan Approved, NewHCStatus di-force ke Pending (bukan dikembalikan error). Server silently corrects inconsistency karena form UI tidak seharusnya submit kombinasi ini

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] HCApprovalStatus consistency check**
- **Found during:** Task 2 (Fix OverrideSave)
- **Issue:** Research (Pitfall 4) mencatat bahwa admin bisa set Status=Approved tapi HCApprovalStatus=Reviewed untuk deliverable yang belum seharusnya. Ini data inconsistency yang perlu dijaga
- **Fix:** Tambah logic: jika req.NewStatus != "Approved" dan req.NewHCStatus == "Reviewed", maka req.NewHCStatus di-reset ke "Pending" sebelum disimpan
- **Files modified:** Controllers/ProtonDataController.cs
- **Verification:** Build pass, logic inline sebelum SaveChangesAsync
- **Committed in:** `9812347f` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Auto-fix esensial untuk data consistency. Tidak ada scope creep.

## Issues Encountered

None — semua temuan riset dikonfirmasi valid. Kode existing sudah lebih matang dari yang dikira (pagination group-boundary dan role-based columns sudah benar).

## User Setup Required

None - tidak ada konfigurasi eksternal yang diperlukan.

## Next Phase Readiness

- Plan 02: Audit Export (MON-04) — ExportHistoriProton perlu role attribute, ExportProgressExcel perlu Coach role, 3 export baru
- Plan 03: Differentiator Features (DIFF-01, DIFF-02, DIFF-03) — workload indicator, batch HC approval, bottleneck chart
- Semua filter cascade sekarang konsisten — export baru di Plan 02 harus menggunakan parameter filter yang sama persis

---
*Phase: 237-audit-monitoring-differentiator-enhancement*
*Completed: 2026-03-23*
