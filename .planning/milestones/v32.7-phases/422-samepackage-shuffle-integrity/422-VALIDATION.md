---
phase: 422
slug: samepackage-shuffle-integrity
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-23
validated: 2026-06-23
---

# Phase 422 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `422-RESEARCH.md` §Validation Architecture. Per-task map finalized at plan/validate-phase.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0) + Playwright (e2e, opsional UI confirm) |
| **Config file** | none (konvensi `[Trait("Category","Integration")]` SQL-gated; pure default) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (pure helper, < 30s) |
| **Full suite command** | `dotnet test` (incl integration real-SQLEXPRESS) |
| **Estimated runtime** | ~60-120 detik (unit+integration); e2e terpisah |

---

## Sampling Rate

- **After every task commit:** `dotnet test --filter "Category!=Integration"` + `dotnet build`
- **After every plan wave:** `dotnet test` (full, real-SQLEXPRESS)
- **Before `/gsd-verify-work`:** Full suite green + build 0-err + migration `dotnet ef database update` lokal sukses + `sqlcmd` cek 0 duplikat PackageNumber
- **Max feedback latency:** ~120 detik

---

## Per-Task Verification Map

> Skeleton dari RESEARCH §Validation Architecture — diisi lengkap oleh planner (per-task) + `/gsd-validate-phase`.

| Requirement | Secure Behavior | Test Type | Target (extend/baru) |
|-------------|-----------------|-----------|----------------------|
| SHFX-01 | Import ke Pre ber-SamePackage → Post ter-sync; tanpa SamePackage → tidak; helper no-op bila bukan Pre/linkedPost null/!SamePackage | integration | `SamePackageSyncTests.cs` (baru) |
| SHFX-02 | Toggle ON→SamePackage=true+sync; OFF→false+paket clone DIPERTAHANKAN; guard anyStarted→REJECT, belum-mulai→ALLOW | integration | `SamePackageToggleGuardTests.cs` (baru) |
| SHFX-03 | `IsSessionEditLocked` true bila PostTest&&SamePackage; 5 endpoint POST reject saat locked (no-write), lolos saat tak-locked | unit + integration | `SessionEditLockTests.cs` (baru, pola `ShuffleLockGuardTests`) |
| SHFX-04 | newPost tambah-peserta warisi SamePackage = repPost.SamePackage | integration | `SamePackageInheritTests.cs` (baru) |
| SHFX-05 | CreatePackage MAX+1 (no bentrok pasca-delete-tengah); unique index tolak duplikat→DbUpdateException; migration dedup renumber→0 duplikat | integration | `PackageNumberUniqueTests.cs` + `PackageNumberMigrationTests.cs` (baru) |
| SHFX-06 | Lock-detection pakai SiblingPrePostAwarePredicate type-aware (Pre mulai → Post TIDAK terkunci). ⚠️ scope = lock-detection SAJA, JANGAN ubah propagation write UpdateShuffleSettings (regresi) | integration | `SiblingTypeAwareLockTests.cs` (baru) atau extend `SiblingPrePostFilterTests` |
| SHFX-07 | `ShouldShowKMinTruncationWarning` ON-path true bila ≥2 paket-ber-soal+ON+mismatch; `PackageSizeAnalysis.Compute` paritas dgn view-lama (hasMismatch/referenceCount) | unit | EXTEND `ShuffleToggleRulesTests.cs` + `PackageSizeAnalysisTests.cs` (baru) |

---

## Wave 0 Requirements

- [x] Buat 9 test file baru + extend 1: `PackageSizeAnalysisTests`, `SessionEditLockRulesTests`, `SamePackageSyncTests`, `SamePackageToggleGuardTests`, `SessionEditLockTests`, `SamePackageInheritTests`, `PackageNumberUniqueTests`, `PackageNumberMigrationTests`, `SiblingTypeAwareLockTests`, EXTEND `ShuffleToggleRulesTests`. Semua hijau.
- [x] Reuse fixture real-SQLEXPRESS (`ProtonCompletionFixture`/`RetakeServiceFixture`, pola `ShuffleLockGuardTests.cs:22-30`). Tidak ada framework baru (xUnit 2.9.3).

*Existing xUnit infrastructure covers all phase requirements (extend/baru, jangan bikin framework baru).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Toggle SamePackage ON/OFF + confirm-before; lock banner + tombol hide; warning shuffle ON+SamePackage / K=min / mismatch; toast reject | SHFX-02/03/07 | UI confirm/toast/banner Razor — verifikasi live | Playwright e2e @5270 atau UAT browser: picu toggle (overwrite+lock / unlock keep), edit Post locked (reject), Import sync, warning render non-blocking |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (0 MISSING)
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-23 — semua 7 REQ COVERED otomatis, 0 gap.

---

## Validation Audit 2026-06-23

State A audit (skeleton finalized). Semua 7 REQ (SHFX-01..07) → COVERED otomatis oleh 10 test file (9 baru + 1 extend), semua hijau. Full suite **692 passed / 0 failed / 2 skipped** (re-run pasca code-review-fix). migration `AddPackageNumberUniqueIndex` applied + 0-dup. UI confirm/lock/warning diverifikasi checkpoint UAT 6/6 PASS (Manual-Only by design — render-class, `422-UAT.md`).

| Requirement | Test (green) | Status |
|-------------|--------------|--------|
| SHFX-01 | SamePackageSyncTests | ✅ COVERED |
| SHFX-02 | SamePackageToggleGuardTests (6: ON/OFF-keep/guard/allow/dangling-UPA/non-paired) + UAT | ✅ COVERED |
| SHFX-03 | SessionEditLockRulesTests + SessionEditLockTests | ✅ COVERED |
| SHFX-04 | SamePackageInheritTests | ✅ COVERED |
| SHFX-05 | PackageNumberUniqueTests + PackageNumberMigrationTests | ✅ COVERED |
| SHFX-06 | SiblingTypeAwareLockTests (lock-only + propagation no-regress) | ✅ COVERED |
| SHFX-07 | ShuffleToggleRulesTests + PackageSizeAnalysisTests | ✅ COVERED |

| Metric | Count |
|--------|-------|
| Requirements | 7 (SHFX-01..07) |
| COVERED (automated) | 7 |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

Auditor spawn + test-gen dilewati (0 gap; semua REQ sudah ada test hijau dari eksekusi 422-01/02/03).
