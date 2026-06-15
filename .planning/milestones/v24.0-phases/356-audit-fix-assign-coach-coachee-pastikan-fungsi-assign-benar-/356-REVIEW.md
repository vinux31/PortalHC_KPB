---
phase: 356-audit-fix-assign-coach-coachee
reviewed: 2026-06-09T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Controllers/CoachMappingController.cs
  - Helpers/CoacheeEligibilityCalculator.cs
  - HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs
  - Views/Admin/CoachCoacheeMapping.cshtml
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: resolved
resolution: "WR-01 + WR-02 fixed in commit e672a110 (2026-06-09). 3 Info accepted (AF-7 First() = old-code parity; AF-1 N-query acceptable admin-tool; AF-4 deferred to backlog 999.5)."
---

# Phase 356: Code Review Report

> **Resolution (2026-06-09, commit `e672a110`):** WR-01 (AF-1 per-unit `myStatuses` scoping) and WR-02 (Edit `ex.Message` leak) both FIXED. Build 0 error + 135/135 tests green. 3 Info findings accepted as-is (AF-7 `First()` non-determinism = verified old-code parity; AF-1 N-query = acceptable for admin-tool dropdown; AF-4 ±5s window = intentionally deferred → backlog Phase 999.5).

**Reviewed:** 2026-06-09
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Review difokuskan HANYA pada perubahan Phase 356 (AF-1..AF-7 + AF-2 view), bukan kode pre-existing. Semua endpoint mempertahankan `[Authorize(Roles = "Admin, HC")]` / `[Authorize(Roles = "Admin")]` + `[ValidateAntiForgeryToken]`; anti-forgery token di view tetap utuh (`@Html.AntiForgeryToken()` + header `RequestVerificationToken` di semua fetch). Tidak ada migration baru — konstrain 0-migration terpenuhi. String user-facing seluruhnya Bahasa Indonesia.

Penilaian umum: kualitas tinggi. AF-6 menghindari info-leak dengan benar (raw `dbEx.Message` hanya ke logger), AF-3 transaction + cascade mengikuti pola FIX-01 yang konsisten, dan parity AF-7 (3 cabang) terverifikasi identik terhadap loop N+1 lama via git diff. Tidak ada temuan Critical.

Dua Warning bersifat correctness pada AF-1 (parity eligibility per-unit) dan satu inkonsistensi info-leak yang masih ada di endpoint Edit (di-flag karena AF-6 baru saja menetapkan standar "no raw message" — bukan kode 356 inti, tapi kontras tajam di file yang sama). Sisanya Info.

## Warnings

### WR-01: AF-1 — `myStatuses` dihitung lintas-unit, `expectedCount` per-unit → coachee multi-track-assignment bisa lolos/gagal salah

**File:** `Controllers/CoachMappingController.cs:1375-1414`
**Issue:** `progressRecords` di-batch-load hanya difilter `assignedCoacheeIds.Contains(p.CoacheeId) && trackDeliverableIds.Contains(p.ProtonDeliverableId)` (deliverable SEMUA unit di track). Lalu `myStatuses` per-coachee = SEMUA progress coachee untuk track ini, tanpa filter unit. Sementara `expectedCount` dihitung per-unit (`Unit.Trim() == resolvedUnit.Trim()`).

Asumsi parity-nya: progress coachee dibuat oleh `AutoCreateProgressForAssignment` yang HANYA membuat deliverable unit-nya, jadi `myStatuses.Count == expectedCount`. Asumsi ini benar untuk kasus single-assignment normal. TETAPI: jika seorang coachee punya >1 progress-set untuk track yang sama dari unit berbeda (mis. pindah unit lalu re-assign track yang sama tanpa cleanup, atau dua `ProtonTrackAssignment` untuk track sama di unit beda — perhatikan `GetEligibleCoachees` TIDAK memfilter `progressRecords` per-`ProtonTrackAssignmentId`), maka `myStatuses` akan berisi progress dari >1 unit sedangkan `expectedCount` hanya satu unit. Akibatnya `Count != expectedCount` → coachee yang sebenarnya sudah lulus unit-nya jadi TIDAK eligible (false-negative), atau sebaliknya bila status campuran.

Bandingkan dengan `AutoCreateProgressForAssignment` (L1429-1498) yang memang scoping per-assignment, sehingga eligibility check idealnya juga di-scope ke deliverable unit yang sama yang dipakai `expectedCount`.

