---
phase: 297-admin-pre-post-test
plan: "04"
subsystem: AssessmentAdminController
tags: [pre-post-test, delete, reset, renewal, integrity]
dependency_graph:
  requires: [297-01, 297-02, 297-03]
  provides: [delete-pre-post-group, reset-guard-pre-post, renewal-d24]
  affects: [Controllers/AssessmentAdminController.cs]
tech_stack:
  added: []
  patterns: [cascade-delete-via-linked-group-id, status-guard-before-action, renewal-fk-post-only]
key_files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
decisions:
  - "D-24: RenewsSessionId dan RenewsTrainingId hanya di-assign pada Post session saat creation, Pre session tidak punya renewal FK"
  - "D-16 terpenuhi oleh desain existing — ResetAssessment hanya reset 1 session, tidak ada cascade"
  - "D-19: Guard di DeleteAssessment individual — blokir jika AssessmentType PreTest atau PostTest"
metrics:
  duration_minutes: 15
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 1
---

# Phase 297 Plan 04: Delete/Reset Guards Pre-Post Test + Renewal D-24 Summary

**One-liner:** DeletePrePostGroup cascade via LinkedGroupId + ResetAssessment guard blokir reset Pre jika Post Completed + RenewsSessionId hanya pada Post session (D-16..D-24).

## Objective

Implementasi DeletePrePostGroup, guard ResetAssessment untuk Pre-Post, guard DeleteAssessment individual, dan verifikasi renewal flow — memastikan integritas data Pre-Post tanpa orphan record.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | DeletePrePostGroup + DeleteAssessment guard | e9125944 | Controllers/AssessmentAdminController.cs |
| 2 | ResetAssessment guard D-17 + RenewsSessionId D-24 | e9125944 | Controllers/AssessmentAdminController.cs |

## What Was Built

### Task 1: DeletePrePostGroup (D-18, D-19)

Action baru `DeletePrePostGroup(int linkedGroupId)` yang:
- Query semua sessions dengan `LinkedGroupId == linkedGroupId`
- Cascade delete: PackageUserResponses → AssessmentAttemptHistory → AssessmentPackages + Questions + Options → AssessmentSessions
- Audit log dengan detail linked group
- Guard di `DeleteAssessment` individual: blokir jika `AssessmentType == "PreTest"` atau `"PostTest"` — redirect dengan pesan "Gunakan 'Hapus Grup'"

### Task 2: ResetAssessment guard (D-17) + RenewsSessionId D-24

Guard di `ResetAssessment` (D-17):
- Jika session adalah PreTest dan punya `LinkedSessionId`, cek status Post
- Jika Post sudah Completed, blokir reset dengan pesan "Reset Post-Test terlebih dahulu"
- D-16 terpenuhi oleh desain existing — logic reset hanya mempengaruhi 1 session, Post tidak tersentuh

RenewsSessionId D-24:
- Post session creation di CreateAssessment POST kini memiliki `RenewsSessionId = model.RenewsSessionId` dan `RenewsTrainingId = model.RenewsTrainingId`
- Pre session sudah tidak memiliki `RenewsSessionId` dari awal (benar)

## Verification

- `dotnet build` passes (0 errors, 70 warnings — semua warning pre-existing)
- `DeletePrePostGroup` mengandung `a.LinkedGroupId == linkedGroupId` ✓
- `DeletePrePostGroup` mengandung `_context.AssessmentSessions.RemoveRange(groupSessions)` ✓
- `DeletePrePostGroup` di audit log ✓
- `DeleteAssessment` mengandung `Gunakan 'Hapus Grup'` ✓
- `ResetAssessment` mengandung `assessment.AssessmentType == "PreTest"` ✓
- `ResetAssessment` mengandung `linkedPost.Status == "Completed"` ✓
- `ResetAssessment` mengandung `Reset Post-Test terlebih dahulu` ✓
- Post session mengandung `RenewsSessionId = model.RenewsSessionId` ✓

## Deviations from Plan

None — plan dieksekusi persis seperti yang tertulis. Kedua tasks dikerjakan dalam satu file sehingga hanya menghasilkan 1 commit (bukan 2 terpisah), namun semua kriteria acceptance terpenuhi.

## Known Stubs

None.

## Threat Flags

None — semua ancaman T-297-09 hingga T-297-11 telah dimitigasi:
- T-297-09: `[Authorize]` + `[ValidateAntiForgeryToken]` + validasi `groupSessions.Any()` pada DeletePrePostGroup
- T-297-10: Backend check `AssessmentType` sebelum allow individual delete
- T-297-11: Backend check `LinkedSessionId` + Post status sebelum allow Pre reset

## Self-Check: PASSED

- File modified: `Controllers/AssessmentAdminController.cs` — FOUND
- Commit e9125944 — FOUND (`git log --oneline -5` menampilkan commit ini)
