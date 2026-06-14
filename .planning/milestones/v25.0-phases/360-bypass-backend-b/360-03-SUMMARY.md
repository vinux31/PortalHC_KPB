---
phase: 360-bypass-backend-b
plan: 03
status: complete
completed: 2026-06-10
commits:
  - 4c86ede5: "feat(360-03): BypassValidator pure §5 + 17 unit test (TDD)"
  - 64e9285e: "feat(360-03): ProtonBypassService.ExecuteInstantBypassAsync §5.1 (CL-A/B(a)/C) + DI"
  - a16bf69b: "test(360-03): 7 integration test §5.1 real-SQL (Pitfall 1 + B-06 + D-16b + E15 + PBYP-05)"
one_liner: "Inti ProtonBypassService: validasi pure §5 (17 unit test, B-03 CL-A wajib allApproved+final) + ExecuteInstantBypassAsync 1-transaksi (E8 di resolve, penanda sebelum deactivate, bootstrap unit-form, coach E15/D-16/D-16b) + 7 integration test real-SQL."
---

# 360-03 Summary — ProtonBypassService Inti

## Signature / Kontrak (untuk plan 04/05/07)
```csharp
// Services/ProtonBypassService.cs
public record BypassRequest(string CoacheeId, int SourceProtonTrackId, int TargetProtonTrackId,
    string TargetUnit, string? TargetCoachId, string Reason, string Mode, int? DurationMinutes, string InitiatedById);
public record BypassResult(bool Success, string Message, int? PendingId = null, bool ShowAttachPackageReminder = false);
public record BypassValidationInput(string Reason, int ActiveSourceTrackId, int TargetTrackId,
    int SourceTahun, int TargetTahun, string Mode, bool SourceComplete, bool SourceHasFinal);
public static class BypassValidator { public static (bool Valid, string Message) Validate(BypassValidationInput v); }
public class ProtonBypassService // ctor: ApplicationDbContext, ProtonCompletionService, INotificationService, AuditLogService, ILogger<>
{
    public Task<BypassResult> ExecuteInstantBypassAsync(BypassRequest req); // §5.1 CL-A/B(a)/C; CL-B(b) ditolak (jalur pending plan 04)
}
```
DI: `Program.cs:60` `AddScoped<ProtonBypassService>` (satu arah — TANPA grading di ctor, Open Q3).

## Keputusan Dibaking (konsumsi plan 04/05/07)
- **D-14 (cancel exam aktif S):** set `AssessmentSession.Status = "Dibatalkan"` (WHERE Category=="Assessment Proton" && ProtonTrackId==Source && UserId==Coachee && Status!="Completed"). Tak hapus data; nilai tak bentrok Status existing {Open, Upcoming, Completed, Menunggu Penilaian}.
- **D-16b (keep-coach + ganti-unit):** `TargetCoachId==null` && `(mappingLama.AssignmentUnit ?? "").Trim() != TargetUnit.Trim()` → `mappingLama.AssignmentUnit = TargetUnit` dalam transaksi (TANPA deactivate/recreate — E15 aman). Wajib direplikasi di jalur Confirm (plan 05, via reuse step §5.1).
- **E8/B-04:** cek tepat-1-assignment-aktif di langkah resolve (0a) ExecuteInstant — `count != 1` → tolak. BypassSaveAsync (plan 04) WAJIB cek E8 yang sama SEBELUM dispatch semua mode.
- **Tahun untuk Δ-validasi = `ProtonTrack.Urutan`** (int), bukan parse string TahunKe.
- **Coach swap (E15):** deactivate lama (`IsActive=false`, `EndDate=now`) + flush DULU → create baru `{StartDate=UtcNow, AssignmentSection=warisi lama}` (W-13/I-04). Catch filtered-unique 2601/2627 → pesan ramah.
- **CL-B(a) force-approve:** progress source → Approved/ApprovedAt/ApprovedById/HCApprovalStatus="Reviewed" + `DeliverableStatusHistory` StatusType="Bypassed-AutoApprove" per progress (D-13) → LALU `EnsureAsync(..., "Bypass", reason)` SEBELUM deactivate (Pitfall 1).

## Verification
- Unit: `ProtonBypassValidation` **17/17** (CL-A wajib `SourceComplete && SourceHasFinal` B-03; D-D; E14; D-B; mode whitelist; alasan).
- Integration: `ProtonBypassServiceTests` **7/7** real-SQL (CL-B(a) penanda-sebelum-deactivate; CL-A; CL-C; bootstrap unit-FORM ≠ mapping; coach E15+StartDate/Section; D-16b keep-coach+ganti-unit single-mapping; B-06 CL-C turun count tetap N + `IsEligiblePerUnit` true).
- `dotnet build` 0 warning / 0 error; unit suite penuh 169/169.

## Deviations
- TDD RED-run formal dilewati untuk validator (test + implementasi ditulis berurutan dalam satu step inline; 17 test tetap membuktikan perilaku). Dicatat jujur.
- Komentar "GradingService" di service di-reword ("layanan grading") supaya AC `grep -cF GradingService → 0` valid.
- `hcName` resolve: `FullName` non-nullable di ApplicationUser → pakai `IsNullOrWhiteSpace` fallback `UserName` → `InitiatedById`.

## Key Files
created:
  - HcPortal.Tests/ProtonBypassValidationTests.cs (17 test pure)
  - HcPortal.Tests/ProtonBypassServiceTests.cs (7 integration + FakeNotificationService + seed helpers — reusable plan 04/05)
modified:
  - Services/ProtonBypassService.cs (validator + ExecuteInstantBypassAsync)
  - Program.cs (:60 DI)

## Next
Plan 04: `BypassSaveAsync` (dispatch + E8 semua mode + D-10 single-pending) + jalur CL-B(b) (force-approve + bare session UserId/AssessmentType B-05 + pending Menunggu) + `MarkPendingReadyIfAnyAsync`/`RevertPendingToMenungguAsync` (NO transaksi — hot-path grading).
