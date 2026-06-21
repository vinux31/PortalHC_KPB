---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
plan: 04
subsystem: controller
tags: [retake, controller, refactor, endpoint, sibling-propagation, v32.4]
requires:
  - "Services/RetakeService.cs — ExecuteAsync(sessionId, actorUserId, actorName, actionType, reason) + RetakeResult (plan 405-03)"
  - "Helpers/RetakeRules.cs — ShouldHideRetakeToggle(assessmentType, isManualEntry) (plan 405-02)"
  - "3 kolom AssessmentSession (AllowRetake/MaxAttempts/RetakeCooldownHours) + migration AddRetakeColumnsAndArchive (plan 405-01, migration=TRUE)"
provides:
  - "ResetAssessment (HC) delegasi ke RetakeService.ExecuteAsync (actionType=ResetAssessment, reason=hc_reset) + TempData.Remove token (RTK-06)"
  - "Endpoint POST UpdateRetakeSettings (RBAC Admin/HC + AntiForgery + sibling propagation + clamp + audit + PRG) (RTK-04)"
  - "Standard add-users bulk-add mewarisi 3 kolom retake dari savedAssessment (RTK-01)"
  - "ManagePackages ViewBag retake state (AllowRetake/MaxAttempts/RetakeCooldownHours/HideRetakeToggle/RetakeMaxAttemptsUsedInGroup) untuk card UI Phase 406"
affects:
  - "Phase 406 (UI admin: card config ManagePackages konsumsi ViewBag + form POST UpdateRetakeSettings)"
  - "Phase 407 (UI worker self-service: panggil RetakeService.CanRetakeAsync + ExecuteAsync actionType=RetakeAssessment)"
tech_stack:
  added: []
  patterns:
    - "Controller delegasi ke service bersama (ResetAssessment inline → _retakeService.ExecuteAsync); guard HC TETAP di controller"
    - "Config endpoint mirror UpdateShuffleSettings (sibling key Title/Category/Schedule.Date + RBAC + AntiForgery + audit + PRG)"
    - "Clamp server-side (Math.Clamp) sebagai defense-in-depth atas [Range] model-validation"
    - "TempData.Remove token di caller (HTTP-scoped) BUKAN di service (must-fix #1)"
    - "Bulk-add explicit-copy config dari savedAssessment (anti silent EF-default)"
key_files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
decisions:
  - "ResetAssessment GANTI total blok inline archive→delete→reset→audit→SignalR (78 baris) dgn 1 panggilan service + error-redirect + TempData.Remove (22 baris). Guard HC (IsResettable/Pre-Post/status) TETAP di controller (di atas delegasi). Net: -56 baris di method, logika identik (sekarang via service dgn 3 koreksi 405-03)."
  - "UpdateRetakeSettings TIDAK menyalin lock-guard IsShuffleLocked dari UpdateShuffleSettings — retake config dapat diubah kapan saja (D-02 retroaktif). Hanya guard ShouldHideRetakeToggle (PreTest/Manual)."
  - "Bulk-add pre/post (:1944/:1965) sengaja TIDAK disentuh — copy dari model ViewModel (tanpa field retake di 405) → jatuh ke EF default (false/2/24) yang benar untuk Pre (D6) + Post (default-off sampai admin ON)."
metrics:
  duration: "8m"
  completed: "2026-06-21"
  tasks: 3
  files: 1
migration: false
notify_it: false
---

# Phase 405 Plan 04: Backend Core — Wire Controller (Reset Refactor + Config Endpoint) Summary

