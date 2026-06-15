---
phase: 361-bypass-ui-b
reviewed: 2026-06-11T06:23:03Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Controllers/ProtonDataController.cs
  - Views/ProtonData/Override.cshtml
  - tests/e2e/proton-bypass.spec.ts
  - docs/SEED_JOURNAL.md
findings:
  critical: 0
  warning: 1
  info: 8
  total: 9
status: issues_found
---

# Phase 361: Code Review Report

**Reviewed:** 2026-06-11T06:23:03Z
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Review standard atas 4 file Phase 361 "Bypass UI (B)": perubahan controller (Override GET `ViewBag.AllCoaches` :242-248, `BypassPendingList` extended select :1542-1575), restrukturisasi `Override.cshtml` menjadi 2 tab Bootstrap + wizard 3-langkah + IIFE Tab2, spec Playwright baru, dan entry jurnal seed.

**Penilaian keseluruhan: solid.** Fokus keamanan ASVS L1 (T-361-08..12) terpenuhi:

- **XSS (innerHTML):** Semua data server yang dirender via string-concat innerHTML di Tab2 melewati `escB()` (nama, sourceTrack/targetTrack, targetUnit, reason, targetCoachNama, coacheeId di atribut data, recap wizard, sourceTahunKe/targetTahunKe/targetTrackName). Nilai numerik (`p.id`, `skorExam`, `progressApproved/Total`) berasal dari kolom int server. `showToast` memakai `textContent` untuk pesan. `escB`/`escHtml` tidak meng-escape `'`, tetapi semua injeksi atribut memakai double-quote — aman.
- **CSRF:** Keempat endpoint POST (`OverrideSave`, `BypassSave`, `BypassConfirm`, `BypassCancelPending`) ber-`[ValidateAntiForgeryToken]`; semua fetch POST klien mengirim header `RequestVerificationToken` dari `@Html.AntiForgeryToken()` (Override.cshtml:23).
- **Double-submit guard:** `wizSubmit`, `btnSaveOverride`, dan `postPendingAction` men-disable tombol selama request (catatan kecil di IN-05).
- **Stale-state:** Konfirmasi/batal divalidasi ulang server-side (service Phase 360); deep-link stale ditangani toast D-06 — tetapi ada satu cabang error yang menyesatkan (WR-01).
- **Bahasa Indonesia:** Seluruh copy user-facing berbahasa Indonesia.

Tidak ada temuan Critical. 1 Warning (cabang error deep-link menampilkan pesan keliru), 8 Info (semantik toast, duplikasi helper, timezone display, advisory prior-phase). Entry SEED_JOURNAL.md baris 168 (fixture 361) lengkap dan berstatus `cleaned` — sesuai SOP SEED_WORKFLOW.

## Warnings

### WR-01: Deep-link menampilkan toast "sudah diproses" yang keliru saat fetch pending gagal

**File:** `Views/ProtonData/Override.cshtml:742-750, 1124-1138`
**Issue:** `loadPendingPanel()` menangkap kegagalan fetch (network error, sesi expired yang redirect ke HTML login sehingga `resp.json()` throw, atau HTTP 500) dengan menampilkan alert error di container lalu `return` — tanpa sinyal ke pemanggil. `handleDeepLink()` tetap lanjut: `pendingRows` masih `[]`, `find()` gagal, dan user diberi toast **"Pending bypass sudah diproses atau tidak ditemukan."** padahal pending-nya mungkin masih ada — request-nya yang gagal. HC bisa salah menyimpulkan bypass sudah dikonfirmasi/dibatalkan oleh orang lain.
**Fix:** Kembalikan status sukses dari `loadPendingPanel()` dan hentikan deep-link bila gagal:
```javascript
async function loadPendingPanel() {
    var container = document.getElementById('pendingPanelContainer');
    try {
        var resp = await fetch(appUrl('/ProtonData/BypassPendingList'));
        if (!resp.ok) throw new Error('HTTP ' + resp.status);
        pendingRows = await resp.json();
    } catch (err) {
        container.innerHTML = '<div class="alert alert-danger mb-0">Terjadi kesalahan jaringan. Silakan coba lagi.</div>';
        return false;
    }
    // ... render ...
    return true;
}

// handleDeepLink:
var loaded = await loadPendingPanel();
if (!loaded) return; // jangan klaim "sudah diproses" saat request gagal
```

## Info

### IN-01: Variant toast `info` dipetakan ke kelas `text-bg-warning`

**File:** `Views/ProtonData/Override.cshtml:642`
**Issue:** `{ ..., warning: 'text-bg-warning', info: 'text-bg-warning' }` — variant `info` tampil kuning (warning). Toast stale deep-link D-06 memakai `'info'` sehingga terlihat seperti peringatan. Jika disengaja demi kontras, beri komentar; jika tidak, ini typo copy-paste.
**Fix:** `info: 'text-bg-info'` (atau komentar eksplisit bila kuning memang disengaja).

### IN-02: Duplikasi helper escape `escHtml` (Tab1) dan `escB` (Tab2)

**File:** `Views/ProtonData/Override.cshtml:630-637, 660-667`
**Issue:** Dua fungsi identik di dua IIFE. Dapat dimaklumi karena Tab1 dibungkus utuh tanpa diubah, tetapi helper escape adalah kandidat utama untuk dinaikkan ke scope top-level (seperti `showToast`) agar satu sumber kebenaran saat nanti perlu di-harden (mis. escape `'`).
**Fix:** Angkat satu fungsi escape ke top-level `<script>` bersama `showToast`; biarkan Tab1 mereferensikannya.

