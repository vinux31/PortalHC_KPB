---
phase: 249-null-safety-input-validation
plan: "02"
subsystem: Views
tags: [null-safety, view, bugfix]
dependency_graph:
  requires: []
  provides: [SAFE-04, SAFE-05]
  affects: [Views/Admin/WorkerDetail.cshtml, Views/CMP/ExamSummary.cshtml]
tech_stack:
  added: []
  patterns: [null-coalescing operator, safe cast as int?]
key_files:
  modified:
    - Views/Admin/WorkerDetail.cshtml
    - Views/CMP/ExamSummary.cshtml
decisions:
  - "Gunakan var fullName = Model.FullName ?? \"\" agar initials computation aman terhadap null"
  - "Gunakan as int? ?? 0 daripada (int) cast untuk ViewBag values yang bisa null"
metrics:
  duration: "~3 menit"
  completed: "2026-03-24"
  tasks: 1
  files_modified: 2
---

# Phase 249 Plan 02: Null-safe View Fixes Summary

**One-liner:** Null-coalescing untuk FullName di WorkerDetail dan safe cast `as int?` untuk ViewBag di ExamSummary mencegah NullReferenceException dan InvalidCastException.

## Objective

Fix null-unsafe patterns di 2 view files untuk mencegah crash saat data null.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Null-safe FullName + null-safe ViewBag | 68c7d791 | WorkerDetail.cshtml, ExamSummary.cshtml |

## Changes Made

### SAFE-04 — WorkerDetail.cshtml

**Masalah:** `Model.FullName.Length` pada baris ~15 crash dengan NullReferenceException jika FullName null.

**Fix:**
- Tambah `var fullName = Model.FullName ?? "";` sebelum komputasi initials
- Ganti `Model.FullName.Length` dengan `fullName.Length`
- Ganti 2 display `@Model.FullName` dengan `@(Model.FullName ?? "")`

### SAFE-05 — ExamSummary.cshtml

**Masalah:** Hard cast `(int)ViewBag.UnansweredCount` dan `(int)ViewBag.AssessmentId` crash dengan InvalidCastException jika ViewBag null.

**Fix:**
- Ganti dengan `ViewBag.UnansweredCount as int? ?? 0`
- Ganti dengan `ViewBag.AssessmentId as int? ?? 0`

## Verification

- Build: 0 errors, 72 warnings (pre-existing)
- `(int)ViewBag` di ExamSummary.cshtml: 0 occurrences
- `as int?` di ExamSummary.cshtml: 2 occurrences
- `FullName ?? ""` di WorkerDetail.cshtml: 3 occurrences
- `Model.FullName.Length` di WorkerDetail.cshtml: 0 occurrences

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Views/Admin/WorkerDetail.cshtml: FOUND
- Views/CMP/ExamSummary.cshtml: FOUND
- Commit 68c7d791: FOUND
