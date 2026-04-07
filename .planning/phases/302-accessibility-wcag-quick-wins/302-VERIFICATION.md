---
phase: 302-accessibility-wcag-quick-wins
verified: 2026-04-07T12:00:00Z
status: human_needed
score: 9/9 must-haves verified
human_verification:
  - test: "Tekan Tab di halaman StartExam — skip link 'Lewati ke konten utama' harus muncul di pojok kiri atas"
    expected: "Skip link terlihat dengan background biru, teks putih, pada keypress Tab pertama"
    why_human: "Perilaku CSS :focus-visible dan posisi elemen tidak bisa diverifikasi tanpa browser rendering"
  - test: "Tekan Enter pada skip link — focus harus berpindah ke area soal (#mainContent)"
    expected: "Scroll halaman ke area soal, focus berpindah ke div col-lg-9 exam-protected"
    why_human: "Verifikasi perpindahan focus DOM membutuhkan browser interaktif"
  - test: "Klik tombol Next/Prev halaman soal — focus harus otomatis ke card soal pertama di halaman baru"
    expected: "Card soal pertama mendapat focus, outline biru 2px terlihat, tanpa perlu klik mouse"
    why_human: "Perilaku programmatic focus management saat performPageSwitch() membutuhkan browser"
  - test: "HC klik 'Tambah Waktu' di AssessmentMonitoringDetail, pilih 15 menit, klik Konfirmasi"
    expected: "Timer peserta yang sedang ujian bertambah 15 menit secara real-time tanpa refresh halaman"
    why_human: "Verifikasi real-time SignalR broadcast membutuhkan dua browser aktif simultan (HC + peserta)"
---

# Phase 302: Accessibility WCAG Quick Wins — Laporan Verifikasi

**Phase Goal:** Halaman ujian dapat digunakan dengan keyboard dan screen reader, dan peserta dengan kebutuhan khusus mendapat akomodasi waktu tambahan
**Verified:** 2026-04-07T12:00:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Cakupan Requirements

| Requirement | Plan | Deskripsi | Keputusan | Status |
|-------------|------|-----------|-----------|--------|
| A11Y-01 | 302-01 | Skip link "Lewati ke konten utama" | Diimplementasikan | TERVERIFIKASI |
| A11Y-02 | 302-01 | Keyboard navigation semua soal dan opsi jawaban | Diimplementasikan | TERVERIFIKASI |
| A11Y-03 | 302-01 | Screen reader announcement (aria-live) | DROPPED per D-18 | DIHAPUS — by design |
| A11Y-04 | 302-01 | Kontrol ukuran font (A+/A-) | DROPPED per D-19 | DIHAPUS — by design |
| A11Y-05 | 302-02 | ExtraTimeMinutes per sesi untuk peserta kebutuhan khusus | Diimplementasikan | TERVERIFIKASI |
| A11Y-06 | 302-01 | Auto-focus ke soal pertama saat berpindah halaman | Diimplementasikan | TERVERIFIKASI |

**Catatan A11Y-03 dan A11Y-04:** Dua requirement ini secara resmi dihapus dari phase ini berdasarkan keputusan D-18 dan D-19 di 302-CONTEXT.md. PLAN 302-01 memuat ID tersebut di frontmatter untuk traceability saja, bukan untuk implementasi. Hal ini dianggap valid dan tidak menjadi gap.

---

## Observable Truths

