---
phase: 411-remove-restore-backend-live
reviewed: 2026-06-21T06:39:16Z
depth: deep
files_reviewed: 3
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/EditAssessment.cshtml
  - HcPortal.Tests/FlexibleParticipantRemoveTests.cs
findings:
  critical: 0
  warning: 3
  info: 4
  total: 7
status: issues_found
---

# Phase 411: Code Review Report

**Reviewed:** 2026-06-21T06:39:16Z
**Depth:** deep
**Files Reviewed:** 3
**Status:** issues_found

## Summary

Phase 411 menambahkan backend remove/restore peserta live: satu private core `RemoveParticipantCoreAsync` (hybrid hard/soft + Pre/Post pair-as-unit via `LinkedSessionId`) dibungkus 3 endpoint (`RemoveParticipantLive` JSON, `RestoreParticipantLive` JSON, `DeleteAssessmentPeserta` redirect), plus un-hide form delete-peserta di `EditAssessment.cshtml`, plus 14 test de-tautology (read-path InMemory + write-path SQLEXPRESS dengan mini-DI cascade nyata).

**Keamanan: SOLID.** Ketiga endpoint punya `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (load-bearing — tak ada global antiforgery/fallback-policy di Program.cs). Proton reject sebelum write. Bind hanya skalar primitif (no mass-assignment). Hybrid hard/soft 100% server-side dari kolom DB. Soft-remove benar-benar TIDAK menyentuh `Score`/`IsPassed`/`NomorSertifikat`/`ManualSertifikatUrl`/`Status`/`response` (Pitfall 2 terverifikasi via test B1/B2). Cascade D-01 terverifikasi: `RecordCascadeDeleteService:221-222` memang `RemoveRange` UPA, dan service Scoped berbagi `ApplicationDbContext` Scoped yang sama dengan controller dalam satu request — jadi konsistensi tracking benar. **0 Critical.**

3 Warning bersifat correctness/non-repudiation pada edge-case (audit non-atomik vs pola 410, overwrite metadata partner yang sudah-removed, hard-delete pair tidak atomik lintas-sesi). 4 Info bersifat spec-literal/coverage/konsistensi tampilan. Tidak ada yang memblokir; semua punya jalur perbaikan jelas.

Build `HcPortal.csproj` + `HcPortal.Tests.csproj` sukses 0 error.

## Warnings

### WR-01: Audit soft-remove tidak atomik dengan mutasi (menyimpang dari pola 410 yang ditiru)

**File:** `Controllers/AssessmentAdminController.cs:2697-2701`
**Issue:** Pada jalur SOFT, mutasi 3 kolom di-commit dulu (`await _context.SaveChangesAsync()` :2697), BARU audit ditulis terpisah (`_auditLog.LogAsync` :2699, yang `SaveChangesAsync` internal lagi). Tak ada transaksi pembungkus. Bila `LogAsync` gagal, soft-remove sudah ter-commit tapi audit row hilang — endpoint lempar 500 SETELAH penghapusan terjadi, sehingga operator mengira penghapusan gagal padahal sukses tanpa jejak. Ini melanggar klaim non-repudiation T-411-06 + must-have "Setiap remove menulis audit row" pada edge-case. **Pola 410 `AddParticipantsLive:2455-2459` justru menulis audit DALAM `BeginTransactionAsync`/`CommitAsync` — 411 seharusnya cermin pola itu** tapi tidak.
**Fix:** Bungkus mutasi soft + audit dalam satu transaksi eksplisit, audit sebelum commit:
```csharp
using var tx = await _context.Database.BeginTransactionAsync();
foreach (var s in new[] { session, partner }.Where(x => x != null && x.RemovedAt == null))
{
    s!.RemovedAt = DateTime.UtcNow; s.RemovedBy = actorId; s.RemovalReason = reason;
}
await _context.SaveChangesAsync();
await _auditLog.LogAsync(actorId, actorName, "RemoveParticipantLive",
    $"Removed participant session [ID={sessionId}] '{title}' mode=soft reason='{reason}'",
    sessionId, "AssessmentSession");
