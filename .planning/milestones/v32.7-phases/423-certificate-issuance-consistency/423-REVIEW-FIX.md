---
phase: 423-certificate-issuance-consistency
status: fixed
source: 423-REVIEW.md
fixed: 2026-06-24
critical_fixed: 1
warning_fixed: 2
---

# 423 Review-Fix — 1 Critical + 2 Warning RESOLVED

## CR-01 (CRITICAL) — FIXED
**SITE 3 `FinalizeEssayGrading` tak sync `session.IsPassed`/`CompletedAt` pasca `ExecuteUpdateAsync` → gate `ShouldIssueCertificate(session)` selalu false → cert tak pernah terbit dari jalur essay-finalize.**
Fix (`Controllers/AssessmentAdminController.cs`): capture `completedAtSync` + sinkron in-memory `session.IsPassed=isPassed; session.Score=finalPercentage; session.Status=Completed; session.CompletedAt=completedAtSync` SEBELUM gate cert (paritas SITE 1/2 GradingService). Bug class identik yg sudah diperbaiki SITE 1/2; SITE 3 (controller) terlewat krn integration test cuma drive GradeAndCompleteAsync (SITE 1), bukan FinalizeEssayGrading.

## WR-01 (WARNING) — FIXED
**`TryAssignNextSeqAsync` `updated==0` → `return true` tanpa bedakan "sudah terisi" vs "sessionId tak valid".**
Fix (`Helpers/CertNumberHelper.cs`): `updated==0` → re-query `AnyAsync(s.Id==sessionId && s.NomorSertifikat != null)` → true HANYA bila cert benar terisi (idempotent OK; anomali → false utk sinyal non-destruktif HC).

## WR-02 (WARNING) — FIXED
**Pesan error CERT-04 tampil `wc.UserId` (GUID Identity).**
Fix (`Controllers/TrainingAdminController.cs`): tampilkan nomor offending `'{wc.NomorSertifikat}'` (lebih bermakna bagi HC) bukan GUID.

## CI-01 (INFO) — accepted (no action)

## Verification
- `dotnet build` 0 errors.
- `dotnet test --filter Cert|RetakeThenPassCert|EssayFinalize` — **64/64 PASS** (no regresi; RetakeThenPassCert + EssayFinalize hijau).
- Coverage note: FinalizeEssayGrading cert path (SITE 3) tak ter-unit-test langsung (controller HTTP path); fix = mirror SITE 1/2 yg ter-test. Nyquist validate (2g) cek gap coverage.
