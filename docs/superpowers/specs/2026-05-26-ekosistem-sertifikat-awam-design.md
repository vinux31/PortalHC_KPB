# Ekosistem Sertifikat — Versi Awam (HC non-IT)

**Tanggal**: 2026-05-26
**Author**: Rino (via Claude Opus 4.7, brainstorming skill)
**Target file**: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`
**Audience**: Manager / HC non-IT (paham aplikasi, bukan developer)
**Companion doc (teknis)**: `docs/sertifikat-ecosystem/index.html` (v1.0, 1355 baris, untuk developer)

---

## 1. Tujuan

Bikin satu file HTML self-contained yang menjelaskan struktur, sistem, logic, dan arsitektur ekosistem sertifikat Portal HC KPB dalam bahasa awam untuk HC non-IT. File ini bersanding dengan `index.html` (teknis developer) di folder yang sama. HC bisa baca dari atas ke bawah dalam satu sesi dan memahami:

- Apa itu sertifikat di Portal HC dan kenapa penting
- Dari mana asal sertifikat (2 sumber)
- Perjalanan sertifikat dari terbit sampai renewal
- Kapan sertifikat tidak terbit (3 alasan)
- Status sertifikat dan kapan berubah
- Siapa pegang apa (6 peran + hak akses)
- Peta menu aplikasi (4 modul)

## 2. Non-Goals

- **Tidak** mengulang detail teknis `index.html` (endpoint, controller, file:line, ER diagram detail, SQL, audit bug)
- **Tidak** menampilkan bug/gap/security finding (HC tidak perlu tahu — drop total per Q6=A)
- **Tidak** menggantikan `index.html` — sifatnya komplementer, audience banner di top mengarahkan developer ke `index.html`
- **Tidak** bikin sidebar TOC (single-page scroll, narrative top-to-bottom)
- **Tidak** ada per-page deep dive berdasarkan endpoint — peta menu cukup level "modul + 1 kalimat fitur"

## 3. Tech Stack

| Item | Pilihan | Alasan |
|---|---|---|
| File format | Single HTML, no build | Konsisten dengan `index.html`, mudah distribute |
| CSS framework | Bootstrap 5.3.0 (CDN) | Sama dengan `index.html` |
| Icons | Bootstrap Icons 1.11.0 (CDN) | Sama dengan `index.html` |
| Diagram engine | Mermaid 10.x (CDN) | Konsisten `index.html` (Q4=a) |
| Lang attr | `id` (Indonesian) | CLAUDE.md aturan |
| Theme | Light + dark toggle | Reuse pattern `index.html` |
| Layout | Single-column scroll, `max-width: 900px` center | Narrative storytelling (Q3=c) |
| Highlight.js | **Tidak dipakai** | Tidak ada code block teknis |

## 4. Konten — 8 Section

### §1 Apa & Kenapa (~150 kata)

Definisi awam sertifikat di Portal HC: "Bukti resmi bahwa pekerja sudah punya kompetensi tertentu (lulus assessment atau selesai training), dipakai untuk tracking pengembangan kompetensi, evidence coaching, dan syarat naik level/role di KPB."

Kenapa penting:
- Compliance pengembangan SDM KPB
- Evidence sah kompetensi pekerja per jenis training
- Basis perhitungan renewal cycle (kompetensi tidak permanent, perlu refresh)

**4 mini card** (di-card Bootstrap, h3 angka + caption kecil):
- **2** Sumber sertifikat
- **4** Status sertifikat
- **6** Peran sistem
- **30 hari** Threshold notifikasi pre-expired

### §2 Ekosistem 4-Kotak (~120 kata)

Mermaid `flowchart LR`:
```
Sumber [Assessment Online + Training Manual]
  → Penyimpanan [Database Portal HC]
  → Status [Aktif / Akan Expired / Expired / Permanent]
  → Notifikasi [Pekerja + HC dapat alert]