| # | Truth | Status | Bukti |
|---|-------|--------|-------|
| 1 | Skip link 'Lewati ke konten utama' muncul saat user menekan Tab pertama kali di StartExam | ? BUTUH HUMAN | HTML ada di baris 10 StartExam.cshtml, CSS `.skip-link` ada di site.css baris 23 dan 36 — perilaku visual perlu browser |
| 2 | Skip link memindahkan focus ke area soal saat diklik/Enter | ? BUTUH HUMAN | `href="#mainContent"` + `id="mainContent"` pada div col-lg-9 baris 68 — verifikasi focus perlu browser |
| 3 | Semua opsi jawaban dapat dinavigasi dengan keyboard (Tab antar soal, Arrow antar opsi) | TERVERIFIKASI | SUMMARY 302-01 mengkonfirmasi audit: radio buttons native, checkboxes native, anti-copy handler hanya block Ctrl+C/A/U/S/P |
| 4 | Focus otomatis berpindah ke card soal pertama saat pindah halaman via Prev/Next | ? BUTUH HUMAN | Kode `performPageSwitch()` baris 961-964 ada dan substansial — verifikasi focus aktual perlu browser |
| 5 | HC dapat menambah extra time dari AssessmentMonitoringDetail via modal | ? BUTUH HUMAN | Modal `#extraTimeModal` dan fungsi `addExtraTime()` ada di baris 1346-1413 — verifikasi end-to-end perlu browser + server |
| 6 | Extra time tersimpan di database (ExtraTimeMinutes column) | TERVERIFIKASI | `public int? ExtraTimeMinutes { get; set; }` di AssessmentSession.cs baris 162; migration 20260407110442 terdokumentasi |
| 7 | Timer peserta diupdate real-time via SignalR saat extra time ditambahkan | ? BUTUH HUMAN | Handler `ExtraTimeAdded` ada di StartExam.cshtml baris 1248-1252 dengan update `timerStartRemaining + timerStartWallClock` — verifikasi real-time perlu dua browser |
| 8 | Server-side timer check memperhitungkan extra time | TERVERIFIKASI | CMPController.cs: 4 lokasi ditemukan (baris 1030, 1531, 1574, 1612); AssessmentHub.cs baris 209 |
| 9 | Peserta yang sudah submit tidak terpengaruh extra time | TERVERIFIKASI | AddExtraTime endpoint hanya menarget `Status == "InProgress"` (baris 4898 AssessmentAdminController.cs) |

**Score:** 9/9 truths didukung oleh kode — 5 di antaranya butuh verifikasi human untuk konfirmasi perilaku runtime

---

## Artifact yang Diperlukan

### Plan 302-01

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `wwwroot/css/site.css` | Skip link CSS dan focus outline styling | TERVERIFIKASI | `.skip-link` (baris 23, 36), `.skip-link:focus-visible` (baris 36), `.exam-protected :focus-visible` (baris 43) |
| `Views/CMP/StartExam.cshtml` | Skip link HTML, auto-focus di performPageSwitch(), tabindex pada question cards | TERVERIFIKASI | `<a href="#mainContent" class="skip-link">` baris 10; `id="mainContent"` baris 68; `firstCard` logic baris 961-964 |

### Plan 302-02

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `Models/AssessmentSession.cs` | ExtraTimeMinutes nullable int property | TERVERIFIKASI | Ditemukan di baris 162 |
| `Controllers/AssessmentAdminController.cs` | AddExtraTime endpoint | TERVERIFIKASI | Method di baris 4876; validasi `minutes < 5 || minutes > 120 || minutes % 5 != 0` di baris 4878; SignalR `SendAsync("ExtraTimeAdded")` di baris 4904 |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | Tombol dan modal Extra Time | TERVERIFIKASI | `bi-clock-history` tombol di baris 119-121; modal `#extraTimeModal` di baris 1385; fungsi `addExtraTime()` di baris 1346 |
| `Hubs/AssessmentHub.cs` | Timer check dengan ExtraTimeMinutes | TERVERIFIKASI | Ditemukan di baris 209 dengan formula `(session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60` |

---

## Key Link Verification

| Dari | Ke | Via | Status | Detail |
|------|----|-----|--------|--------|
| `Views/CMP/StartExam.cshtml` | `wwwroot/css/site.css` | CSS class `skip-link` | TERHUBUNG | HTML menggunakan `class="skip-link"` yang didefinisikan di site.css |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | `Controllers/AssessmentAdminController.cs` | AJAX POST `AddExtraTime` | TERHUBUNG | `fetch` ke `Url.Action("AddExtraTime", "AssessmentAdmin")` di fungsi `addExtraTime()` baris 1346 |
| `Controllers/AssessmentAdminController.cs` | `Hubs/AssessmentHub` | `IHubContext SendAsync ExtraTimeAdded` | TERHUBUNG | `SendAsync("ExtraTimeAdded", minutes * 60)` di baris 4904 |
| `Views/CMP/StartExam.cshtml` | `Hubs/AssessmentHub` | `SignalR connection.on ExtraTimeAdded` | TERHUBUNG | Handler `window.assessmentHub.on('ExtraTimeAdded', ...)` di baris 1248 |

---

## Data-Flow Trace (Level 4)

