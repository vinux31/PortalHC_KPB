---
phase: 360-bypass-backend-b
reviewed: 2026-06-10T12:05:10Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - Services/ProtonBypassService.cs
  - Services/GradingService.cs
  - Controllers/ProtonDataController.cs
  - Controllers/AssessmentAdminController.cs
  - Helpers/ProtonDeliverableBootstrap.cs
  - Models/ProtonModels.cs
  - HcPortal.Tests/ProtonBypassServiceTests.cs
  - HcPortal.Tests/ProtonBypassEndpointTests.cs
  - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
findings:
  critical: 0
  warning: 3
  info: 7
  total: 10
status: issues
---

# Phase 360: Code Review Report — Bypass Backend (B)

**Reviewed:** 2026-06-10T12:05:10Z
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues (0 critical / 3 warning / 7 info)

## Summary

Review fokus perubahan Phase 360: `ProtonBypassService` (validator pure §5, eksekusi instan §5.1, jalur pending §5.2, confirm §5.3, cancel §8.1, 2 hook grading), 4 titik hook §7 (`GradingService.GradeAndCompleteAsync`, `RegradeAfterEditAsync` Pass→Fail dan Fail→Pass, `AssessmentAdminController.FinalizeEssayGrading`), 6 endpoint bypass di `ProtonDataController`, helper `ProtonDeliverableBootstrap`, model `PendingProtonBypass`/`Origin`, plus 3 file test.

Kualitas keseluruhan bagus: konvensi proyek dipatuhi konsisten — D6 (pesan ramah tanpa `ex.Message`) di semua catch, `BeginTransactionAsync` all-or-nothing di 4 mutator service, `ExecuteUpdateAsync` WHERE-guard untuk flip status atomik (D-12), hook hot-path grading TANPA transaksi (Pitfall 4), DI satu arah grading→bypass (Open Q3), Pitfall 1 (penanda sebelum deactivate) terjaga dan ter-test. Notifikasi `SendByTemplateAsync` try-catch internal (return false, tidak melempar ke hot-path grading) — diverifikasi di `Services/NotificationService.cs:239-273`. Atribut endpoint (Authorize class-level Admin,HC + antiforgery 3 POST mutator) di-lock via reflection test. DI `ProtonBypassService` terdaftar (`Program.cs:60`).

Tidak ada temuan critical. 3 warning: (1) cek dobel-pending D-10 tidak race-safe (di luar transaksi, index sengaja non-unique), (2) `TargetUnit` tidak divalidasi non-kosong — bisa mengkorup `AssignmentUnit` mapping coach aktif via cabang D-16b, (3) force-approve menimpa metadata approval (`ApprovedById`/`ApprovedAt`) pada progress yang sudah Approved sah oleh coach.

## Warnings

### WR-01: Cek dobel-pending D-10 tidak race-safe — dobel-klik BypassSave CL-B(b) bisa membuat 2 pending + 2 bare session

