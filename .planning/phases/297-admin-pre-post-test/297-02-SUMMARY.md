---
phase: 297-admin-pre-post-test
plan: 02
subsystem: assessment-admin
tags: [pre-post-test, monitoring, manage-assessment, grouping, expandable-rows]
dependency_graph:
  requires: [297-01]
  provides: [monitoring-prepost-grouping, manage-assessment-prepost-badge, ppt-delete-group-ui]
  affects: [AssessmentAdminController, AssessmentMonitoring-view, ManageAssessment-partial]
tech_stack:
  added: []
  patterns: [linkedGroupId-grouping, dynamic-anonymous-concat, bootstrap-collapse-subrow]
key_files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/AssessmentMonitoring.cshtml
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
decisions:
  - "SubRowStatus helper sebagai local function pakai IEnumerable<dynamic> cast untuk avoid anonymous type limitation"
  - "ManageAssessment grouping pakai List<dynamic> Concat karena anonymous types tidak bisa Concat langsung"
  - "Aksi dropdown di monitoring hanya muncul untuk standard group (D-13) — Pre-Post hanya bisa expand sub-row"
  - "Tombol delete Pre-Post mengarah ke DeletePrePostGroup (belum diimplementasi di Plan 04)"
metrics:
  duration: "~20 menit"
  completed_date: "2026-04-07"
  tasks_completed: 3
  tasks_total: 3
  files_changed: 3
---

# Phase 297 Plan 02: Monitoring Pre-Post + ManageAssessment Badge Summary

**One-liner:** Monitoring grouping Pre-Post by LinkedGroupId dengan expandable sub-rows Pre/Post, dan ManageAssessment badge Pre-Post Test + delete grup endpoint.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | AssessmentMonitoring backend — grouping Pre-Post by LinkedGroupId | 6579097f | Controllers/AssessmentAdminController.cs |
| 2 | AssessmentMonitoring view — expandable parent row + sub-rows Pre/Post | b41a72d2 | Views/Admin/AssessmentMonitoring.cshtml |
| 3 | ManageAssessment — grouping Pre-Post + badge card | 4dfa869e | Controllers/AssessmentAdminController.cs, Views/Admin/Shared/_AssessmentGroupsTab.cshtml |

## What Was Built

### Task 1: AssessmentMonitoring Backend Grouping

Modifikasi `AssessmentMonitoring` action di `AssessmentAdminController`:
- **Query Select** diperluas: tambah `a.AssessmentType`, `a.LinkedGroupId`, `a.DurationMinutes`
- **Split sessions:** `prePostSessions` (LinkedGroupId != null) dan `standardSessions` (LinkedGroupId == null) — D-33
- **Pre-Post group by LinkedGroupId:** Bangun `MonitoringGroupViewModel` dengan `IsPrePostGroup=true`, `PreSubRow`, dan `PostSubRow`
  - Parent stat (D-11): TotalCount/CompletedCount/PassedCount dari Post sessions; fallback ke Pre jika Post belum ada
  - Status derived (D-29): Open jika ada yg Open/InProgress, Upcoming jika ada Upcoming, Closed otherwise
  - `SubRowStatus` helper sebagai local function menggunakan `IEnumerable<dynamic>` cast
- **Standard group by (Title, Category, Schedule.Date):** logika existing, `IsPrePostGroup=false` (default)
- Gabungkan dengan `Concat`, sort by Schedule desc

### Task 2: AssessmentMonitoring View

Modifikasi `AssessmentMonitoring.cshtml`:
- **Parent row:** Badge `bg-primary` "Pre-Post" + `ppt-expand-btn` dengan `bi-chevron-down` saat `item.IsPrePostGroup`
- **Sub-row collapse:** `<tr id="ppt-sub-{RepresentativeId}" class="collapse">` dengan nested table
  - Pre-Test sub-row: badge `bg-info`, jadwal, durasi, stat selesai, status badge, link "Detail Pre"
  - Post-Test sub-row: badge `bg-secondary`, jadwal, durasi, stat selesai, status badge, link "Detail Post"
- **Aksi dropdown:** Disembunyikan untuk Pre-Post group (D-13) — hanya muncul di standard group
- **JavaScript:** Toggle `bi-chevron-down` ↔ `bi-chevron-up` via `show.bs.collapse` / `hide.bs.collapse` events

### Task 3: ManageAssessment Backend + View

**Controller:**
- Query Select diperluas: tambah `a.AssessmentType`, `a.LinkedGroupId`
- Split `mgPrePostSessions` / `mgStandardSessions`
- `prePostGrouped`: group by LinkedGroupId, `IsPrePostGroup=true`, Users dari PreTest sessions saja, `List<dynamic>` untuk Concat
- `standardGrouped`: group by (Title, Category, Schedule.Date), `IsPrePostGroup=false`, `LinkedGroupId=(int?)null`, `List<dynamic>`
- Concat dan sort by Schedule desc

**View `_AssessmentGroupsTab.cshtml`:**
- Badge `bg-primary rounded-pill` "Pre-Post Test" di kolom judul saat `IsPrePostGroup==true`
- Aksi delete: Pre-Post group mengarah ke `DeletePrePostGroup` dengan `linkedGroupId` (D-19); standard tetap `DeleteAssessmentGroup`
- Konfirmasi modal Pre-Post menjelaskan dampak cascade lengkap

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Type Constraint] List<dynamic> untuk Concat anonymous types**
- **Found during:** Task 3 — C# anonymous types tidak bisa di-Concat langsung karena berbeda tipe
- **Issue:** `prePostGrouped.Concat(standardGrouped)` tidak bisa dikompilasi jika keduanya anonymous type berbeda
- **Fix:** Ubah `.ToList()` menjadi `.ToList<dynamic>()` pada kedua Select, sehingga Concat bekerja pada `List<dynamic>`
- **Files modified:** Controllers/AssessmentAdminController.cs

**2. [Rule 1 - Type Safety] SubRowStatus dengan explicit dynamic cast**
- **Found during:** Task 1 — local function dengan `IEnumerable<dynamic>` perlu explicit cast dari anonymous type list
- **Fix:** Gunakan `.Cast<dynamic>()` saat memanggil `SubRowStatus(preSubs.Cast<dynamic>())`
- **Files modified:** Controllers/AssessmentAdminController.cs

## Known Stubs

Tombol `DeletePrePostGroup` di ManageAssessment mengarah ke action yang belum diimplementasi. Action ini akan dibuat di Plan 04. Saat ini tombol tersebut merender dengan benar di UI (tidak 404 karena belum ada POST request — hanya form render), tapi akan 404 jika user klik submit. Ini bukan stub data/display — fitur inti Plan 02 (display, grouping, badge) sudah fully wired.

## Threat Flags

Tidak ada surface baru yang tidak tercakup threat model. `DeletePrePostGroup` sudah terdaftar di T-297-05 dan akan dimitigasi di Plan 04.

## Self-Check: PASSED

Files modified:
- FOUND: Controllers/AssessmentAdminController.cs
- FOUND: Views/Admin/AssessmentMonitoring.cshtml
- FOUND: Views/Admin/Shared/_AssessmentGroupsTab.cshtml

Commits:
- FOUND: 6579097f
- FOUND: b41a72d2
- FOUND: 4dfa869e