```

1 paragraf pendek per kotak:
- **Sumber** — 2 jalur: online (auto dari assessment lulus) + manual (HC upload bukti training external)
- **Penyimpanan** — Database menyimpan tanggal terbit, valid sampai kapan, nomor, link file PDF
- **Status** — Status tidak disimpan; dihitung sistem real-time dari tanggal kadaluarsa + jenis sertifikat
- **Notifikasi** — Sistem kirim notif otomatis 30 hari sebelum expired ke pekerja terkait dan HC

### §3 Perjalanan Sertifikat (~250 kata)

Mermaid `flowchart TD` — alur happy path:
```
Pekerja ikut Assessment/Training
  → Selesai/Submit
  → Lulus?
    YA → Sertifikat Terbit (auto atau manual upload)
      → Status Aktif
      → 30 hari sebelum expired → Notifikasi (pekerja + HC)
      → Renewal (training/assessment baru)
      → Sertifikat Baru (terhubung ke sertifikat lama via "renewal chain")
    TIDAK → 3 alasan (lihat sub-list)
```

Highlight 2 jalur paralel di narasi:
- **Otomatis**: pekerja lulus assessment online → sistem auto-generate nomor sertifikat format `KPB/{NOMOR-URUT}/{ROMAWI-BULAN}/{TAHUN}` (contoh `KPB/15/V/2026`)
- **Manual**: HC upload bukti sertifikat external (training di luar sistem) → nomor di-input manual oleh HC

**Sub-section: "Kapan sertifikat TIDAK terbit"** — 3 alasan (HC sering bingung, ini bukan bug):
1. **Gagal lulus** — Skor di bawah passing percentage → sertifikat tidak terbit, status assessment "Failed"
2. **Lulus tapi flag mati** — Assessment di-konfigurasi tanpa flag "Generate Certificate" (misal: latihan/simulasi tanpa sertifikat resmi) → lulus tapi tidak ada sertifikat
3. **Masih nunggu penilaian essay** — Lulus pilihan ganda tapi ada soal essay yang belum dinilai HC → status "Pending Grading", sertifikat menyusul setelah essay dinilai

### §4 Status & Kapan Berubah (~180 kata)

**4 kartu warna** (Bootstrap card grid 4-kolom responsive):

| Status | Warna | Kapan Masuk | Arti | Aksi HC |
|---|---|---|---|---|
| **Aktif** | Hijau | Sertifikat valid, > 30 hari sebelum expired | Pekerja tetap kompeten, no action | Monitor saja |
| **Akan Expired** | Kuning | ≤ 30 hari menuju tanggal expired | Notif otomatis aktif, pekerja perlu siap-siap renewal | Schedule training renewal |
| **Expired** | Merah | Tanggal expired sudah lewat | Kompetensi expired, perlu renewal segera | Trigger Renewal Certificate |
| **Permanent** | Biru | Jenis sertifikat = "Permanent" (training tertentu) | Tidak pernah expired | No action |

Catatan teknis ringan (1 paragraf):
> Status bukan kolom database — dihitung sistem real-time setiap kali halaman dibuka, berdasarkan tanggal kadaluarsa + jenis sertifikat. Artinya: Anda tidak perlu update status manual; sistem otomatis menggeser sertifikat dari Aktif → Akan Expired → Expired sesuai waktu.

### §5 Renewal — Kenapa & Bagaimana (~180 kata)

**Kenapa renewal perlu**: Kompetensi tidak permanent. Sertifikat tertentu (misal K3, safety, sertifikasi profesi) wajib refresh periodik agar pekerja tetap kompeten dan compliant.

**Kapan trigger**:
- **Otomatis** — 30 hari sebelum `ValidUntil` (tanggal kadaluarsa), sistem kirim notifikasi ke pekerja + HC
- **Manual** — HC/Admin proaktif via menu "Renewal Certificate" sebelum notif keluar

**Siapa boleh trigger**: Hanya **Admin (L1)** dan **HC (L2)**. Peran lain tidak punya akses menu Renewal Certificate.

Mermaid `flowchart LR`:
```
Sertifikat Lama (mau expired)
  → Form Renewal (input oleh HC/Admin)
  → Training/Assessment baru dilakukan pekerja
  → Sertifikat Baru terbit
  → Terhubung ke Sertifikat Lama (renewal chain)