**File:** `Services/ProtonBypassService.cs:252-256` (cek D-10), `Services/ProtonBypassService.cs:269-379` (`ExecutePendingBypassAsync` tanpa re-check dalam tx), `Data/ApplicationDbContext.cs:420-426` (index non-unique)
**Issue:** Cek D-10 (`AnyAsync` Status Menunggu/Siap) dijalankan di `BypassSaveAsync` SEBELUM dan DI LUAR transaksi `ExecutePendingBypassAsync`. Dua request konkuren (kasus paling realistis: HC dobel-klik tombol Simpan — UI Phase 361 belum ada untuk mencegahnya) sama-sama lolos cek, lalu sama-sama insert → 2 baris `PendingProtonBypass` aktif + 2 bare `AssessmentSession` untuk worker yang sama, mematahkan invariant D-10. Komentar di `ApplicationDbContext.cs:421` mencatat keputusan sadar "app-level check, bukan DB constraint", tetapi app-level check yang ada tidak ditempatkan di dalam transaksi sehingga tetap berlubang. Berbeda dengan jalur instan yang re-check E8 + validator di dalam tx, jalur pending tidak melakukan re-check apa pun terhadap D-10 (TOCTOU). Catatan: setelah dobel-pending terjadi, `MarkPendingReadyIfAnyAsync` (`FirstOrDefaultAsync`) hanya mem-flip salah satu, dan `ConfirmBypassAsync` pending kedua akan terblokir cek D-11 — tapi sisa pending zombie + bare session yatim tetap perlu dibersihkan manual.
**Fix:** Tegakkan di DB dengan filtered unique index — pola yang sudah dipakai proyek untuk `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (E15), dan `ExecutePendingBypassAsync` sudah berpola catch `DbUpdateException`:
```csharp
// Data/ApplicationDbContext.cs — ganti index non-unique:
builder.Entity<PendingProtonBypass>(entity =>
{
    entity.HasIndex(p => new { p.CoacheeId, p.Status })
        .HasDatabaseName("IX_PendingProtonBypasses_CoacheeId_Status");
    entity.HasIndex(p => p.CoacheeId)
        .IsUnique()
        .HasFilter("[Status] IN (N'Menunggu', N'Siap')")
        .HasDatabaseName("IX_PendingProtonBypasses_CoacheeId_ActiveUnique");
});
```
Tambah catch `DbUpdateException` spesifik di `ExecutePendingBypassAsync` (pesan ramah "Worker sudah punya rencana bypass aktif."). Butuh migration tambahan — selaras flag migration=true fase ini. Minimal-fix alternatif (mempersempit tapi TIDAK menutup window): pindahkan cek D-10 ke dalam transaksi `ExecutePendingBypassAsync`.

### WR-02: `TargetUnit` kosong lolos validasi server — cabang D-16b mengkorup `AssignmentUnit` mapping coach aktif jadi string kosong

**File:** `Controllers/ProtonDataController.cs:1615-1622` (validasi V5 tanpa TargetUnit), `Services/ProtonBypassService.cs:443-450` (D-16b), `Helpers/ProtonDeliverableBootstrap.cs:30-34` (warning-only)
**Issue:** Komentar V5 di `BypassSave` menyatakan "jangan percaya form UI Phase 361", tapi hanya `CoacheeId`, `Reason`, `Mode` yang divalidasi — `TargetUnit` (default `""`) tidak. Konsekuensi bila kosong: (a) bootstrap deliverable di-skip dengan warning teks saja — bypass tetap "berhasil", worker mendarat di tahun tujuan tanpa satu pun progress; (b) lebih buruk, di `MoveAssignmentAsync` cabang D-16b: `(mappingLama.AssignmentUnit ?? "").Trim() != req.TargetUnit.Trim()` → `"U-X" != ""` → true → `mappingLama.AssignmentUnit = ""` — unit mapping coach aktif yang sah TERTIMPA jadi kosong, merusak resolve unit gate 100% (W-04/W-08/W-10) dan tampilan mapping. `BypassValidator` pure juga tidak menerima TargetUnit, jadi tidak ada lapisan yang menangkapnya.
**Fix:**
```csharp
// ProtonDataController.BypassSave — tambah ke blok validasi V5:
if (string.IsNullOrWhiteSpace(req.TargetUnit))
    return Json(new { success = false, message = "Unit tujuan wajib diisi." });
```
Plus guard defensif di `MoveAssignmentAsync` (lapisan service, melindungi caller lain seperti `ConfirmBypassAsync`):
```csharp
else if (mappingLama != null && !string.IsNullOrWhiteSpace(req.TargetUnit)
         && (mappingLama.AssignmentUnit ?? "").Trim() != req.TargetUnit.Trim())
