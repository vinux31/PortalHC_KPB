---
phase: 423-certificate-issuance-consistency
plan: 01
wave: 1
completed: 2026-06-24
migration: false
requirements: [CERT-01, CERT-02, CERT-03, CERT-06]
status: complete
---

# 423-01 SUMMARY — Keystone: CertIssuanceRules helper + seq harden

> Catatan: executor agent drop koneksi setelah commit kedua task; SUMMARY + finalize bookkeeping diselesaikan oleh orchestrator (build hijau + test 20/20 terverifikasi).

## What was built

**Task 1 — `Helpers/CertIssuanceRules.cs` (NEW, pure static, EF-free; analog `SessionEditLockRules`)** + `HcPortal.Tests/CertIssuanceRulesTests.cs` (truth-table). Commit `3b5696f7`.
- `ShouldIssueCertificate(AssessmentSession)` — CERT-01/D-01: `GenerateCertificate && IsPassed==true && AssessmentType != PreTest`. Gate tunggal dipakai 4 site (Wave 2).
- `DeriveValidUntil(string? certType, DateTime? completedAt)` — CERT-02/06/D-04/D-05/D-10: base `DateOnly.FromDateTime(CompletedAt)`; Permanent→null, Annual→+1y, ThreeYear("3-Year")→+3y, non-kanonik/null→null (caller pakai input HC apa adanya).
- `ResemblesAutoCertFormat(string?)` — CERT-04/D-02: regex `^KPB/\d{3}/[IVX]+/\d{4}$` (tolak nomor manual menyerupai auto).
- `PendingAgeBadgeClass(DateTime? completedAtUtc, DateTime nowUtc)` — CERT-07/D-09: >7d `bg-danger`, >3d `bg-warning text-dark`, else `bg-secondary`.

**Task 2 — `Helpers/CertNumberHelper.TryAssignNextSeqAsync` (NEW method, harden seq)**. Commit `28449608`.
- CERT-03/D-03: retry (maxAttempts=8) + jitter `Task.Delay(Random.Shared.Next(10,60))` di atas filtered unique index `IX_AssessmentSessions_NomorSertifikat_Unique`. Race-safe: `ExecuteUpdateAsync` `WHERE Id==sessionId && NomorSertifikat==null`. `updated==0`→idempotent (sudah terisi)→return true. Return **bool** (false setelah semua attempt → caller WAJIB non-destruktif: sesi sudah Completed/IsPassed, JANGAN rollback, tandai utk HC — diwire di Wave 2). Loop dikonsolidasi dari 3 site grading-time (kill-drift). **migration=FALSE** (index sudah ada).

## Verification
- `dotnet build` — **0 errors** (25 warning pre-existing).
- `dotnet test --filter ~CertIssuanceRulesTests` — **20/20 PASS** (59ms).

## Deviations
- `PendingAgeBadgeClass` pakai `bg-warning text-dark`/`bg-danger`/`bg-secondary` (idiom badge `AssessmentMonitoringDetail.cshtml`) bukan `text-bg-*` literal di plan — fungsional setara; Wave 3 render mengikuti idiom view aktual.
- Executor koneksi drop sebelum SUMMARY/STATE finalize → diselesaikan orchestrator (kedua task commit utuh, build+test hijau).

## For next wave (423-02)
- `CertIssuanceRules.ShouldIssueCertificate` → wire ke 4 site (GradingService GradeAndCompleteAsync:287 + RegradeAfterEditAsync:520, AssessmentAdminController FinalizeEssayGrading:3887, TrainingAdminController AddManualAssessment:759).
- `TryAssignNextSeqAsync` → ganti loop inline di 3 site grading-time; pada return false → tandai non-destruktif + AuditLog "CertIssuanceFailed".
- `DeriveValidUntil` → set ValidUntil setelah cert tersimpan (canonical types).
- `ResemblesAutoCertFormat` → reject di AddManualAssessment + try/catch DbUpdateException friendly.
