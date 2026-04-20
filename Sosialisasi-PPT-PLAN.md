# Planning — Sosialisasi Aplikasi PortalHC KPB (Interactive HTML Deck)

**File output:** `wwwroot/documents/guides/Sosialisasi-Aplikasi-PortalHC-KPB.html`
**Format:** HTML slide deck interaktif (1 file self-contained)
**Tanggal plan:** 17 April 2026

---

## 1. Tujuan Dokumen

PPT interaktif ini bertujuan memperkenalkan dan mensosialisasikan aplikasi **HC Portal KPB** (Human Capital Portal — Kilang Pertamina Balikpapan) kepada seluruh pengguna internal Pertamina KPB. Fokusnya:

1. **Memperkenalkan** apa itu HC Portal KPB, latar belakang, dan manfaatnya
2. **Mendemonstrasikan** fitur utama secara visual & interaktif
3. **Mengedukasi** pengguna tentang role mereka masing-masing & cara mengakses
4. **Memotivasi** adopsi: kenapa ini penting dibanding cara lama (manual/Excel)

---

## 2. Target Audiens

Tiga lapisan audiens potensial — deck harus bisa melayani keduanya:

| Segmen | Kebutuhan | Cara Di-handle di Deck |
|--------|-----------|------------------------|
| **Manajemen (Direktur, VP, Manager)** | Big picture, manfaat bisnis, ROI | Slide eksekutif di awal (Tujuan, Manfaat, Metrics) |
| **HC / Admin / IT** | Paham semua fitur, cara kelola data | Slide mendalam di bagian "Fitur untuk Pengelola" |
| **Coach / Supervisor / Coachee / Pekerja** | Cara menjalankan tugas sehari-hari | Slide "Role Journey" per peran, alur step-by-step |

**Pendekatan:** Deck disusun progresif — 15–20 slide inti yang semua orang lihat, lalu ada **slide interaktif "Pilih Role Anda"** yang menampilkan journey berbeda per role (navigasi conditional via JavaScript).

---

## 3. Outline Slide (Draft — Untuk Didiskusikan)

Total estimasi: **22 slide** (durasi presentasi ±25–30 menit, bisa dipercepat dengan skip optional slide).

### BAGIAN A — PEMBUKA (3 slide)

| # | Slide | Konten Utama | Elemen Interaktif |
|---|-------|--------------|-------------------|
| 1 | **Cover** | Judul "Sosialisasi Aplikasi HC Portal KPB", logo Pertamina, tagline, versi, tanggal | Tombol "Mulai Presentasi" dengan animasi entry |
| 2 | **Agenda** | Daftar isi 6 bagian utama | Klik item → loncat ke bagian tsb. |
| 3 | **Latar Belakang** | Kenapa HC Portal dibangun — permasalahan proses manual, kebutuhan centralized system | Timeline animated (sebelum → sesudah) |

### BAGIAN B — PENGENALAN (3 slide)

| # | Slide | Konten | Interaktif |
|---|-------|--------|-----------|
| 4 | **Apa itu HC Portal KPB?** | Definisi, 2 modul utama (CMP + CDP), siapa penggunanya | Tab-switcher CMP vs CDP |
| 5 | **Tujuan & Manfaat** | 4 pilar: Transparansi, Efisiensi, Akurasi Data, Integrasi | Cards yang bisa di-hover untuk detail |
| 6 | **Arsitektur Sistem (Simplified)** | Diagram: User → Portal → Database + LDAP Auth | Diagram interaktif (hover node → penjelasan) |

### BAGIAN C — ROLE & HAK AKSES (2 slide)

| # | Slide | Konten | Interaktif |
|---|-------|--------|-----------|
| 7 | **Struktur Role** | 6 level hierarchy (Admin → HC → Manager → Section Head → Coach → Coachee) | Hierarchy chart clickable → detail akses per role |
| 8 | **Pilih Role Anda** | Branching: user klik role mereka → sisa deck menyesuaikan highlight | **Core interactive slide** — tombol 6 role |

### BAGIAN D — FITUR UTAMA (8 slide)

