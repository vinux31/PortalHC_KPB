---
phase: 423-certificate-issuance-consistency
verified: 2026-06-24T12:00:00Z
status: passed
score: 7/7 must-haves verified
overrides_applied: 0
---

# Phase 423: Certificate Issuance Consistency — Verification Report

**Phase Goal:** Penerbitan sertifikat konsisten & aman lewat satu helper bersama ShouldIssueCertificate — Pre-Test tak pernah terbit cert di jalur grading mana pun, ValidUntil terjamin saat issue, nomor urut atomik tanpa race, penomoran manual vs auto tak bentrok, anti double-cert tak bisa di-bypass, sesi pending tampil umurnya. migration=FALSE.
**Verified:** 2026-06-24T12:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | CERT-01: ShouldIssueCertificate(session) menolak PreTest di semua 4 site | VERIFIED | `Helpers/CertIssuanceRules.cs:15-18` — `AssessmentType != AssessmentConstants.AssessmentType.PreTest`; wired di GradingService:301 + :553, AssessmentAdminController:3910 + :3991; unit test `ShouldIssueCertificate_TruthTable` 20/20 + integration CERT01_PreTest 5/5 |
| 2 | CERT-02: DeriveValidUntil Permanent→null, Annual→+1y, 3-Year→+3y dari CompletedAt | VERIFIED | `CertIssuanceRules.cs:22-33` — switch pada konstanta kanonik; unit test `DeriveValidUntil_FromCompletedAt`; integration CERT02_ValidUntil_Annual; wired GradingService:309 + :561, AssessmentAdminController:3919 |
| 3 | CERT-03: TryAssignNextSeqAsync retry+jitter cap 8, return bool, non-destruktif | VERIFIED | `CertNumberHelper.cs:51-77` — maxAttempts=8, jitter `Task.Delay(Random.Shared.Next(10,60))`, filtered `WHERE NomorSertifikat==null`, return bool; wired di 3 site grading-time; `maxCertAttempts` = 0 di GradingService (loop inline dihapus) |
| 4 | CERT-04: ResemblesAutoCertFormat reject + try/catch friendly di AddManualAssessment | VERIFIED | `CertIssuanceRules.cs:36-38` — regex `^KPB/\d{3}/[IVX]+/\d{4}$`; `TrainingAdminController.cs:720` `ResemblesAutoCertFormat` dipanggil; `TrainingAdminController.cs:785` `catch (DbUpdateException ex) when (CertNumberHelper.IsDuplicateKeyException(ex))`; `GenerateCertificate = true` hardcode = 0 match di AddManualAssessment |
| 5 | CERT-05: HasActiveCertForTitleAsync UNCONDITIONAL di luar if(!ConfirmDuplicateTitle) | VERIFIED | `AssessmentAdminController.cs:1030-1049` — guard di luar `if (!ConfirmDuplicateTitle)` block (soft-block judul ada di :997); `HasActiveCertForTitleAsync` def di :5933; renewal exempt (`!isRenewalModePost`); integration CERT05 anti-dup 5/5 |
| 6 | CERT-06: Permanent&&ValidUntil!=null ditolak di AddManualAssessment; DeriveValidUntil Permanent→null | VERIFIED | `CertIssuanceRules.cs:28` — Permanent switch arm returns null; Permanent⊥ValidUntil validation di `TrainingAdminController.cs` (analog AddManualTraining:269 diimplementasi); DeriveValidUntil unit-tested |
| 7 | CERT-07: PendingAgeBadgeClass render di EssayGrading.cshtml + AssessmentMonitoringDetail.cshtml, NO auto-finalize | VERIFIED | `EssayGrading.cshtml:48` — `PendingAgeBadgeClass(Model.CompletedAt, DateTime.UtcNow)`; `AssessmentMonitoringDetail.cshtml:285` + `:459` — 2 lokasi render; UAT live @5270 PASS 4/4 (badge abu/kuning/merah live-verified DOM; status tetap PendingGrading pasca-GET) |