```

**Renewal chain** (1 paragraf awam):
> Sertifikat baru hasil renewal "diingat" sistem terhubung ke sertifikat lama. Manfaatnya: HC bisa lihat riwayat lengkap kompetensi pekerja (sertifikat ke-1 → renewal ke-1 → renewal ke-2, dst). Sertifikat lama yang sudah di-renew otomatis dianggap "sudah ditangani" — tidak masuk daftar outstanding meskipun statusnya Expired.

### §6 Peran & Hak Akses (~200 kata)

Tabel **6 peran** × **4 kapabilitas** (sumber: `index.html` §6 line 614, fix nama vs draft awal):

| Peran | Level | Lihat | Buat | Edit | Hapus | Scope |
|---|---|---|---|---|---|---|
| **Admin** | L1 | ✓ semua | ✓ | ✓ | ✓ | Full (semua section, semua role) |
| **HC** | L2 | ✓ semua | ✓ | ✓ | ✓ | Full (sama dengan Admin) |
| **Manager** | L3 | ✓ semua | ✗ | ✗ (kecuali field tertentu) | ✗ | Full lihat, terbatas tulis |
| **SectionHead** | L4 | ✓ section saja | ✗ | ✗ | ✗ | Hanya pekerja di section yang sama |
| **Coach** | L5 | ✓ coachee map atau diri sendiri | ✗ | ✗ | ✗ | Dual-mode (mapped coachees vs own) |
| **Coachee** | L6 | ✓ diri sendiri | ✗ (kecuali submit exam) | ✗ | ✗ | Hanya sertifikat pribadi |

1 kalimat per peran di bawah tabel:
- **Admin/HC** — Operator sistem, akses penuh semua fitur sertifikat
- **Manager** — Pimpinan unit, bisa lihat semua sertifikat untuk reporting
- **SectionHead** — Atasan section, lihat sertifikat pekerja di section-nya saja
- **Coach** — Pembimbing, lihat sertifikat coachee yang di-assign + diri sendiri
- **Coachee** — Pekerja akhir, lihat dan unduh sertifikat pribadi saja

### §7 Peta Menu Aplikasi (~250 kata)

**4 modul** (Bootstrap card grid 2x2 atau 4-kolom):

**1. CMP — Competency Management Program**
- Menu utama: Records, Budget Training, Certificate, Submit Exam, Export Records
- Di sini Anda bisa: lihat daftar sertifikat pekerja, kelola anggaran training, lihat detail sertifikat per pekerja, ikut assessment online (Coachee), export Excel
- Akses: semua role (dengan scope sesuai peran)

**2. CDP — Competency Development Program**
- Menu utama: Certification Management, Export Sertifikat Excel
- Di sini Anda bisa: dashboard utama tracking sertifikat (status, expired, renewal outstanding), export laporan sertifikat per kriteria
- Akses: semua role (dengan scope sesuai peran)

**3. Admin Panel — Kelola Data**
- Menu utama: Manage Assessment, Renewal Certificate, Add/Edit Training, Finalize Essay Grading
- Di sini Anda bisa: setup assessment baru (soal, passing score, flag generate cert), trigger renewal sertifikat, tambah/edit data training manual, nilai essay yang pending grading
- Akses: Admin + HC only

**4. Notifikasi**
- Menu utama: List notifikasi user
- Di sini Anda bisa: lihat alert pre-expired (30 hari sebelum), notif renewal triggered, notif sertifikat terbit
- Akses: semua role (notif personal per user)

Tiap card pakai Bootstrap Icon (misal `bi-clipboard-check` CMP, `bi-award` CDP, `bi-gear` Admin, `bi-bell` Notifikasi).

### §8 Glosarium (~100 kata)

Format `<dl class="row">` (Bootstrap definition list). 10-15 istilah:

- **CMP** — Competency Management Program; modul utama untuk pengelolaan sertifikat & assessment online
- **CDP** — Competency Development Program; modul dashboard tracking sertifikat
- **KKJ** — Kompetensi Kerja Jabatan; standar kompetensi per jabatan di KPB
- **PROTON** — Professional Refinery Operations Competency Development; program besar pengembangan kompetensi RU IV
- **Sertifikat Permanent** — Jenis sertifikat tanpa tanggal kadaluarsa
- **ValidUntil** — Tanggal kadaluarsa sertifikat (kolom database)
- **Renewal Chain** — Rantai sertifikat hasil renewal yang terhubung satu sama lain
- **Assessment** — Ujian online di Portal HC (pilihan ganda + essay)
- **Pending Grading** — Status assessment yang menunggu HC menilai essay
- **Generate Certificate flag** — Pengaturan apakah assessment menerbitkan sertifikat saat lulus
- **Section** — Unit kerja di KPB, basis scope L4 SectionHead
- **Mapped Coachee** — Pekerja yang di-assign ke seorang Coach via tabel mapping
- **L1-L6** — Level peran sistem (1 paling tinggi, 6 paling terbatas)
- **Nomor Sertifikat** — Format `KPB/{nomor-urut}/{romawi-bulan}/{tahun}`, auto untuk sertifikat assessment

## 5. Layout & Visual Detail

- **Audience banner top**: alert Bootstrap warna info, "Dokumen ini untuk Manager/HC non-IT. Versi teknis untuk developer: lihat [`index.html`](./index.html)."
- **Header**: judul "Ekosistem Sertifikat Portal HC KPB — Panduan Awam", subtitle "Untuk Manager & HC non-IT", tanggal, versi
- **Theme toggle**: top-right button (dark/light), reuse pattern `index.html`
- **Section divider**: horizontal rule + spacing antar section (mirror pola `index.html`)
- **Mini-nav sticky top**: bar horizontal sticky berisi 8 link section (§1-§8) di atas konten, sembunyikan di mobile (`d-none d-md-flex`). Bukan sidebar — tetap single-page scroll, nav hanya alat lompat cepat.
- **Footer**: link ke `index.html` (developer doc) + footer copyright PT Pertamina

## 6. File Path & Naming

- Path final: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`
- Bersanding dengan: `index.html`, `bug-findings.html` (existing)
- Tidak ada asset terpisah (single file, semua CSS inline atau CDN)