await tx.CommitAsync();
```
(Catatan: `LogAsync` memanggil `SaveChangesAsync` internal — di dalam `BeginTransactionAsync` flush itu tetap belum commit hingga `tx.CommitAsync()`, jadi atomik. Lihat preseden A4 di RecordCascadeDeleteService:252-253.)

### WR-02: Soft loop menimpa metadata removal partner yang SUDAH soft-removed (asimetri dengan restore loop)

**File:** `Controllers/AssessmentAdminController.cs:2691`
**Issue:** Loop soft-remove pakai `.Where(x => x != null)` tanpa cek `RemovedAt`. Bila pasangan Pre/Post berada di state-drift (partner sudah `RemovedAt != null`, session aktif), memanggil remove pada session akan **menimpa `RemovedAt`/`RemovedBy`/`RemovalReason` partner** dengan timestamp/aktor/alasan baru — menghapus konteks audit asli (kapan & kenapa partner dihapus pertama kali). Loop restore di :2600 sudah BENAR menggunakan guard `x.RemovedAt != null`; loop soft inkonsisten dengan itu.
**Fix:** Samakan dengan restore loop — hanya tulis ke yang belum-removed:
```csharp
foreach (var s in new[] { session, partner }.Where(x => x != null && x.RemovedAt == null))
```
Idempotency wrapper hanya menjaga `session` (bukan partner), jadi guard per-elemen ini perlu untuk integritas metadata partner. Likelihood rendah (pair normalnya selalu dihapus bersama) → Warning, bukan Critical.

### WR-03: Hard-delete pasangan Pre/Post tidak atomik lintas dua sesi (orphan separuh-pair bila cascade ke-2 gagal)

**File:** `Controllers/AssessmentAdminController.cs:2709-2714`
**Issue:** Jalur HARD me-loop `cascade.ExecuteAsync` per sesi. Tiap cascade atomik 1-tx secara individual, TAPI loop dua-cascade tidak atomik sebagai unit. Bila cascade `session` sukses+commit lalu cascade `partner` gagal, method return `Fail` namun `session` SUDAH terhapus permanen — pasangan tertinggal separuh (partner yatim, `LinkedSessionId` sudah ter-null-clear oleh delta #8 cascade :236-237). Tak ada kompensasi rollback. User lihat error padahal satu sesi sudah hilang.
**Fix:** Likelihood rendah (cascade jarang gagal di tengah pair) tapi konsekuensinya data inkonsisten. Opsi pragmatis: (a) dokumentasikan + accept sebagai risiko residual di threat model (single-tx-per-cascade adalah desain `RecordCascadeDeleteService`, nested-tx pada ctx berbagi akan konflik), ATAU (b) pre-validasi kedua sesi bersih SEBELUM mulai cascade apa pun (sudah dilakukan via `anyHasData==false`), dan bila cascade pertama sukses tapi kedua gagal, log error eksplisit "PAIR-HALF-DELETED sessionId={x} partnerId={y}" agar terdeteksi reconciliation. Minimal tambahkan log peringatan saat partner cascade gagal pasca session sukses:
```csharp
if (!result.Success)
{
    _logger.LogError("Hard-delete pair gagal sebagian: session={SessionId} sudah-terhapus, partner={PartnerId} gagal cascade",
        sessionId, s.Id);
    return RemoveOutcome.Fail(result.ErrorMessage ?? "Gagal menghapus peserta.");
}
```

## Info

### IN-01: `RestoreParticipantLive` tak punya Proton-reject eksplisit (spec-literal deviation)

**File:** `Controllers/AssessmentAdminController.cs:2585-2590`
**Issue:** Plan must-have menyebut "Sesi Proton ditolak 400 di semua endpoint remove/restore/DeletePeserta", namun `RestoreParticipantLive` hanya cek `RemovedAt == null`. Secara keamanan AMAN — sesi Proton tak pernah bisa soft-removed (RemoveParticipantLive reject Proton sebelum write), sehingga sesi Proton selalu `RemovedAt == null` dan restore return 400 "Sesi ini tidak dalam keadaan dihapus." Outcome (no Proton mutation) terpenuhi, hanya pesan errornya bukan "Proton tidak didukung".
**Fix:** Opsional untuk konsistensi pesan: tambah `if (session.Category == "Assessment Proton") return BadRequest(new { error = "Penghapusan/pemulihan tidak didukung untuk Assessment Proton." });` sebelum cek `RemovedAt`. Tidak wajib (tak ada celah keamanan).

### IN-02: Peserta soft-removed masih tampil di daftar peserta `EditAssessment` (inkonsisten PLIV-01)

**File:** `Controllers/AssessmentAdminController.cs:1687-1690, 1729-1734`
**Issue:** Query `assignedUsers` di GET `EditAssessment` (cabang Pre/Post + standalone) TIDAK filter `RemovedAt == null`, padahal PLIV-01 (409) mengexclude soft-removed di query monitoring lain (lihat :121, :3264, :3774). Jadi peserta yang sudah soft-removed tetap muncul di tabel peserta EditAssessment dengan Status aslinya. Karena 411 meng-un-hide tombol Hapus, tombol bisa muncul untuk peserta soft-removed (bila Status bukan InProgress/Completed). Klik → `DeleteAssessmentPeserta` → idempotency `RemovedAt != null` → "Peserta sudah dikeluarkan." Jadi tidak berbahaya (idempotent menangkap), tapi tampilan tak konsisten. Pre-existing di view-layer, bukan diintroduksi 411 — namun un-hide form memperjelasnya.
**Fix:** Pertimbangkan menambah `&& a.RemovedAt == null` pada kedua query atau menampilkan badge "Dikeluarkan" — kemungkinan masuk scope UI Phase 412 (panel "Peserta Dikeluarkan"). Cukup catat untuk 412.

### IN-03: Test coverage — restore Pre/Post pair (clear KEDUA partner) belum ada

**File:** `HcPortal.Tests/FlexibleParticipantRemoveTests.cs:552-595`
**Issue:** B5 menguji restore single-session clear-3-kolom, dan B6/C3 menguji remove Pre/Post pair. Namun tak ada test yang meng-assert `RestoreParticipantLive` pada satu sesi Pre/Post juga meng-clear partner-nya (simetri PRMV-05 sisi restore — loop :2600). Klaim "restore clears all 3 cols on both partners" tak ter-cover.
**Fix:** Tambah test: soft-remove pair (B6 setup) → `RestoreParticipantLive(preId)` → assert KEDUA `preId` dan `postId` punya `RemovedAt/RemovedBy/RemovalReason == null`. Masuk Plan 411-02 atau Phase 413.

### IN-04: Duplikasi 3-baris resolve-actor di 3 endpoint

**File:** `Controllers/AssessmentAdminController.cs:2569-2571, 2592-2594, 2630-2632`
**Issue:** Blok resolve actor (`GetUserAsync` + `actorId` + `actorName` format NIP-FullName) di-copy verbatim 3 kali (juga di AddParticipantsLive :2395-2397, total 4 lokasi). Plan memang menginstruksikan "verbatim 3 baris", jadi ini disengaja untuk konsistensi, tapi kandidat ekstraksi helper `(string actorId, string actorName) ResolveActorAsync()`.
**Fix:** Opsional refactor jadi private helper bila menyentuh area ini lagi. Tidak mendesak (pola konsisten + terbukti).

---

_Reviewed: 2026-06-21T06:39:16Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: deep_
