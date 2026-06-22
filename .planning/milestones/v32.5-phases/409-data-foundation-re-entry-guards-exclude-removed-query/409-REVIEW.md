---
phase: 409-data-foundation-re-entry-guards-exclude-removed-query
reviewed: 2026-06-21T01:51:44Z
depth: standard
files_reviewed: 7
files_reviewed_list:
  - Models/AssessmentSession.cs
  - Data/ApplicationDbContext.cs
  - Migrations/20260621011101_AddParticipantRemovalColumns.cs
  - Controllers/CMPController.cs
  - Hubs/AssessmentHub.cs
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/ParticipantRemovalGuardTests.cs
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: issues_found
---

# Phase 409: Laporan Code Review

**Direview:** 2026-06-21T01:51:44Z
**Kedalaman:** standard
**File Direview:** 7
**Status:** issues_found

## Ringkasan

Phase 409 membangun fondasi soft-remove peserta: 3 kolom nullable (`RemovedAt`/`RemovedBy`/`RemovalReason`) + migration EF, guard re-entry server-authoritative di `StartExam`/`SubmitExam`, predikat `RemovedAt == null` di 3 method Hub, dan 3 exclude-query di monitoring `AssessmentAdminController`.

Penilaian umum: inti implementasi **solid dan benar**. Migration sinkron dengan model snapshot (maxLength 500 cocok di tiga lokasi), helper `IsParticipantRemoved` adalah single-source-of-truth yang testable, guard ditempatkan tepat (SEBELUM mark-InProgress di `StartExam`, SEBELUM grading di `SubmitExam`), dan keputusan deteksi berbasis `RemovedAt` (bukan `Status`) sudah konsisten dengan spec §B2. Boundary `UserAssessmentHistory` yang sengaja TIDAK di-exclude juga terjaga (test boundary GREEN).

Namun ditemukan **2 read/write-path aktif yang terlewat** dari hardening soft-remove — keduanya jalur live yang dipakai pekerja saat ujian, bukan jalur boundary yang sengaja dikecualikan. Karena Phase 411 (penulisan `RemovedAt` aktual) belum ada, dampaknya belum live, tetapi keduanya melemahkan invarian read-path yang menjadi tujuan eksplisit Phase 409 ("any missed read-path that counts removed sessions in active flows?"). Selain itu ada 1 kelemahan de-tautology pada test JoinBatch.

## Warnings

### WR-01: `SaveAnswer` (MC single-choice) tidak mem-filter sesi soft-removed — celah write-path konsisten dengan guard Hub

**File:** `Controllers/CMPController.cs:357-369`
**Issue:**
Phase 409 menambahkan guard `&& s.RemovedAt == null` ke DUA method Hub yang menulis jawaban (`SaveTextAnswer` :146 untuk Essay, `SaveMultipleAnswer` :213 untuk MultipleAnswer) sebagai defense-in-depth (PRMV-03 / A1). Tetapi jalur penyimpanan jawaban **Single-Answer / pilihan ganda** TIDAK lewat Hub — ia lewat action controller `SaveAnswer` (`CMPController.SaveAnswer`, dipanggil dari `Views/CMP/StartExam.cshtml:512` `SAVE_ANSWER_URL` pada tiap perubahan radio). Action ini hanya mengecek ownership dan `Status` (Completed/Abandoned/Cancelled), tidak mengecek `RemovedAt`:

```csharp
// Session must still be in progress
if (session.Status == "Completed" || session.Status == "Abandoned" || session.Status == "Cancelled")
    return Json(new { success = false, error = "Session already closed" });
```

Karena soft-remove TIDAK mengubah `Status` (spec §B2 — justru alasan helper `IsParticipantRemoved` dibuat), pekerja yang sudah soft-removed namun masih punya tab ujian terbuka tetap bisa menyimpan jawaban MC (dan memicu SignalR `progressUpdate` ke monitor) via `SaveAnswer`. Ini inkonsisten dengan keputusan Hub yang sengaja memblok write Essay/MA. `SubmitExam` tetap akan men-discard di akhir (guard :1602), jadi tidak ada data yang ter-grade — namun invarian "sesi removed tak boleh menulis jawaban" yang ditegakkan di Hub bocor di jalur MC, dan progress palsu masih ter-broadcast ke layar monitoring HC.

**Fix:** Tambahkan guard simetris dengan Hub, tepat setelah cek ownership/Status (sebelum upsert response):
```csharp
// v32.5 Phase 409 (PRMV-03 / A1): sesi soft-removed tak boleh tulis jawaban (paritas dgn Hub SaveText/MultipleAnswer).
if (IsParticipantRemoved(session))
    return Json(new { success = false, error = "Anda telah dikeluarkan dari ujian ini." });
```

### WR-02: Daftar ujian pekerja (`Assessment`) masih menampilkan sesi soft-removed sebagai actionable/clickable

**File:** `Controllers/CMPController.cs:208-214`
**Issue:**
Kedua guard `StartExam` (:918) dan `SubmitExam` (:1605) me-redirect pekerja yang removed ke `RedirectToAction("Assessment")`. Tetapi action `Assessment` (`CMPController.Assessment` :196) — halaman tujuan redirect itu sendiri — membangun daftar ujian aktif tanpa mem-filter `RemovedAt`:

```csharp
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.UserId == userId);
query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress");
```

