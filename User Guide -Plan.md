# Halaman "Panduan & Bantuan" — Implementation Plan

Membuat halaman **Panduan Pengguna (User Guide)** dan **FAQ** yang interaktif untuk HC Portal. Layout utama menggunakan **card grid** yang mengelompokkan panduan per modul/fitur, serta section **FAQ** di bagian bawah dengan gaya **line text** (bukan card). Bahasa Indonesia, hanya bisa diakses setelah login, link di navbar.

---

## Proposed Changes

### 1. Controller — HomeController

#### [MODIFY] [HomeController.cs](file:///c:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/HomeController.cs)

Menambahkan 1 action method baru `Guide()`, ditempatkan setelah `Index()` action:

```csharp
public async Task<IActionResult> Guide()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userRoles = await _userManager.GetRolesAsync(user);
    ViewBag.UserRole = userRoles.FirstOrDefault() ?? "User";
    return View();
}
```

---

### 2. View — Halaman Panduan & Bantuan

#### [NEW] [Guide.cshtml](file:///c:/Users/Administrator/Desktop/PortalHC_KPB/Views/Home/Guide.cshtml)

#### 2.1. Hero Section
- Judul **"Panduan & Bantuan"** dengan gradient konsisten dashboard
- Subtitle: *"Pelajari cara menggunakan setiap fitur HC Portal"*
- **Role badge** menunjukkan role user saat ini

#### 2.2. Search Bar
- Client-side filter untuk memfilter card panduan dan FAQ sekaligus
- Placeholder: *"Cari panduan... (contoh: assessment, coaching, IDP)"*

#### 2.3. Card Grid — Panduan Per Modul

Layout **responsive grid** (3 kolom desktop, 2 tablet, 1 mobile). Setiap card mewakili satu modul/page. Klik card → expand/collapse accordion di bawahnya yang menampilkan step-by-step.

| Card | Icon | Deskripsi Singkat | Akses |
|------|------|-------------------|-------|
| 📚 **CMP** | `bi-journal-bookmark-fill` | Competency Management: KKJ, CPDP Mapping, Assessment, Sertifikat, Training Records | Semua role |
| 📈 **CDP** | `bi-graph-up-arrow` | Career Development: Plan IDP, Coaching Proton, Deliverable, Evidence, Dashboard | Semua role |
| 👤 **Akun & Profil** | `bi-person-circle` | Kelola profil, ganti password, settings | Semua role |
| 🗄️ **Kelola Data** | `bi-database-fill` | Import data Proton, override, sinkronisasi | **Admin & HC only** |
| ⚙️ **Admin Panel** | `bi-gear-wide-connected` | Kelola pekerja, KKJ/CPDP files, mapping, assessment, audit log | **Admin & HC only** |

> Card "Kelola Data" dan "Admin Panel" disembunyikan via `@if (ViewBag.UserRole == "Admin" || ViewBag.UserRole == "HC")` di Razor.

#### 2.4. Konten Dalam Setiap Card (Expanded)

Saat card diklik, accordion di bawahnya expand menampilkan step-by-step dengan numbered steps:

**Card 1 — CMP:**
| Step | Judul | Deskripsi |
|------|-------|-----------|
| 1 | Akses CMP | Klik menu "CMP" di navbar |
| 2 | Library KKJ | Lihat daftar KKJ berdasarkan posisi |
| 3 | CPDP Mapping | Lihat pemetaan CPDP |
| 4 | Mulai Assessment | Klik "Mulai" pada assessment tersedia → exam terbuka |
| 5 | Timer & Navigasi | Perhatikan timer, gunakan panel navigasi soal |
| 6 | Submit Assessment | Klik "Submit", konfirmasi pengiriman |
| 7 | Lihat Hasil | Tab "Results" untuk skor dan detail |
| 8 | Sertifikat | Download sertifikat untuk assessment lulus |
| 9 | Training Records | Lihat riwayat pelatihan |
| 10 | Records Team | *(Atasan/HC)* Lihat records anggota tim |

**Card 2 — CDP:**
| Step | Judul | Deskripsi |
|------|-------|-----------|
| 1 | Akses CDP | Klik menu "CDP" di navbar |
| 2 | Plan IDP | Lihat silabus/training yang perlu diselesaikan |
| 3 | Coaching Proton | Masuk ke modul coaching terstruktur |
| 4 | Kelola Deliverable | Tambah, edit, hapus deliverable |
| 5 | Upload Evidence | Upload bukti penyelesaian deliverable |
| 6 | Approval Flow | Submit deliverable → menunggu approval Coach/Atasan |
| 7 | Dashboard CDP | *(HC/Admin)* Monitoring progress seluruh pekerja |

