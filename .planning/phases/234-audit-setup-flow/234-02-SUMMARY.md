---
phase: 234-audit-setup-flow
plan: "02"
subsystem: AdminController / CoachCoacheeMapping
tags: [transaction, integrity, progression-warning, coach-coachee]
dependency_graph:
  requires: []
  provides: [atomic-deactivate, atomic-reactivate, progression-warning-assign]
  affects: [CoachCoacheeMappingDeactivate, CoachCoacheeMappingReactivate, CoachCoacheeMappingAssign]
tech_stack:
  added: []
  patterns: [BeginTransactionAsync, explicit-variable-capture]
key_files:
  modified:
    - Controllers/AdminController.cs
decisions:
  - "Gunakan explicit variable capture (var originalEndDate = mapping.EndDate) bukan fragile EF OriginalValues API"
  - "Progression check berbasis TrackType + Urutan agar Panelman dan Operator tidak cross-check"
  - "Warning-only (bukan block) untuk progression — user bisa override dengan ConfirmProgressionWarning"
metrics:
  duration: "15 min"
  completed_date: "2026-03-22"
  tasks: 2
  files_modified: 1
---

# Phase 234 Plan 02: Transaction Wrapping + Progression Warning Summary

One-liner: Transaction atomik pada cascade deactivate/reactivate mapping + progression warning Tahun 1→2→3 via ConfirmProgressionWarning flag.

## Objective

Audit dan fix coach-coachee mapping cascade integrity + tambah assignment progression validation di AdminController.

## Tasks Completed

### Task 1: Transaction Wrapping Deactivate + Reactivate (D-05, D-08)

**CoachCoacheeMappingDeactivate** — Dibungkus `BeginTransactionAsync`. Jika cascade deactivate ProtonTrackAssignments gagal, seluruh operasi di-rollback. Error di-log dengan mapping ID.

**CoachCoacheeMappingReactivate** — Dibungkus `BeginTransactionAsync`. Fix utama: ganti `entry.OriginalValues["EndDate"]` (fragile EF API) dengan `var originalEndDate = mapping.EndDate` yang di-capture SEBELUM `mapping.EndDate = null`. Ini lebih reliable dan tidak bergantung pada EF change tracker state.

**Commit:** 3165aa3

### Task 2: Progression Warning Tahun 1→2→3 di Assign (D-09, D-10, D-11, D-12)

Ditambahkan progressive check sebelum create mapping baru:
- Cari track sebelumnya berdasarkan `TrackType == requestedTrack.TrackType && Urutan == requestedTrack.Urutan - 1`
- Untuk setiap coachee: skip jika sudah pernah punya assignment di requested track (D-11: reactivated scenario)
- Cek apakah semua `ProtonDeliverableProgress.Status == "Approved"` di previous track
- Jika belum selesai dan user belum konfirmasi: return `{ warning = true, message = "...", incompleteCount }`
- Jika user set `ConfirmProgressionWarning = true`: lanjut assign (D-09: warning only)

`ConfirmProgressionWarning` ditambahkan ke `CoachAssignRequest` class.

**Commit:** 3165aa3

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check

- [x] Controllers/AdminController.cs dimodifikasi
- [x] `BeginTransactionAsync` ada di CoachCoacheeMappingDeactivate (L4292)
- [x] `BeginTransactionAsync` ada di CoachCoacheeMappingReactivate (L4373)
- [x] `var originalEndDate = mapping.EndDate` sebelum modifikasi (L4369)
- [x] `OriginalValues["EndDate"]` tidak ada lagi di file
- [x] `ConfirmProgressionWarning` property di CoachAssignRequest (L7682)
- [x] `incompleteCoachees` list logic di CoachCoacheeMappingAssign (L3991)
- [x] `warning = true` JSON response (L4020)
- [x] Commit 3165aa3 exist
