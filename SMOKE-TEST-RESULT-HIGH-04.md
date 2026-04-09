# Smoke Test Result — HIGH-04 Import Reactivation ProtonTrackAssignment

**Tanggal:** 2026-04-09
**Branch:** main (after fix HIGH-04)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277 (HC login — `meylisa.tjiang@pertamina.com`)
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev` (Windows Auth)

**Scope:** Verifikasi `ImportCoachCoacheeMapping` sekarang ikut mereaktivasi `ProtonTrackAssignment` terakhir milik coachee ketika mapping direaktivasi via Excel import (sebelumnya hanya `CoachCoacheeMapping.IsActive=true`, assignment dibiarkan inactive → CoachingProton view kosong).

---

## Fix Summary

| File | Change |
|------|--------|
| `Controllers/CoachMappingController.cs` `ImportCoachCoacheeMapping` (blok transaction line 369-411) | Di dalam transaction block yang sudah ada (D-13), setelah `AddRange(newMappings)`, tambahkan loop: untuk setiap `reactivatedMappings`, query `ProtonTrackAssignments` terakhir per coachee (`GroupBy CoacheeId` → `OrderByDescending(Id).First()`). Jika `!IsActive` → set `IsActive=true`, `DeactivatedAt=null`, increment `reactivatedAssignmentCount`. Counter dipromosikan ke scope method agar dapat dipakai di audit log setelah transaction commit. |
| `Controllers/CoachMappingController.cs` audit log description | `$"Import {successCount} mapping baru, {reactivatedCount} diaktifkan kembali ({reactivatedAssignmentCount} ProtonTrackAssignment ikut direaktivasi), {skipCount} dilewati, {errorCount} error"` |
| `BUG-HUNT-REPORT-PROTON-COACHING.md` | HIGH-04 ditandai ✅ FIXED 2026-04-09 |

**Policy choice:** reactivate **only last** (highest Id) assignment per coachee, not create new. Jika user butuh track berbeda, mereka bisa gunakan `CoachCoacheeMappingAssign` endpoint dengan `ProtonTrackId` eksplisit. Ini aman karena import path tidak tahu track yang diinginkan.

Build: `dotnet build` → **0 errors, 0 warnings**.

---

## Setup

- HC Admin: `meylisa.tjiang@pertamina.com`
- Coach: Rustam Santiko (`6821c3d9-0c3e-4352-a91e-7728d6c9e4f9`) — NIP sementara `RUSTAM01` (diset via sqlcmd karena Rustam tidak punya NIP di DB, direvert ke NULL saat cleanup)
- Coachee: Iwan (`66227777-1974-43ca-8bdd-e5586fa4a5b8`) — NIP `123456`
- Pre-seed state:
  - `CoachCoacheeMappings`: 1 inactive mapping Rustam→Iwan (StartDate 2026-01-01, EndDate 2026-03-01, Section GAST, Unit Alkylation Unit 065)
  - `ProtonTrackAssignments` Iwan: Id=3 (track 4, IsActive=0), Id=4 (track 5, IsActive=0) — keduanya inactive, Id=4 adalah "last"
- File Excel: `_tmp_import_high04.xlsx` (dibuat via openpyxl dengan header `NIP Coach`/`NIP Coachee`, row: `RUSTAM01` / `123456`)
- Upload jalur: Playwright `page.evaluate` → dispatch change event pada `#importMappingFile` → submit form container via `fetch(form.action, {method:'POST', body: new FormData(form)})` → antiforgery token otomatis ikut karena ada di form.

---

## Scenario A — Reactivate mapping + last assignment

**Action:** POST `/Admin/ImportCoachCoacheeMapping` dengan file xlsx 1 baris.

**Response:** 200, redirect ke `/Admin/CoachCoacheeMapping`.

**Verifikasi DB:**
| Check | Expected | Actual |
|---|---|---|
| `CoachCoacheeMappings` Rustam→Iwan `IsActive` | 1 | 1 ✅ (mapping Id=11, StartDate diupdate ke 2026-04-09) |
| `ProtonTrackAssignments` Id=4 (track 5, last) `IsActive` | 1 | 1 ✅ (direaktivasi) |
| `ProtonTrackAssignments` Id=3 (track 4, older) `IsActive` | 0 | 0 ✅ (tidak disentuh — policy "only last") |
| `AuditLogs` entry `ImportCoachCoacheeMapping` description | mention `1 ProtonTrackAssignment ikut direaktivasi` | `Import 0 mapping baru, 1 diaktifkan kembali (1 ProtonTrackAssignment ikut direaktivasi), 0 dilewati, 0 error` ✅ |

**Result: ✅ PASS**

---

## Scenario B — Transaction atomicity (code review)

Tidak dijalankan runtime — repro sulit. Code review:
- Blok reactivation berada **di dalam** transaction existing `using var transaction = await _context.Database.BeginTransactionAsync();` (line 370).
- Jika exception terjadi saat query/update ProtonTrackAssignments, `catch` block memanggil `await transaction.RollbackAsync()` → baik mapping reactivation maupun assignment reactivation di-rollback bersama.
- `reactivatedAssignmentCount` dideklarasi di scope method (bukan try block) supaya tetap bisa diakses audit log setelah commit. Nilai awal 0; hanya terisi jika transaction commit sukses dan iterasi loop mengeksekusi update.

**Result: ✅ PASS by code review**

---

## Scenario C — Coachee tanpa assignment sama sekali (code review)

Jika coachee yang direaktivasi tidak punya `ProtonTrackAssignment` historis (mis. mapping lama dibuat via Assign endpoint tanpa `ProtonTrackId`), query `lastAssignments` akan mengembalikan list kosong untuk coachee tsb → loop tidak menyentuh apapun → `reactivatedAssignmentCount` tidak bertambah. Mapping tetap direaktivasi (sesuai perilaku lama), tanpa regresi.

**Result: ✅ PASS by code review**

---

## Cleanup

- `DELETE CoachCoacheeMappings WHERE CoachId=Rustam AND CoacheeId=Iwan` (1 baris, mapping Id=11 yang baru dibuat seeded).
- `UPDATE ProtonTrackAssignments SET IsActive=0 WHERE Id IN (3,4)` — revert ke state pre-test.
- `UPDATE Users SET NIP=NULL WHERE Id=Rustam` — hapus NIP temporary.
- File `_tmp_import_high04.xlsx` dihapus.
- Verifikasi akhir: `final_map=0`, `final_asg_active=0`, `rustam_nip=NULL` ✅.

AuditLog entry dari test dibiarkan (tidak mengganggu integritas).

---

## Verdict

**HIGH-04 FIXED ✅** — Import Excel flow kini menutup gap yang di-flag bug hunt:
1. Mapping reactivation tetap berjalan seperti semula (backward compatible).
2. Ditambah: reaktivasi `ProtonTrackAssignment` terakhir per coachee → CoachingProton view level ≤3 akan kembali memunculkan coachee (scope filter `ProtonTrackAssignments.IsActive` kini terpenuhi).
3. Hanya reactivate assignment **terakhir** (OrderByDescending Id) — tidak membuat assignment baru, tidak menyentuh track lain. Konservatif & deterministik.
4. Semua berada dalam transaction existing (D-13) → rollback atomic jika ada exception.
5. Audit log diperkaya dengan count `ProtonTrackAssignment ikut direaktivasi` untuk traceability.
