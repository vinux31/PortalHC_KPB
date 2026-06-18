---
phase: 397-link-pre-post-ke-room-existing
reviewed: 2026-06-18T11:48:26Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - Controllers/InjectAssessmentController.cs
  - Services/InjectAssessmentService.cs
  - Models/InjectAssessmentDtos.cs
  - ViewModels/InjectAssessmentViewModel.cs
  - Views/Admin/InjectAssessment.cshtml
  - HcPortal.Tests/InjectLinkPrePostTests.cs
  - HcPortal.Tests/InjectAntiDoubleLinkTests.cs
  - HcPortal.Tests/InjectPreviewPairingTests.cs
  - HcPortal.Tests/InjectCrossGroupingTests.cs
  - HcPortal.Tests/UnlinkInjectGroupTests.cs
  - HcPortal.Tests/InjectViewModelMapTests.cs
  - tests/e2e/inject-assessment-397.spec.ts
findings:
  critical: 0
  warning: 3
  info: 5
  total: 8
status: issues_found
---

# Phase 397: Code Review Report

**Reviewed:** 2026-06-18T11:48:26Z
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Phase 397 (INJ-12) menambahkan penautan Pre/Post per-pekerja bidirectional ke room existing: Kasus A (adopt `LinkedGroupId` grup online tanpa menyentuh data online) dan Kasus B (tulis stiker `LinkedGroupId` ke SEMUA sesi room standalone). Implementasi solid pada poros-poros yang paling penting:

- **Integritas data online TERJAGA.** Baik commit (`InjectBatchAsync`), unlink (`UnlinkInjectGroupAsync`), maupun stiker Kasus B hanya menyentuh kolom link (`LinkedGroupId`/`LinkedSessionId`). Tidak ada jalur yang menulis `Score`/`Status`/`IsPassed`/responses sesi online. Diverifikasi langsung di test (`KasusA_Adopt_OnlineScoreStatusUnchanged`, `KasusB_WriteSticker_AllTargetSessions_AuditPerMutated`, `Unlink_RevertBidirectional_OnlineUnchanged_AuditUndo`) dan e2e (Score/Status before==after + audit `LinkPrePost`).
- **Server-authoritative link resolution.** `req.LinkedGroupId`/`LinkedSessionId` mentah dari client TIDAK pernah dipercaya — `ResolveLinkContextAsync` re-resolve dari `LinkTargetRepId` + validasi tipe-lawan (T-397-06/13). `MapToRequest` sengaja membiarkan `LinkedGroupId`/`LinkedSessionId` null (dikunci `InjectViewModelMapTests.Maps_LinkTargetRepId_from_chip`).
- **Atomicity.** Commit dibungkus `BeginTransactionAsync` dengan rollback penuh; stiker Kasus B + write-back bidirectional + audit semua dalam tx yang sama. `UnlinkInjectGroupAsync` juga atomic. Diverifikasi `AtomicRollback_NoInjectSession_NoOnlineLinkMutation` dan `Unlink_Atomic_InvalidGroupLeavesStateIntact`.
- **RBAC + CSRF.** Semua endpoint POST baru (`PreviewPairing`, `UnlinkInjectGroup`) ber-`[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`; `SearchLinkTargets` GET read-only RBAC-only (tepat). Kelas `AdminBaseController` juga `[Authorize]` (defense-in-depth).
- **XSS-safety.** Seluruh data room/worker/error (title, category, NIP, nama, pesan) dirender via `.textContent`, bukan `innerHTML`. `innerHTML` hanya dipakai untuk markup statis bertuliskan tetap (spinner, ikon) tanpa interpolasi data server.
- **Grouping-key Kasus B konsisten.** Picker controller (`SearchLinkTargets` standalone group: `Title + Category + Schedule.Date`) cocok persis dengan service write-to-all (`ResolveLinkContextAsync` Kasus B: `Title + Category + Schedule.Date`), sehingga himpunan picker == himpunan stiker.

Tidak ada temuan Critical. Tiga Warning bersifat edge-case/operasional (bukan kebocoran integritas inti) dan lima Info bersifat konsistensi/kebersihan. Semua aman untuk di-ship; Warning sebaiknya dicatat ke backlog atau ditangani saat sentuhan berikutnya.

## Warnings

### WR-01: `UnlinkInjectGroupAsync` dapat melepas tautan LINTAS-BATCH pada Kasus A (scope unlink lebih luas dari ekspektasi host UI)

