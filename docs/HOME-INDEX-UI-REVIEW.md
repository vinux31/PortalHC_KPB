# UI Review — Home/Index Dashboard

**Tanggal:** 2026-03-23
**URL:** http://localhost:5277/Home/Index
**Role yang diaudit:** Admin

---

## Skor Ringkasan

| Pilar | Skor | Keterangan |
|-------|------|------------|
| Copywriting | 3/4 | Bahasa campuran ID/EN, beberapa label kurang konsisten |
| Visuals | 3/4 | Layout bersih, card well-structured, icon konsisten |
| Color | 3/4 | Palet solid, hero gradient menarik, beberapa kontras bisa ditingkatkan |
| Typography | 3/4 | Hierarki jelas, Inter font bagus, sizing minor issue |
| Spacing | 4/4 | Spacing konsisten, responsive breakpoints tertangani |
| Experience Design | 3/4 | Flow jelas, empty state tertangani, beberapa area bisa lebih informatif |

**Overall: 19/24**

---

## 1. Copywriting (3/4)

### Temuan Positif
- Greeting personal "Selamat Malam, Admin KPB!" — sentuhan humanis
- Label progress jelas: "2 / 10 completed", "0 / 0 deliverables approved"
- Alert sertifikat expired dengan CTA "Lihat Detail" — actionable

### Temuan Negatif
- **Bahasa campuran:** "Competency Development Portal", "Competency Management Portal", "Assessment", "Progress Overview", "Upcoming Events" semua dalam bahasa Inggris. Sementara UI lain menggunakan Bahasa Indonesia ("Kelola Data", "Panduan", "Ingat saya di perangkat ini"). Sebaiknya pilih satu bahasa secara konsisten.
- **"N/A" untuk unit** — Admin tidak punya unit, tapi menampilkan "N/A" terlihat kurang polished. Pertimbangkan untuk menyembunyikan badge jika null.
- **Inkonsistensi elemen teks:** Card pertama pakai `<small>`, card kedua pakai `<h6>`, card ketiga pakai `<small>`. Walaupun visual mirip, semantik HTML tidak konsisten.

### Rekomendasi
1. Tentukan bahasa utama UI — jika Bahasa Indonesia, terjemahkan "Progress Overview" → "Ringkasan Progres", "Upcoming Events" → "Agenda Mendatang"
2. Sembunyikan badge unit jika nilainya null daripada menampilkan "N/A"
3. Seragamkan elemen HTML untuk label card (semua `<small>` atau semua `<span>`)

---

## 2. Visuals (3/4)

### Temuan Positif
- Hero section gradient menarik dengan rounded corners (24px) — modern
- Avatar placeholder dengan icon user — clean
- Card menu dengan hover effect `translateY(-4px)` — interaktif
- Icon Bootstrap Icons konsisten di semua card
- Shadow halus (`shadow-sm`) memberikan depth tanpa berlebihan

### Temuan Negatif
- **Hero avatar hanya icon generik** — tidak ada foto atau inisial user (padahal navbar sudah menampilkan inisial "AK"). Inkonsistensi antara hero dan navbar.
- **Card CMP ketiga** memiliki background yang sedikit berbeda (bg-info lebih terang) dibanding dua card lainnya — visual weight tidak seimbang
- **Empty state "Upcoming Events"** hanya icon + teks, bisa lebih engaging

### Rekomendasi
1. Gunakan inisial user di hero avatar (sama seperti navbar) untuk konsistensi
2. Tambahkan ilustrasi ringan atau sub-teks pada empty state events

---

## 3. Color (3/4)

### Temuan Positif
- Hero gradient `#667eea → #764ba2` (biru-ungu) — profesional dan distinctive
- Warna progress bar semantik: primary (CDP), success (Assessment), warning (Coaching)
- Alert expired menggunakan merah/pink — tepat untuk peringatan
- Card icon background menggunakan `bg-opacity-10` — subtle dan elegan

### Temuan Negatif
- **Kontras hero badge** — teks putih di atas `rgba(255,255,255,0.25)` background. Readability bisa berkurang pada monitor tertentu.
- **Coaching 0% berwarna orange/warning** — semantik kurang tepat, 0% bukan "warning", lebih ke "not started". Pertimbangkan warna netral (gray) untuk 0%.
- **Tanggal di hero** menggunakan `opacity-75` — bisa kurang terbaca