**Fix:** Scope `myStatuses` ke deliverable unit coachee, sehingga sumbu perbandingan identik di dua sisi. Pre-load mapping `deliverableId → unit` sekali, lalu filter:
```csharp
// pre-load sekali (di luar foreach), unit per deliverable utk track ini:
var deliverableUnit = await _context.ProtonDeliverableList
    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId)
    .Select(d => new { d.Id, Unit = d.ProtonSubKompetensi!.ProtonKompetensi!.Unit })
    .ToDictionaryAsync(x => x.Id, x => (x.Unit ?? "").Trim());

// di dalam foreach:
var myStatuses = progressRecords
    .Where(p => p.CoacheeId == coacheeId
             && deliverableUnit.TryGetValue(p.ProtonDeliverableId, out var u)
             && u == resolvedUnit.Trim())
    .Select(p => p.Status)
    .ToList();
```
Jika tim sengaja mengandalkan invariant "1 coachee = 1 assignment aktif per track per unit" (yang memang ditegakkan oleh unique-index + alur assign), dokumentasikan invariant ini sebagai komentar di L1406-1410 agar reviewer berikutnya tidak menebak. Tanpa scope eksplisit, ini fragile terhadap data legacy/migrasi.

### WR-02: Info-leak `ex.Message` mentah di CoachCoacheeMappingEdit kontras dengan standar AF-6

**File:** `Controllers/CoachMappingController.cs:816`
**Issue:** AF-6 (L646-651) baru saja menetapkan standar Phase 356: "jangan expose `dbEx.Message` mentah; detail hanya ke logger." Namun di file yang sama, `CoachCoacheeMappingEdit` masih mengembalikan `$"Gagal menyimpan perubahan: {ex.Message}"` ke client. Ini membocorkan detail internal (nama tabel/kolom, constraint, potongan SQL) ke admin UI dan tidak konsisten dengan pesan ramah yang dipakai AF-3/AF-6 (`"Operasi gagal. Semua perubahan dibatalkan."`). Endpoint Edit BUKAN bagian inti scope 356, tetapi karena AF-6 mengubah endpoint Assign yang bersebelahan langsung dan menetapkan pola, inkonsistensi ini layak diangkat.
**Fix:**
```csharp
catch (Exception ex)
{
    await tx.RollbackAsync();
    _logger.LogError(ex, "CoachCoacheeMappingEdit failed for mapping {MappingId}; rolled back", req.MappingId);
    return Json(new { success = false, message = "Gagal menyimpan perubahan. Operasi dibatalkan." });
}
```
Catatan: `ImportCoachCoacheeMapping` L379 juga punya pola serupa (`$"Gagal membaca file Excel: {ex.Message}"`) — itu lebih bisa diterima karena umumnya pesan parsing ClosedXML, tapi pertimbangkan untuk diselaraskan jika ingin konsisten penuh.

## Info

### IN-01: AF-7 — `prevByCoachee` non-deterministik saat multi prev-assignment (sama seperti kode lama, terdokumentasi)

**File:** `Controllers/CoachMappingController.cs:524-526`
**Issue:** `GroupBy(...).ToDictionary(g => g.Key, g => g.First().Id)` memilih satu prev-assignment secara non-deterministik bila seorang coachee punya >1 assignment untuk `prevTrack`. Ini SETARA dengan `FirstOrDefaultAsync` tanpa `OrderBy` di kode lama (parity benar, sudah diverifikasi via git diff d44fc92c — zero behavior change sesuai klaim commit). Komentar "FirstOrDefault-equivalent" sudah ada. Tidak ada regresi; dicatat hanya sebagai jejak edge-case yang diwariskan.
**Fix:** Opsional — bila ingin deterministik, tambahkan `.OrderByDescending(a => a.Id)` sebelum `.First()` (samakan dengan pola reuse-assignment di L608). Tidak wajib untuk Phase 356.

### IN-02: AF-1 — `expectedCount` dieksekusi sebagai query per-coachee di dalam foreach (N query)

**File:** `Controllers/CoachMappingController.cs:1389-1404`
**Issue:** Untuk tiap coachee, ada 2-3 round-trip DB: resolve `AssignmentUnit`, fallback `User.Unit`, dan `CountAsync` `expectedCount`. Untuk dropdown admin-tool (jumlah coachee per-track kecil, dipanggil saat admin memilih track di form CreateAssessment) ini dapat diterima dan BUKAN concern performa v1 (out-of-scope). Dicatat agar tidak terkejut bila track berisi puluhan coachee.
**Fix:** Bila kelak jadi hot-path, pre-load tiga dict sekali (unit per coachee via mappings + users; expectedCount per unit) lalu loop in-memory — pola yang sama persis dengan AF-7. Tidak perlu untuk Phase 356 (admin tool, low cardinality).