| # | Slide | Modul | Konten | Interaktif |
|---|-------|-------|--------|-----------|
| 9 | **Modul CMP — Competency Management** | CMP | Overview CMP: Assessment, Dokumen KKJ, Records, Analytics, Budget Training | Accordion expandable |
| 10 | **Assessment — Ujian Kompetensi** | CMP | 6 kategori (OJ, IHT, Licencor, OTS, HSSE, Proton), 2 tipe (Standard, Pre-Post Test), PG/Essay | Interactive mock exam screen (simulasi klik soal) |
| 11 | **Analytics Dashboard** | CMP | Item Analysis, Gain Score, Pass Rate | Dummy chart yang bisa di-hover (Chart.js) |
| 12 | **Modul CDP — Continuous Development** | CDP | Coaching Proton, Plan IDP, Deliverable, Evidence, Certification | Accordion expandable |
| 13 | **Coaching Proton Journey** | CDP | Flow Tahun 1 → Tahun 2 → Tahun 3 (Panelman / Operator) | Interactive stepper horizontal |
| 14 | **Kelola Data (Admin/HC)** | Admin | 12 sub-fitur: Worker, Assessment, Category, KKJ/CPDP, Workload, Audit Log, Maintenance, Impersonation, dll | Grid icons — klik → tooltip detail |
| 15 | **Fitur Keamanan** | Security | Anti-Copy, LDAP, Audit Log, Role-based access, Maintenance Mode | Cards dengan icon shield |
| 16 | **Notifikasi & Real-time Monitoring** | Ops | Notification bell, Assessment monitoring live, Import Excel | Animated mock notification |

### BAGIAN E — DEMO & CARA AKSES (3 slide)

| # | Slide | Konten | Interaktif |
|---|-------|--------|-----------|
| 17 | **Cara Mengakses Portal** | URL Production (`appkpb.pertamina.com/KPB-PortalHC`), login via LDAP Pertamina, akun test untuk demo | QR code + clickable link |
| 18 | **Demo Login & Navigasi** | Screenshot/GIF/video embedded: login → dashboard → menu | Embedded video atau screenshot carousel |
| 19 | **Panduan & Support** | Link ke 7 dokumen panduan yang sudah ada, kontak HC/IT | Link cards clickable |

### BAGIAN F — PENUTUP (3 slide)

| # | Slide | Konten | Interaktif |
|---|-------|--------|-----------|
| 20 | **Roadmap Pengembangan** | Versi 1.0 → 2.0, fitur yang sudah rilis vs coming soon | Timeline animated |
| 21 | **Quiz Interaktif (Opsional)** | 3–5 soal pilihan ganda ringan tentang isi presentasi untuk engagement | **Interactive quiz** dengan scoring |
| 22 | **Penutup & Q&A** | Thank you, kontak, "Mulai gunakan sekarang!" | Tombol CTA ke portal |

---

## 4. Elemen Interaktif yang Direncanakan

Inilah fitur yang membuat ini "deck interaktif", bukan sekadar PPT statis:

| Fitur Interaktif | Teknologi | Tujuan |
|------------------|-----------|--------|
| **Keyboard navigation** (← → Space Esc) | Vanilla JS | Navigasi seperti PowerPoint |
| **Progress bar** di bawah | CSS + JS | User tahu posisinya |
| **Slide counter** (5 / 22) | JS | Orientation |
| **Agenda clickable** | Anchor JS | Loncat langsung ke bagian |
| **Tab switcher** (CMP vs CDP) | JS toggle | Lihat info sesuai kebutuhan |
| **Accordion** (expand fitur) | CSS details/summary | Detail on-demand tanpa overload |
| **Hover tooltips** pada role/fitur | CSS/JS | Detail tambahan |
| **Role selector** (branching slide 8) | JS state | Journey berbeda per audiens |
| **Mock UI screens** (fake login, fake exam) | HTML/CSS | Demo tanpa buka app |
| **Mini quiz** (slide 21) | JS scoring | Engagement & retensi |
| **Chart interaktif** (Chart.js) | Chart.js CDN | Demo Analytics Dashboard |
| **Dark mode toggle** (opsional) | CSS variables | Kenyamanan presentasi |
| **Print-to-PDF** | @media print | Cetak sebagai handout |
| **Fullscreen mode** | Fullscreen API | Mode presentasi |

---

## 5. Style Visual