**File:** `Services/InjectAssessmentService.cs:743-745` (dan dipicu dari `Controllers/InjectAssessmentController.cs:265-269`)
**Issue:** Pada Kasus A (adopt), `resolvedGroupId == rep.LinkedGroupId` = ID grup ONLINE yang sudah ada. Saat unlink, query memuat **semua** sesi inject yang `LinkedGroupId == injectGroupId && IsManualEntry`:

```csharp
var injectSessions = await _context.AssessmentSessions
    .Where(s => s.LinkedGroupId == injectGroupId && s.IsManualEntry)
    .ToListAsync();
```

Bila DUA batch inject berbeda (mis. inject Pre batch-1 lalu inject Post batch-2) sama-sama tertaut ke grup yang sama, satu kali `UnlinkInjectGroup` akan melepas tautan **kedua** batch inject sekaligus + revert sibling-nya. Host UI (`#postCommitLinkSurface`) hanya menampilkan ID grup commit terakhir dan tidak membatasi unlink ke batch tertentu — pengguna mengira hanya melepas batch yang baru saja di-commit. Ini bukan kebocoran integritas (skor/status online tetap utuh, audit dicatat per-sesi, atomic), tetapi efek samping lebih luas dari yang diharapkan dan tak reversibel dari UI.
**Fix:** Pertimbangkan scoping unlink ke batch/sesi yang relevan saja (mis. lewatkan kumpulan `SuccessSessionIds` atau identitas batch ke `UnlinkInjectGroupAsync` dan filter `injectIds` di atasnya), atau dokumentasikan eksplisit di notice modal bahwa "melepas tautan akan melepaskan SELURUH sesi inject di grup ini" agar ekspektasi pengguna benar. Minimal tambahkan di copy modal `#unlinkConfirmModalBody`.

### WR-02: Sibling tipe-lawan ber-UserId duplikat dipilih senyap via `g.First()` (komentar menjanjikan "Guard >1" yang tidak diterapkan)

**File:** `Services/InjectAssessmentService.cs:130-132` (lihat juga `PreviewPairingAsync` :683-685)
**Issue:** Komentar baris 123 menyatakan "Guard >1 (A2)", tetapi kode hanya melakukan:

```csharp
siblingByUserId = siblingQuery
    .GroupBy(s => s.UserId)
    .ToDictionary(g => g.Key, g => g.First());   // tak ada urutan eksplisit
```

Bila satu UserId punya >1 sesi tipe-lawan di grup target (mis. dua Post online untuk satu user — anomali yang bisa terjadi sebelum aturan anti-dobel sepenuhnya tegak), `g.First()` memilih sibling secara non-deterministik (tanpa `OrderBy`), dan `LinkedSessionId` write-back bisa menunjuk ke sesi yang "salah". Anti-double-link preflight (D-08) hanya menolak sibling tipe-SAMA, bukan tipe-LAWAN ganda, sehingga skenario ini tidak tertangkap. Dampak terbatas pada fidelitas pointer `LinkedSessionId` (display gain-score memasangkan by `LinkedGroupId + UserId`, jadi grouping tetap benar) — karena itu Warning, bukan Critical.
**Fix:** Tambahkan urutan deterministik agar pilihan stabil/reproducible, dan idealnya warning bila >1 ditemukan:

```csharp
.ToDictionary(g => g.Key, g => g.OrderBy(s => s.CompletedAt ?? DateTime.MaxValue).ThenBy(s => s.Id).First());
```

Selaraskan juga `PreviewPairingAsync` :683-685 agar preview == commit pointer.

### WR-03: `UnlinkInjectGroup` men-set `TempData` padahal respons hanya JSON (TempData bocor ke render halaman berikutnya)

**File:** `Controllers/InjectAssessmentController.cs:270-272`
**Issue:** Action mengembalikan `Json(...)` (klien memanggil via `fetch` dan mengganti surface in-place tanpa reload), tetapi juga menulis `TempData["Success"]`/`TempData["Error"]`:

```csharp
if (res.Success) TempData["Success"] = res.Message ?? "Tautan dilepas.";
else TempData["Error"] = res.Message ?? "Gagal melepas tautan.";
return Json(new { ok = res.Success, message = ... });
```

