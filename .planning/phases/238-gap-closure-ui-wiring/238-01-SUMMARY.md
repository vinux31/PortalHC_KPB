---
phase: 238-gap-closure-ui-wiring
plan: "01"
subsystem: UI/Views
tags: [ui-wiring, coaching, export, gap-closure]
dependency_graph:
  requires: []
  provides: [progression-warning-override-ui, edit-delete-session-buttons, export-laporan-buttons]
  affects: [Views/Admin/CoachCoacheeMapping.cshtml, Views/CDP/Deliverable.cshtml, Views/CDP/CoachingProton.cshtml]
tech_stack:
  added: []
  patterns: [AJAX-confirm-override, role-gated-buttons, Url.Action-export-links]
key_files:
  created: []
  modified:
    - Views/Admin/CoachCoacheeMapping.cshtml
    - Views/CDP/Deliverable.cshtml
    - Views/CDP/CoachingProton.cshtml
decisions:
  - "Progression warning confirm dialog: re-send payload dengan ConfirmProgressionWarning=true pada OK"
  - "Edit/Delete session: role-gated coach pemilik (session.CoachId == currentUserId) + HC/Admin"
  - "3 export baru: hanya tampil untuk HC/Admin karena audience utama laporan agregat"
metrics:
  duration_minutes: 15
  completed_date: "2026-03-23"
  tasks_completed: 3
  tasks_total: 3
  files_changed: 3
---

# Phase 238 Plan 01: Gap Closure UI Wiring Summary

**One-liner:** Hubungkan 3 backend endpoint ke UI — progression warning override confirm dialog, Edit/Delete coaching session buttons, dan 3 tombol export laporan agregat HC/Admin.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Wire progression warning override di CoachCoacheeMapping AJAX | 592f80d7 | Views/Admin/CoachCoacheeMapping.cshtml |
| 2 | Tambah tombol Edit/Delete coaching session di Deliverable.cshtml | ac86e91d | Views/CDP/Deliverable.cshtml |
| 3 | Tambah 3 tombol export baru di CoachingProton.cshtml | b7ca92dc | Views/CDP/CoachingProton.cshtml |

## What Was Done

### Task 1: Progression Warning Override (CoachCoacheeMapping.cshtml)
AJAX handler CoachCoacheeMappingAssign sebelumnya hanya menangani `data.success` dan `data.message`. Ditambahkan branch `data.warning` yang:
1. Menampilkan `confirm(data.message)` — pesan dari backend tentang coachee yang belum selesai
2. Jika user klik OK: kirim ulang payload yang sama dengan tambahan `ConfirmProgressionWarning: true`
3. Jika user klik Cancel: tidak ada aksi

### Task 2: Edit/Delete Session Buttons (Deliverable.cshtml)
- Deklarasi `currentUserId` dan `isHcOrAdmin` di awal block Razor
- Di dalam foreach session, setelah tanggal, tambahkan tombol dengan role gate `isHcOrAdmin || session.CoachId == currentUserId`
- Tombol Edit: link GET ke `/CDP/EditCoachingSession?id={session.Id}`
- Tombol Delete: form POST ke `/CDP/DeleteCoachingSession` dengan hidden `id` dan confirm dialog

### Task 3: 3 Export Buttons (CoachingProton.cshtml)
Tepat setelah block export existing (SrSpv/SH/HC/Admin per coachee), tambahkan block baru khusus HC/Admin:
- Bottleneck Report → `ExportBottleneckReport`
- Coaching Tracking → `ExportCoachingTracking`
- Workload Summary → `ExportWorkloadSummary`

## Deviations from Plan

None - plan executed exactly as written.

## Build Verification

`dotnet build --no-restore`: 0 errors, 66 warnings (semua warnings pre-existing CA1416 LDAP).

## Self-Check: PASSED
