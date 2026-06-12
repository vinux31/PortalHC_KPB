---
phase: 367-delete-records-cascade-overhaul
plan: 01
subsystem: services
tags: [cascade-delete, traversal, preview, mirror-heuristic]
requires: []
provides:
  - "RecordCascadeDeleteService.CollectCascadeIds (BFS traversal bersama preview==execute)"
  - "RecordCascadeDeleteService.BuildPreviewAsync (pohon korban read-only)"
  - "RecordCascadeDeleteService.FindMirrorCandidates (mirror legacy #15, validasi server-side dipakai 02)"
  - "DI AddScoped<RecordCascadeDeleteService>"
affects:
  - Services/RecordCascadeDeleteService.cs
  - Program.cs
tech-stack:
  added: []
  patterns: ["BFS cycle-guard HashSet visited", "read-only preview convention (analog CoachCoacheeMappingDeletePreview)"]
key-files:
  created:
    - Services/RecordCascadeDeleteService.cs
    - HcPortal.Tests/RecordCascadeServiceTests.cs
    - HcPortal.Tests/MirrorHeuristicTests.cs
  modified:
    - Program.cs
key-decisions:
  - "CollectCascadeIds/FindMirrorCandidates public (konvensi proyek: reachable dari Tests tanpa InternalsVisibleTo) — deviasi minor dari plan yang menulis internal"
  - "Ctor minimal (context+logger); Plan 02 menambah ProtonCompletionService/AuditLogService/IWebHostEnvironment"
requirements-completed: ["#15", "spec-3.1-traversal", "spec-3.1-preview"]
duration: "18 min"
completed: 2026-06-12
---

# Phase 367 Plan 01: Cascade Engine Foundation (Read-Only) Summary

Membangun fondasi `RecordCascadeDeleteService` — bagian READ-ONLY cascade engine: traversal BFS renewal lintas `TrainingRecords`↔`AssessmentSessions` dengan cycle guard, `BuildPreviewAsync` (pohon korban tanpa mutasi), dan heuristik kandidat mirror legacy (#15). Service ter-DI. Plan 02 menambah `ExecuteAsync` (mutasi) di file yang sama.

**Tasks:** 2/2 | **Files:** 3 created + 1 modified | **Tests:** 11 [Fact] (5 traversal + 6 mirror/preview)

## What was built

- **`CollectCascadeIds(rootType, rootId)`** — traversal BFS bersama (dipakai preview DAN execute → invariant preview==execute). Lintas tabel via `RenewsSessionId`/`RenewsTrainingId`, arah anak sesuai tipe induk (Pitfall 2). Cycle guard `HashSet<(string,int)> visited`. Whitelist `rootType` ∈ {"session","training"} (V5).
- **`BuildPreviewAsync(rootType, rootId)`** — pohon `CascadeNode` read-only (ZERO RemoveRange/Add/SaveChanges). Turunan renewal + kandidat mirror (IsMirrorCandidate=true).
- **`FindMirrorCandidates(session)`** — TR mirror milik user sama, judul match (`==` ATAU `"Assessment: " + Title`), tanggal ±1 hari. BEDA dari guard duplikat #12 (EXACT). Reuse di execute 02 untuk validasi mirror-ID server-side (IDOR/V5).
- **DI** `AddScoped<RecordCascadeDeleteService>` (Program.cs).

## Verification

- `dotnet build` (solution) — 0 error (24 warning pre-existing).
- `dotnet test --filter "FullyQualifiedName~RecordCascadeServiceTests|...MirrorHeuristicTests"` — 11/11 pass.
- Quick suite `dotnet test --filter "Category!=Integration"` — **190/190 pass** (no regression, +11 baru).

## Deviations from Plan

**[Rule 2 - Konvensi proyek] CollectCascadeIds/FindMirrorCandidates `public` bukan `internal`** — Found during: Task 1. Plan menulis `internal`, tapi proyek TIDAK pakai `InternalsVisibleTo` (konvensi: members test-reachable dibuat public, lihat `CMPController:3969`/`CDPController`). Task 1 menguji `CollectCascadeIds` langsung (sebelum `BuildPreviewAsync` ada). Fix: public. Files: `RecordCascadeDeleteService.cs`. Verification: test memanggil langsung, 11/11 pass.

**Total deviations:** 1 auto-fixed (visibility convention). **Impact:** Tidak ada — API tetap, hanya visibility lebih longgar (sah untuk service).

## Issues Encountered

None.

## Self-Check: PASSED

- `Services/RecordCascadeDeleteService.cs` ada — `grep "public record CascadeNode"` ✓, `grep "HashSet<(string, int)> visited"` ✓, kedua arah `RenewsSessionId == node.Id` + `RenewsTrainingId == node.Id` ✓.
- `grep "public async Task<List<CascadeNode>> BuildPreviewAsync"` ✓, `AddDays(-1)`+`AddDays(1)` ✓, `"Assessment: " + session.Title` ✓.
- `Program.cs` `grep "AddScoped<HcPortal.Services.RecordCascadeDeleteService>"` ✓.
- Migration = FALSE (zero schema change) ✓.

Ready for 367-02 (ExecuteAsync mutasi di file service yang sama).