```

### WR-03: Force-approve menimpa `ApprovedById`/`ApprovedAt` progress yang SUDAH Approved sah — provenance approval coach hilang

**File:** `Services/ProtonBypassService.cs:159-177` (CL-B(a)), `Services/ProtonBypassService.cs:303-321` (CL-B(b))
**Issue:** Loop force-approve D-13 memproses SEMUA progress source tanpa filter status. Progress yang sebelumnya sudah `Approved` sah oleh coach ikut ditimpa: `ApprovedById` diganti jadi HC inisiator bypass, `ApprovedAt` di-reset ke sekarang, dan history `Bypassed-AutoApprove` ditulis untuk baris yang faktanya tidak berubah status. Akibatnya kolom provenance di baris progress menyesatkan (seolah HC yang meng-approve semua), dan audit history bising. Skenario ulang (pending dibatalkan → buat pending baru) menambah baris history duplikat lagi. Tujuan D-13 (semua progress source berakhir Approved + jejak) tetap tercapai tanpa menyentuh baris yang sudah Approved.
**Fix:**
```csharp
foreach (var p in progresses.Where(p => p.Status != "Approved"))
{
    p.Status = "Approved";
    // ... (sisanya sama, termasuk history Bypassed-AutoApprove)
}
```
Terapkan di kedua lokasi (CL-B(a) §5.1 dan CL-B(b) §5.2).

## Info

### IN-01: Deteksi duplicate-key via `Message.Contains("2601"/"2627")` — kondisi mati, andalkan nama index saja

**File:** `Services/ProtonBypassService.cs:197-200`, `Services/ProtonBypassService.cs:521-524`
**Issue:** Pesan `SqlException` duplicate key TIDAK memuat angka error (2601/2627 adalah `SqlException.Number`), jadi dua kondisi `Contains` itu praktis tidak pernah true; filter hanya bekerja lewat cek nama index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (yang memang muncul di pesan 2601).
**Fix:** Reuse helper yang sudah ada: `CertNumberHelper.IsDuplicateKeyException(dbEx)`, atau cek `(dbEx.InnerException as SqlException)?.Number is 2601 or 2627`.

### IN-02: Audit log dobel — service mencatat di dalam tx, controller mencatat lagi (termasuk saat gagal)

**File:** `Controllers/ProtonDataController.cs:1632-1634`, `1657-1659`, `1675-1677` vs `Services/ProtonBypassService.cs:187-189`, `362-364`, `511-513`, `587-589`
**Issue:** Setiap aksi bypass sukses menghasilkan 2 baris AuditLog dengan konten tumpang-tindih (action `ProtonBypass` dari service + `ProtonBypassSave`/`Confirm`/`Cancel` dari controller). Controller juga mencatat saat `result.Success == false`. Bila pencatatan attempt-gagal memang disengaja (jejak percobaan), pertimbangkan menjadikan log controller khusus kasus gagal saja agar tidak dobel di kasus sukses.
**Fix:** Pilih satu sumber kanonik (service-side, karena ikut transaksi) dan jadikan log controller kondisional `if (!result.Success)` — atau dokumentasikan redundansi sebagai disengaja.

### IN-03: `TempData["Warning"]` di endpoint JSON AJAX — tidak akan tampil sampai full-page load berikutnya

**File:** `Controllers/ProtonDataController.cs:1629-1630`
**Issue:** `BypassSave` adalah endpoint JSON (dipanggil via fetch/AJAX dari wizard Phase 361); `TempData` baru ter-render di request HTML berikutnya, padahal respons JSON sudah membawa `showAttachPackageReminder` + `message` untuk ditampilkan UI. Berpotensi banner "telat muncul" di halaman lain.
**Fix:** Hapus baris TempData (UI pakai field JSON), atau beri komentar eksplisit bahwa banner next-page-load memang diinginkan sebagai pengingat ganda D-02.

### IN-04: `BypassList` inner-join Users vs `BypassPendingList` left-join — worker yatim hilang dari Tab2 tapi pending-nya tampil

**File:** `Controllers/ProtonDataController.cs:1503-1507` vs `Controllers/ProtonDataController.cs:1538-1539`
**Issue:** `BypassList` memakai inner join ke `Users` sehingga assignment dengan `CoacheeId` tanpa baris user (model tanpa FK constraint) tidak muncul di tabel worker; `BypassPendingList` memakai `DefaultIfEmpty` sehingga pending worker yang sama tetap tampil. Inkonsisten — HC bisa melihat pending milik worker yang tidak ada di daftar.
**Fix:** Samakan: pakai `join ... into uj from u in uj.DefaultIfEmpty()` di `BypassList` dengan fallback nama `p.CoacheeId`.

### IN-05: Tiga POST `[FromBody]` tanpa null-guard `req` — body kosong/JSON invalid → NRE 500

**File:** `Controllers/ProtonDataController.cs:1610-1616`, `1649-1654`, `1667-1672`
**Issue:** Controller MVC biasa (bukan `[ApiController]`) tidak auto-400 saat binding `[FromBody]` gagal; `req` bisa null sehingga `req.CoacheeId` / `req.PendingId` melempar `NullReferenceException` (500 polos, bukan pesan ramah D6).
**Fix:** Tambah di awal masing-masing action: `if (req == null) return Json(new { success = false, message = "Request tidak valid." });`

### IN-06: Re-grade Pass→Fail SETELAH pending "Selesai" (worker sudah pindah) — revert no-op, penanda Exam source terhapus

**File:** `Services/ProtonBypassService.cs:634-639`, `Services/GradingService.cs:492-497`
**Issue:** `RevertPendingToMenungguAsync` hanya menangani Status="Siap". Bila exam linked di-re-grade Pass→Fail setelah `ConfirmBypassAsync` (pending "Selesai", worker sudah pindah), hook `RemoveExamOriginAsync` tetap menghapus penanda Origin="Exam" tahun asal sementara worker tinggal di tahun tujuan (tetap exempt via Origin="Bypass" — tidak crash, tapi histori kelulusan tahun asal hilang). Spec menetapkan undo-executed = C (out of scope Phase 360) — dicatat di sini untuk jejak audit Phase 363 (T1-T10 alur PROTON).
**Fix:** Tidak perlu perubahan kode sekarang; masukkan skenario "re-grade setelah Selesai" ke daftar audit Phase 363 / backlog undo-executed.

### IN-07: Test exempt gate mereplikasi predikat controller + hardcode nomor baris — risiko drift senyap

**File:** `HcPortal.Tests/ProtonYearGateIntegrationTests.cs:64-78`, `133`
**Issue:** `SkippedByCrossYearGateAsync` menyalin manual logika gate `AssessmentAdminController.cs:1372-1379` (diakui di komentar). Bila gate di controller berubah, test tetap hijau terhadap salinan lama (false confidence); komentar nomor baris (`:1368-1397`, `:1383-1396`) juga mudah basi. Mempengaruhi keandalan test sebagai jaring regresi.
**Fix:** Ekstrak predikat gate (a) ke helper statis (mis. `ProtonGatePredicates.IsBypassExempt(...)`) yang dipakai controller DAN test — selaras pola `CoacheeEligibilityCalculator` yang sudah dipakai untuk gate (b).

---

## Catatan Verifikasi (tanpa temuan)

- **Pitfall 1** (penanda sebelum deactivate): urutan benar di §5.1 CL-B(a) (`EnsureAsync` di `ProtonBypassService.cs:179-181` sebelum `MoveAssignmentAsync`), ter-test (`CL_BSatuA_TerbitPenandaSource_SebelumDeactivate`).
- **Pitfall 4** (hook hot-path no-tx): `MarkPendingReadyIfAnyAsync`/`RevertPendingToMenungguAsync` tanpa `BeginTransactionAsync`; flip atomik via `ExecuteUpdateAsync` WHERE-guard; notif `SendByTemplateAsync` try-catch internal return-false (`NotificationService.cs:239-273`) — kegagalan notif tidak menjatuhkan grading.
- **4 titik hook §7 lengkap**: `GradingService.cs:314` (lulus normal), `:549` (Fail→Pass), `:496` (Pass→Fail revert), `AssessmentAdminController.cs:3769` (essay finalize, menutup celah early-return hasEssay) — semua di dalam guard `Category == "Assessment Proton" && ProtonTrackId.HasValue` (W-09).
- **T-360-17**: `excludeSessionId` di `MoveAssignmentAsync` mencegah sesi bukti kelulusan ikut "Dibatalkan"; W-03 guard `Status != "Completed"` di cancel melindungi jejak Completed-gagal.
- **Keamanan endpoint**: class-level `[Authorize(Roles = "Admin,HC")]`, 3 POST mutator `[ValidateAntiForgeryToken]`, di-lock reflection test (`ProtonBypassEndpointTests`). Tidak ada injection (EF parameterized), tidak ada secret, D6 dipatuhi di semua jalur error.
- **DI**: `Program.cs:60` scoped; arah dependensi grading→bypass satu arah (tanpa circular).
- **Jalur instan race-safe memadai**: re-check E8 + validator DI DALAM tx (`ExecuteInstantBypassAsync:104-143`) — dobel-klik kedua terblokir "Assignment aktif tidak sesuai track asal" setelah commit pertama.

---

_Reviewed: 2026-06-10T12:05:10Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