**Card 3 — Akun & Profil:**
| Step | Judul | Deskripsi |
|------|-------|-----------|
| 1 | Lihat Profil | Klik avatar kanan atas → "My Profile" |
| 2 | Edit Informasi | Ubah nama, posisi, unit kerja |
| 3 | Ganti Password | Settings → password lama & baru |
| 4 | Logout | Avatar → "Logout" |

**Card 4 — Kelola Data (Admin & HC only):**
| Step | Judul | Deskripsi |
|------|-------|-----------|
| 1 | Akses Kelola Data | Klik "Kelola Data" di navbar |
| 2 | Import Data Proton | Sinkronisasi data pekerja dari Proton |
| 3 | Override Data | Ubah/override data yang diimport |
| 4 | Validasi Data | Pastikan data benar setelah import |

**Card 5 — Admin Panel (Admin & HC only):**
| Step | Judul | Deskripsi |
|------|-------|-----------|
| 1 | Kelola Pekerja | Tambah/edit/hapus pekerja, import Excel |
| 2 | KKJ Matrix Files | Upload dan kelola file KKJ per jabatan |
| 3 | CPDP Files | Upload dan kelola file CPDP |
| 4 | Coach-Coachee Mapping | Mapping Coach-Coachee, aktivasi/nonaktivasi |
| 5 | Kelola Assessment | Buat assessment, kelola paket soal, assign |
| 6 | Monitoring Assessment | Progress assessment seluruh pekerja |
| 7 | Audit Log | Log aktivitas sistem & user |

---

### 3. FAQ Section — Line Text Style

FAQ ditempatkan di **paling bawah halaman**, **terpisah** dari card grid. Menggunakan layout **line text** — bukan card, melainkan:

- Setiap pertanyaan = **satu baris teks bold** yang bisa diklik (expand/collapse)
- Jawaban muncul di bawahnya sebagai **paragraf teks biasa** dengan indentasi
- Separator: garis tipis (`border-bottom: 1px solid #e9ecef`) antar pertanyaan
- Dikelompokkan per kategori dengan **heading teks** (bukan card header)

**Contoh visual:**

```
─────────────────────────────────────
  Frequently Asked Questions (FAQ)
─────────────────────────────────────

🔐 Akun & Login

▸ Bagaimana cara login ke HC Portal?
  Buka halaman login, masukkan email dan password...
  ─────────────────────────────────
▸ Bagaimana jika lupa password?
  Hubungi Admin/HC untuk reset password...
  ─────────────────────────────────
▸ Bagaimana cara mengganti password?
  Masuk ke Settings melalui menu dropdown...
  ─────────────────────────────────

📝 Assessment

▸ Apa itu assessment?
  Assessment adalah ujian kompetensi...
  ─────────────────────────────────
▸ Bagaimana cara mengerjakan assessment?
  Buka CMP → klik "Mulai"...
  ─────────────────────────────────
```

#### Daftar FAQ Lengkap:

**🔐 Akun & Login**
1. Bagaimana cara login ke HC Portal?
2. Bagaimana jika lupa password?
3. Siapa yang bisa mendaftarkan akun baru?
4. Bagaimana cara mengganti password?
5. Kenapa saya tidak bisa mengakses menu tertentu?

**📝 Assessment**
6. Apa itu assessment?
7. Bagaimana cara mengerjakan assessment?
8. Apakah ada batas waktu mengerjakan?
9. Bisakah saya mengerjakan ulang assessment?
10. Bagaimana cara melihat hasil assessment?
11. Bagaimana cara download sertifikat?

**🎯 CDP & Coaching**
12. Apa perbedaan CMP dan CDP?
13. Apa itu IDP (Individual Development Plan)?
14. Bagaimana cara melihat Plan IDP?
15. Apa itu Coaching Proton?
16. Bagaimana cara upload evidence?
17. Siapa yang bisa approve deliverable?
18. Bagaimana cara melihat coaching progress saya?

**📊 KKJ & CPDP**
19. Apa itu KKJ Matrix?
20. Di mana saya bisa melihat KKJ saya?
21. Apa itu CPDP?
22. Bagaimana cara upload file KKJ/CPDP? *(Admin/HC)*

**🛠️ Admin & Kelola Data**
23. Bagaimana cara menambahkan pekerja baru?
24. Bagaimana cara import data pekerja massal?
25. Bagaimana cara mapping Coach-Coachee?
26. Apa itu Audit Log?
27. Bagaimana cara sinkronisasi data dari Proton?

