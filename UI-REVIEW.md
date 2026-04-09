# UI Review — Home/Index Page

**Proyek:** PortalHC KPB — ASP.NET Core MVC Internal Portal Pertamina
**Halaman diaudit:** Home/Index (`Views/Home/Index.cshtml`)
**Tanggal audit:** 09 April 2026
**Baseline:** Standar 6-pilar abstrak (tidak ada UI-SPEC.md)
**Screenshots:** Tidak diambil (dev server tidak terdeteksi di port 3000, 5173, 8080, 5000)
**Mode audit:** Code-only (analisis Razor views, CSS, controller)

---

## Pillar Scores

| Pilar | Skor | Temuan Utama |
|-------|------|--------------|
| 1. Copywriting | 2/4 | Bahasa campur (Indonesia + Inggris); inkonsistensi elemen HTML pada label menu card |
| 2. Visuals | 3/4 | Hero section secara visual kuat; avatar hero memakai generic icon bukan inisial seperti navbar |
| 3. Color | 3/4 | Warna hero tidak terhubung ke design token; penggunaan multi-warna semantik sudah tepat |
| 4. Typography | 3/4 | Skala font size terkontrol; `<small>` vs `<h6>` pada menu card inkonsisten secara semantik |
| 5. Spacing | 4/4 | Semua spacing pakai Bootstrap utility scale; tidak ada arbitrary value; sangat konsisten |
| 6. Experience Design | 2/4 | Tidak ada loading state; progress card tidak menangani kasus 0 total; label sub-progress tidak terlokalisasi |

**Total: 17/24**

---

## Top 3 Priority Fixes

1. **Bahasa campur di navigasi dan konten dashboard** — Pengguna internal Pertamina mendapat pengalaman tidak konsisten saat setengah konten dalam Bahasa Indonesia dan setengahnya dalam Bahasa Inggris. Dampak: kesan tidak profesional pada portal resmi perusahaan. Perbaikan: di `_Layout.cshtml` ganti "My Profile" → "Profil Saya", "Settings" → "Pengaturan", "Logout" → "Keluar"; di `Index.cshtml` ganti "Upcoming Events" → "Kegiatan Mendatang", "Progress Overview" → "Ringkasan Progres", dan sub-label "deliverables approved / completed / submitted" ke padanan Bahasa Indonesia.

2. **Progress card tidak menangani state 0 total** — Jika user belum memiliki assignment CDP, assessment, atau coaching, progress card menampilkan "0 / 0 deliverables approved" yang terkesan broken dan membingungkan. Dampak: user baru melihat dashboard yang terkesan rusak tanpa panduan aksi selanjutnya. Perbaikan: tambahkan kondisi `@if (Model.Progress.CdpTotal == 0)` di `Index.cshtml` untuk menampilkan pesan kontekstual seperti "Belum ada track CDP yang ditugaskan".

3. **Label menu card menggunakan elemen HTML tidak konsisten** — Kartu "Assessment" (baris 65) memakai `<h6>` sedangkan dua kartu lainnya memakai `<small>`. Ini menyebabkan ketebalan teks tidak seragam pada tiga kartu navigasi utama. Dampak: grid navigasi terlihat tidak rapi secara tipografi. Perbaikan: standarisasi ketiga label ke `<p class="mb-0 fw-semibold text-dark small text-center">` di `Index.cshtml` baris 55, 65, 75.

---

## Detailed Findings

### Pilar 1: Copywriting (2/4)

**Masalah 1 — Bahasa campur di dropdown navigasi (Severity: Tinggi)**

File: `Views/Shared/_Layout.cshtml` baris 157–170

Dropdown user menampilkan label Bahasa Inggris:
- Baris 157: "My Profile"
- Baris 158: "Settings"
- Baris 170: "Logout"

Sementara konten utama halaman dalam Bahasa Indonesia ("Selamat Pagi", "Tidak ada event mendatang", dll). Ini adalah portal internal resmi Pertamina — konsistensi bahasa adalah keharusan, bukan opsional.

**Masalah 2 — Label section dashboard dalam Bahasa Inggris**

File: `Views/Home/Index.cshtml` baris 87 dan 133

- Baris 87: "Progress Overview"
- Baris 133: "Upcoming Events"

**Masalah 3 — Label sub-progress tidak terlokalisasi**

File: `Views/Home/Index.cshtml` baris 99, 110, 121

- Baris 99: "deliverables approved"
- Baris 110: "completed"
- Baris 121: "submitted"

Semua dalam Bahasa Inggris tanpa penjelasan kontekstual.

**Yang sudah baik:**
- Salam waktu ("Selamat Pagi/Siang/Sore/Malam") dinamis dan berbasis waktu WIB — excellent.
- Empty state Upcoming Events: "Tidak ada event mendatang" — sudah Bahasa Indonesia.
- Pesan error/notifikasi sertifikat dari controller sudah dalam Bahasa Indonesia.

