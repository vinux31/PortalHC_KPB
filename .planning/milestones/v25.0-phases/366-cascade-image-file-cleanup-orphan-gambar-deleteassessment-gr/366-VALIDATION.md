---
phase: 366
slug: cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-12
---

# Phase 366 — Validation Strategy

> Reconstructed from artifacts (State B — no VALIDATION.md at plan time; research skipped, validation captured here retroactively).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8 / C#) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate config) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ImageCleanupIntegrationTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~24s full suite (229 tests); ~1s integration filter |

**Note:** 2 integration tests (`ImageCleanupIntegrationTests`) require a live SQL Server (`localhost\SQLEXPRESS`) — they create a disposable `HcPortalDB_Test_<guid>` (dropped per run). On SQL-less CI, skip via `dotnet test --filter "Category!=Integration"`.

---

## Sampling Rate

- **After every task commit:** `dotnet build` + relevant filter (`PackageImageDeleteTests`/`PackageImageSyncTests` for Plan 01; `ImageCleanupIntegrationTests` for Plan 02/03)
- **After every plan wave:** `dotnet test` (full)
- **Before completion:** Full suite green (achieved: 229/229)
- **Max feedback latency:** ~24s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 366-01-01/02 | 01 | 1 | SC1-helper-extract | T-366-01/02/03 | Helper hanya terima path DB-sourced; warn-only; perilaku 3 call-site identik | unit/behavior | `dotnet test --filter "FullyQualifiedName~PackageImageDeleteTests\|FullyQualifiedName~PackageImageSyncTests"` | ✅ | ✅ green (11/11) |
| 366-02-01/02 | 02 | 2 | SC2-cascade-install, SC3-shared-survive | T-366-04/05/06 | Collect-before-RemoveRange + helper-after-CommitAsync; ref-count post-commit cegah over-delete shared | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ImageCleanupIntegrationTests"` | ✅ | ✅ green (2/2) |
| 366-03-01 | 03 | 3 | SC4-test-uat, SC3-shared-survive | T-366-05/08 | Orphan deleted (SC#2) + shared survives (SC#3) atas real-SQL; fixture disposable drop | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ImageCleanupIntegrationTests"` | ✅ | ✅ green (2/2) |
| 366-03-02 | 03 | 3 | SC4-test-uat (D-04 reconcile) | — | Tak ada 2 logika ref-count divergen; full suite tanpa regresi | regression | `dotnet test` | ✅ | ✅ green (229/229) |
| 366-03-03 | 03 | 3 | SC4-test-uat (UAT) | T-366-09 | File fisik orphan terhapus end-to-end di app nyata | manual (browser+FS) | — (lihat Manual-Only) | N/A | ✅ performed PASS |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure (xUnit `HcPortal.Tests`) covers all phase requirements. 1 test file added (`ImageCleanupIntegrationTests.cs`) — no framework install needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UAT end-to-end hapus assessment bergambar → file fisik bersih (Skenario A/C) | SC4-test-uat (SC#2) | Butuh live app @localhost:5277 + inspeksi filesystem `wwwroot/uploads/questions/` + UI flow nyata; tak ada automated command browser. **Underlying SC#2 sudah otomatis** di `OrphanPath_Deleted_WhenFullCascade` — UAT ini konfirmatori. | Start `Authentication__UseActiveDirectory=false dotnet run` → admin login → buat assessment bergambar → Manage Packages/Questions upload PNG → Hapus Grup → cek file di `uploads/questions/{pkg}/` HILANG + DB cascade 0. **PERFORMED 2026-06-12 PASS** (366-03-SUMMARY.md). |
| UAT shared Pre/Post selamat (Skenario B) | SC3-shared-survive | Butuh setup Pre-Post SamePackage shared-image + single-side delete via UI. **Underlying SC#3 sudah otomatis** di `SharedPrePostPath_Survives_WhenOneSideDeleted` (real-SQL). | Browser-retest dilewati (banyak langkah); dijamin oleh integration test + pola DeletePrePostGroup identik dgn DeleteAssessmentGroup yang browser-verified. |

---

## Validation Sign-Off

- [x] Semua task punya `<automated>` verify (SC1-3 fully automated; SC4-UAT inheren manual + underlying SC#2/#3 automated)
- [x] Sampling continuity: tiap wave ada automated verify (no 3 consecutive tanpa automated)
- [x] Wave 0 covers all references (infra existing + 1 file baru)
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set

**Approval:** approved 2026-06-12 (retroactive — SC#1-4 automated-covered; UAT browser manual-only confirmatory, performed PASS)