## 7. Estimasi & Acceptance Criteria

- **Estimasi**: ~500-700 baris HTML (jauh < `index.html` 1355 baris)
- **Acceptance criteria**:
  1. File `ekosistem-sertifikat.html` ada di `docs/sertifikat-ecosystem/`
  2. 8 section lengkap (§1-§8) sesuai breakdown
  3. 3 diagram Mermaid render (§2, §3, §5)
  4. 6 peran di §6 sesuai `index.html` line 614 (Admin/HC/Manager/SectionHead/Coach/Coachee, bukan nama draft awal)
  5. Sub "Kapan sertifikat TIDAK terbit" di §3 cover 3 alasan (Gagal/Lulus-tanpa-flag/Pending-Grading)
  6. Audience banner top arahkan ke `index.html`
  7. Light+dark theme toggle berfungsi
  8. Tidak ada section bugs/gap (drop total per Q6=A)
  9. Bahasa awam — tidak sebut endpoint, controller, file:line, SQL, DB schema column kecuali ValidUntil/CertificateType yang sudah masuk glosarium
  10. Single-page scroll, no sidebar TOC (mini-nav sticky opsional)
  11. Render OK di Chrome+Edge desktop (Playwright spot check)

## 8. Out of Scope (Eksplisit Bukan Bagian Spec Ini)

- Tidak generate PDF/print stylesheet (HTML web saja)
- Tidak bikin versi i18n English (Bahasa Indonesia only per CLAUDE.md)
- Tidak update `index.html` (file teknis tetap as-is, hanya jadi referensi)
- Tidak bikin landing page index baru untuk folder (ekosistem-sertifikat dan index berdiri sendiri)
- Tidak tambah Mermaid di §4 (pakai card grid)
- Tidak tambah screenshot UI aplikasi (text-based card untuk §7)

## 9. Risk & Mitigation

| Risk | Mitigation |
|---|---|
| HC pakai dokumen ini sebagai source-of-truth bug/gap (padahal kita drop total) | Audience banner: link ke `index.html` untuk detail teknis termasuk bug |
| Nomor 30 hari berubah di kode masa depan | Glosarium catat "30 hari (per implementasi 2026-05-26)"; revisi dokumen kalau threshold berubah |
| Nama 6 peran berubah di RBAC masa depan | Footer catat tanggal snapshot; revisi dokumen kalau RBAC matrix berubah |
| Mermaid load gagal di network offline | Tetap CDN (sama dengan `index.html`); kalau perlu offline → user copy seluruh folder ke local |
