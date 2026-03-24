---
phase: 241-seed-data-uat
plan: 01
subsystem: seed-data
tags: [seed, assessment, coach-coachee, proton, uat]
dependency_graph:
  requires: []
  provides: [SeedUatDataAsync, UAT-assessment-data, coach-coachee-mapping, proton-track-assignment]
  affects: [Data/SeedData.cs]
tech_stack:
  added: []
  patterns: [idempotent-seed, stub-methods-for-next-plan]
key_files:
  created: []
  modified:
    - Data/SeedData.cs
decisions:
  - Stub methods SeedCompletedAssessmentPassAsync/FailAsync/SeedProtonAssessmentsAsync dikosongkan (Task.CompletedTask) agar Plan 02 mengimplementasi tanpa konflik
  - Idempotency guard by Title "OJT Proses Alkylation Q1-2026" dipilih karena Title unik dan mudah dicek
  - ProtonTrackAssignment dibuat via lookup ProtonTracks (TrackType+TahunKe) bukan hardcode ID untuk portabilitas
metrics:
  duration: 10m
  completed_date: "2026-03-24"
  tasks_completed: 1
  files_modified: 1
---

# Phase 241 Plan 01: Seed UAT Data — Entry Point + Assessment Reguler Open Summary

SeedData.cs di-extend dengan SeedUatDataAsync yang menyediakan coach-coachee mapping Rustam->Rino, ProtonTrackAssignment Operator Tahun 1, kategori Assessment OJT/Proton, dan assessment reguler open "OJT Proses Alkylation Q1-2026" dengan 15 soal 4 ET dan 2 UserPackageAssignment.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Tambah SeedUatDataAsync entry point + coach-coachee + kategori + assessment reguler open | 5ee97464 | Data/SeedData.cs |

## What Was Built

### SeedUatDataAsync (entry point)
- Dipanggil dari `InitializeAsync` dalam blok `if (environment.IsDevelopment())`
- Idempotency guard: cek `AssessmentSessions.AnyAsync(s => s.Title == "OJT Proses Alkylation Q1-2026")`
- Lookup user Rino, Iwan, Rustam by email — skip jika null

### SeedCoachCoacheeMappingAsync
- Guard: `CoachCoacheeMappings.AnyAsync(m => m.CoacheeId == rinoId && m.IsActive)`
- Insert: CoachId=Rustam, CoacheeId=Rino, IsActive=true, Section=GAST, Unit=Alkylation Unit (065)

### SeedProtonTrackAssignmentAsync
- Lookup `ProtonTracks.FirstOrDefaultAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1")`
- Guard: `ProtonTrackAssignments.AnyAsync(a => a.CoacheeId == rinoId && a.ProtonTrackId == track.Id && a.IsActive)`
- Insert: CoacheeId=Rino, AssignedById=Rustam

### SeedAssessmentCategoriesAsync
- Guard: `AssessmentCategories.AnyAsync(c => c.Name == "Assessment OJT")`
- Seed: "Assessment OJT" (parent, SortOrder=1) → "Alkylation" (child)
- Seed: "Assessment Proton" (parent, SortOrder=2)

### SeedRegularAssessmentOpenAsync
- AssessmentSession: Title="OJT Proses Alkylation Q1-2026", Status="Open", Schedule=now+7d, DurationMinutes=60
- AssessmentPackage: PackageName="Paket A", PackageNumber=1
- 15 PackageQuestion dengan 4 ElemenTeknis merata:
  - Q1-4: Proses Distilasi
  - Q5-8: Keselamatan Kerja
  - Q9-12: Operasi Pompa
  - Q13-15: Instrumentasi
- 4 PackageOption per soal (1 correct, 3 distractors), konten semi-realistis kilang
- 2 UserPackageAssignment: Rino + Iwan, ShuffledQuestionIds dan ShuffledOptionIdsPerQuestion ter-serialisasi

### Stub Methods (Plan 02)
- `SeedCompletedAssessmentPassAsync` — Task.CompletedTask
- `SeedCompletedAssessmentFailAsync` — Task.CompletedTask
- `SeedProtonAssessmentsAsync` — Task.CompletedTask

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

1. Stub methods mengembalikan `Task.CompletedTask` (bukan `throw NotImplementedException`) agar seed tidak crash saat Plan 02 belum diimplementasi
2. Idempotency guard di level entry point (SeedUatDataAsync) + per-sub-method guard untuk granularitas

## Self-Check: PASSED

- Data/SeedData.cs: FOUND
- Commit 5ee97464: FOUND
- `SeedUatDataAsync` ada: FOUND (line 247)
- `SeedCoachCoacheeMappingAsync` ada: FOUND (line 291)
- `SeedProtonTrackAssignmentAsync` ada: FOUND (line 312)
- `SeedAssessmentCategoriesAsync` ada: FOUND (line 340)
- `SeedRegularAssessmentOpenAsync` ada: FOUND (line 368)
- 15 soal terdefinisi: FOUND
- 4 ET: Proses Distilasi, Keselamatan Kerja, Operasi Pompa, Instrumentasi: FOUND
- Build: 0 CS errors (MSB file-copy error bukan kompilasi, karena app sedang running)