---

### Pilar 2: Visuals (3/4)

**Masalah 1 — Avatar hero menggunakan generic icon, bukan inisial**

File: `Views/Home/Index.cshtml` baris 15–17

Hero section menampilkan `<i class="fas fa-user">` (ikon generik). Sementara navbar sudah menggunakan inisial nama pengguna yang dipersonalisasi (`@displayInitials` di `_Layout.cshtml:150`). Ketidakkonsistenan ini melemahkan kesan personal dari hero greeting yang sudah menyebut nama pengguna secara langsung.

Perbaikan: Hitung inisial dari `Model.CurrentUser.FullName` di view dan tampilkan sebagai teks di dalam `.hero-avatar`, menggantikan ikon generik.

**Masalah 2 — Tidak ada active state pada menu card**

File: `wwwroot/css/home.css` baris 65–72

Hover state sudah ada (`translateY(-4px)`), namun tidak ada CSS `:active` state. Pengguna tidak mendapat konfirmasi visual instan saat klik sebelum navigasi terjadi.

Perbaikan: Tambahkan `.menu-card:active { transform: translateY(0); box-shadow: 0 4px 8px rgba(0,0,0,0.08) !important; }`.

**Yang sudah baik:**
- Hero section dengan gradient ungu memiliki focal point yang kuat dan langsung menyambut pengguna secara personal.
- Layout tiga kartu menu navigasi sudah jelas, konsisten, dan mudah dipindai secara visual.
- Progress bar dengan warna berbeda per metrik (biru/hijau/kuning) sudah tepat secara hierarki warna semantik.
- Event item card memiliki hover effect yang menandai elemen sebagai clickable.
- Grid 2 kolom pada Upcoming Events efisien untuk ruang yang tersedia di col-lg-8.

---

### Pilar 3: Color (3/4)

**Masalah 1 — Warna hero section tidak terhubung ke design token**

File: `wwwroot/css/home.css` baris 9

```css
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
```

Warna `#667eea` (biru-ungu) dan `#764ba2` (ungu) tidak terhubung ke `var(--bs-primary)` atau CSS custom property apapun. Hero section secara visual tampak terpisah dari sistem warna Bootstrap primary yang digunakan di seluruh elemen lain portal. Jika warna primary berubah, hero tidak akan ikut berubah.

**Masalah 2 — Hardcoded `#fff` pada event item card**

File: `wwwroot/css/home.css` baris 87

`.event-item-card { background: #fff }` — hardcoded, tidak menggunakan `var(--bs-white)` atau `var(--bs-body-bg)`.

**Yang sudah baik:**
- Penggunaan warna semantik sudah tepat dan konsisten: success (Assessment), warning (Coaching), primary (CDP).
- `bg-opacity-10` pada ikon kartu memberikan tint tipis yang elegan tanpa overuse warna.
- Jumlah elemen yang menggunakan `bg-primary` terkontrol (8 kemunculan, semuanya kontekstual — tidak dekoratif semata).
- Badge count pada header Upcoming Events menggunakan `bg-primary` dengan tepat sebagai elemen data.

---

### Pilar 4: Typography (3/4)

**Masalah 1 — Inkonsistensi elemen HTML pada label menu card**

File: `Views/Home/Index.cshtml` baris 55, 65, 75

```
Baris 55:  <small class="text-center mb-0 text-dark fw-semibold">Competency Development Portal</small>
Baris 65:  <h6 class="text-center mb-0 text-dark">Assessment</h6>
Baris 75:  <small class="text-center mb-0 text-dark fw-semibold">Competency Management Platform</small>
```

`<h6>` memiliki default `font-weight: bold` dan ukuran sedikit lebih besar dari `<small>`. Selain itu kartu "Assessment" tidak memiliki `fw-semibold` seperti dua kartu lainnya — menghasilkan perbedaan ketebalan visual yang tidak disengaja.

**Masalah 2 — Custom rem values tidak selaras dengan skala Bootstrap**

File: `wwwroot/css/home.css` baris 33, 39, 46, 139

- `font-size: 2rem` (hero avatar)
- `font-size: 2.5rem` (hero greeting)
- `font-size: 1.1rem` (hero subtitle)
- `font-size: 1.75rem` (hero greeting mobile)

Nilai `1.1rem` dan `1.75rem` tidak ada dalam skala Bootstrap `fs-1` sampai `fs-6`. Gunakan `fs-4` (1.5rem) atau `fs-5` (1.25rem) sebagai pendekatan yang lebih dekat untuk menghindari nilai non-standar.

**Distribusi ukuran font yang ditemukan:**

| Kelas/Nilai | Konteks |
|-------------|---------|
| `fs-1` | Ikon empty state event |
| `fs-3` | Ikon menu card (3x) |
| `fs-4` | Logo navbar |
| `fs-5` | Hari di hero |
| `fs-6` | Tanggal di hero |
| `2.5rem` (custom) | Hero greeting |
| `1.75rem` (custom, mobile) | Hero greeting mobile |
| `1.1rem` (custom) | Hero subtitle |

