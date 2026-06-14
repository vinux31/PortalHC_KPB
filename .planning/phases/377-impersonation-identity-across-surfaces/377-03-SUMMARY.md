---
phase: 377-impersonation-identity-across-surfaces
plan: 03
subsystem: impersonation-identity
tags: [impersonation, identity, cmp, write-on-get, d-03]
requires: ["377-02 (resolver)"]
provides:
  - "CMPController.GetCurrentUserRoleLevelAsync impersonation-aware (~9 caller self-read auto-fix)"
  - "3 bypass surface (Assessment/ExportRecords/StartExam) routed ke effective user"
  - "StartExam write-on-GET guard (read-only invariant) + Records D-03 kosong+hint"
affects:
  - "Views/CMP/Records.cshtml (null-guard + hint)"
  - "Models/ViewModels/CMPRecordsViewModel.cs (User nullable)"
tech-stack:
  added: []
  patterns: ["resolver hulu single-source (kill-drift)", "write-on-GET guard", "D-03 Option A fail-closed UI"]
key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Records.cshtml
    - Models/ViewModels/CMPRecordsViewModel.cs
key-decisions:
  - "GetCurrentUserRoleLevelAsync rewrite konsumsi GetEffectiveUserAsync → 9 caller self-read (Records/RecordsWorkerDetail/Certificate/CertificatePdf/Results/BuildSertifikat 3658/TeamView) auto-fix di hulu"
  - "D-03 Option A: mode-role → Records render User=null + UnifiedRecords kosong + ViewBag.ImpersonateRoleHint, BUKAN redirect Login (Pitfall 1) BUKAN identitas admin"
  - "CMPRecordsViewModel.User → nullable (ApplicationUser?); Records.cshtml null-guard Model.User?.Section (L298)"
  - "ExportRecords mode-role → RedirectToAction(Records); genuinely-null → Challenge"
  - "StartExam write-on-GET (auto-transition SaveChangesAsync) di-guard if(!IsImpersonating()) — read-only invariant (T-377-09)"
requirements-completed: [IMP-01, IMP-02]
duration: "~40 min"
completed: 2026-06-14
---

# Phase 377 Plan 03: CMP Impersonation-Aware Summary

Surface akar bug LIVE 999.6 (impersonate Iwan → /CMP/Records tampil 2 assessment admin). Rewrite resolver CMP konsumsi `GetEffectiveUserAsync` (D-05 hulu) → ~9 caller self-read terfix otomatis. Route 3 bypass + guard write-on-GET + D-03 Records.

## Tasks
- **Task 1** (commit `c6d7b517`): rewrite `GetCurrentUserRoleLevelAsync` (branch UseRealUser/RoleModeEmpty/TargetUser); D-03 Option A di Records (mode-role → kosong+hint, User=null); `CMPRecordsViewModel.User` → nullable; `Records.cshtml` null-guard `Model.User?.Section` + hint block. Build 0 err, filtered 18/18 (ResultsAuth+Impersonation).
- **Task 2a+2b** (commit `10696093`): Assessment(203) + ExportRecords + StartExam(867) route ke `GetCurrentUserRoleLevelAsync` (bukan `GetUserAsync(User)` langsung); StartExam auto-transition `SaveChangesAsync` di-wrap `if(!_impersonationService.IsImpersonating())` (Pitfall 3). Full suite 368/368.

## Auto-fixed caller (via resolver hulu, TIDAK disentuh manual)
Records:481, RecordsWorkerDetail:545, Certificate:1733, CertificatePdf:1839, Results:2080, BuildSertifikatRowsAsync:3658, TeamView 660/721/774 (level/section efektif X, D-01). `IsResultsAuthorized` UTUH (git diff bersih).

## Verification
- `dotnet build HcPortal.csproj` 0 err.
- Filtered `ResultsAuthorization|Impersonation` 18/18; **full suite 368/368** (no regression SC4, T-377-12).
- Grep: StartExam `if(!_impersonationService.IsImpersonating())` membungkus Save ✓; Assessment/ExportRecords/StartExam tak lagi `GetUserAsync(User)` langsung ✓; Records `Pilih user spesifik` + path `RedirectToAction("Login"` terpisah ✓; `Model.User?.` null-guard ✓.

## Threats mitigated
T-377-08 (IDOR Records/Assessment/Results → effective X.Id), T-377-09 (StartExam write-on-GET guarded), T-377-10 (mode-role kosong+hint, bukan Login/admin), T-377-11 (full-fidelity ownership X via IsResultsAuthorized), T-377-12 (SC4 suite green).

## Deviations from Plan
None — Option A locked dieksekusi verbatim. Catatan: BuildSertifikatRowsAsync di CMP:3658 (bukan 3870 plan — drift 378, lihat 377-AUDIT.md); tetap auto-fixed via resolver, tak butuh edit terpisah.

## Catatan untuk UAT (Plan 06)
- SC2: impersonate user X → /CMP/Records, /CMP/Assessment, ExportRecords tampil/ekspor data X.
- D-03: impersonate ROLE (mis. HC) → /CMP/Records kosong + alert "Pilih user spesifik...", TIDAK ada nama admin.
- Pitfall 3: impersonate X → buka StartExam assessment Upcoming-due → status DB TIDAK berubah (cek AssessmentSessions.Status).

## Next
Ready **377-04** (CDP): inject ImpersonationService (Pitfall 2) + rewrite CDP resolver konsumsi GetEffectiveUserAsync (seragamkan nullable) + null-guard 3740/3859.

## Self-Check: PASSED
- resolver branch 3-way + GetEffectiveUserAsync ✓
- D-03 Records kosong+hint User=null, view null-safe ✓
- 3 bypass routed + StartExam guard ✓
- IsResultsAuthorized utuh, full suite 368/368 ✓