### IN-03: Listener `change` ganda pada `#wizTargetUnit`

**File:** `Views/ProtonData/Override.cshtml:724-726, 739, 984-989`
**Issue:** `wireCascade('wizTargetBagian','wizTargetUnit', ...)` menambah listener change pada unit (memanggil `validateStep`), dan loop `['wizTargetTrack','wizTargetUnit','wizCoach']` menambah listener kedua (`validateStep` + `buildRecap`). Per change, `validateStep` terpanggil dua kali. Tidak ada efek samping (idempotent), hanya kerja duplikat dan jejak maintenance.
**Fix:** Keluarkan `'wizTargetUnit'` dari array di :984, atau pindahkan `buildRecap` ke callback `wireCascade`.

### IN-04: `fmtTanggal` mem-parse timestamp UTC-naive sebagai waktu lokal

**File:** `Views/ProtonData/Override.cshtml:671-676`; `Controllers/ProtonDataController.cs:1565,1568`
**Issue:** `createdAt`/`tanggalExam` disimpan `DateTime.UtcNow` tetapi setelah dibaca EF ber-`Kind=Unspecified`, sehingga JSON tanpa sufiks `Z` dan `new Date(iso)` menafsirkannya sebagai waktu lokal. Tanggal "Dibuat"/"Tanggal Exam" tampil dalam tanggal UTC, bukan WIB — event yang terjadi 00:00–06:59 WIB tampil mundur satu hari. Pola pre-existing di seluruh app (display-only).
**Fix:** Serialisasi dengan sufiks UTC (mis. `DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc)` atau format `o` + `Z`) lalu biarkan `toLocaleDateString('id-ID')` mengonversi; atau format tanggal di server seperti pola `OverrideDetail` (`dd MMM yyyy HH:mm`).

### IN-05: `postPendingAction` re-enable tombol sebelum panel selesai reload + `pendingCount` tidak direset saat error

**File:** `Views/ProtonData/Override.cshtml:1063-1086, 747-751`
**Issue:** Urutan di `postPendingAction`: `btn.disabled = false` (:1081) dieksekusi **sebelum** `await loadPendingPanel()` (:1084), membuka jendela singkat double-click pada DOM lama (POST duplikat ditolak server karena state pending sudah berubah — tidak ada dampak data, hanya toast error membingungkan). Terpisah: saat `loadPendingPanel` gagal, badge `#pendingCount` tetap menampilkan angka lama di samping alert error.
**Fix:** Pindahkan re-enable tombol ke setelah `await loadPendingPanel()`, dan set `pendingCount` ke `'0'`/`'—'` di cabang catch.

### IN-06: (Advisory — kode Phase 360) `BypassSave` menulis `TempData["Warning"]` di endpoint JSON dan audit log tanpa status hasil

**File:** `Controllers/ProtonDataController.cs:1649-1654`
**Issue:** Di luar scope perubahan 361 (prior-phase), dicatat sebagai advisory: (a) `TempData["Warning"] = result.Message` pada endpoint yang dikonsumsi via fetch JSON — reminder sudah disampaikan via `showAttachPackageReminder` di JSON; TempData akan ikut ter-render di full page load berikutnya = notifikasi dobel. (b) Audit log `ProtonBypassSave` ditulis tanpa indikator sukses/gagal, berbeda dengan `BypassConfirm`/`BypassCancelPending` yang menyertakan `result.Success` — entry audit untuk attempt yang gagal terbaca seolah bypass terjadi.
**Fix:** Hapus baris TempData; sertakan `{(result.Success ? "berhasil" : result.Message)}` di deskripsi audit, konsisten dengan dua endpoint lainnya.

### IN-07: (Advisory — markup Tab1 prior-phase) Link evidence dibangun tanpa validasi skema URL

**File:** `Views/ProtonData/Override.cshtml:547`
**Issue:** `evidenceEl.innerHTML = '<a href="' + basePath + escHtml(data.evidencePath) + '"...'` — `escHtml` mencegah breakout atribut, tetapi tidak menetralkan skema (mis. `javascript:`). Risiko rendah karena `EvidencePath` digenerate server-side saat upload (bukan input user bebas) dan diprefix `basePath`. Tab1 dibungkus tanpa perubahan di fase ini — advisory saja.
**Fix:** Bila ingin defense-in-depth: validasi `data.evidencePath` diawali `/uploads/` sebelum dirender.

### IN-08: Spec e2e — locator toast berpotensi strict-mode flake; kredensial dev tertulis di komentar

**File:** `tests/e2e/proton-bypass.spec.ts:18, 170, 185, 205, 247`
**Issue:** (a) Assertion seperti `expect(page.locator('.toast.text-bg-success')).toBeVisible()` melanggar Playwright strict mode bila >1 toast sejenis aktif bersamaan — toast hidup 4 detik dan T3 menjalankan dua submit beruntun; saat ini lolos 6/6 karena timing, tetapi rapuh terhadap perubahan kecepatan eksekusi. (b) Komentar :18 menuliskan password dev lokal `123456` — sudah terdokumentasi sebagai kredensial dev-only, tetapi sebaiknya cukup merujuk helper/`reference_dev_credentials` tanpa menulis nilai password di file repo.
**Fix:** (a) Pakai `.last()` atau filter teks: `page.locator('.toast.text-bg-success').last()` / `page.locator('.toast', { hasText: '...' })`. (b) Ganti komentar menjadi rujukan ke helper auth.

---

_Reviewed: 2026-06-11T06:23:03Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
