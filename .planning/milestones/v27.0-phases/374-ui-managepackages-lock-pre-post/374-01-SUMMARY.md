---
phase: 374-ui-managepackages-lock-pre-post
plan: 01
subsystem: shuffle-toggle-foundation
tags: [helper, tdd, wave-0, real-sql]
requires: []
provides:
  - "HcPortal.Helpers.ShuffleToggleRules (pure decision helper)"
  - "Wave 0 contract tests (lock-guard + propagate) for endpoint Plan 02"
affects:
  - "Plan 02 endpoint UpdateShuffleSettings (MUST call ShuffleToggleRules in GET + POST)"
tech-stack:
  added: []
  patterns:
    - "Pure static helper (no EF/DbContext) — single-source decision shared GET ViewBag + POST guard (kills Pitfall 2 divergence)"
    - "Real-SQL replica tests via IClassFixture<ProtonCompletionFixture> + [Trait(Category,Integration)]"
key-files:
  created:
    - "Helpers/ShuffleToggleRules.cs"
    - "HcPortal.Tests/ShuffleToggleRulesTests.cs"
    - "HcPortal.Tests/ShuffleLockGuardTests.cs"
    - "HcPortal.Tests/ShuffleUpdateEndpointTests.cs"
  modified: []
key-decisions:
  - "Namespace block-style HcPortal.Helpers (match ShuffleEngine.cs), bukan file-scoped"
  - "Lock helper signature IsShuffleLocked(bool anyStarted, bool anyAssignment) — caller menyuplai fakta DB; helper tetap pure"
  - "File A propagate test pakai key LENGKAP (Title+Category+Schedule.Date) untuk buktikan sibling key spec §5, bukan hanya Title"
requirements-completed: [SHUF-10, SHUF-11, SHUF-12, SHUF-14]
duration: ~12 min
completed: 2026-06-13
---

# Phase 374 Plan 01: Shuffle Toggle Foundation Summary

SATU helper pure `ShuffleToggleRules` (lock/hide/warning) + 3 Wave 0 test file yang mengunci kontrak endpoint `UpdateShuffleSettings` SEBELUM endpoint ditulis (Plan 02).

**Durasi:** ~12 min | **Task:** 3 | **File:** 4 created.

## Yang Dibangun

1. **`Helpers/ShuffleToggleRules.cs`** — 3 method static pure (tanpa EF/DbContext):
   - `IsShuffleLocked(bool anyStarted, bool anyAssignment)` → SHUF-11
   - `ShouldHideShuffleToggle(string? category, string? tahunKe, bool isManualEntry)` → SHUF-14 (`"Assessment Proton"`+`"Tahun 3"` ATAU manual)
   - `ShouldShowSizeMismatchWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)` → SHUF-12
2. **`ShuffleToggleRulesTests.cs`** — 14 `[Theory]` cases (lock 4, hide 5, warning 5), pure tanpa DB.
3. **`ShuffleUpdateEndpointTests.cs`** — real-SQL `UpdateShuffleSettings_PropagatesToAllSiblings` (SHUF-10), filter key lengkap.
4. **`ShuffleLockGuardTests.cs`** — real-SQL 3 test (SHUF-11): reject-when-started, allow-when-clean, reject-when-assignment. Guard via `ShuffleToggleRules.IsShuffleLocked` → mengikat helper ke kontrak.

## Kontrak untuk Plan 02 (WAJIB referensi)

- Namespace: `HcPortal.Helpers`. Class: `ShuffleToggleRules` (static).
- GET ManagePackages: `ViewBag.IsShuffleLocked = ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment)`; `ViewBag.HideShuffleToggle = ShuffleToggleRules.ShouldHideShuffleToggle(...)`.
- POST UpdateShuffleSettings: hitung `anyStarted`/`anyAssignment` dari siblings (pola test), panggil `IsShuffleLocked` SAMA. Locked → TempData error, NO write. Clean → foreach propagate + `UpdatedAt=now` + audit.
- Sibling key: `Title == ... && Category == ... && Schedule.Date == ...` (spec §5).

## Verifikasi

- `dotnet build HcPortal.csproj` → Build succeeded.
- `dotnet test --filter "FullyQualifiedName~ShuffleToggleRules"` → 14/14 pass.
- `dotnet test --filter "FullyQualifiedName~ShuffleLockGuard|...ShuffleUpdateEndpoint"` → 4/4 pass.
- `dotnet test --filter "FullyQualifiedName~Shuffle"` → **41/41 pass** (23 baseline + 18 baru, no regression).
- Helper grep-clean: tidak ada `_context`/`DbContext`.

## Deviations from Plan

**[Rule 1 - Bug] Komentar doc trip grep verification** — Found during: Task 1 | Komentar "Tidak menyentuh DbContext" memicu grep `DbContext` false-positive | Reword → "Tidak menyentuh database/EF" | Files: Helpers/ShuffleToggleRules.cs | Verified: grep clean | Commit: 75b3ee53 (amended).

**Total deviations:** 1 auto-fixed (1 cosmetic). **Impact:** nol fungsional — kode helper sudah pure sejak awal, hanya komentar.

## Self-Check: PASSED

- 4 file created ada di disk.
- 5 commit (`75b3ee53` helper, test unit, test Wave 0) di git log.
- Semua acceptance_criteria 3 task re-run PASS.

Ready for **374-02** (endpoint + GET ViewBag enrich).
