---
phase: 426-audit-log-editorganizationunit
reviewed: 2026-06-24T13:12:28Z
depth: standard
files_reviewed: 2
files_reviewed_list:
  - Controllers/OrganizationController.cs
  - HcPortal.Tests/OrganizationControllerTests.cs
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 426: Code Review Report

**Reviewed:** 2026-06-24T13:12:28Z
**Depth:** standard
**Files Reviewed:** 2
**Status:** issues_found (hanya Info — tidak ada Critical/Warning)

## Summary

Review difokuskan pada DIFF Phase 426: satu blok audit-log aditif yang disisipkan ke `EditOrganizationUnit` (setelah `await tx.CommitAsync();`), plus 5 xUnit baru (T1-T5) dan factory `MakeControllerWithUser`/`FakeUserStore`/`MakeUserManager` di file test.

Penilaian tingkat tinggi: **blok audit benar dan aman.** Tiga keputusan terkunci diverifikasi terpenuhi:
- **D-01 (only-on-change):** gate `oldName != name.Trim() || oldParentId != parentId` adalah union yang tepat dari predikat cascade rename (L203 `oldName != name.Trim()`) dan reparent (L247 `oldParentId != parentId`). Pada titik gate, mutasi `unit.Name`/`unit.ParentId` sudah terjadi, namun variabel `oldName`/`oldParentId` di-snapshot di L139-140 sebelum mutasi, sehingga perbandingan tetap benar. Diverifikasi oleh T4 (no-change → 0 baris).
- **D-02 (single combined row):** satu pemanggilan `LogAsync` per edit, mencakup rename+reparent sekaligus. Diverifikasi oleh T3.
- **D-03 (raw parent IDs):** deskripsi memuat `parent {old}→{new}` tanpa query DB tambahan; `null` direpresentasikan sebagai literal "null". Diverifikasi oleh T2.

Pola `try/catch {}` swallow-on-failure dan penempatan setelah commit memastikan kegagalan audit tidak memblokir respons edit (T5 PASS: `_userManager`=null → NRE di-swallow → edit tetap sukses). Mirror dari `DeleteOrganizationUnit` (L558-568) konsisten.

Build/test tidak dijalankan dalam review ini (read-only), namun factory `MakeUserManager` disalin verbatim dari `RetakeExamEndpointTests.cs` (signature & `UpperInvariantLookupNormalizer` terkonfirmasi cocok) dan signature `AuditLogService.LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` cocok dengan call-site.

Tidak ada isu korektnes maupun keamanan. Tiga catatan Info di bawah bersifat dokumentatif/maintainability dan tidak memerlukan tindakan blocking.

## Info

### IN-01: Audit-write berada di LUAR transaksi cascade (beda pola dari WorkerController)

**File:** `Controllers/OrganizationController.cs:308-327`
**Issue:** Blok audit dijalankan SETELAH `await tx.CommitAsync()` (L308) sementara `using var tx` (L182) masih dalam scope (baru di-dispose di akhir method). `AuditLogService.LogAsync` memanggil `_context.SaveChangesAsync()` sendiri (AuditLogService.cs:41). Karena transaksi sudah committed, `SaveChangesAsync` ini menulis baris audit di luar transaksi cascade (transaksi implisit per-statement). Ini BERBEDA dari pola mapan di `WorkerController.DeleteWorker` (L911) yang memanggil `LogAsync` INSIDE transaksi sebelum `CommitAsync` (audit jadi atomik dengan operasi domain).

Konsekuensi: bila penulisan audit gagal, perubahan unit TETAP ter-commit (audit hilang). Ini SESUAI keputusan terkunci (swallow-on-failure, "must not block the edit response") dan mirror `DeleteOrganizationUnit` yang juga audit di luar/ setelah operasi domain. Secara fungsional aman pada SQL Server — bukan bug. Dicatat hanya agar perbedaan pola lintas-controller terdokumentasi (jejak audit Edit bersifat best-effort, bukan transaksional).

**Fix:** Tidak ada perubahan diperlukan — perilaku sesuai desain D-01/swallow-on-failure. Jika di masa depan diinginkan audit transaksional, pindahkan blok audit ke sebelum `tx.CommitAsync()` (meniru WorkerController). Pertimbangkan menambahkan komentar singkat di L310 yang menyatakan "audit best-effort, di luar tx cascade (mirror DeleteOrganizationUnit)" agar pembaca tidak salah mengira ia atomik.

### IN-02: `catch {}` menelan SEMUA exception tanpa logging diagnostik

**File:** `Controllers/OrganizationController.cs:326`
**Issue:** `catch { /* audit log failure tidak block response */ }` menangkap seluruh tipe exception dan tidak menulis apa pun ke logger. Pola identik dipakai di `DeleteOrganizationUnit:568` (konsisten dengan precedent). Namun `WorkerController` (L919) menggunakan `catch (Exception ex) { _logger.LogWarning(ex, ...); }` yang lebih informatif — kegagalan audit yang berulang (mis. skema AuditLogs rusak) jadi tak terlihat sama sekali di Organization controller.

**Fix:** Pertimbangkan menyelaraskan dengan pola WorkerController bila `ILogger` tersedia di `AdminBaseController`:
```csharp
catch (Exception ex) { /* logger?.LogWarning(ex, "Audit gagal EditOrganizationUnit id={Id}", unit.Id); */ }
```
Non-blocking; konsistensi dengan `DeleteOrganizationUnit` saat ini lebih diprioritaskan, jadi penyelarasan keduanya sekaligus lebih baik daripada hanya satu.

### IN-03: `name.Trim()` dievaluasi berulang di blok audit (duplikasi minor)

**File:** `Controllers/OrganizationController.cs:312,321`
**Issue:** `name.Trim()` dipanggil ulang di gate audit (L312) dan di interpolasi deskripsi (L321), selain pemanggilan sebelumnya di L203/L306. Tidak ada bug — `unit.Name` sudah di-set ke `name.Trim()` di L306 sebelum blok audit, sehingga `name.Trim()` di L312/L321 dijamin sama dengan `unit.Name`. Ini murni duplikasi micro yang mengurangi keterbacaan, bukan masalah korektnes.

**Fix:** Opsional — gunakan `unit.Name` (sudah = `name.Trim()`) di blok audit untuk satu sumber kebenaran:
```csharp
if (oldName != unit.Name || oldParentId != parentId)
{
    ...
    $"Edited organization unit '{oldName}'→'{unit.Name}' [ID={unit.Id}] " +
    ...
}
```
Bersifat kosmetik; perilaku saat ini sudah benar.

---

_Reviewed: 2026-06-24T13:12:28Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