### Rekomendasi
1. Naikkan opacity badge hero ke `rgba(255,255,255,0.35)` atau tambahkan backdrop-blur
2. Gunakan warna gray/muted untuk progress 0% dan warna warning hanya saat ada data aktual

---

## 4. Typography (3/4)

### Temuan Positif
- Font Inter — modern, professional, sangat baik untuk dashboard
- Hierarki jelas: hero greeting 2.5rem/800 → card labels small/semibold → muted helper text
- Font weights bervariasi dengan tepat (300-800)

### Temuan Negatif
- **Hero greeting 2.5rem** bisa overflow pada nama panjang di layar kecil (responsive breakpoint hanya turun ke 1.75rem)
- **Footer copyright "v1.2"** — versi lama, seharusnya mengikuti versi milestone saat ini

### Rekomendasi
1. Tambahkan `text-truncate` atau `font-size: clamp()` pada greeting untuk nama sangat panjang
2. Update versi di footer agar dinamis atau sesuai versi aktual

---

## 5. Spacing (4/4)

### Temuan Positif
- Bootstrap grid `g-4` konsisten untuk gap antar card
- Hero padding `3rem 2.5rem` — nyaman, tidak cramped
- Progress items dengan `mb-4` memberikan breathing room
- Responsive breakpoints tertangani: padding hero berkurang di mobile
- Card padding `py-4` pada menu cards — cukup ruang untuk click target

### Tidak ada temuan negatif signifikan.

---

## 6. Experience Design (3/4)

### Temuan Positif
- **Dashboard informatif** — greeting + role + unit + tanggal + progress + events — semua info penting ada
- **Alert sertifikat expired** muncul kontekstual dengan link langsung ke halaman renewal
- **Card navigasi** sebagai shortcut ke fitur utama — mengurangi klik
- **Empty state** tertangani dengan pesan "Tidak ada event mendatang"
- **Notification badge (10)** di navbar — user tahu ada yang perlu diperhatikan

### Temuan Negatif
- **Progress Overview untuk Admin** menampilkan data personal (0/0 deliverables) yang kurang relevan. Admin mungkin lebih butuh summary organisasi.
- **Tidak ada quick action** — dashboard hanya informatif, tidak ada tombol aksi langsung (misal "Mulai Assessment", "Lihat Coaching")
- **Upcoming Events kosong** mengambil ruang besar (col-lg-8) tanpa konten — proporsi tidak seimbang dengan Progress Overview (col-lg-4) yang berisi data

### Rekomendasi
1. Pertimbangkan dashboard view berbeda untuk role Admin vs Worker (Admin: summary organisasi, Worker: progress personal)
2. Tambahkan CTA button di card menu atau progress section
3. Swap proporsi kolom ketika events kosong, atau collapse events section

---

## Top 3 Perbaikan Prioritas

1. **Konsistensi bahasa** — pilih satu bahasa (ID atau EN) untuk semua label UI di dashboard
2. **Hero avatar inisial** — gunakan inisial user (seperti navbar) bukan icon generik, untuk konsistensi visual
3. **Sembunyikan badge "N/A"** — jangan tampilkan unit badge jika user tidak punya unit assignment

---

## Catatan Teknis Minor

| Item | File | Baris | Detail |
|------|------|-------|--------|
| Inkonsistensi HTML semantik | `Views/Home/Index.cshtml` | 55, 65, 75 | Card 1 & 3 pakai `<small>`, card 2 pakai `<h6>` |
| Mixed icon libraries | `Views/Home/Index.cshtml` + `_Layout.cshtml` | - | Hero pakai Font Awesome (`fas`), card pakai Bootstrap Icons (`bi`). Pilih satu. |
| Copyright year hardcoded | `Views/Account/Login.cshtml` | - | "© 2025" — seharusnya dinamis |
| CSS comment bahasa campur | `wwwroot/css/home.css` | 29 | Layout comment: "Sedikit styling agar Navbar terlihat 'mahal'" di `_Layout.cshtml` |