| Aspek | Rekomendasi |
|-------|-------------|
| **Framework** | Pure HTML + CSS + Vanilla JS (+ Chart.js via CDN untuk chart saja) — self-contained 1 file, tidak perlu Reveal.js |
| **Warna utama** | Biru `#1565c0` (konsisten dengan panduan existing), aksen hijau `#2e7d32`, merah `#c62828`, orange `#e65100` |
| **Warna Pertamina** | Bisa ditambah sebagai aksen: merah `#ed1c24`, hijau `#009640`, biru navy `#002e6d` |
| **Font** | Segoe UI / Tahoma (konsisten dengan dokumen existing) |
| **Ukuran slide** | 16:9 (1280×720 atau responsive full viewport) |
| **Transisi** | Smooth slide (fade + slight slide) via CSS transitions |
| **Icon** | Unicode emoji + inline SVG (tidak butuh icon library) |
| **Konsistensi** | Style ikut panduan existing (`Panduan-Penggunaan-Website-HC-Portal-KPB.html`) |

---

## 6. Hal yang Perlu Didiskusikan / Dikonfirmasi

Sebelum saya mulai build, ada beberapa keputusan yang perlu input dari Anda:

### 🔴 Keputusan Prioritas Tinggi

1. **Scope slide** — Apakah 22 slide terlalu banyak? Mau dipangkas ke ±15 (versi ringkas) atau oke dengan lengkap?
2. **Target audiens utama** — Ini akan dipresentasikan ke *manajemen* dulu, ke *user/karyawan* dulu, atau dipakai untuk keduanya?
3. **Durasi presentasi** — ±15 menit, ±30 menit, atau self-service (user buka sendiri)?
4. **Fitur yang di-highlight** — Apakah semua fitur (CMP + CDP + Admin) perlu dimasukkan, atau fokus ke 1 modul saja?

### 🟡 Keputusan Visual

5. **Warna dominan** — Biru `#1565c0` (ikut existing), atau ganti ke warna corporate Pertamina?
6. **Logo** — Apakah ada file logo Pertamina/KPB yang mau dipasang? (saya belum nemu di project, kalau ada file gambarnya kirim path-nya)
7. **Screenshot** — Pakai screenshot asli dari aplikasi (perlu saya generate/ambil manual) atau cukup mockup HTML/CSS?

### 🟢 Keputusan Fitur Interaktif

8. **Quiz di akhir** — Mau ada quiz interaktif (slide 21) atau skip?
9. **Role branching (slide 8)** — Mau ada fitur "Pilih Role Anda" yang ubah journey, atau semua user lihat konten yang sama?
10. **Demo video** — Mau ada video embedded (perlu file) atau cukup screenshot carousel?

### 🔵 Keputusan Teknis

11. **Lokasi file** — Simpan di `wwwroot/documents/guides/` (ikut konvensi existing, bisa diakses via URL) atau di folder lain?
12. **Offline vs CDN** — Butuh Chart.js untuk chart → apakah boleh pakai CDN, atau harus fully offline (chart pakai SVG statis)?
13. **Export** — Perlu ada tombol "Export PDF" / "Print handout"?

---

## 7. Estimasi Effort

| Tahap | Estimasi |
|-------|----------|
| Finalisasi plan & outline (diskusi ini) | 30 menit |
| Design + skeleton HTML + navigation + styling | 1–2 jam |
| Isi konten slide (22 slide) | 2–3 jam |
| Elemen interaktif (quiz, chart, tab, role selector) | 1–2 jam |
| Polish, testing lintas browser, responsive check | 30 menit |
| **Total** | **±5–7 jam development** |

Bisa dikerjakan bertahap — per bagian (A, B, C, D, E, F) dengan review di tiap bagian.

---

## 8. Langkah Selanjutnya

1. ✅ **Plan ini saya serahkan ke Anda** untuk review
2. ⏳ **Diskusi keputusan** di bagian 6 — jawab pertanyaan atau koreksi outline
3. ⏳ **Revisi plan** berdasarkan feedback Anda
4. ⏳ **Approval** — Anda kasih go untuk mulai build
5. ⏳ **Build bertahap** — saya kerjakan per bagian, review per bagian

---

*Dokumen ini adalah draft untuk diskusi. Setelah final, plan ini bisa diarsipkan atau dihapus.*