### IN-03: AF-4 — komentar defer akurat; window ±5 detik tetap rapuh by design (terdokumentasi, di-defer ke backlog)

**File:** `Controllers/CoachMappingController.cs:1041-1056`
**Issue:** Korelasi reactivation via `DeactivatedAt` dalam window ±5 detik dapat ikut me-restore assignment yang dinonaktifkan secara independen dalam window yang sama. Komentar AF-4 sudah secara eksplisit mengakui ini, menjelaskan mengapa di-defer (butuh kolom `DeactivatedByMappingEventId` = migration, dilarang di phase 0-migration), dan memberi trigger promote via backlog. Ini keputusan sadar, bukan bug yang lolos. Dicatat agar tracking-nya tidak hilang.
**Fix:** Tidak ada aksi di Phase 356 (sesuai instruksi "JANGAN ubah logic window di phase ini"). Pastikan item backlog benar-benar terdaftar agar tidak terlupa saat volume reactivate / track multi-unit meningkat.

---

## Verifikasi konstrain & klaim (lolos)

- **Authz preserved:** semua endpoint 356-touched mempertahankan atribut role + anti-forgery. AF-5 `ApproveReassignSuggestion` tetap `[Authorize(Roles = "Admin")]`. OK.
- **AF-3 transaction safety:** `BeginTransactionAsync` membungkus `IsCompleted/IsActive=false/EndDate` + cascade deactivate + `SaveChangesAsync` + `CommitAsync`; audit log POST-commit; rollback di catch dengan pesan ramah. `IsActive=false` membebaskan unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (D-03) — benar. Cascade mirror pola Deactivate, stamp `DeactivatedAt`, TIDAK `RemoveRange` (histori progress dipertahankan, D-04). OK.
- **AF-6 ordering:** `catch (DbUpdateException ...) when (...)` ditempatkan SEBELUM `catch (Exception ex)` generik — urutan benar (specific-before-generic). Filter cocokkan nama index + SQL error 2601/2627. Info-leak dihindari (raw message hanya ke logger). OK.
- **AF-5 warn-only:** 3 `SendAsync` dibungkus satu `try/catch` warn-only (`_logger.LogWarning`) setelah `SaveChangesAsync` commit; kegagalan notif tidak menggagalkan reassign. OK.
- **AF-7 parity:** 3 cabang (skip / prev-null / not-all-approved) direproduksi identik; `prevProgressCount > 0` guard dipertahankan; pesan warning + `incompleteCount` verbatim. Diverifikasi via git diff. OK.
- **AF-2 view guard:** `updateAssignmentDefaults()` me-reset (re-enable + hapus `text-muted` + sembunyikan hint) saat 0 centang; mengunci unit dari centang pertama; HANYA toggle `disabled`/`text-muted`, tidak menyentuh `style.display` (milik `filterCoacheesBySection`) — pemisahan tanggung jawab benar, tidak ada konflik dua fungsi. `submitAssign()` backstop menolak >1 unit terpilih (sabuk pengaman race DOM). Server-side AF-6 unique-index tetap menjadi otoritas final. OK.
- **D-06 badge reorder:** cek `IsCompleted` DULU sebelum `IsActive`, sehingga mapping graduated (kini `IsActive=false` akibat AF-3) menampilkan badge "Graduated" bukan tombol "Aktifkan" yang menyesatkan. Logika branch view benar. OK.
- **Tests:** 4 `[Fact]` mengcover full-approved/zero/partial/expectedCount==0. Cukup untuk helper murni. Catatan: tidak ada test yang mengcover skenario lintas-unit dari WR-01 (helper sendiri benar; gap ada di call-site, bukan helper).
- **Migration=false:** tidak ada perubahan skema; reuse kolom `DeactivatedAt`/`IsCompleted`/`CompletedAt` yang sudah ada. OK.
- **Bahasa Indonesia:** semua pesan user-facing (Json message, alert JS, TempData, badge) Bahasa Indonesia. OK.

---

_Reviewed: 2026-06-09_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
