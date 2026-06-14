---
phase: 377-impersonation-identity-across-surfaces
plan: 02
subsystem: impersonation-identity
tags: [impersonation, identity, resolver, middleware, shared-core, fail-closed]
requires: ["377-01 (test contract + audit)"]
provides:
  - "ImpersonationService.GetEffectiveUserAsync/GetEffectiveTargetUserId/ResolveEffectiveUserDecision (D-05 single-source)"
  - "ImpersonationMiddleware D-04 fail-closed (target null → Stop+redirect)"
affects:
  - "Controllers/CMPController.cs (Plan 03 konsumsi GetEffectiveUserAsync)"
  - "Controllers/CDPController.cs (Plan 04), HomeController.cs (Plan 05)"
tech-stack:
  added: []
  patterns: ["pure decision + instance wrapper (kill-drift)", "fail-closed middleware gate (pola auto-expire)"]
key-files:
  created: []
  modified:
    - Services/ImpersonationService.cs
    - Middleware/ImpersonationMiddleware.cs
key-decisions:
  - "Resolver = pure ResolveEffectiveUserDecision (diuji) + GetEffectiveTargetUserId (instance) + GetEffectiveUserAsync (UserManager=parameter, service tak inject UM)"
  - "Konsumen Wave 3 branch by Decision: UseRealUser→resolve real principal; RoleModeEmpty→null kosong+hint (D-03); TargetUser→X"
  - "D-04 DRY di middleware SetContextItems (sudah FindByIdAsync L135) — ubah signature → Task<bool>; false=sudah redirect, caller short-circuit _next"
  - "Fail-closed: target null → Stop()+redirect /Admin/Index + pesan, JANGAN fallback admin (T-377-06)"
requirements-completed: [IMP-01, IMP-02]
duration: "~25 min"
completed: 2026-06-14
---

# Phase 377 Plan 02: Effective-User Resolver + Middleware D-04 Summary

Fondasi single-source (D-05, pola shared-core 363/365/366). Tambah effective-user resolver di `ImpersonationService` (GREEN-kan 7 test RED Plan 01) + fail-closed D-04 di middleware. Controller Wave 3 tinggal panggil `GetEffectiveUserAsync` di hulu.

## Tasks
- **Task 1:** `ImpersonationService.cs` — enum `EffectiveUserDecision` (top-level namespace) + pure static `ResolveEffectiveUserDecision` (5 aturan blueprint) + `GetEffectiveTargetUserId()` + `async GetEffectiveUserAsync(UserManager)` (FQN, service tak inject UM). Method existing TIDAK diubah (hanya ADD). Commit `92d27cbd`. **Test RED Plan 01 → GREEN 7/7.**
- **Task 2:** `ImpersonationMiddleware.cs` — `SetContextItems` signature → `Task<bool>`; di branch `mode=="user"` `targetUser==null` → `service.Stop()` + TempData "User yang di-impersonate tidak ditemukan." + redirect `/Admin/Index` + `return false`. 2 call-site (GET L74, whitelisted-write L107) short-circuit `if(!await ...) return;`. Whitelist/read-only-block/auto-expire TAK disentuh. Commit `bb152320`.

## API final (konsumen Wave 3)
```csharp
public enum EffectiveUserDecision { UseRealUser, RoleModeEmpty, TargetUser }
public static EffectiveUserDecision ResolveEffectiveUserDecision(bool isImpersonating, bool isExpired, string? mode, string? targetUserId);
public string? GetEffectiveTargetUserId();
public async Task<(ApplicationUser? User, EffectiveUserDecision Decision)> GetEffectiveUserAsync(UserManager<ApplicationUser> userManager);
```
Pola konsumen: `var (effUser, decision) = await _impersonationService.GetEffectiveUserAsync(_userManager);`
- `UseRealUser` → caller resolve `_userManager.GetUserAsync(User)` (SC4 identik)
- `RoleModeEmpty` → user=null → surface kosong + hint (D-03, hindari redirect-Login Pitfall 1)
- `TargetUser` → user=X (sudah resolved)

## Verification
- `dotnet build HcPortal.csproj` exit 0.
- `dotnet test --filter ImpersonationIdentity` → **7/7 GREEN** (RED→GREEN).
- Full xUnit suite `dotnet test` → **368/368 GREEN** (361 prior + 7 baru), 0 fail — **no regression SC4** (T-377-07).
- Middleware memuat `User yang di-impersonate tidak ditemukan` + `service.Stop()` di branch user/null; `SetContextItems` return `Task<bool>`.

## Deviations from Plan
None — plan dieksekusi verbatim (code block plan dipakai apa adanya). DRY D-04 = middleware (A2) sesuai rekomendasi plan.

## Threats mitigated
T-377-04 (fail-open admin → fail-closed RoleModeEmpty), T-377-05 (expired→UseRealUser short-circuit), T-377-06 (target deleted → middleware Stop+redirect), T-377-07 (SC4 no-regression, suite green).

## Next
Ready **377-03** (Wave 3): rewrite `CMPController.GetCurrentUserRoleLevelAsync` konsumsi resolver → ~9 caller self-read auto-fix + route 3 bypass (Assessment/ExportRecords/StartExam) + D-03 null-handling Records + guard write-on-GET StartExam.

## Self-Check: PASSED
- Resolver pure+instance+async ada, enum top-level ✓
- Plan 01 test GREEN 7/7, full suite 368/368 ✓
- middleware D-04 string + Stop() + Task<bool> + 2 short-circuit ✓
- method existing utuh (git diff hanya ADD service) ✓
