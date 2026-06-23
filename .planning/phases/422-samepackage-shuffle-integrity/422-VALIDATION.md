---
phase: 422
slug: samepackage-shuffle-integrity
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
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

- [ ] Buat 8 test file baru + extend 1 (lihat RESEARCH §Wave 0 Gaps): `PackageSizeAnalysisTests`, `SamePackageSyncTests`, `SamePackageToggleGuardTests`, `SessionEditLockTests`, `SamePackageInheritTests`, `PackageNumberUniqueTests`, `PackageNumberMigrationTests`, `SiblingTypeAwareLockTests` (atau extend `SiblingPrePostFilterTests`), EXTEND `ShuffleToggleRulesTests`.
- [ ] Reuse fixture real-SQLEXPRESS (`RetakeServiceFixture`/`ProtonCompletionFixture`, pola `ShuffleLockGuardTests.cs:22-30`). Tidak ada framework baru (xUnit 2.9.3 sudah ada).

*Existing xUnit infrastructure covers all phase requirements (extend/baru, jangan bikin framework baru).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Toggle SamePackage ON/OFF + confirm-before; lock banner + tombol hide; warning shuffle ON+SamePackage / K=min / mismatch; toast reject | SHFX-02/03/07 | UI confirm/toast/banner Razor — verifikasi live | Playwright e2e @5270 atau UAT browser: picu toggle (overwrite+lock / unlock keep), edit Post locked (reject), Import sync, warning render non-blocking |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
