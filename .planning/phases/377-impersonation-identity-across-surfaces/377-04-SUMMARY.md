---
phase: 377-impersonation-identity-across-surfaces
plan: 04
subsystem: impersonation-identity
tags: [impersonation, identity, cdp, di-injection, nullable]
requires: ["377-02 (resolver)"]
provides:
  - "CDPController impersonation-aware (DI ImpersonationService + resolver nullable seragam CMP)"
  - "BuildSertifikatRowsAsync(l5OwnDataOnly) resolve effective X (SC3)"
affects: []
tech-stack:
  added: []
  patterns: ["constructor DI (template CMP)", "resolver nullable seragam", "fail-closed empty (D-03)"]
key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
key-decisions:
  - "Inject ImpersonationService ke CDP constructor (Pitfall 2 — sebelumnya tidak ada; Program.cs:63 AddScoped sudah daftar, tak sentuh Program.cs)"
  - "GetCurrentUserRoleLevelAsync seragam ke NULLABLE (dari non-null user!) + impersonation-aware (pola IDENTIK CMP Plan 03)"
  - "CertificationManagement:3740 → cmUser?.Section (mode-role null → tak bocor section admin, T-377-16)"
  - "BuildSertifikatRowsAsync: short-circuit if(user==null) return (empty, roleLevel) — mode-role → 0 row (D-03), cegah full-access leak via scopedUserIds=null + null-safe"
requirements-completed: [IMP-01, IMP-02]
duration: "~20 min"
completed: 2026-06-14
---

# Phase 377 Plan 04: CDP Impersonation-Aware Summary

CDP self-cert (l5OwnDataOnly) impersonation-aware + kill split-brain CMP/CDP (D-05). Tangani Pitfall 2 (CDP belum inject ImpersonationService) dulu agar build tak break.

## Tasks
- **Task 1** (DI): field `_impersonationService` + param constructor + assignment (template CMP). Build 0 err (Pitfall 2 ditangani). Program.cs UTUH.
- **Task 2** (rewrite): `GetCurrentUserRoleLevelAsync` → nullable + impersonation-aware (3-branch UseRealUser/RoleModeEmpty/TargetUser). Caller 3740 `cmUser?.Section`. BuildSertifikatRowsAsync short-circuit `if(user==null) return (empty, roleLevel)`.

## Keputusan null-handling
- **CertificationManagement ViewBag.UserBagian (mode-role):** `cmUser?.Section` → null → view tampil tanpa filter bagian admin (tak bocor section admin, konsisten D-03).
- **BuildSertifikatRowsAsync (mode-role):** short-circuit 0 row. Rationale: roleLevel efektif bisa 1-3 (impersonate role admin/HC) → branch HasFullAccess `scopedUserIds=null` akan tampil SEMUA cert = bocor. Short-circuit `if(user==null)` jamin D-03 kosong + null-safe (cegah NRE `user.Section`/`user.Id`).

## Verification
- `dotnet build HcPortal.csproj` 0 err (DI + nullable null-safe, tak ada NRE-warning).
- Full xUnit suite **368/368** (no regression SC4, termasuk CDPControllerAuthTests). T-377-17.
- CDP `GetCurrentUserRoleLevelAsync` callers (3 total): def + 3755 (cmUser?.Section) + 3875 (BuildSertifikat null-guarded) — semua null-safe.

## Threats mitigated
T-377-13 (BuildSertifikat l5OwnDataOnly → X.Id), T-377-14 (DI Pitfall 2 build), T-377-15 (NRE nullable migration), T-377-16 (ViewBag section admin leak → null), T-377-17 (SC4).

## Deviations from Plan
**[Rule 2 — clarify]** Task 2C plan menyarankan "user?.Id/user?.Section default aman → 0 match". Dipakai pendekatan lebih kuat: **short-circuit `if(user==null) return empty`** di top BuildSertifikatRowsAsync. Alasan: path HasFullAccess (`scopedUserIds=null`) tidak yield 0 dengan default-null saja (yield ALL) → bocor untuk impersonate-role-admin. Short-circuit = D-03-correct + null-safe penuh. Tidak ubah scoping logic. **Impact:** lebih aman, sesuai intent D-03.

## Next
Wave 3 selesai (03/04/05). Ready **377-06** (Wave 4): integration & verification (e2e SC2/SC3/SC4 + ResultsAuthorization impersonate-fidelity matrix + seed + full regression + UAT browser checkpoint).

## Self-Check: PASSED
- DI inject (field+param+assignment), Program.cs utuh ✓
- resolver nullable + GetEffectiveUserAsync, seragam CMP ✓
- caller 3740/3859 null-safe ✓
- build 0 err, full suite 368/368 ✓
