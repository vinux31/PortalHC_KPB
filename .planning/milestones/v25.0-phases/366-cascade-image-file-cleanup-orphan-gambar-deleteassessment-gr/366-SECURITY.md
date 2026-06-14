---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
status: secured
asvs_level: 1
threats_total: 9
threats_closed: 9
threats_open: 0
audited: 2026-06-12
---

# SECURITY.md — Phase 366: Cascade Image File Cleanup (Orphan Gambar)

**Phase:** 366 — cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
**ASVS Level:** 1
**Threats Closed:** 9/9
**Audited:** 2026-06-12
**Auditor:** gsd-security-auditor

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-366-01 | Tampering | mitigate | CLOSED | `Helpers/ImageFileCleanup.cs:17-22` — parameter `IEnumerable<string> paths` hanya dari kolom DB ImagePath; `Path.Combine(webRootPath, relUrl.TrimStart('/')...)` confine di webroot; tidak ada path eksternal user |
| T-366-02 | Denial of Service | accept→mitigate | CLOSED | `Helpers/ImageFileCleanup.cs:30-38` — `try/catch (Exception fex)` warn-only per file, `logger.LogWarning`, tidak melempar, tidak rollback DB |
| T-366-03 | Information Disclosure | accept | CLOSED | Lihat Accepted Risks di bawah — path relatif `/uploads/...` di log warn non-sensitive, pola existing |
| T-366-04 | Tampering | mitigate | CLOSED | `AssessmentAdminController.cs:2319-2325`, `2514-2520`, `2705-2711` — `imagePaths` dikumpul dari `packages/allPackages` (kolom DB, di-Include); helper confine webroot; 0 user-supplied path |
| T-366-05 | Tampering | mitigate | CLOSED | `Helpers/ImageFileCleanup.cs:27-29` — `AnyAsync(x => x.ImagePath == relUrl)` Q + O post-commit; `HcPortal.Tests/ImageCleanupIntegrationTests.cs` Fact `SharedPrePostPath_Survives_WhenOneSideDeleted` (real-SQL) membuktikan shared-path SC#3 selamat; 0 `exclusionSet` di controller |
| T-366-06 | Denial of Service | mitigate | CLOSED | `AssessmentAdminController.cs:2342-2346`, `2539-2542`, `2727-2732` — helper dipanggil SETELAH `tx.CommitAsync()` di ketiga cascade; try/catch warn-only internal helper tidak rollback DB |
| T-366-07 | Tampering | accept | CLOSED | Lihat Accepted Risks di bawah — test pakai relUrl literal `/uploads/questions/test/*.jpg` controlled by test; `Path.Combine` confined di temp-dir |
| T-366-08 | Denial of Service | mitigate | CLOSED | `HcPortal.Tests/ImageCleanupIntegrationTests.cs:62-66` — `DisposeAsync` memanggil `EnsureDeletedAsync`; `InitializeAsync:54-59` catch mid-setup drop DB + throw `XunitException` |
| T-366-09 | Information Disclosure | mitigate | CLOSED | UAT @localhost:5277 atas DB lokal; SEED_WORKFLOW diterapkan (snapshot `C:\Temp\HcPortalDB_Dev_pre366uat_20260612.bak` + `docs/SEED_JOURNAL.md` cleaned — dikonfirmasi 366-03-SUMMARY.md) |

---

## Accepted Risks

| Threat ID | Category | Justification | Owner |
|-----------|----------|---------------|-------|
| T-366-03 | Information Disclosure | Path relatif `/uploads/questions/{id}/{file}` di `LogWarning` bukan data sensitif (tidak mengandung kredensial/PII/secret). Pola ini sudah ada di codebase sebelum Phase 366. Nilai informasi bagi penyerang sangat rendah: path uploads sudah diketahui dari HTML response publik. | Dev |
| T-366-07 | Tampering (test-only) | Test menggunakan relUrl literal yang dikontrol sepenuhnya oleh test harness, bukan input eksternal. `Path.Combine` confined di temp-dir. Scope: test-only, tidak ada production surface. | Dev |

---

## Key Mitigation Evidence Summary

### T-366-01 / T-366-04 — Path Traversal Prevention
- Helper `Helpers/ImageFileCleanup.cs:32`: `Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))`
- Input `paths` hanya dari kolom DB `ImagePath` (ditulis upload flow yang tervalidasi FileUploadHelper Phase 325)
- Tidak ada parameter path dari request user; 0 `..` injection surface

### T-366-02 / T-366-06 — DoS Prevention (warn-only, no abort)
- `Helpers/ImageFileCleanup.cs:30-38`: `try { File.Delete } catch (Exception fex) { logger.LogWarning ... }` — tidak melempar
- Helper dipanggil POST-CommitAsync di ketiga method cascade (baris L2342/L2539/L2727); kegagalan FS tidak rollback DB

### T-366-05 — Over-delete Prevention (shared Pre/Post)
- `Helpers/ImageFileCleanup.cs:27-29`: ref-count AnyAsync Q + AnyAsync O post-commit; `continue` jika masih direferensikan
- 0 `exclusionSet` di `AssessmentAdminController.cs` (D-05 murni post-commit AnyAsync)
- Integration test `SharedPrePostPath_Survives_WhenOneSideDeleted` (real-SQL) PASS (2026-06-12)

### T-366-08 — DB Leak Prevention (disposable test)
- `ImageCleanupFixture.DisposeAsync`: `EnsureDeletedAsync` selalu dipanggil (sukses/gagal)
- `InitializeAsync` catch: drop DB + XunitException; tidak pernah sentuh `HcPortalDB_Dev`

---

## Unregistered Flags

*Tidak ada flag tambahan dari ## Threat Flags di SUMMARY files yang tidak terpetakan ke threat register.*

(366-01-SUMMARY: tidak ada threat flags baru. 366-02-SUMMARY: tidak ada. 366-03-SUMMARY: deviation FK seed — test-only, bukan threat baru; auto-fixed.)

---

## Catatan Auditor

- Seluruh 6 call-site `ImageFileCleanup.DeleteUnreferencedAsync` di `AssessmentAdminController.cs` terverifikasi (3 Plan 01 + 3 Plan 02).
- Urutan atomic per method cascade (collect SEBELUM RemoveRange, helper SETELAH CommitAsync) terkonfirmasi di baris L2318-2346, L2513-2542, L2704-2732.
- 0 inline AnyAsync predikat tersisa di controller (semua dialihkan ke helper).
- Full test suite 229/229 passed (dikonfirmasi 366-03-SUMMARY); integration test 2/2 PASS real-SQL.
- UAT browser SC#2 approved (DeleteAssessmentGroup → file fisik terhapus, DB cascade bersih).
