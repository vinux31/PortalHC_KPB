# Smoke Test Result — HIGH-02 CoachCoacheeMappingAssign Transactional Wrap

**Tanggal:** 2026-04-09
**Branch:** main (after fix HIGH-02)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277 (HC login — `meylisa.tjiang@pertamina.com` / `123456`)
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev` (Windows Auth, `-C -I`)

**Scope:** Verifikasi endpoint `POST /Admin/CoachCoacheeMappingAssign` kini dibungkus `BeginTransactionAsync` — happy path tidak regress, error handler memastikan rollback atomic + response deterministik, dan audit log/notification tetap jalan di luar transaction.

---

## Fix Summary

| File | Change |
|------|--------|
| `Controllers/CoachMappingController.cs` `CoachCoacheeMappingAssign` (line 515-584) | Blok `AddRange(newMappings)` + ProtonTrack side-effect loop (dengan intermediate `SaveChangesAsync` untuk flush assignment Id) + final `SaveChangesAsync` dibungkus `await using var tx = await _context.Database.BeginTransactionAsync()` → `try { ... await tx.CommitAsync(); } catch (Exception ex) { _logger.LogError(...); await tx.RollbackAsync(); return Json({success=false, message="Gagal menyimpan assignment. Operasi dibatalkan."}); }`. Audit log + notification dispatch tetap di luar tx (best-effort side-effect). |
| `BUG-HUNT-REPORT-PROTON-COACHING.md` | HIGH-02 ditandai `✅ FIXED 2026-04-09` |

Build: `dotnet build` → **0 errors, 0 warnings**.

Static grep:
- `BeginTransactionAsync` match baru di `CoachMappingController.cs:515`.
- `"CoachCoacheeMappingAssign failed"` match baru di `CoachMappingController.cs:580`.

---

## Setup

- HC Admin: `meylisa.tjiang@pertamina.com` (untuk otorisasi endpoint Admin/HC)
- Coach target: Rustam Santiko (`6821c3d9-0c3e-4352-a91e-7728d6c9e4f9`)
- Coachee 1: Iwan (`66227777-1974-43ca-8bdd-e5586fa4a5b8`)
- Coachee 2: Moch Regan Sabela Widyadhana (`7e6a5798-1e24-47f2-ba1f-c10d81a832ea`)
- ProtonTrack: Id=4 (Operator - Tahun 1; tidak ada prev track → bypass D-09 progression warning)
- Section/Unit: `GAST` / `Alkylation Unit (065)` (validasi `GetSectionUnitsDictAsync` pass)
- Baseline pre-test: 1 mapping existing untuk Rustam, 0 active `ProtonTrackAssignment` untuk kedua coachee pada track 4.

Request dikirim via Playwright `page.evaluate(fetch)` memakai session cookie HC + antiforgery token yang diambil dari DOM `/Admin/CoachCoacheeMapping`.

---

## Scenario A — Happy path (2 coachee + ProtonTrack) → commit transaction

**Request:**
```json
POST /Admin/CoachCoacheeMappingAssign
{
  "CoachId": "6821c3d9-0c3e-4352-a91e-7728d6c9e4f9",
  "CoacheeIds": ["66227777-1974-43ca-8bdd-e5586fa4a5b8", "7e6a5798-1e24-47f2-ba1f-c10d81a832ea"],
  "StartDate": "2026-04-09",
  "AssignmentSection": "GAST",
  "AssignmentUnit": "Alkylation Unit (065)",
  "ProtonTrackId": 4
}
```

**Response:** `200 {"success":true,"message":"2 mapping berhasil dibuat."}`

**Verifikasi DB:**
| Check | Expected | Actual |
|---|---|---|
| `CoachCoacheeMappings` baru untuk kedua coachee (IsActive=1) | 2 | 2 ✅ |
| `ProtonTrackAssignments` track=4 IsActive=1 untuk kedua coachee | 2 | 2 ✅ |
| `ProtonDeliverableProgresses` untuk Iwan | > 0 | 3 ✅ (reused dari inactive assignment Id=3 yang direaktivasi — path `existing != null` di line 545) |
| `ProtonDeliverableProgresses` untuk Moch Regan | > 0 | 3 ✅ (path `else` → new assignment + `AutoCreateProgressForAssignment` menulis progress rows; memakai `SaveChangesAsync` intermediate line 563 yang kini hidup di dalam transaction) |
| `AuditLogs` entry `Assign` dalam 5 menit terakhir | 1 | 1 ✅ |

**Catatan penting:** Scenario A memverifikasi bahwa intermediate `SaveChangesAsync` (line 563) yang flush assignment Id **tetap bekerja di dalam transaction**. EF Core mengizinkan multiple SaveChanges per transaction; ini pola identik yang sudah diterapkan di fix CRIT-02 (`CoachCoacheeMappingEdit`). Progress rows Regan (path create-new) + assignment reuse Iwan (path reactivate) — keduanya commit bersama.

**Result: ✅ PASS** (no regression)

---

## Scenario D — Invalid input (CoacheeIds kosong) → guard sebelum transaction

**Request:** sama seperti A tapi `"CoacheeIds": []`.

**Response:** `200 {"success":false,"message":"Data tidak lengkap."}`

**Verifikasi:**
- Guard `req == null || req.CoacheeIds == null || !req.CoacheeIds.Any()` di awal method (sebelum blok transaction line 515) → return early tanpa masuk transaction scope.
- Tidak ada mutasi DB (tidak perlu verifikasi count — tidak ada path yang mengubah state).

**Result: ✅ PASS** (guard path tidak tersentuh oleh fix; early-return sebelum `BeginTransactionAsync`)

---

## Scenario B — Simulasi failure mid-flow (rollback atomic)

**Status: Code review only** (tidak dijalankan runtime — sesuai plan "skip kalau repro sulit").

**Analisis kode:**
1. Intermediate `SaveChangesAsync` di line 563 menulis `ProtonTrackAssignments` (INSERT) → row terikat pada transaction `tx` karena `BeginTransactionAsync` dipanggil sebelum AddRange.
2. `AutoCreateProgressForAssignment(...)` (line 564) menulis `ProtonDeliverableProgress` rows → juga terikat transaction.
3. Jika iterasi kedua dari loop `foreach (var coacheeId in req.CoacheeIds)` melempar exception (mis. `AutoCreateProgressForAssignment` gagal karena kompetensi tidak lengkap):
   - Control flow jatuh ke `catch (Exception ex)` di line 578.
   - `_logger.LogError(ex, "CoachCoacheeMappingAssign failed (CoachId={CoachId}, Coachees={Count})", ...)` tercatat.
   - `await tx.RollbackAsync()` dipanggil → **semua** perubahan dalam transaction (mapping baru, deactivate existing track, assignment baru coachee 1 yang sudah ter-flush via intermediate SaveChanges, progress rows coachee 1 yang sudah terisi) di-rollback.
   - Response: `{success:false, message:"Gagal menyimpan assignment. Operasi dibatalkan."}`
4. Audit log + notification dispatch (line 586-604) tidak tereksekusi karena `return` di catch block mengakhiri method.

**Kontras dengan perilaku sebelum fix:** tanpa transaction, step 3 akan meninggalkan mapping coachee 1 + assignment + sebagian progress coachee 1 di DB (sudah di-flush oleh `SaveChangesAsync` intermediate), sementara coachee 2 tidak punya apa-apa — state campur, butuh SQL cleanup manual.

**Result: ✅ PASS by code review** (pola identik dengan CRIT-02 fix yang sudah diverifikasi runtime)

---

## Scenario C — Audit log & notification tetap jalan on success

Tercakup oleh Scenario A:
- `AuditLogs` entry `Assign` tercatat 1 baris dalam 5 menit terakhir ✅.
- Notification dispatch terjadi setelah `return Json(...)` sukses (dalam try/catch sendiri, tidak blok response) — tidak diverifikasi langsung di tabel `UserNotifications` karena path notification tidak dimodifikasi oleh fix ini dan sudah aman di luar transaction.

**Result: ✅ PASS**

---

## Cleanup

Dilakukan via sqlcmd setelah Scenario A:
- `DELETE ProtonDeliverableProgresses` untuk assignment yang dibuat dalam 10 menit terakhir (hanya progress Regan — 3 baris).
- `DELETE ProtonTrackAssignments` yang `AssignedAt > -10min` (hanya assignment baru Regan).
- `DELETE CoachCoacheeMappings` untuk Rustam→(Iwan, Regan) dengan `StartDate='2026-04-09'` (2 baris).
- `UPDATE ProtonTrackAssignments SET IsActive=0 WHERE Id=3` (Iwan assignment Id=3 direaktivasi oleh reuse path — dikembalikan ke state awal IsActive=0).

Verifikasi akhir:
- `remaining_mapping` untuk Rustam = 1 (baseline restored).
- `remaining_assignments` track=4 Iwan/Regan = 1 (hanya Id=3 Iwan, IsActive=0 seperti awal).
- `remaining_progress_iwan3` = 3 (pre-existing progress rows yang dibuat saat seeding awal, tidak disentuh).
- `remaining_progress_regan` = 0 (bersih).

Audit log test entry tidak dihapus (tidak mengganggu integritas).

---

## Verdict

**HIGH-02 FIXED ✅** — `CoachCoacheeMappingAssign` sekarang transactional:
1. Happy path (Scenario A) tidak regress; batch insert mapping + reuse/create ProtonTrackAssignment + AutoCreateProgressForAssignment semua commit atomik.
2. Intermediate `SaveChangesAsync` tetap valid di dalam transaction EF Core.
3. Guard path (Scenario D) tidak tersentuh — early return sebelum transaction scope.
4. Rollback pattern (Scenario B) konsisten dengan fix CRIT-02/HIGH-03 yang sudah diverifikasi, dengan error log terstruktur `"CoachCoacheeMappingAssign failed (CoachId=..., Coachees=...)"` untuk diagnosa.
5. Audit log + notification tetap di luar transaction (best-effort, tidak memaksa rollback jika gagal).