**One-liner:** Wave 4 (FINAL) — wiring HC retake ke `AssessmentAdminController`: `ResetAssessment` sekarang mendelegasikan ke `RetakeService.ExecuteAsync` (claim-atomik + snapshot per-soal via service) + clear `TempData[TokenVerified_{id}]` (must-fix #1), endpoint baru `UpdateRetakeSettings` (RBAC + AntiForgery + sibling-propagation Title/Category/Schedule.Date + clamp + audit + PRG, mirror `UpdateShuffleSettings`), dan standard add-users bulk-add mewarisi 3 kolom retake dari `savedAssessment` (anti silent EF-default) — semua dalam 1 file controller, `ResetGuardTests` 2/2 + unit suite 436/438 + RetakeService integration 5/5 tetap hijau.

## What Was Built

Wave 4 menutup chain Phase 405 (semua 4 plan: 405-01 data+migration → 405-02 helper pure → 405-03 service → **405-04 controller wire**). Empat perubahan terpisah di `Controllers/AssessmentAdminController.cs` (4 jika dihitung dengan inject DI; di-commit dalam 3 task atomik).

### Task 1 — Inject RetakeService + refactor ResetAssessment (RTK-06) — commit `e1d5defc`

**A. Inject `RetakeService`:** Tambah parameter ke-13 `HcPortal.Services.RetakeService retakeService` ke constructor (setelah `protonBypassService`) + field `private readonly HcPortal.Services.RetakeService _retakeService;` + assignment `_retakeService = retakeService;`. DI `AddScoped<RetakeService>` sudah teregistrasi di `Program.cs` (plan 405-03), jadi resolusi runtime otomatis.

**B. Refactor `ResetAssessment` (:4192):** GANTI blok inline `:4238-4323` (archive→delete→ET-cleanup→ExecuteUpdateAsync-reset→audit→SignalR, 78 baris) dengan:
- `_retakeService.ExecuteAsync(sessionId: id, actorUserId: rsUser?.Id, actorName, actionType: "ResetAssessment", reason: "hc_reset")`
- `if (!rsResult.Success)` → `TempData["Error"] = rsResult.Error` + redirect AssessmentMonitoringDetail.
- **`TempData.Remove($"TokenVerified_{id}")`** setelah sukses (must-fix #1 — StartExam pakai `TempData.Peek` non-consume, token stale WAJIB di-clear oleh caller; service HTTP-scoped tak sentuh TempData).

**DIPERTAHANKAN di controller:** Guard HC `IsResettable` (`:4193` tetap `public static`), Pre-Post block (`:4211-4225`), status guard (`:4228-4236`), trailing success `TempData["Success"]` + redirect. Inline `AssessmentAttemptHistory.Add` / `ExecuteUpdateAsync` / `SendAsync("sessionReset")` TIDAK lagi ada di controller (grep `SendAsync("sessionReset")` count = 0 di controller — sekarang di service).

### Task 2 — Endpoint UpdateRetakeSettings + ManagePackages ViewBag (RTK-04) — commit `7e3fc4aa`

**A. `UpdateRetakeSettings`** (sisip setelah `UpdateShuffleSettings`):
- `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` (T-405-13/14).
- Params `(int assessmentId, bool allowRetake, int maxAttempts, int retakeCooldownHours)`.
- Guard `RetakeRules.ShouldHideRetakeToggle(AssessmentType, IsManualEntry)` → reject PreTest/Manual (T-405-17).
- `Math.Clamp(maxAttempts, 1, 5)` + `Math.Clamp(retakeCooldownHours, 0, 168)` (T-405-15, defense-in-depth).
- Sibling propagation key identik shuffle: `Title == && Category == && Schedule.Date ==` → foreach set 3 field + UpdatedAt → SaveChanges.
- Audit `LogAsync("UpdateRetakeSettings", ...)` try/catch warn-only.
- PRG: `TempData["Success"]` + RedirectToAction ManagePackages.
- **TIDAK ada** lock-guard `IsShuffleLocked` (retake config tak terkunci-saat-mulai, beda dari shuffle — D-02 retroaktif).

**B. ManagePackages ViewBag** (sisip setelah `ViewBag.HideShuffleToggle`):
- `ViewBag.AllowRetake` / `MaxAttempts` / `RetakeCooldownHours` (saved-state untuk render checked/value).
- `ViewBag.HideRetakeToggle = RetakeRules.ShouldHideRetakeToggle(...)`.
- `ViewBag.RetakeMaxAttemptsUsedInGroup` = max attempt-history per-user di grup (Title/Category) + 1 — warning non-blocking untuk card 406.

### Task 3 — Carry 3 kolom retake di standard add-users bulk-add (RTK-01) — commit `e6abb938`

Di block `newSessions = filteredNewUserIds.Select(uid => new AssessmentSession {...})` (:2166, copy dari `savedAssessment`), sisip setelah `ShuffleOptions`:
```csharp
AllowRetake = savedAssessment.AllowRetake,
MaxAttempts = savedAssessment.MaxAttempts,
RetakeCooldownHours = savedAssessment.RetakeCooldownHours,
```
Pekerja baru di assessment existing mewarisi policy retake sibling (bukan jatuh diam-diam ke EF default). Pre/Post add path (`:1944`/`:1965`) **TIDAK disentuh** (copy dari `model` ViewModel tanpa field retake → compile-error bila ditambah; jatuh ke EF default false/2/24 yang BENAR untuk Pre D6 + Post default-off).

## Verification Results

| Cek | Hasil |
|-----|-------|
| `dotnet build` (full, baseline) | Build succeeded, 0 Error ✓ |
| `dotnet build` (setelah Task 1) | Build succeeded, 0 Error (sempat 1 error CS7036 dari test ctor → fixed) ✓ |
| `dotnet build` (setelah Task 2) | Build succeeded, 0 Error ✓ |
| `dotnet build` (setelah Task 3) | Build succeeded, 0 Error ✓ |
| `dotnet test --filter ResetGuardTests` | **Passed! 2/2, 0 failed** (regresi RTK-06 hijau, IsResettable tetap static) ✓ |
| `dotnet test --filter "Category!=Integration"` | **Passed! 436/438 (2 skipped), 0 failed** — no regresi unit (baseline 405-03 identik) ✓ |
| `dotnet test --filter RetakeServiceTests` | **Passed! 5/5, 0 failed** (SQLEXPRESS, service masih jalan pasca-delegasi controller) ✓ |
| Grep `_retakeService.ExecuteAsync` + `TempData.Remove($"TokenVerified_` | verified :4251 + :4270 ✓ |
| Grep `SendAsync("sessionReset")` di controller | count = **0** (dipindah ke service) ✓ |
| Grep `UpdateRetakeSettings` + `Math.Clamp` + sibling-key | verified :5567 / :5580-5581 / :5585 ✓ |
| Grep `AllowRetake = savedAssessment.AllowRetake` | 1 occurrence (standard add-users only; pre/post untouched) ✓ |
| `IsResettable` tetap `public static` | verified :4193 ✓ |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking adapt] `AssessmentWindowRemovalTests` ctor break dari signature controller baru**
- **Found during:** Task 1 (build setelah inject RetakeService)
- **Issue:** `HcPortal.Tests/AssessmentWindowRemovalTests.cs:48` meng-konstruksi `AssessmentAdminController` secara manual (bukan via DI). Penambahan param ke-13 `retakeService` → `error CS7036: no argument given that corresponds to required parameter 'retakeService'`.
- **Fix:** Tambah `retakeService: null!` ke pemanggilan ctor (mengikuti pola eksplisit file itu: "null! substitutes for ctor deps that the tested code path never dereferences" — action `ManageAssessmentTab_Assessment` yang diuji TIDAK pakai `_retakeService`). Zero logika baru.
- **Files modified:** HcPortal.Tests/AssessmentWindowRemovalTests.cs
- **Commit:** `e1d5defc` (bundel dgn Task 1)

Selebihnya plan dieksekusi persis: signature `RetakeService.ExecuteAsync` / `RetakeResult` cocok dengan plan `<interfaces>` (di-verify dari `Services/RetakeService.cs`); `RetakeRules.ShouldHideRetakeToggle(string?, bool)` cocok; `UpdateShuffleSettings` sibling-key di-mirror VERBATIM; semua field model (`AllowRetake`/`MaxAttempts`/`RetakeCooldownHours`/`UpdatedAt`/`AssessmentType`/`IsManualEntry`) + `AssessmentAttemptHistory.UserId/Title/Category` di-verify ada sebelum edit.

## TDD Gate Compliance

Plan 405-04 `type: execute` (bukan `type: tdd`) — tidak ada gate RED/GREEN per-task. Regresi `ResetGuardTests` (existing, RTK-06 guard) dipakai sebagai safety-net dan tetap hijau pasca-refactor.

## Notes for Downstream Plans

- **Phase 406** (UI admin): Render card config retake di view ManagePackages (`~/Views/Admin/ManagePackages.cshtml`) konsumsi `ViewBag.AllowRetake`/`MaxAttempts`/`RetakeCooldownHours`/`HideRetakeToggle`/`RetakeMaxAttemptsUsedInGroup` + form POST `UpdateRetakeSettings` (3 input + antiforgery token). Hide card bila `HideRetakeToggle==true`. Warning non-blocking bila `MaxAttempts < RetakeMaxAttemptsUsedInGroup`. Bila perlu wire `model` → pre/post bulk-add untuk field retake (saat ini EF-default), lakukan di 406 dengan menambah field ke ViewModel.
- **Phase 407** (UI worker self-service): `CMP/RetakeExam` controller WAJIB re-cek `await _retakeService.CanRetakeAsync(id)` (server-authoritative, jangan trust client) + ownership UserId → `_retakeService.ExecuteAsync(id, workerId, workerName, "RetakeAssessment", "worker_retake")` + `TempData.Remove($"TokenVerified_{id}")`.
- `RetakeService` adalah satu-satunya entry point reset/retake produksi sekarang (HC + worker). Jangan duplikasi logika archive/delete di controller.

## Migration Carry (notify IT)

**Plan 405-04 = migration=FALSE** (perubahan controller murni, 0 schema).

⚠️ **Plan 405-01 = migration=TRUE** (`AddRetakeColumnsAndArchive` `20260621065918` — 3 kolom `AssessmentSession` + tabel `AssessmentAttemptResponseArchive`, applied lokal `HcPortalDB_Dev` saja). Saat promosi/deploy bundle v32.4 (bareng v32.1+v32.3): notify IT **migration=TRUE** dengan commit hash migration 405-01 (`69db727a`). Carry lama tetap berlaku (360 `PendingProtonBypass`, 372 `ShuffleToggles`, 399 `AddUserUnitsTable`). NOT pushed.

## Known Stubs

Tidak ada. Semua ViewBag mengekspos nilai model riil (`assessment.AllowRetake` dll) — bukan hardcode/placeholder. Card UI yang mengonsumsi ViewBag di-render Phase 406 (didokumentasikan di plan sebagai scope 406, BUKAN stub di plan ini).

## Threat Flags

Tidak ada surface keamanan baru di luar `<threat_model>` plan. Semua mitigasi diterapkan:
- **T-405-13** (CSRF) mitigate: `[ValidateAntiForgeryToken]` di UpdateRetakeSettings (Task 2) + ResetAssessment existing (Task 1 keep).
- **T-405-14** (EoP non-Admin/HC) mitigate: `[Authorize(Roles = "Admin, HC")]` di UpdateRetakeSettings + ResetAssessment.
- **T-405-15** (out-of-range maxAttempts/cooldown) mitigate: `Math.Clamp` server-side.
- **T-405-16** (stale token re-entry) mitigate: `TempData.Remove($"TokenVerified_{id}")` pasca-ExecuteAsync sukses.
- **T-405-17** (retake config untuk PreTest/Manual) mitigate: guard `ShouldHideRetakeToggle` reject.

## Self-Check: PASSED

- File modified: `Controllers/AssessmentAdminController.cs` — FOUND.
- SUMMARY: `.planning/phases/405-.../405-04-SUMMARY.md` — FOUND (this file).
- Commits: `e1d5defc` (refactor RTK-06), `7e3fc4aa` (feat RTK-04), `e6abb938` (feat RTK-01) — semua FOUND di git log.