Karena tidak ada redirect/render setelah ini, `TempData` tidak dikonsumsi pada request ini dan akan bertahan ke navigasi berikutnya — pada GET `/Admin/InjectAssessment` berikutnya, view me-render blok `@if (TempData["Success"]/["Error"] != null)` sehingga muncul toast "Tautan dilepas." yang membingungkan (peristiwa lama). Fungsional tidak rusak, tetapi UX menyesatkan.
**Fix:** Hapus dua baris `TempData` di action ini — surface sukses/gagal sudah ditangani sepenuhnya oleh JS (`btnConfirmUnlink` → `notice.textContent`). JSON saja sudah cukup.

## Info

### IN-01: `SearchLinkTargets` menggabungkan grouped + standalone lalu `Take(50)` tanpa `OrderBy` — hasil non-deterministik & bisa terpotong tak adil

**File:** `Controllers/InjectAssessmentController.cs:235`
**Issue:** `grouped.Concat(standalone).Take(50)` tidak memiliki urutan eksplisit; bila hasil >50, baris yang muncul (dan yang terpotong) tidak deterministik dan condong ke `grouped` lebih dulu. Untuk picker pencarian ini umumnya tidak masalah, tapi UX akan lebih baik bila room paling relevan/terbaru muncul.
**Fix:** Urutkan sebelum `Take`, mis. `OrderByDescending(r => r.CompletedAt ?? r.Schedule)` (perlu proyeksi konsisten lebih dulu) lalu `.Take(50)`.

### IN-02: `PreviewPairing` mengandalkan default binder `"Standard"` saat `AssessmentType` null, tetapi `PreviewPairingAsync` juga default `"Standard"` — redundan tapi tak konsisten

**File:** `Controllers/InjectAssessmentController.cs:251` & `Services/InjectAssessmentService.cs:660`
**Issue:** Controller meneruskan `req.AssessmentType ?? "Standard"` sementara DTO `PreviewPairingRequest.AssessmentType` sudah berdefault `"Standard"` (non-nullable string, baris 645). Operator `??` praktis dead-code karena properti tak pernah null setelah binding. Tidak berbahaya, hanya membingungkan pembaca.
**Fix:** Sederhanakan ke `req.AssessmentType` (sudah dijamin non-null), atau jadikan properti nullable bila memang ingin membedakan "tidak dikirim".

### IN-03: Magic string tipe assessment & ActionType audit tersebar tanpa konstanta terpusat

**File:** `Services/InjectAssessmentService.cs:366` (`"LinkPrePost"`), `:386` (`"ManualInject"`), `:399` (`"ManualInjectSkipped"`), `:800` (`"LinkPrePostUndo"`)
**Issue:** Tipe assessment sudah dikonstanta-kan via `AssessmentConstants.AssessmentType.*` (bagus), tetapi `ActionType` audit masih literal string yang tersebar dengan komentar manual menjaga MaxLength(50). Risiko typo/divergensi bila kelak di-query di tempat lain (test sudah mengandalkan string persis ini, mis. `InjectLinkPrePostTests:273`).
**Fix:** Pusatkan ke konstanta (mis. `AuditActionTypes.LinkPrePost`) agar emisi dan query memakai sumber yang sama.

### IN-04: Komentar baris 75 di `InjectAssessmentService.cs` (SCOPE Plan 01: "Body diisi Plan 02") sudah usang

**File:** `Services/InjectAssessmentService.cs:17`
**Issue:** XML-doc class masih berbunyi "SCOPE Plan 01: hanya KONTRAK (ctor DI + signature). Body diisi Plan 02." Service kini penuh terimplementasi melewati Plan 393/395/396/397. Komentar usang dapat menyesatkan pembaca berikutnya.
**Fix:** Perbarui ringkasan XML-doc agar mencerminkan cakupan aktual (commit + link Pre/Post + auto-gen).

### IN-05: `renderResultsEmpty`/`renderResultsError` memakai `innerHTML` untuk markup statis — aman, tapi tidak konsisten dengan pola `.textContent` di sekitarnya

**File:** `Views/Admin/InjectAssessment.cshtml:2087, 2096, 2105`
**Issue:** Beberapa render memakai `innerHTML = '<div ...>'` untuk markup statis tanpa data server (spinner/ikon/pesan tetap). Ini tidak menimbulkan XSS karena tidak ada interpolasi nilai server, dan baris 2096 sengaja memisahkan teks dinamis ke `span.textContent` (benar). Hanya catatan konsistensi — pola dominan di file ini adalah konstruksi DOM + `.textContent`.
**Fix:** Opsional — biarkan apa adanya (aman) atau seragamkan ke `createElement` untuk konsistensi gaya. Tidak ada perubahan keamanan yang diperlukan.

---

_Reviewed: 2026-06-18T11:48:26Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