Karena soft-remove tidak mengubah `Status`, sesi yang sudah removed (Open/Upcoming/InProgress) tetap muncul sebagai kartu ujian dengan tombol Mulai/Lanjutkan. Akibatnya pekerja yang sudah dikeluarkan di-redirect ke halaman ini, melihat lagi kartu ujian yang sama, klik Mulai → guard `StartExam` bounce balik ke halaman yang sama (loop konfusional), dan secara tampilan tidak pernah benar-benar "dikeluarkan". Guard server tetap menahan akses ujian (aman secara data/integritas), tetapi ini persis kategori "missed read-path that counts/shows removed sessions in active flows" yang menjadi fokus Phase 409. Tab/Monitoring HC sudah dibersihkan (3 exclude-query), namun daftar sisi-pekerja luput.

Catatan boundary: ini BERBEDA dari `UserAssessmentHistory` (riwayat — sengaja menampilkan sesi removed agar sertifikat utuh & reversibel). `Assessment` adalah daftar ujian AKTIF/actionable, bukan riwayat — menampilkan sesi removed di sini bukan boundary yang disengaja melainkan jalur yang terlewat.

**Fix:** Tambahkan filter di query daftar aktif (jangan ke `completedHistory` :324 yang setara riwayat):
```csharp
query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress");
// v32.5 Phase 409 (PLIV-01): sesi soft-removed tak muncul di daftar ujian aktif pekerja (paritas dgn Tab/Monitoring HC).
query = query.Where(a => a.RemovedAt == null);
```
(Jika tampil-untuk-transparansi diinginkan, render sebagai badge "Dikeluarkan" non-clickable, bukan kartu actionable. Pilih salah satu secara eksplisit — bukan diam-diam clickable.)

## Info

### IN-01: Test JoinBatch mereplikasi predikat produksi (de-tautology parsial)

**File:** `HcPortal.Tests/ParticipantRemovalGuardTests.cs:325-328`
**Issue:**
Test `JoinBatch_Predicate_Rejects_RemovedSession` menulis ulang predikat secara inline (`s.UserId == ... && s.Status == "InProgress" && s.RemovedAt == null`) alih-alih memanggil `AssessmentHub.JoinBatch`. Komentar test mengakui kompromi ini ("Hub butuh Context/Groups → query-level"), dan predikat memang dijalankan terhadap schema SQL nyata (bukan POCO in-memory), jadi nilainya tidak nol. Namun ini tetap copy dari string predikat produksi: jika seseorang mengubah predikat di `Hubs/AssessmentHub.cs:31` (mis. menghapus `&& s.RemovedAt == null`), test ini akan TETAP hijau karena memegang salinannya sendiri. Berbeda dari test (4)/(5) yang memanggil helper produksi asli `CMPController.IsParticipantRemoved`.

**Fix:** Ekstrak predikat JoinBatch menjadi seam testable (mirip pola `IsParticipantRemoved`), mis. expression `AssessmentHub.ActiveJoinableSession(userId)` yang dipakai produksi DAN test — agar perubahan predikat tertangkap. Jika refactor Hub dianggap overkill untuk fase fondasi, terima sebagai keterbatasan terdokumentasi dan catat sebagai tech-debt.

### IN-02: Guard StartExam/SubmitExam tidak punya test yang mengeksekusi wiring guard end-to-end

**File:** `HcPortal.Tests/ParticipantRemovalGuardTests.cs:271-309`
**Issue:**
Test (4)/(5) memverifikasi helper `IsParticipantRemoved` (benar, de-taut bagus) tetapi tidak mengeksekusi blok `if (IsParticipantRemoved(...)) { ...redirect }` di dalam `StartExam`/`SubmitExam`. Jika seseorang menghapus blok guard tetapi membiarkan helper, kedua test tetap hijau. Pengamatan tambahan (`removed.StartedAt == null`, `Status != InProgress/Completed`) sebenarnya hanya mencerminkan state seed, bukan hasil eksekusi guard — sehingga assert itu bersifat dekoratif, bukan membuktikan guard menahan. Ini konsisten dengan pola codebase (action `StartExam`/`SubmitExam` punya dependency berat — userManager/env — sehingga tak pernah dipanggil langsung di unit test), jadi bukan regresi pola, melainkan gap cakupan yang perlu disadari.

**Fix:** Jika kelayakan memungkinkan, tambahkan satu integration test yang memanggil action `StartExam` asli dengan sesi removed dan assert `RedirectToActionResult` + `TempData["Error"]`. Bila biaya wiring dependency terlalu tinggi, dokumentasikan eksplisit bahwa wiring guard diverifikasi via UAT/manual, bukan automated.

### IN-03: Warning "ada peserta sedang mengerjakan" ikut menghitung sesi soft-removed

**File:** `Controllers/AssessmentAdminController.cs:2113-2116`
**Issue:**
Cek `hasInProgress` (`StartedAt != null && CompletedAt == null`) untuk menampilkan TempData info saat edit batch tidak mengecualikan `RemovedAt`. Setelah Phase 411 aktif, sesi InProgress yang sudah soft-removed akan tetap memicu pesan "Ada peserta yang sedang mengerjakan ujian.". Ini hanya nudge informasional non-blocking (bukan correctness/security), dan berada di luar 3 query yang sengaja di-scope Phase 409. Diangkat sebagai Info agar tidak hilang saat Phase 411 — bukan blocker.

**Fix (opsional, defer ke 411 bila diinginkan):** Tambahkan `&& s.RemovedAt == null` agar pesan konsisten dengan basis peserta aktif.

---

_Direview: 2026-06-21T01:51:44Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
