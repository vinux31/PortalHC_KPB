---
phase: 377-impersonation-identity-across-surfaces
plan: 01
subsystem: impersonation-identity
tags: [audit, impersonation, identity, red-test, diagnose-first]
requires: []
provides:
  - "377-AUDIT.md (peta otoritatif call-site identity-resolution, parity-verified)"
  - "ImpersonationIdentityTests.cs (RED — kontrak ResolveEffectiveUserDecision untuk Plan 02)"
affects:
  - "Services/ImpersonationService.cs (Plan 02 implement ResolveEffectiveUserDecision + enum + GetEffectiveUserAsync)"
  - "Controllers/CMPController.cs:2388 + CDPController.cs:3696 (resolver di-rewrite Wave 2-3)"
tech-stack:
  added: []
  patterns: ["audit-first deliverable (pola 328)", "pure xUnit Theory RED (analog ResultsAuthorizationTests)", "Wave-0 Nyquist gate"]
key-files:
  created:
    - .planning/phases/377-impersonation-identity-across-surfaces/377-AUDIT.md
    - HcPortal.Tests/ImpersonationIdentityTests.cs
  modified: []
key-decisions:
  - "11 call-site IN-SCOPE (9 via 2 resolver + 3 direct-bypass route: Assessment/ExportRecords/StartExam)"
  - "Borderline DokumenKkj (CMP:86) OUT default (A3) — docs per-bagian, bukan data-diri; follow-up flag bila UAT temukan leak"
  - "CDP CertificationManagement (3740) OUT default (A4) — admin-scope cert mgmt, no data-leak risk"
  - "DRIFT 378: CMP CertificationManagement (plan:3693) DIHAPUS → redirect stub CMP:3589→CDP (N/A); BuildSertifikatRowsAsync 3870→3658"
requirements-completed: [IMP-02]
duration: "~30 min"
completed: 2026-06-14
---

# Phase 377 Plan 01: Audit-First + RED Scaffold Summary

Diagnose-first (audit-first, mirror SC1). Materialize `377-AUDIT.md` — enumerator otoritatif fix-set (D-06): semua call-site `GetUserAsync(User)`/`GetCurrentUserRoleLevelAsync` lintas CMP/CDP/Home ter-triage IN-SCOPE / BORDERLINE / OUT, parity-verified. Plus RED test pure-logic kontrak resolver (Wave-0 Nyquist) yang di-GREEN-kan Plan 02.

## Tasks
- **Task 1:** `377-AUDIT.md` — 3 tabel kolom eksak D-07. 11 IN-SCOPE (Records/RecordsWorkerDetail/Certificate/CertificatePdf/Results/Assessment/ExportRecords/StartExam/BuildSertifikat CMP+CDP/Home.Index), 5 BORDERLINE team-view+DokumenKkj, OUT per-pola (write-actor/admin-scope/guide). Parity Grep CMP(25)/CDP(33)/Home(3) semua ter-triage. Commit `001be31b`. AC: 9/9 literal present (CMP:481/867, CDP:3696, Home:38, IN/BORDERLINE/OUT, DokumenKkj, CertificationManagement).
- **Task 2:** `ImpersonationIdentityTests.cs` — `[Theory] ResolveEffectiveUserDecision_Matrix`, **7 InlineData** (UseRealUser non-impersonate + isImpersonating=false-dominant + expired; RoleModeEmpty mode-role + target-null D-04 + empty-string; TargetUser SC2). NO Moq/HTTP. Commit `aa917192`. RED confirmed.

## RED State Evidence (gate Plan 02 GREEN)
```
ImpersonationIdentityTests.cs(25,83): error CS0246: The type or namespace name
  'EffectiveUserDecision' could not be found
ImpersonationIdentityTests.cs(17-23): error CS0103: The name 'EffectiveUserDecision' does not exist
```
Pure missing-symbol (enum + static method `ResolveEffectiveUserDecision` diimplementasi Plan 02). Tidak ada compile issue lain. **RRED benar (Wave-0).**

## Deviations from Plan

**[Rule 2 — context drift dari Phase 378 paralel] Line number plan sebagian obsolete.**
- Found during: Task 1 (verifikasi line vs code current).
- Issue: Plan 01 ditulis sebelum 378 ship. 378 (`5cd3bda6`/`6e439d06`/`bfee5a16`) **hapus `CMP/CertificationManagement` full action** (jadi redirect stub `CMP:3589`→CDP canonical) + geser `BuildSertifikatRowsAsync` ke 3658 (plan: 3870).
- Resolution: Audit pakai **line CURRENT (verified 2026-06-14)**; CMP CertificationManagement diklasifikasi **N/A (drift)** dengan §DRIFT CATALOG eksplisit; CMP:3658 dipakai sebagai IN-SCOPE BuildSertifikat. Acceptance literal `CertificationManagement` tetap terpenuhi (via CDP:3740 + drift note).
- Impact: none — fix-set tetap akurat. Plan 03 nanti tak perlu sentuh CMP CertificationManagement (sudah redirect).

**Total deviations:** 1 (Rule 2, drift-aware). **Impact:** audit lebih akurat dari plan; tidak ada kerja terbuang.

## Verification
- 377-AUDIT.md ada + 3 tabel + parity Grep terverifikasi (9/9 literal AC pass).
- ImpersonationIdentityTests.cs ada, namespace `HcPortal.Tests`, 7 InlineData, build RED (CS0246/CS0103 = ekspektasi Wave-0).

## Next
Ready **377-02** (Wave 2 GREEN): implement `ResolveEffectiveUserDecision` (pure, sesuai 5 aturan blueprint) + enum `EffectiveUserDecision` + `GetEffectiveUserAsync()` resolver di `ImpersonationService` + fail-closed D-04 di middleware. Test RED Plan 01 → GREEN.

## Self-Check: PASSED
- 377-AUDIT.md ada, 3 tabel, parity, borderline (DokumenKkj+CertificationManagement) terklasifikasi eksplisit + rationale ✓
- ImpersonationIdentityTests.cs: 7 InlineData, ResolveEffectiveUserDecision call, ImpersonationService ref, no Integration trait ✓
- RED confirmed (CS0246/CS0103 symbol absent) ✓
- git diff Controllers/Services kosong (Task audit+test only) ✓
