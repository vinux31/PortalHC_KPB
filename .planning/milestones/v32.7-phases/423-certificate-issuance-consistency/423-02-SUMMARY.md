---
phase: 423-certificate-issuance-consistency
plan: 02
wave: 2
completed: 2026-06-24
migration: false
requirements: [CERT-01, CERT-03, CERT-04, CERT-05]
status: complete
---

# 423-02 SUMMARY — Wire CertIssuanceRules ke 4 site + anti-dup + manual hardening

> Catatan: executor wave-2 drop koneksi mid-Task-2; orchestrator menyelesaikan SITE 3 method def + SITE 4 + finalize. Task 3 (integration) di-spawn fokus terpisah.

## What was built (4 commits)

- **`b922a27c`** Task 1 — SITE 1 `GradingService.GradeAndCompleteAsync` + SITE 2 `RegradeAfterEditAsync`: gate inline → `CertIssuanceRules.ShouldIssueCertificate(session)`; loop seq → `CertNumberHelper.TryAssignNextSeqAsync`; derive+set ValidUntil; sinyal-gagal non-destruktif.
- **`11e249d5`** Task 2a — SITE 3 `AssessmentAdminController.FinalizeEssayGrading`: gate via `ShouldIssueCertificate` (sebelumnya TANPA cek PreTest) + `TryAssignNextSeqAsync` + `certError` PXF-08 dikonsisten-kan. **Anti-dup guard `HasActiveCertForTitleAsync` UNCONDITIONAL** (di LUAR cabang `if(!ConfirmDuplicateTitle)`, setara double-renewal guard :1014-1028; renewal exempt via `RenewsSessionId==null`; "aktif" = `ValidUntil==null || ValidUntil>=today`).
- **`2e2a9312`** Task 2b — SITE 4 `TrainingAdminController.AddManualAssessment`: **stop hardcode** `GenerateCertificate=true` → `!string.IsNullOrWhiteSpace(wc.NomorSertifikat)` (D-01/D-10); `ResemblesAutoCertFormat` reject (CERT-04); Permanent⊥ValidUntil parity (CERT-06, analog AddManualTraining:269); try/catch `DbUpdateException`+`IsDuplicateKeyException` → pesan ramah (bukan 500).
- **`0f4eefed`** Task 3 — `HcPortal.Tests/CertIssuanceIntegrationTests.cs` (real-SQL, IClassFixture<RetakeServiceFixture>+NoOpHubContext) **5/5 hijau**: CERT-01 PreTest no-cert (site nyata), CERT-03 exactly-1-cert `^KPB/\d{3}/[IVX]+/\d{4}$`, CERT-02 ValidUntil Annual, CERT-05 anti-dup block/expired-pass/renewal-exempt, CERT-03 seq-fail signal.

## Verification
- `dotnet build` 0 errors.
- Full suite `dotnet test HcPortal.Tests` — **717 passed / 2 skipped / 0 failed** (incl Integration @SQLEXPRESS).
- grep: ShouldIssueCertificate ≥3 sites in AssessmentAdmin (+GradingService 2), HasActiveCertForTitleAsync ×2 (def+call), GenerateCertificate=true hardcode di AddManualAssessment ==0, ResemblesAutoCertFormat+IsDuplicateKeyException di TrainingAdmin ≥1. RBAC `[Authorize(Admin,HC)]`+`[ValidateAntiForgeryToken]` utuh.

## Deviations / Bug caught
- **Latent bug (BUG CAUGHT by capstone test):** gate `ShouldIssueCertificate(session)` membaca `session.IsPassed`/`CompletedAt` STALE — objek in-memory tak ter-refresh setelah `ExecuteUpdateAsync` bulk → cert TIDAK PERNAH terbit utk sesi baru lulus. Terkonfirmasi: `RetakeThenPassCertTests` MERAH (certCount=0) sebelum fix. **Fix:** sinkron field in-memory `session.IsPassed=isPassed; session.CompletedAt=completedAtSync` di SITE 1 (`GradeAndCompleteAsync`) + SITE 2 (Fail→Pass `RegradeAfterEditAsync`) — committed dalam `0f4eefed`. Pasca-fix capstone hijau. Lesson: gate baca objek in-memory WAJIB disinkron setelah bulk ExecuteUpdateAsync (EF tak refresh tracked entity).
- Executor wave-2 drop koneksi → orchestrator selesaikan inline (semua task commit utuh, build+suite hijau).

## For next wave (423-03)
- `CertIssuanceRules.PendingAgeBadgeClass` → render badge umur PendingGrading di `Views/Admin/EssayGrading.cshtml` + `AssessmentMonitoringDetail.cshtml` (>3d kuning, >7d merah, NO auto-finalize) + Playwright + checkpoint UAT @5270.
