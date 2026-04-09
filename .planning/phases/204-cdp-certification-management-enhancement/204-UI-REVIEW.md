# Phase 204 — UI Review

**Audited:** 2026-04-09
**Baseline:** Abstract 6-pillar standards (tidak ada UI-SPEC.md)
**Screenshots:** Tidak diambil — audit dilakukan berbasis kode dan page snapshot dari Playwright

---

## Pillar Scores

| Pillar | Score | Key Finding |
|--------|-------|-------------|
| 1. Copywriting | 4/4 | Semua label kontekstual dalam Bahasa Indonesia; CTA dan status badge tepat sasaran |
| 2. Visuals | 3/4 | Hierarki visual jelas, namun toggle berada di baris yang sama dengan 5 dropdown sehingga terasa padat di layar kecil |
| 3. Color | 3/4 | Satu hardcoded hex color (#6f42c1) di badge Assessment; warna lain menggunakan Bootstrap utilities secara konsisten |
| 4. Typography | 4/4 | Hanya menggunakan `fs-2 fw-bold`, `small`, dan body default — tidak ada proliferasi ukuran |
| 5. Spacing | 4/4 | Konsisten menggunakan Bootstrap spacing utilities (g-3, mb-4, py-4, ps-3, p-0); tidak ada arbitrary value |
| 6. Experience Design | 3/4 | Loading state ada, empty state ada, error modal ada — namun error pada filter AJAX (refreshTable) diam tanpa feedback ke pengguna |

**Overall: 21/24**

---

## Top 3 Priority Fixes

1. **Error AJAX filter diam tanpa feedback** — Jika `refreshTable` gagal (jaringan putus, server error), container hanya kembali dari loading state tanpa pesan apapun; pengguna menyangka filter berhasil padahal tabel tidak ter-update — Tambahkan fallback di blok `.catch` pada `refreshTable`: tampilkan alert atau pesan inline di dalam `container` menggunakan pola yang sama dengan error modal history (`alert alert-danger`).

2. **Hardcoded hex color untuk badge Assessment** — `style="background-color: #6f42c1 !important;"` di `_CertificationManagementTablePartial.cshtml` baris 79 tidak mengikuti Bootstrap token; jika tema diganti warna ini tidak ikut berubah — Ganti ke `<span class="badge bg-purple">` dengan CSS variable `--bs-purple`, atau gunakan kelas Bootstrap `badge` dengan utilitas warna yang sudah terdefinisi (misalnya `text-bg-purple` bila Bootstrap 5.3 dipakai).

3. **Toggle "Tampilkan Riwayat Renewal" tidak memiliki visual cue saat aktif** — Ketika toggle ON, baris renewed muncul dengan opacity 0.5 tetapi tidak ada indikator di filter bar bahwa mode "history mode" sedang aktif — Tambahkan teks kecil atau badge "History ON" di samping toggle saat checked, atau berikan warna berbeda pada label toggle saat aktif, supaya pengguna yang scroll ke bawah tahu filter sedang dalam kondisi non-default.

---

## Detailed Findings

### Pillar 1: Copywriting (4/4)

Semua string yang relevan untuk phase ini sudah kontekstual:

- Label toggle: "Tampilkan Riwayat Renewal" — deskriptif, tidak generik
- Empty state: "Belum ada data sertifikat" (`_CertificationManagementTablePartial.cshtml` baris 40) — jelas dan spesifik
- Error modal history: "Gagal memuat riwayat sertifikat. Periksa koneksi lalu coba lagi." (`CertificationManagement.cshtml` baris 350) — actionable dengan petunjuk konkret
- Loading modal: "Memuat riwayat sertifikat..." (`CertificationManagement.cshtml` baris 343) — informatif
- CTA: "Export Excel", "Kembali ke CDP", "Reset" — semua tepat konteks
- Status badge: "Aktif", "Expired", "Akan Expired", "Permanent" — konsisten dengan domain bahasa

Tidak ditemukan generic label seperti "Submit", "OK", atau "Click Here" di file yang diaudit.

### Pillar 2: Visuals (3/4)

Kekuatan:
- Summary cards dengan warna semantik (biru/hijau/amber/merah) memberikan focal point yang kuat di atas halaman
- Hierarki jelas: cards > filter bar > tabel, masing-masing dalam card dengan shadow-sm
- Icon Bootstrap di header (`bi-patch-check`) dan dalam summary cards memberikan anchor visual per kategori
- Badge status (`bg-success`, `bg-danger`) terbaca jelas di tabel

Temuan:
- Toggle switch diletakkan di `col-md-3` dalam baris yang sama dengan 5 dropdown `col-md-2` dan 1 reset `col-md-1`. Total grid = 5×2 + 1 + 3 = 14 kolom, melebihi 12. Bootstrap akan wrap, tetapi perilaku ini tidak konsisten di semua breakpoint dan mungkin menyebabkan toggle turun ke baris baru secara tidak terduga di layar sedang (tablet landscape).
- Saat toggle ON, baris renewed muncul redup (opacity 0.5) namun tidak ada penanda visual di area filter bahwa tampilan sedang dalam mode non-default. Pengguna bisa tidak sadar dan salah interpretasi data.

### Pillar 3: Color (3/4)

Temuan utama:
- `_CertificationManagementTablePartial.cshtml` baris 79: `style="background-color: #6f42c1 !important;"` — satu-satunya hardcoded hex di kedua file yang diaudit. Perlu diganti dengan Bootstrap token.

Penggunaan warna lain sudah benar:
- Summary cards: `text-primary`, `text-success`, `text-warning`, `text-danger` — semantic dan konsisten
- Badge: `bg-success`, `bg-danger`, `bg-warning text-dark`, `bg-secondary`, `bg-primary` — semua Bootstrap utilities
- Loading spinner: `text-primary` via CSS variable Bootstrap

Warna aksen (`text-primary`) digunakan dengan tepat: hanya pada angka summary card total dan link nama pekerja — tidak overused pada elemen dekoratif.

### Pillar 4: Typography (4/4)

Distribusi ukuran font di kedua file:
- `fs-2 fw-bold` — angka summary cards (emphasis utama)
- `small` (Bootstrap) — label filter, footer pagination, modal subtitle
- Body default — konten tabel
- `h2` — heading halaman

Hanya 3 level tipografi yang digunakan. Tidak ditemukan class `text-xs/sm/base/lg/xl` dari Tailwind (proyek menggunakan Bootstrap). Tidak ada proliferasi ukuran yang membingungkan hierarki.

### Pillar 5: Spacing (4/4)

Spacing konsisten menggunakan Bootstrap scale:
- Container: `py-4` (top/bottom padding halaman)
- Antar section: `mb-4` (summary cards, filter bar)
- Cards: `g-3` (grid gap summary cards), `g-2` (filter row gap)
- Tabel: `p-0` (card body), `ps-3` (kolom No dan Nama), `py-4` (empty state)
- Modal: `mx-3 mt-3` (error alert)

Tidak ditemukan arbitrary value `[Xpx]` atau `[Xrem]`. Semua spacing mengikuti Bootstrap 4/8-point grid.

### Pillar 6: Experience Design (3/4)

State yang sudah ditangani dengan baik:
- **Loading state:** `dashboard-loading` CSS class dengan spinner animasi (`::after` pseudo-element) diterapkan saat `refreshTable()` berjalan (`CertificationManagement.cshtml` baris 282, 302, 307)
- **Empty state:** "Belum ada data sertifikat" dengan colspan penuh, `text-center text-muted py-4` (`_CertificationManagementTablePartial.cshtml` baris 37-41)
- **Error state modal:** Error pada `openHistoryModal` fetch ditangkap dengan alert danger yang informatif (baris 350)
- **Disabled state:** Filter Unit dan Sub Kategori di-disable secara programatik saat belum ada parent dipilih (baris 111, 137); toggle di-reset saat Reset diklik

Gap yang ditemukan:
- **refreshTable catch block kosong** (`CertificationManagement.cshtml` baris 304-308): Hanya `console.error(e)` dan hapus class loading. Tidak ada feedback visual ke pengguna jika filter AJAX gagal. Pengguna melihat tabel lama tanpa indikasi ada masalah.
- **Export Excel tidak memiliki loading/error feedback:** `exportExcel()` hanya redirect `window.location.href` — jika server error, browser menampilkan halaman error generik tanpa context. Ini di luar scope Phase 204 tapi layak dicatat.
- Tidak ada konfirmasi untuk aksi destruktif — tidak relevan di halaman ini karena semua aksi bersifat read/download.

---

## Registry Safety

Shadcn tidak diinisialisasi di project ini (`components.json` tidak ditemukan). Registry audit dilewati.

---

## Files Audited

- `Views/CDP/CertificationManagement.cshtml` — View utama: layout, filter bar, summary cards, JS handler
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Partial tabel: baris sertifikat, badge, pagination, empty state
- `.planning/phases/204-cdp-certification-management-enhancement/204-01-SUMMARY.md`
- `.planning/phases/204-cdp-certification-management-enhancement/204-01-PLAN.md`
- `.planning/phases/204-cdp-certification-management-enhancement/204-CONTEXT.md`