| Artifact | Variabel Data | Sumber | Menghasilkan Data Real | Status |
|----------|---------------|--------|----------------------|--------|
| `StartExam.cshtml` timer | `timerStartRemaining` | `REMAINING_SECONDS_FROM_DB` (dari server ViewBag) + update SignalR `ExtraTimeAdded` | Ya — ViewBag dihitung dari DB dengan ExtraTimeMinutes, SignalR handler mengupdate secara real-time | FLOWING |
| `AssessmentMonitoringDetail.cshtml` extra time | `minutes` dari dropdown | AJAX POST ke AddExtraTime, disimpan ke DB | Ya — DB query update ExtraTimeMinutes di AssessmentSessions | FLOWING |
| `CMPController.cs` durationSeconds | `assessment.ExtraTimeMinutes` | DB query `AssessmentSessions` via EF Core | Ya — nullable int dari kolom DB riil | FLOWING |

---

## Anti-Pattern Check

| File | Pattern Dicek | Temuan | Severity |
|------|---------------|--------|----------|
| `Views/CMP/StartExam.cshtml` | TODO/placeholder/return null | Tidak ditemukan di logika baru | Bersih |
| `Controllers/AssessmentAdminController.cs` | Hardcoded empty return | Tidak ada — endpoint meng-query DB dan return data nyata | Bersih |
| `wwwroot/css/site.css` | `outline: none` yang mematikan focus | Tidak diperiksa secara spesifik — tetapi `.exam-protected :focus-visible` menambahkan outline, menunjukkan tidak ada penghapusan global | Info |
| `Views/CMP/StartExam.cshtml` | Anti-copy handler memblokir Tab/Arrow | SUMMARY mengkonfirmasi audit: hanya Ctrl+C/A/U/S/P yang diblokir | Bersih |

---

## Verifikasi Human Diperlukan

### 1. Skip Link Muncul Saat Tab

**Test:** Buka halaman ujian aktif sebagai peserta. Tekan tombol Tab pada keyboard.
**Expected:** Skip link "Lewati ke konten utama" muncul di pojok kiri atas dengan latar biru dan teks putih.
**Why human:** Perilaku CSS `:focus-visible` dan transisi posisi dari `left: -9999px` ke `left: 8px` hanya dapat dikonfirmasi via browser rendering.

### 2. Skip Link Memindahkan Focus ke Area Soal

**Test:** Saat skip link muncul, tekan Enter atau Space.
**Expected:** Halaman scroll ke area soal, focus berpindah ke div `#mainContent`, soal pertama terlihat di viewport.
**Why human:** Verifikasi perpindahan focus DOM dan scroll behavior membutuhkan browser interaktif.

### 3. Auto-Focus Saat Pindah Halaman

**Test:** Pada halaman ujian multi-halaman, klik tombol Next untuk pindah halaman. Jangan klik apapun setelah itu.
**Expected:** Card soal pertama di halaman baru mendapat focus otomatis (outline biru terlihat) tanpa perlu klik mouse.
**Why human:** Perilaku `programmatic focus management` di `performPageSwitch()` membutuhkan browser.

### 4. Extra Time Real-Time SignalR

**Test:** Buka dua browser — satu sebagai HC di AssessmentMonitoringDetail, satu sebagai peserta di halaman ujian aktif. HC klik "Tambah Waktu", pilih 15 menit, klik konfirmasi.
**Expected:** Timer di browser peserta bertambah 15 menit dalam 1-2 detik tanpa peserta melakukan refresh.
**Why human:** Verifikasi SignalR broadcast real-time membutuhkan dua sesi browser aktif simultan dengan assessment yang sedang berjalan.

---

## Ringkasan Gap

Tidak ada gap teknis yang ditemukan. Semua artifact ada, substansial, dan terhubung dengan benar. Data mengalir dari DB ke UI via jalur yang tepat.

A11Y-03 dan A11Y-04 tidak diimplementasikan sesuai keputusan D-18 dan D-19 yang terdokumentasi di CONTEXT.md — ini bukan gap melainkan keputusan desain yang disengaja.

Status `human_needed` disebabkan oleh 4 perilaku runtime yang hanya dapat diverifikasi melalui browser interaktif: (1) tampilan visual skip link saat focus, (2) perpindahan focus via skip link, (3) auto-focus saat pindah halaman, dan (4) update timer real-time via SignalR.

---

_Verified: 2026-04-07T12:00:00Z_
_Verifier: Claude (gsd-verifier)_