Total 8 ukuran — batas ideal untuk halaman tunggal adalah 5-6 ukuran.

**Yang sudah baik:**
- Hanya 2 font weight (`fw-bold`, `fw-semibold`) digunakan di seluruh Index — sangat terkontrol.
- Hierarki bold/semibold diterapkan dengan benar: heading bold, nilai metrik bold, label semibold.
- Font Inter dimuat dari Google Fonts dengan weight lengkap (300–800).

---

### Pilar 5: Spacing (4/4)

Spacing pada halaman ini adalah yang paling konsisten di antara semua pilar. Semua nilai menggunakan Bootstrap utility scale standar tanpa exception.

**Distribusi spacing di Index.cshtml:**

| Kelas | Frekuensi | Catatan |
|-------|-----------|---------|
| `mb-2` | 5x | Spacing dalam progress item |
| `py-4` | 4x | Card body padding vertikal |
| `p-3` | 4x | Ikon dan event card |
| `mb-3` | 4x | Event card dan progress item |
| `mb-4` | 3x | Antar progress item |
| `py-3` | 2x | Card header |
| `mt-2` | 2x | Badge tanggal dan empty state |
| `p-2` | 1x | Event icon |
| `gap-4` | 1x | Hero flex gap |
| `gap-3` | 1x | Event item gap |

Tidak ditemukan arbitrary values (`[24px]`, `[1.3rem]`) di views maupun CSS home.css.

Satu catatan minor: `style="height: 8px;"` pada tiga elemen progress bar (baris 96, 107, 118) adalah inline style yang bisa dipindahkan ke `.progress-item .progress { height: 8px; }` di `home.css`. Namun ini tidak mengganggu konsistensi spacing secara material.

---

### Pilar 6: Experience Design (2/4)

**Masalah 1 — Tidak ada loading state (Severity: Tinggi)**

File: `Views/Home/Index.cshtml` — tidak ada skeleton loader, spinner, atau placeholder.

Halaman ini memuat data dari beberapa async query di controller (progress, upcoming events, cert alert counts). Jika salah satu query lambat karena beban database atau network, pengguna melihat halaman kosong atau setengah render tanpa indikasi bahwa sistem sedang bekerja. Untuk portal internal dengan data production, ini adalah risiko UX nyata terutama bagi user dengan koneksi terbatas.

**Masalah 2 — Progress card tidak menangani kasus 0 total**

File: `Views/Home/Index.cshtml` baris 91–122

Tidak ada kondisi untuk user yang belum memiliki data. Hasil yang terlihat user baru:
- "0 / 0 deliverables approved" — terkesan broken
- "0 / 0 completed" — ambigu
- "0 / 0 submitted" — tanpa konteks

Perbaikan:
```razor
@if (Model.Progress.CdpTotal == 0)
{
    <small class="text-muted fst-italic">Belum ada track CDP yang ditugaskan</small>
}
else
{
    @* progress bar yang ada *@
}
```

**Masalah 3 — Menu card tidak memiliki aria-label eksplisit**

File: `Views/Home/Index.cshtml` baris 50, 60, 70

Tiga link navigasi utama tidak memiliki `aria-label`. Meskipun screen reader bisa membaca teks dari elemen anak, menambahkan `aria-label="Buka Competency Development Portal"` dst. pada masing-masing `<a>` akan memperjelas tujuan navigasi.

**Yang sudah baik:**
- Empty state Upcoming Events sudah ada dengan ikon visual + teks deskriptif ("Tidak ada event mendatang").
- Progress bar sudah memiliki `aria-valuenow`, `aria-valuemin`, `aria-valuemax` — aksesibilitas ARIA sudah benar.
- Conditional cert alert banner hanya muncul untuk role HC/Admin dan hanya saat ada data — tidak mengganggu user reguler.
- AOS animation menghormati `prefers-reduced-motion: reduce` — implementasi best practice aksesibilitas.
- Impersonation read-only mode memblokir submit actions dengan badge "Mode Read-Only" dan toast AJAX 403 — sangat thoughtful.
- TempData alerts (Success/Warning/Error) sudah dismissible dengan ikon kontekstual yang tepat.

---

## Files Audited

| File | Deskripsi |
|------|-----------|
| `Views/Home/Index.cshtml` | Halaman utama dashboard (176 baris) |
| `Views/Shared/_Layout.cshtml` | Layout template (295 baris) |
| `wwwroot/css/home.css` | Custom CSS halaman Home (156 baris) |
| `wwwroot/css/site.css` | Custom CSS global (71 baris) |
| `Controllers/HomeController.cs` | Controller logic (379 baris) |

Registry audit: Tidak dijalankan (tidak ada `components.json` — proyek tidak menggunakan shadcn).
