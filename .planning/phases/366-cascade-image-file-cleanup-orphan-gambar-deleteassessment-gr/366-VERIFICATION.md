---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
verified: 2026-06-12T08:30:00Z
status: passed
score: 4/4
overrides_applied: 0
re_verification: false
---

# Phase 366: Cascade Image File Cleanup — Verification Report

**Phase Goal:** Hapus file gambar fisik orphan saat cascade delete besar — DeleteAssessment / DeleteAssessmentGroup / DeletePrePostGroup di AssessmentAdminController.cs (saat ini RemoveRange Questions/Options dari DB tanpa sentuh file di wwwroot/uploads/questions/{packageId}). Caranya: ekstrak helper ref-count static dari 3 call-site inline Phase 353, pasang di 3 cascade method. Migration=false. Backend-only.

**Verified:** 2026-06-12T08:30:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Helper static `ImageFileCleanup.DeleteUnreferencedAsync` diekstrak dari 3 call-site inline; 3 call-site lama memakai helper; perilaku tak berubah | VERIFIED | `Helpers/ImageFileCleanup.cs` ada, 42 baris, `public static async Task DeleteUnreferencedAsync`, ref-count `AnyAsync` Q+O, try/catch warn-only. Controller: grep `_context.PackageQuestions.AnyAsync(x => x.ImagePath` = 0x (inline lama tergantikan); grep `ImageFileCleanup.DeleteUnreferencedAsync` = 6x total, 3 di call-site lama (L5799/L6750/L6841 pakai `_logger`). |
| 2 | 3 method Delete* kumpul ImagePath Distinct SEBELUM RemoveRange, eksekusi hapus file SETELAH tx.CommitAsync (pola Phase 333); batch-aware via post-commit AnyAsync | VERIFIED | DeleteAssessment L2318-2325: collect `imagePaths` dari packages SEBELUM L2334 `RemoveRange`; helper call L2346 SETELAH `tx.CommitAsync()` L2342. DeleteAssessmentGroup L2513-2520: collect SEBELUM L2529 `RemoveRange`; helper L2542 SETELAH CommitAsync L2539. DeletePrePostGroup L2704-2711: collect SEBELUM L2718 `RemoveRange`; helper L2731 SETELAH CommitAsync L2727. Tanpa exclusion-set (D-05). |
| 3 | Gambar yang masih direferensikan di luar batch TIDAK terhapus (shared-path Pre/Post selamat) — dijamin oleh post-commit AnyAsync tanpa exclusion-set | VERIFIED | Integration test `SharedPrePostPath_Survives_WhenOneSideDeleted` (L182) exercise helper produksi nyata atas real-SQL: hapus 1 sisi Pre, Post masih mereferensikan path → AnyAsync true → SKIP. Test 2/2 passed (per SUMMARY-03). Pola identik di semua 3 cascade method (tidak ada exclusion-set di controller — grep `exclusionSet|excludeSet` = 0x). |
| 4 | dotnet build 0 error + dotnet test hijau (229/229) + UAT @5277 (DeleteAssessmentGroup file fisik terhapus + DB cascade bersih) | VERIFIED | `dotnet build` verified: 0 error (24 warning pre-existing). Full suite 229/229 passed per SUMMARY-03 (termasuk 2 real-SQL integration test `ImageCleanupIntegrationTests` dan 7 test dari `PackageImageDeleteTests`). UAT SC#2: assessment bergambar "Pre Test UAT366 Cleanup Orphan" dihapus via DeleteAssessmentGroup → file `wwwroot/uploads/questions/63/20260612032822864_84ef8b90_01-login.png` TERHAPUS + DB cascade bersih. Folder kosong tetap ada = expected (CONTEXT Deferred Area 4, bukan defect). |