**❓ Umum**
28. Browser apa yang direkomendasikan?
29. Apakah bisa diakses dari HP?
30. Siapa yang harus dihubungi jika ada kendala?
31. Bagaimana cara mengetahui role saya?
32. Apakah data saya aman?

---

### 4. Styling

#### [NEW] [guide.css](file:///c:/Users/Administrator/Desktop/PortalHC_KPB/wwwroot/css/guide.css)

CSS terpisah (~300-400 lines), design system konsisten dengan `home.css`:

**Card Styling:**
- Glassmorphism cards dengan hover effect (lift + shadow)
- Icon besar di card header dengan gradient background
- Responsive grid: `grid-template-columns: repeat(auto-fill, minmax(320px, 1fr))`
- Card expand/collapse transition smooth

**FAQ Styling:**
- **Bukan card** — simple line text layout
- Pertanyaan: `font-weight: 600`, cursor pointer, padding vertikal
- Jawaban: `color: #6c757d`, padding-left indentasi, slide-down animation
- Separator: `border-bottom: 1px solid #e9ecef`
- Kategori heading: `font-size: 1.1rem`, uppercase, letter-spacing, margin-top
- Chevron icon (▸/▾) untuk indikator expand/collapse

**Umum:**
- Font Inter, AOS animations
- Step number badges dengan gradient
- Responsive & mobile-friendly
- Print-friendly `@media print`

---

### 5. Navigation

#### [MODIFY] [_Layout.cshtml](file:///c:/Users/Administrator/Desktop/PortalHC_KPB/Views/Shared/_Layout.cshtml)

Link **"Panduan"** di navbar, antara CDP dan Kelola Data:

```diff
 <li class="nav-item">
     <a class="nav-link text-dark" asp-controller="CDP" asp-action="Index">CDP</a>
 </li>
+
+ <li class="nav-item">
+     <a class="nav-link text-dark" asp-controller="Home" asp-action="Guide">
+         <i class="bi bi-question-circle me-1"></i>Panduan
+     </a>
+ </li>
 
 @if (User.IsInRole("Admin") || User.IsInRole("HC"))
```

---

## Ringkasan File Changes

| File | Action | Estimasi | Deskripsi |
|------|--------|----------|-----------|
| `HomeController.cs` | MODIFY | +8 lines | Tambah `Guide()` action |
| `Views/Home/Guide.cshtml` | **NEW** | ~500-600 lines | Halaman Panduan (card grid + FAQ line text) |
| `wwwroot/css/guide.css` | **NEW** | ~300-400 lines | Styling cards & FAQ |
| `Views/Shared/_Layout.cshtml` | MODIFY | +5 lines | Navbar link |

**Total: 2 file baru, 2 file dimodifikasi**

---

## Arsitektur Halaman

```
Guide.cshtml
├── Hero Section (judul + role badge)
├── Search Bar (filter cards & FAQ)
├── Card Grid (panduan per modul)
│   ├── Card: CMP (10 steps) ── expand/collapse
│   ├── Card: CDP (7 steps) ── expand/collapse
│   ├── Card: Akun & Profil (4 steps) ── expand/collapse
│   ├── Card: Kelola Data (4 steps) ── Admin/HC only
│   └── Card: Admin Panel (7 steps) ── Admin/HC only
├── ─── Separator ───
└── FAQ Section (line text style)
    ├── 🔐 Akun & Login (5 FAQ)
    ├── 📝 Assessment (6 FAQ)
    ├── 🎯 CDP & Coaching (7 FAQ)
    ├── 📊 KKJ & CPDP (4 FAQ)
    ├── 🛠️ Admin & Kelola Data (5 FAQ)
    └── ❓ Umum (5 FAQ)
```

---

## Verification Plan

### Build Test
```bash
cd c:\Users\Administrator\Desktop\PortalHC_KPB
dotnet build
```

### Manual Browser Verification

> [!IMPORTANT]
> Tidak ada unit test di proyek ini. Verifikasi manual via browser setelah `dotnet run`.

1. Login ke website
2. ✅ Link **"Panduan"** muncul di navbar
3. ✅ Halaman terbuka, Hero section + role badge tampil
4. ✅ **5 cards** tampil (3 untuk semua role, 2 untuk Admin/HC)
5. ✅ Klik card → accordion expand menampilkan steps
6. ✅ Card Admin/HC tersembunyi untuk role Coachee
7. ✅ **FAQ section** tampil di bawah dengan line text style
8. ✅ Klik pertanyaan FAQ → jawaban expand di bawahnya
9. ✅ Search bar memfilter card dan FAQ
10. ✅ Responsive design (resize ke mobile)