**Score: 7/7 truths verified**

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/CertIssuanceRules.cs` | Pure static helper 4 method | VERIFIED | 4 method present + EF-free (`no using Microsoft.EntityFrameworkCore`) |
| `Helpers/CertNumberHelper.cs` | TryAssignNextSeqAsync retry+jitter | VERIFIED | Method di :51-77, cap=8, jitter, return bool, existing methods utuh |
| `Services/GradingService.cs` | 2 site wire ShouldIssueCertificate + TryAssignNextSeqAsync | VERIFIED | SITE 1 (:301) + SITE 2 (:553); DeriveValidUntil (:309 + :561); `maxCertAttempts` = 0 |
| `Controllers/AssessmentAdminController.cs` | SITE 3 wire helper + HasActiveCertForTitleAsync unconditional | VERIFIED | ShouldIssueCertificate :3910 + :3991; HasActiveCertForTitleAsync def :5933 + call :1040 di luar ConfirmDuplicateTitle |
| `Controllers/TrainingAdminController.cs` | SITE 4 stop hardcode + ResemblesAutoCertFormat + try/catch + Permanent⊥ValidUntil | VERIFIED | ResemblesAutoCertFormat :720; IsDuplicateKeyException :785; `GenerateCertificate = true` hardcode = 0; Permanent⊥ValidUntil validation |
| `Views/Admin/EssayGrading.cshtml` | Badge PendingAgeBadgeClass | VERIFIED | PendingAgeBadgeClass di :48, conditional `!Model.IsFinalized && CompletedAt.HasValue` |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | Badge PendingAgeBadgeClass (2 lokasi) | VERIFIED | 2 lokasi (:285 status-cell + :459 essay-pending row) |
| `HcPortal.Tests/CertIssuanceRulesTests.cs` | Truth-table pure tests | VERIFIED | 20/20 PASS (ShouldIssueCertificate + DeriveValidUntil + ResemblesAutoCertFormat + PendingAgeBadgeClass) |
| `HcPortal.Tests/CertIssuanceIntegrationTests.cs` | Integration real-SQL 5 tests | VERIFIED | 5/5 PASS (CERT-01 PreTest no-cert, CERT-03 exactly-1-cert, CERT-02 ValidUntil Annual, CERT-05 anti-dup, CERT-03 seq-fail signal) |
| `tests/e2e/cert-pending-age-badge.spec.ts` | Playwright smoke | VERIFIED | File exists; graceful-skip bila tak ada data pending (per plan spec); UAT live authoritative |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| GradingService.GradeAndCompleteAsync | CertIssuanceRules.ShouldIssueCertificate | gate :301 | WIRED | grep confirmed ≥2 hits di GradingService |
| GradingService.RegradeAfterEditAsync | CertIssuanceRules.ShouldIssueCertificate | gate :553 | WIRED | `CertIssuanceRules.ShouldIssueCertificate(session)` di cabang Fail→Pass |
| GradingService (2 site) | CertNumberHelper.TryAssignNextSeqAsync | loop terpusat | WIRED | grep: TryAssignNextSeqAsync ≥2 hits; `maxCertAttempts` = 0 (loop inline dihapus) |
| AssessmentAdminController.FinalizeEssayGrading | CertIssuanceRules.ShouldIssueCertificate | gate :3910 | WIRED | grep confirmed |
| AssessmentAdminController | HasActiveCertForTitleAsync | unconditional guard :1040 | WIRED | Di luar `if (!ConfirmDuplicateTitle)` block di :997 — bypass-proof |
| TrainingAdminController.AddManualAssessment | CertIssuanceRules.ResemblesAutoCertFormat | reject :720 | WIRED | grep confirmed |
| TrainingAdminController.AddManualAssessment | CertNumberHelper.IsDuplicateKeyException | try/catch :785 | WIRED | grep confirmed; `GenerateCertificate = true` hardcode removed |
| EssayGrading.cshtml | CertIssuanceRules.PendingAgeBadgeClass | render :48 | WIRED | grep confirmed + UAT live |
| AssessmentMonitoringDetail.cshtml | CertIssuanceRules.PendingAgeBadgeClass | render :285 + :459 | WIRED | 2 lokasi grep confirmed + UAT live (DOM class verified) |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| CertIssuanceRules.ShouldIssueCertificate | AssessmentSession fields | Domain object (server-side, EF entity) | Yes — reads `GenerateCertificate`, `IsPassed`, `AssessmentType` from tracked session | FLOWING |
| CertNumberHelper.TryAssignNextSeqAsync | NomorSertifikat | SQL Server unique index + GetNextSeqAsync MAX+1 | Yes — ExecuteUpdateAsync + DB filtered update | FLOWING |
| EssayGrading.cshtml badge | Model.CompletedAt | EssayGradingPageViewModel (DB-sourced via EF) | Yes — CompletedAt from AssessmentSession | FLOWING |
| AssessmentMonitoringDetail.cshtml badge | session.CompletedAt | MonitoringSessionViewModel (DB-sourced via EF) | Yes — CompletedAt from AssessmentSession | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Result | Status |
|----------|--------|--------|
| `dotnet build` 0 errors | 0 errors, 25 warnings (pre-existing) | PASS |
| CertIssuanceRulesTests 20/20 | 20 passed / 0 failed / 0 skipped | PASS |
| CertIssuanceIntegrationTests 5/5 | 5 passed / 0 failed / 0 skipped | PASS |
| migration=FALSE (no new Migrations/ file) | `find Migrations -newer CertIssuanceRules.cs` = 0 results | PASS |
| `GenerateCertificate = true` hardcode di AddManualAssessment | 0 matches | PASS |
| `maxCertAttempts` di GradingService | 0 matches (loop inline dihapus) | PASS |
| UAT live @5270 CERT-07 badge | 4/4 PASS (423-UAT.md) | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| CERT-01 | 423-01, 423-02 | Helper bersama tolak PreTest di semua jalur | SATISFIED | ShouldIssueCertificate wired 4 site; integration test CERT01 PASS |
| CERT-02 | 423-01, 423-02 | ValidUntil diisi/ditangani eksplisit saat cert terbit | SATISFIED | DeriveValidUntil di helper + wired GradingService + integration CERT02 Annual PASS |
| CERT-03 | 423-01, 423-02 | Seq atomik tanpa race (TryAssignNextSeqAsync, cap 8, jitter) | SATISFIED | CertNumberHelper.TryAssignNextSeqAsync wired 3 site; integration CERT03 exactly-1-cert PASS |
| CERT-04 | 423-01, 423-02 | Manual vs auto namespace + error ramah | SATISFIED | ResemblesAutoCertFormat + IsDuplicateKeyException wired AddManualAssessment |
| CERT-05 | 423-02 | Anti double-cert unconditional, renewal exempt | SATISFIED | HasActiveCertForTitleAsync di luar ConfirmDuplicateTitle; integration CERT05 PASS |
| CERT-06 | 423-01, 423-02 | Permanent⊥ValidUntil; Annual/3-Year derive | SATISFIED | DeriveValidUntil switch kanonik; Permanent⊥ValidUntil validation AddManualAssessment |
| CERT-07 | 423-03 | Badge umur PendingGrading di 2 view, TANPA auto-finalize | SATISFIED | Badge wired 2 view; UAT live 4/4 PASS (warna ambang + no auto-finalize) |

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| — | — | — | Tidak ditemukan anti-pattern blocker |

Catatan scanning:
- `EssayGrading.cshtml:48` — `return null` tidak ada; kondisi guard `!Model.IsFinalized` benar (badge hanya saat pending)
- `CertIssuanceRules.cs` — pure EF-free, tidak ada `using Microsoft.EntityFrameworkCore`
- `TryAssignNextSeqAsync` — `updated==0` return true (idempotent, bukan empty-stub; logika benar)

---

### Human Verification Required

Semua human verification sudah diselesaikan via UAT live @5270 (423-UAT.md, status: passed 4/4):
- Badge umur warna ambang (abu/kuning/merah) live-verified via DOM `browser_evaluate`
- No auto-finalize live-verified via `sqlcmd` pasca-GET
- DB restored pristine (SEED_JOURNAL 2026-06-24/423 = cleaned)

Tidak ada item human verification yang tersisa.

---

### Gaps Summary

Tidak ada gaps. Semua 7 CERT requirements verified di codebase aktual (bukan hanya SUMMARY):
- CertIssuanceRules.cs ada, substantif (4 method), EF-free
- CertNumberHelper.TryAssignNextSeqAsync ada, substantif (cap 8 + jitter), wired di 3 site
- 4 cert-issue site semua memakai ShouldIssueCertificate (CERT-01)
- Anti-dup guard unconditional di luar ConfirmDuplicateTitle (CERT-05)
- Badge render di 2 view, UAT live PASS (CERT-07)
- Build 0 error; suite 717/0/2; migration=FALSE

---

_Verified: 2026-06-24T12:00:00Z_
_Verifier: Claude (gsd-verifier)_