**Score:** 4/4 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/ImageFileCleanup.cs` | Helper static ref-count + File.Delete warn-only | VERIFIED | Ada, 42 baris, namespace `HcPortal.Helpers`, `public static class ImageFileCleanup`, `public static async Task DeleteUnreferencedAsync`, guard IsNullOrEmpty, ref-count AnyAsync Q+O, Path.Combine confined webroot, try/catch warn-only. |
| `Controllers/AssessmentAdminController.cs` | 6x helper call (3 swap + 3 cascade install), 0x inline predikat | VERIFIED | Grep: 6x `ImageFileCleanup.DeleteUnreferencedAsync` (L2346/L2542/L2731 cascade + L5799/L6750/L6841 call-site lama). 0x `_context.PackageQuestions.AnyAsync(x => x.ImagePath`. Label source 6 distinct masing-masing 1x. |
| `HcPortal.Tests/ImageCleanupIntegrationTests.cs` | 2 [Fact] real-SQL disposable, Trait Integration | VERIFIED | Ada, 233 baris, fixture `ImageCleanupFixture : IAsyncLifetime`, `[Trait("Category", "Integration")]`, `IClassFixture<ImageCleanupFixture>`. 2 [Fact]: `OrphanPath_Deleted_WhenFullCascade` (L122) + `SharedPrePostPath_Survives_WhenOneSideDeleted` (L182). 2x `ImageFileCleanup.DeleteUnreferencedAsync` (L169/L223). |
| `HcPortal.Tests/PackageImageDeleteTests.cs` | Komentar sumber-kebenaran D-04; ApplyIntent/PathStillReferenced utuh | VERIFIED | Komentar di L35-38: "Production source of truth: Helpers/ImageFileCleanup.cs. Integration coverage: ImageCleanupIntegrationTests.cs". `ApplyIntent` (L138) + `PathStillReferenced` (L30) masih ada dan utuh. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AssessmentAdminController` DeletePackage/EditQuestion/DeleteQuestion | `Helpers/ImageFileCleanup.DeleteUnreferencedAsync` | `await` call menggantikan foreach inline | WIRED | L5799/L6750/L6841 menggunakan `_logger` (field kelas) — benar sesuai Plan 01. |
| `AssessmentAdminController` DeleteAssessment/DeleteAssessmentGroup/DeletePrePostGroup (post-commit) | `Helpers/ImageFileCleanup.DeleteUnreferencedAsync` | `await` call SETELAH tx.CommitAsync | WIRED | L2346/L2542/L2731 menggunakan `logger` (lokal method) — benar sesuai Plan 02. Ordering: collect < RemoveRange < CommitAsync < helper. |
| `collect imagePaths` | `packages/allPackages.SelectMany(Questions+Options).ImagePath` | `Distinct()` sebelum RemoveRange | WIRED | Pola SelectMany dengan `new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath))` + `.Where(!empty).Distinct()` terverifikasi di 3 method. |
| `ImageCleanupIntegrationTests` | `Helpers/ImageFileCleanup.DeleteUnreferencedAsync` (produksi) | panggil helper produksi nyata atas real-SQL DbContext | WIRED | L169/L223 memanggil helper produksi, bukan implementasi mirror. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| `ImageFileCleanup.DeleteUnreferencedAsync` | `paths` (IEnumerable<string>) | Dikumpul dari `packages/allPackages` in-memory setelah `.Include(p=>p.Questions).ThenInclude(q=>q.Options)` | Ya — ImagePath dari baris DB yang di-Include, bukan hardcoded | FLOWING |
| Helper ref-count guard | `stillUsedQ` / `stillUsedO` | `ctx.PackageQuestions.AnyAsync(x=>x.ImagePath==relUrl)` + `ctx.PackageOptions.AnyAsync(...)` — query SQL nyata post-commit | Ya — real DB query setelah commit | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| `Helpers/ImageFileCleanup.cs` ada dengan signature benar | Read file | `public static async Task DeleteUnreferencedAsync` ditemukan, 42 baris | PASS |
| 6x helper call di controller, 0x inline predikat | Grep controller | 6x `ImageFileCleanup.DeleteUnreferencedAsync`; 0x `_context.PackageQuestions.AnyAsync(x => x.ImagePath` | PASS |
| 6 label source distinkt masing-masing 1x | Grep labels | "DeletePackage image"/"question image"/"DeleteQuestion image"/"DeleteAssessment image"/"DeleteAssessmentGroup image"/"DeletePrePostGroup image" masing-masing 1x | PASS |
| Tidak ada exclusion-set (D-05 murni post-commit AnyAsync) | Grep `exclusionSet\|excludeSet` | 0 matches | PASS |
| dotnet build | `dotnet build` | 0 Error(s), 24 Warning(s) pre-existing — Build succeeded | PASS |
| 2 [Fact] integration test ada dan diwired ke helper produksi | Read + Grep | `OrphanPath_Deleted_WhenFullCascade` + `SharedPrePostPath_Survives_WhenOneSideDeleted` keduanya ada; 2x call helper produksi | PASS |
| Mirror D-04 ditandai sumber-kebenaran | Grep PackageImageDeleteTests | Komentar L35-38 menyebut "Production source of truth: Helpers/ImageFileCleanup.cs" dan "Integration coverage: ImageCleanupIntegrationTests.cs" | PASS |
| ApplyIntent/PathStillReferenced utuh (out of scope) | Grep | Keduanya ada di PackageImageDeleteTests.cs L30/L138 | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| SC1-helper-extract | Plan 01 | Helper static `DeleteUnreferencedAsync` diekstrak; 3 call-site lama pakai helper; perilaku identik | SATISFIED | `Helpers/ImageFileCleanup.cs` ada; 0x inline predikat di controller; 3 call-site lama verified |
| SC2-cascade-install | Plan 02 | 3 method Delete* kumpul ImagePath Distinct sebelum RemoveRange, panggil helper setelah CommitAsync | SATISFIED | Ordering verified di L2318-2346, L2513-2542, L2704-2731 |
| SC3-shared-survive | Plan 02 + Plan 03 | Shared-path Pre/Post selamat saat 1 sisi dihapus; tanpa exclusion-set | SATISFIED | Integration test `SharedPrePostPath_Survives_WhenOneSideDeleted` PASS; 0x exclusion-set di controller |
| SC4-test-uat | Plan 03 | build 0 error + dotnet test hijau + UAT @5277 | SATISFIED | build 0 error verified; full suite 229/229 per SUMMARY-03; UAT SC#2 browser-approved per SUMMARY-03 |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Tidak ada | — | Tidak ditemukan anti-pattern blocker atau warning | — | — |

Catatan: `return null`/`return {}` tidak ditemukan di path baru. Helper dan controller memakai alur async penuh. Folder kosong yang tersisa setelah delete file = expected behavior, bukan defect (CONTEXT Deferred Area 4).

---

### Human Verification Required

Tidak ada item yang memerlukan verifikasi human lebih lanjut. UAT SC#2 sudah di-approve (DeleteAssessmentGroup file fisik terhapus + DB cascade bersih). SC#3 (shared Pre/Post selamat) sudah terbukti via integration test real-SQL 2/2 PASS. SC#1 (perilaku identik) terbukti via full suite 229/229 + grep checks.

---

### Gaps Summary

Tidak ada gap. Semua 4 Success Criteria terpenuhi:

- **SC#1** — helper static `ImageFileCleanup.DeleteUnreferencedAsync` diekstrak, 3 call-site lama beralih ke helper (0x inline predikat tersisa, 3x call helper baru di L5799/6750/6841).
- **SC#2** — 3 method cascade (DeleteAssessment/DeleteAssessmentGroup/DeletePrePostGroup) mengikuti pola atomic Phase 333: collect SEBELUM RemoveRange, helper SETELAH CommitAsync; total 6x helper call terkonfirmasi.
- **SC#3** — shared-path Pre/Post dijamin oleh post-commit AnyAsync tanpa exclusion-set; terbukti via integration test [Fact]2 real-SQL.
- **SC#4** — `dotnet build` 0 error; full suite 229/229 passed; UAT browser SC#2 approved (DeleteAssessmentGroup file fisik terhapus).

Deviasi dari plan: 1 (auto-fixed selama eksekusi — FK seed user di integration test, ditangkap build-time, tidak mempengaruhi perilaku produksi).

---

_Verified: 2026-06-12T08:30:00Z_
_Verifier: Claude (gsd-verifier)_
