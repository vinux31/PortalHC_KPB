---
phase: 358-penanda-kelulusan-fondasi-a
plan: 02
status: complete
requirements: [PCOMP-02, PCOMP-03]
commits: [b1359b7c-pred, b1359b7c]
migration: false
---

# Plan 358-02 SUMMARY — ProtonCompletionService (single-source penanda)

## Apa yang dibangun
- **`Services/ProtonCompletionService.cs`** (scoped DI) — sumber TUNGGAL penanda `ProtonFinalAssessment`. Mengekstrak inline create+dedup AssessmentAdminController L3742-3765.
  - `EnsureAsync(coacheeId, protonTrackId, createdById, origin, notes)` → idempotent. Resolve assignment aktif (CoacheeId+ProtonTrackId+IsActive); null → false. Dedup `AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id)`; sudah ada → false. Add penanda (Origin, CompetencyLevelGranted=0, Status="Completed") → true.
  - `RemoveExamOriginAsync(coacheeId, protonTrackId)` → selektif `Origin=="Exam"` only (A-M9). Bypass/Interview KEBAL. RemoveRange + Save.
  - `GetPassedYearsAsync(coacheeId, trackType)` → join FA→Assignment→Track, Distinct TahunKe. TANPA gate (D-02).
- **`Program.cs`** L55: `AddScoped<HcPortal.Services.ProtonCompletionService>()`.
- **Test** +4 [Fact]: `EnsureAsync_Idempotent`, `EnsureAsync_NoAssignment_ReturnsFalse`, `RemoveExamOrigin_SelektifExamOnly` (Bypass NotEmpty), `GetPassedYears_MatchTrackType`.

## Verifikasi
- `dotnet build` 0 error.
- `dotnet test --filter ProtonCompletionServiceTests` → **5/5 pass** (smoke + 4).
- Isolasi antar-fact: coacheeId unik per fact (DB shared dalam fixture).

## Catatan untuk plan berikutnya
- Service siap di-inject GradingService (Plan 03, hook Exam completion + re-grade flip) + AssessmentAdminController (Plan 04, interview refactor + essay defensive + backfill).
- Method signature: `EnsureAsync(coacheeId, protonTrackId, createdById, origin, notes)` — origin="Exam"/"Interview".
- `RemoveExamOriginAsync(coacheeId, protonTrackId)` untuk Pass→Fail flip.
- DB lokal TIDAK disentuh (fixture disposable). migration=false.
