---
phase: 377-impersonation-identity-across-surfaces
plan: 05
subsystem: impersonation-identity
tags: [impersonation, identity, home, dashboard, split-brain]
requires: ["377-02 (resolver)"]
provides:
  - "HomeController.Index impersonation-aware (GetProgress/GetUpcomingEvents = effective user X)"
  - "split-brain L38 (asli) vs L53 (efektif) folded (SC3)"
affects:
  - "Models/DashboardHomeViewModel.cs (CurrentUser nullable)"
  - "Views/Home/Index.cshtml (null-guard + hint)"
tech-stack:
  added: []
  patterns: ["resolver consumption (seragam CMP/CDP)", "D-03 fail-closed UI"]
key-files:
  created: []
  modified:
    - Controllers/HomeController.cs
    - Models/DashboardHomeViewModel.cs
    - Views/Home/Index.cshtml
key-decisions:
  - "Index resolve effective user via GetEffectiveUserAsync; userId = user?.Id ?? '' → GetProgress/GetUpcomingEvents data X (mode-role → kosong, D-03)"
  - "L53 GetEffectiveRoleLevel dipertahankan (sudah efektif) — kini KONSISTEN dengan identitas efektif (split-brain folded)"
  - "DashboardHomeViewModel.CurrentUser → nullable; mode-role CurrentUser=null (strict D-03: TIDAK tampil identitas admin di header, konsisten CMP Records)"
  - "Index.cshtml null-guard 4 deref (FullName×2/Position/Unit) + hint mode-role 'Pilih user spesifik untuk melihat data progress & events'"
  - "Guide/GuideDetail TIDAK disentuh (OUT A4)"
requirements-completed: [IMP-01, IMP-02]
duration: "~15 min"
completed: 2026-06-14
---

# Phase 377 Plan 05: Home/Index Fold Split-Brain Summary

Fold split-brain `HomeController.Index`: L38 user asli (admin) untuk GetProgress/GetUpcomingEvents tapi L53 role-level efektif. Kini effective user X untuk keduanya (SC3). Mode-role → kosong (D-03). Guide OUT.

## Tasks
- **Task 1:** Index resolve effective user (GetEffectiveUserAsync, branch UseRealUser → real + Challenge; else effUser=X/null). `userId = user?.Id ?? ""` → GetProgress/GetUpcomingEvents pakai X. CurrentUser=effective (null saat mode-role). L53 GetEffectiveRoleLevel dipertahankan (kini konsisten). `DashboardHomeViewModel.CurrentUser` → nullable. `Index.cshtml` null-guard 4 deref + hint mode-role.

## Keputusan CurrentUser display (mode-role)
Strict D-03: `CurrentUser=null` (BUKAN admin-header-only). Konsisten dengan CMP Records (Plan 03) — tak tampil identitas admin sama sekali. View null-safe: greeting "Pengguna", Position "Staff", Unit "N/A" default + alert hint. Banner impersonasi existing tetap kasih konteks role.

## Verification
- `dotnet build HcPortal.csproj` 0 err.
- Full xUnit suite **368/368** (no regression SC4, T-377-21).
- git diff HomeController.cs: HANYA Index (Guide/GuideDetail L329/346 UTUH, T-377-20).

## Threats mitigated
T-377-18 (GetProgress/Events → X.Id bukan admin), T-377-19 (mode-role userId="" → 0 record bukan admin), T-377-20 (scope: hanya Index, Guide OUT), T-377-21 (SC4).

## Deviations from Plan
None — plan dieksekusi; dipilih opsi strict (CurrentUser=null + view null-guard) di antara 2 opsi plan, konsisten Plan 03 Records.

## Next
Wave 3 COMPLETE (03 CMP + 04 CDP + 05 Home). Ready **377-06** (Wave 4 — checkpoint UAT): e2e SC2/SC3/SC4 + ResultsAuthorization impersonate-fidelity matrix + seed deterministik + full regression + checkpoint UAT browser.

## Self-Check: PASSED
- Index GetEffectiveUserAsync + GetProgress/Events pakai effective userId ✓
- split-brain folded (L38/L53 konsisten) ✓
- CurrentUser nullable + view null-guard + hint ✓
- Guide utuh, build 0 err, full suite 368/368 ✓
