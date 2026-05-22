---
phase: 321-assessment-edit-jawaban-peserta
plan: 02
type: execute
wave: 2
status: complete
completed_at: 2026-05-22
commits:
  - 1d741852
  - 064abd24
---

# PLAN 02 — GradingService Refactor (SUMMARY)

## Commits

| Hash | Message |
|------|---------|
| `1d741852` | feat(v17.0-p321): add ComputeScoreAndETInternalAsync (pure compute, overrideAnswers? param) |
| `064abd24` | feat(v17.0-p321): add GradingService.RegradeAfterEditAsync + PreviewScoreAsync (recompute + cascade cert/TR on flip) |

## Method Signatures Finalized

```csharp
private async Task<(int totalScore, int maxScore, bool isPassed, List<SessionElemenTeknisScore> etScores)>
    ComputeScoreAndETInternalAsync(AssessmentSession session, IDictionary<int, List<int>>? overrideAnswers = null)

public async Task<(int newScore, bool newIsPassed, int? oldScore, bool? oldIsPassed)>
    RegradeAfterEditAsync(AssessmentSession session)

public async Task<(int newScore, bool newIsPassed)>
    PreviewScoreAsync(AssessmentSession session, IDictionary<int, List<int>> overrideAnswers)
```

## CertNumberHelper Signature Confirmation

Verified live codebase `Helpers/CertNumberHelper.cs`:
- `public static string Build(int seq, DateTime date)` — line 20 ✓ (NOT `Format(year, seq)`)
- `public static async Task<int> GetNextSeqAsync(ApplicationDbContext context, int year)` — line 23 ✓
- `public static bool IsDuplicateKeyException(DbUpdateException ex)` — line 37 ✓

Call site di `RegradeAfterEditAsync` pakai `HcPortal.Helpers.CertNumberHelper.Build(nextSeq, certNow)` + retry loop 3x via `IsDuplicateKeyException` guard.

## T-321-03 Race Mitigation

ExecuteUpdateAsync status guard:
```csharp
await _context.AssessmentSessions
    .Where(s => s.Id == session.Id && s.Status == "Completed")
    .ExecuteUpdateAsync(...)
```
`rowsAffected == 0` → throw `InvalidOperationException` → caller PLAN 03 rollback transaction.

## Cascade Behavior

| Flip | Cert | TrainingRecord |
|------|------|----------------|
| Pass → Fail | NomorSertifikat + ValidUntil = NULL | Status = "Failed" (ExecuteUpdateAsync) |
| Fail → Pass | `CertNumberHelper.Build` retry 3x (only if `GenerateCertificate && AssessmentType != "PreTest"`) | Upsert: insert if missing else Status = "Passed" |
| Pass → Pass / Fail → Fail | No-op | No-op |

## No-Regression Status

- `GradeAndCompleteAsync` body UNCHANGED — diff `git show 1d741852` + `git show 064abd24` confirms additions only, zero modifications ke initial grading flow.
- Smoke test `dotnet run` SKIPPED per user decision — build sebagai gate primary. User manual test akan dilakukan di PLAN 03 ketika controller wire-up.

## PreviewScoreAsync Dry-Run Guarantee

- Return shape: `(int newScore, bool newIsPassed)` only — tidak expose ET breakdown ke caller.
- Body 100% delegate ke `ComputeScoreAndETInternalAsync` — TIDAK ada `SaveChangesAsync`, `AddAsync`, `ExecuteUpdateAsync`, atau `ExecuteDeleteAsync` di method body.
- T-321-03c mitigation: PLAN 03 controller endpoint WAJIB `[Authorize(Roles="Admin, HC")]` + `IsEditableAsync` gate.

## Build Status

- `dotnet build` 0 error, 22 warning (pre-existing, tidak ditambah PLAN 02).
- GradingService.cs: 329 → 586 lines (+257 line, 3 method baru, 0 method removed).

## Handoff ke PLAN 03

- Field `_gradingService` sudah existing di `AssessmentAdminController.cs` — langsung pakai (no `[FromServices]` needed).
- Consume signature:
  - POST SubmitEditAnswers: `await _gradingService.RegradeAfterEditAsync(session);`
  - POST PreviewEditScore: `var (newScore, newIsPassed) = await _gradingService.PreviewScoreAsync(session, overrideAnswers);`
- Caller bertanggung jawab open transaction sebelum `RegradeAfterEditAsync`, commit setelahnya (PLAN 03 Task 8 spec).

## Key Files Modified

- `Services/GradingService.cs` (+3 method, 0 method removed/changed)

## Self-Check: PASSED

- All 2 tasks committed atomically.
- ExecuteUpdateAsync status guard present (T-321-03).
- CertNumberHelper signature verified live (Build NOT Format).
- PreviewScoreAsync zero DB mutation (T-321-03c).
- 0 compile error.
