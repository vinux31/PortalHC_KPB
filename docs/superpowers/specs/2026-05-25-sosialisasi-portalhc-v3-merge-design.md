# Sosialisasi PortalHC v3 — Merge HTML Style + PDF Data

**Tanggal:** 2026-05-25
**Status:** Design — pending user review
**Target file:** `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`
**Sumber data:** `docs/Sosialisasi PortalHC v2 — Slide Deck 2026.pdf` (18 page)

---

## Tujuan

Pertahankan **design + UI HTML existing** (gradient card, stepper, stairs, mockup, dll), tapi **ganti seluruh konten data** dengan data autoritatif dari PDF deck v2. Hasil: deck 23 slide yang konsisten dengan referensi PDF tapi nyaman dilihat di browser.

## Strategi

- **Strategi B** (pilihan user): PDF jadi backbone konten + slide operasional HTML yang relevan dipertahankan.
- **Data prioritas:** PDF autoritatif. Konflik → ikut PDF.
- **Style prioritas:** HTML autoritatif. Visual treatment, warna, layout, animasi.
- **Tanggal sosialisasi:** 25 Mei 2026 (override PDF "18 Mei").
- **Environment:** Production belum live, Development aktif (`http://10.55.3.3/KPB-PortalHC`).

## Keputusan utama (konflik data)

| Konflik | Keputusan |
|---|---|
| Kategori Assessment (HTML 6 vs PDF 2) | **Hybrid**: PDF "2 Jenis" jadi klasifikasi level atas + HTML "5 Kategori Umum" sebagai sub (Proton dipisah ke Bagian 2) |
| Role hierarki (HTML 6 grouped vs PDF 10 granular) | **Hybrid C**: tangga 6 level (visual HTML) + chip detail role per step (data PDF) |
| Platform (HTML 2 vs PDF 3) | **3 platform**: CMP + CDP + BP (Coming Soon) |
| Coaching framing (HTML generic vs PDF Dasar/Lanjutan/Mahir) | **Data PDF, style HTML**: dual-track + tabel 5 aspek |
| Kategori naming | "OJ" → **"OJT"** (On the Job Training) |

## Struktur Final — 23 Slide

Slide counter format: **"SLIDE X / 23"**. Label BAGIAN tampil di eyebrow header tiap slide, tidak di counter.

### Bagian Pengenalan (slide 1-6)

#### Slide 1 — Cover
- **Sumber data:** PDF page 1
- **Style:** HTML cover existing (logo box, gradient strip)
- **Konten:**
  - Logo box "HC" + "HC Portal / KPB"
  - Eyebrow: `🎯 Sosialisasi Aplikasi`
  - Title besar: `HC Portal KPB`
  - Subtitle: `Human Capital Portal — Kilang Pertamina Balikpapan`
  - **Bawah subtitle (baru):** `📅 Balikpapan · 25 Mei 2026`
  - **DROP:** 3 tag bawah (Competency / Continuous Dev / Analytics)

#### Slide 2 — Agenda
- **Sumber data:** HTML s2 (remap target)
- **Style:** HTML agenda-grid existing
- **6 item clickable navigasi:**
  ```
  01 Pengenalan          → slide 3
  02 Sistem Assessment   → slide 7
  03 Assessment Proton   → slide 11
  04 Coaching Proton     → slide 14
  05 Operasional         → slide 21
  06 Q&A                 → slide 23
  ```

#### Slide 3 — Latar Belakang
- **Sumber data:** HTML s3 apa adanya (PDF tidak punya)
- **Style:** HTML latar split-panel existing
- **Konten:** Sebelum (manual/excel/tersebar) vs Sesudah (digital/integrasi/RBAC). 5 bullet × 2 panel.

#### Slide 4 — Apa Itu HC Portal + 3 Prinsip
- **Sumber data:** PDF page 2
- **Style:** HTML definition-box + 3 card grid
- **Konten:**
  - Definition quote: "Sistem informasi berbasis web Tim Human Capital Kilang Pertamina Balikpapan untuk MENGELOLA · MENGEMBANGKAN · MENDAMPINGI kompetensi pekerja lewat tiga platform terpadu: CMP, CDP, BP."
  - 3 card prinsip:
    - 🎯 **Terpusat** — Satu portal untuk seluruh proses kompetensi & pengembangan
    - 📐 **Terstandar** — Kriteria, deliverable, sertifikasi mengacu standard KPB
    - 📊 **Terukur** — Skor, progress, level kompetensi tertrace per pekerja
  - **DROP:** 2 module card lama CMP+CDP (pindah ke slide 5)

#### Slide 5 — 3 Platform (CMP / CDP / BP)
- **Sumber data:** PDF page 3 BIG MENU
- **Style:** HTML module-card extended (3 card horizontal full detail)
- **Konten 3 card:**
  - **CMP — Competency Management Platform**
    - Deskripsi: "Pengelolaan kompetensi terintegrasi — penyusunan KKJ, IDP, pelaksanaan asesmen teknis & Safety."
    - Bullet: Assessment · Assessment Proton · Pre/Post Test · Sertifikasi
  - **CDP — Competency Development Platform**
    - Deskripsi: "Pembelajaran terstruktur untuk menutup gap kompetensi — blended Learning (Assignment, Coaching, Self Study)."
    - Bullet: Coaching Proton · IDP · Training Records
  - **BP — Business Partner** 🚧 COMING SOON (faded style, dashed border)
    - Deskripsi: "Modul HRBP — strategic partner HC & unit operasional untuk workforce planning, employee relations, & advisory."
    - Footer: "For Future — in roadmap"

#### Slide 6 — Struktur Role (10 role · 6 level)
- **Sumber data:** PDF page 4
- **Style:** HTML stairs ASC bottom-up + chip role granular
- **Konten 6 step (bawah ke atas):**
  ```
  L6 👨‍🎓 Coachee                          → Self assessment & IDP
  L5 [🎓 Coach] [👤 Supervisor]            → Coaching & review
  L4 [🏢 Section Head] [🧑‍💼 Sr Supervisor] → Section-level monitor
  L3 [👔 Direktur] [📌 VP] [🧭 Manager]    → Executive dashboard
  L2 👥 HC                                 → All section access
  L1 🛡 Admin                              → Full system control
  ```
- Label prefix **L1-L6** (sesuai PDF), bukan angka 1-6.
- Multi-role step = chip pill horizontal dalam satu step.
- Caption bawah: "⬅ Operational · · · Higher Authority ➡"

### BAGIAN 1 — Sistem Assessment (slide 7-10)

#### Slide 7 — 2 Jenis Assessment
- **Eyebrow:** `BAGIAN 1`
- **Sumber data:** PDF page 5 tabel
- **Style:** 2 kolom card 3-row baru (HTML style gradient)
- **Konten:**
  - Card kiri: **📊 Assessment Umum** — Kategori (Per batch unit operasi/durasi), Metode (Online MCQ, timer otomatis), Penilaian (Otomatis vs passing grade)
  - Card kanan: **🎓 Assessment Proton** — Kategori (Per track per tahun, Panel/Op Th 1-3), Metode (Online Th 1-2 + Interview offline Th 3), Penilaian (Otomatis + Manual panel)
  - Tip box bawah: "💡 Umum = evaluasi reguler · Proton = program 3 tahun"

#### Slide 8 — 5 Kategori Assessment Umum
- **Eyebrow:** `BAGIAN 1 · CMP`
- **Sumber data:** HTML s8 (drop kartu Proton, rename OJ→OJT)
- **Style:** HTML cat-grid existing
- **5 card:**
  - 🔧 **Assessment OJT** — On the Job Training, ujian kompetensi berbasis unit kerja (Alkylation, RFCC NHT, dll)
  - 🏫 **IHT** — In House Training, assessment terkait pelatihan internal perusahaan
  - 📜 **Licencor** — Training Licencor, lisensi & sertifikasi eksternal
  - 📍 **OTS** — On The Spot, assessment langsung di lapangan
  - ⚠️ **HSSE** — Mandatory Health, Safety, Security & Environment training
- Info-bar bawah: "📌 Assessment Proton dibahas di Bagian 2"

#### Slide 9 — Pre & Post Test (Gain Score)
- **Eyebrow:** `BAGIAN 1 · CMP`
- **Sumber data:** PDF page 6
- **Style:** Horizontal stepper 4 step + 2 metric card
- **Stepper:**
  1. 📋 **Pre Test** — Sebelum training, ukur baseline kompetensi
  2. 🎓 **Training** — Sesi pembelajaran (in-class atau on-the-job)
  3. ✅ **Post Test** — Setelah training, ujian paket soal sejenis
  4. 📊 **Gain Score** — Analisis selisih skor
- **2 metric card bawah:**
  - 📈 **Gain Score** — Selisih Post-Pre. Indikator efektivitas training per peserta & per kategori.
  - 🔍 **Item Analysis** — Per-soal: kesulitan, daya beda, distractor power. Bantu HC perbaiki paket soal.

#### Slide 10 — Alur Assessment 7 Step
- **Eyebrow:** `BAGIAN 1 · ALUR`
- **Sumber data:** PDF page 7
- **Style:** 3-swimlane horizontal (Persiapan / Pelaksanaan / Penilaian)
- **Konten swimlane:**
  - **Persiapan (1-2):** 📁 Persiapan Data, 📝 Buat Assessment
  - **Pelaksanaan (3-5):** 💻 Peserta Ujian, 👁 Monitoring, 📤 Submit Ujian
  - **Penilaian (6-7):** ⚙ Penilaian Otomatis, 🏆 Hasil & Laporan
- Output bar bawah: "Output: skor pekerja · status kelulusan · rekap unit"

### BAGIAN 2 — Assessment Proton (slide 11-13)

#### Slide 11 — Assessment Proton 3 Tahun
- **Eyebrow:** `BAGIAN 2`
- **Sumber data:** PDF page 8 (section opener + 3 tahun)
- **Style:** 3 card horizontal Tahun 1/2/3
- **Konten card:**
  - **TAHUN 1** (Panelman / Operator): Track dasar per role · MCQ online · Fokus kompetensi dasar
  - **TAHUN 2** (Panelman / Operator): Track lanjutan · MCQ online · Fokus pendalaman proses
  - **TAHUN 3** (Panelman / Operator) + badge **🎤 OFFLINE INTERVIEW**: Track mahir · Interview offline panel juri

#### Slide 12 — Alur Proton Th 1 & 2 (Online)
- **Eyebrow:** `BAGIAN 2 · ALUR`
- **Sumber data:** PDF page 9
- **Style:** Stepper 4 + cross-ref box + warning bar
- **Stepper:**
  1. Buat Assessment — Kategori "Assessment Proton", pilih track & tahun
  2. Set Paket Soal — Pilih paket sesuai track tahun, set durasi & passing grade
  3. Peserta Ujian Online — Login portal, kerjakan soal dalam timer otomatis
  4. Penilaian Otomatis — Skor otomatis, laporan lulus/tidak per peserta
- **Cross-ref box:** "💡 Mirip Assessment Umum — beda di kategori & paket soal per track"
- **Warning bar:** "⚠ Wajib lulus Tahun N untuk lanjut Tahun N+1"

#### Slide 13 — Alur Proton Th 3 (Interview Offline)
- **Eyebrow:** `BAGIAN 2 · ALUR`
- **Sumber data:** PDF page 10
- **Style:** Stepper 4 + badge OFFLINE besar + callout
- **Stepper:**
  1. Buat Assessment — Pilih track Tahun 3, **tanpa durasi & paket soal**
  2. Interview Offline — Panel juri tatap muka, peserta presentasi & dijuri
  3. Penilaian Kompetensi — Penilaian oleh panel juri
  4. Rekap & Sertifikasi — Input skor ke sistem, sertifikasi level kompetensi
- **Badge header:** `🎤 OFFLINE MODE` (color theme orange/red)
- **Callout:** "🔔 Sistem hanya untuk input skor & rekap"

### BAGIAN 3 — Coaching Proton CDP (slide 14-19)

#### Slide 14 — Coaching Proton Dual Track (Section Opener BAGIAN 3)
- **Eyebrow:** `BAGIAN 3 · CDP`
- **Sumber data:** PDF page 11 (sekaligus section opener Bagian 3)
- **Style:** 2 kolom besar Panelman / Operator + 3 pill per kolom
- **Konten:**
  - Kolom kiri: **👷 PANELMAN** — pill: [Th 1] [Th 2] [Th 3] · "(3 track terpisah)"
  - Kolom kanan: **🔧 OPERATOR** — pill: [Th 1] [Th 2] [Th 3] · "(3 track terpisah)"
- Tagline bawah: "💡 Tiap track berdiri sendiri — hierarki & deliverable independen, promosi per tahun"

#### Slide 15 — IDP & Training Records
- **Eyebrow:** `BAGIAN 3 · CDP`
- **Sumber data:** PDF page 12
- **Style:** 2 kolom card
- **Konten:**
  - Kolom kiri: **📋 IDP — Individual Development Plan (Perpustakaan)**
    - 📂 Repository dokumen IDP per pekerja
    - 📄 Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)
    - 👁 Worker view & download dokumen
    - 🔍 Filter & search per jabatan / unit
  - Kolom kanan: **📚 Training Records — Riwayat Pelatihan**
    - Training internal & eksternal
    - Kategori + sub-kategori
    - Sertifikat upload (PDF/image)
    - Validity period & renewal
- Footer: "💡 Terintegrasi profile pekerja → gap analysis & promosi"

#### Slide 16 — Hierarki Kompetensi per Track
- **Eyebrow:** `BAGIAN 3 · STRUKTUR`
- **Sumber data:** PDF page 13
- **Style:** Tree vertikal 4 level + kolom contoh konkret
- **Konten kiri (tree generic):**
  ```
  📁 Track (Level 0)
      ↓
  📂 Kompetensi (Level 1)
      ↓
  📄 Sub-Kompetensi (Level 2)
      ↓
  🎯 Deliverable (Output)
  ```
- **Konten kanan (contoh ilustratif, bisa diganti waktu review):**
  ```
  📁 Operator - Tahun 1
      ↓
  📂 Safety Operation
      ↓
  📄 LOTO (Lock Out Tag Out)
      ↓
  🎯 Submit prosedur LOTO unit X
  ```
- Catatan bawah: "💡 Independen per track · Semua deliverable selesai = lulus track → promosi"

#### Slide 17 — Progresi 5 Aspek (Tahun 1/2/3)
- **Eyebrow:** `BAGIAN 3 · PROGRESI`
- **Sumber data:** PDF page 14
- **Style:** Tabel 4 kolom × 5 baris + highlight diff Tahun 3
- **Tabel:**
  | Aspek | Tahun 1 | Tahun 2 | Tahun 3 (highlight) |
  |---|---|---|---|
  | 🎯 Fokus | Dasar & pengenalan unit | Lanjutan & pendalaman proses | **Mahir & penguasaan penuh** |
  | 📦 Deliverable | Khusus Tahun 1 | Khusus Tahun 2 | **Khusus Tahun 3** |
  | 🔄 Coaching Process | Evidence → Multi Approval → Final Assessment | Evidence → Multi Approval → Final Assessment | Evidence → Multi Approval → **Final Assessment Interview** |
  | 📝 Assessment | Ujian online MCQ | Ujian online MCQ | **Interview offline panel juri** |
  | 🏆 Akhir Tahun | Sertif Th 1 → lanjut Th 2 | Sertif Th 2 → lanjut Th 3 | **Sertifikasi Final → Kompeten penuh** |
- Highlight Tahun 3 cell yang beda: warna orange/red + bold.

#### Slide 18 — Alur Coaching Th 1 & 2 (8 step)
- **Eyebrow:** `COACHING · TAHUN 1 & 2`
- **Sumber data:** PDF page 15
- **Style:** 3-swimlane (Persiapan / Review Multi-Role / Sertifikasi)
- **Swimlane:**
  - **Persiapan (1-3):** 📋 Siapkan Silabus, 📤 Upload Guidance, 🔗 HC Assign Coachee
  - **Review Multi-Role (4-6):** 📥 Coach Submit Evidence, 👀 Review Multi-Role (Coach+SrSpv+SH+HC), ✅ Approval / Revisi
  - **Sertifikasi (7-8):** 📊 Hitung Progress, 🏅 Sertifikasi
- Output bar: "✅ Output: sertifikat tahun + eligible naik tahun berikutnya"

#### Slide 19 — Alur Coaching Th 3 Mahir (8 step)
- **Eyebrow:** `COACHING · TAHUN 3 (MAHIR)`
- **Sumber data:** PDF page 16
- **Style:** 3-swimlane (Silabus Mahir / Review / Sertifikasi Final) + badge LEVEL MAHIR + highlight diff
- **Swimlane:**
  - **Silabus Mahir (1-3):** 🎓 Silabus Mahir, 🔗 Mapping Th 3, ✍ Kerjakan Deliverable
  - **Review Multi-Role (4-6):** 👀 Review Multi-Role, ✅ Approval / Revisi, 📊 Hitung Progress
  - **Sertifikasi Final (7-8):** 🏆 Sertifikasi Final, ⭐ Penetapan Level
- **Badge header kanan:** `🎯 LEVEL MAHIR`
- **Highlight diff dari Th 1-2:** Step 1 (Silabus Mahir), step 7 (Sertif Final), step 8 (Penetapan Level) di-highlight color khusus.
- Output bar: "🏆 Output: Pekerja kompeten penuh + sertifikasi final + eligible role advance"

### Ringkasan + Operasional (slide 20-23)

#### Slide 20 — Ringkasan Program Proton
- **Eyebrow:** `RINGKASAN`
- **Sumber data:** PDF page 17
- **Style:** 3 card horizontal Tahun 1/2/3 + output box besar
- **Konten 3 card:**
  - **TAHUN 1:** Kompetensi Dasar · Deliverable Tahun 1 · Coaching & Assessment Online
  - **TAHUN 2:** Kompetensi Lanjutan · Deliverable Tahun 2 · Coaching & Assessment Online
  - **TAHUN 3:** Kompetensi Mahir · Deliverable Tahun 3 · Coaching & Review Interview
- **Output box bawah (highlight gradient):**
  ```
  🏆 HASIL AKHIR
  Pekerja Kompeten · Tersertifikasi · Siap Operasi Kompleks
  ```

#### Slide 21 — Integrasi & Keamanan
- **Sumber data:** HTML s12 (sinkronisasi label dengan keputusan baru)
- **Style:** HTML cat-grid 6 card
- **6 card (label updated):**
  - 🔐 **LDAP Pertamina** — Login akun Active Directory Pertamina, Single Sign-On
  - 🛡️ **Anti-Copy** — Perlindungan soal ujian online dari copy-paste · integritas assessment
  - 📋 **Audit Log** — Aksi penting tercatat (login, submit, approval) · transparansi
  - 👥 **Role-Based Access** — **10 role · 6 level** akses sesuai tanggung jawab *(sinkron slide 6)*
  - 🔔 **Notifikasi Real-time** — Assessment, approval coaching, renewal sertifikat
  - 📥 **Import Excel** — Bulk import training records, assessment, soal · hemat waktu admin

#### Slide 22 — Cara Akses (Dev Aktif, Prod Belum)
- **Sumber data:** HTML s14 (swap order + badge status)
- **Style:** 2 card swap (Dev di kiri sebagai aktif)
- **Card kiri — 🟢 DEVELOPMENT** badge `✅ AKTIF SEKARANG`
  - Environment Utama untuk uji & sosial
  - URL: `http://10.55.3.3/KPB-PortalHC`
  - Login: Akun Active Directory Pertamina
  - Jaringan: Intranet
- **Card kanan — 🚧 PRODUCTION** badge `⏳ Belum Aktif`
  - Environment Target
  - URL: `https://appkpb.pertamina.com/KPB-PortalHC` `ⓘ URL perkiraan`
  - Login: Akun Active Directory Pertamina
  - Jaringan: VPN / Intranet
- **Footer:** "📌 Saat ini masih development"

#### Slide 23 — Penutup + Kontak
- **Sumber data:** HTML s15 + PDF page 18 kontak
- **Style:** HTML penutup existing + section kontak baru bawah
- **Konten:**
  - 🙏 (icon besar)
  - Title: `Terima Kasih`
  - Subtitle: "Mari bersama digitalisasi Human Capital Pertamina KPB"
  - Badge Q&A: "● Q&A — Sesi Tanya Jawab"
  - CTA button: `🚀 Akses HC Portal Sekarang →` → arah `http://10.55.3.3/KPB-PortalHC` *(URL Dev, bukan Production karena belum live)*
  - URL display di bawah CTA: `http://10.55.3.3/KPB-PortalHC`
  - **Divider**
  - Section kontak (baru, dari PDF):
    ```
    📞 KONTAK
    PT Kilang Pertamina Balikpapan
    📅 Balikpapan · 25 Mei 2026
    ```

## Slide HTML Existing yang DI-DROP

| # HTML | Judul | Alasan drop |
|---|---|---|
| s5 | 3 Pilar Manfaat (Efisiensi/Data-Driven/Standardisasi) | Diganti PDF 3 prinsip (Terpusat/Terstandar/Terukur) di slide 4 |
| s7 | Modul CMP — Mockup Browser | Data fiktif (Assessment row dummy), tidak sesuai data PDF |
| s9 | Modul CDP — Mockup Browser | Data fiktif (Coaching row dummy), tidak sesuai data PDF |
| s10 | Coaching Proton Journey (Foundation/Intermediate/Cert) | Diganti slide 14 dual-track + slide 17 tabel progresi |
| s11 | Dashboard Analytics (KPI 156/87%/+23%/42) | Data fiktif, tidak ada referensi PDF |
| s13 | Progress Penyiapan Program | Drop per keputusan user |

## Style Tokens yang Dipertahankan dari HTML

- Color palette: variabel `--red`, dark/light mode toggle (body.dark)
- Cover: `.cover`, `.cover-strip`, `.logo-box`, `.logo-mark`, `.cover-title`
- Slide layout: `.slide`, `.slide-header`, `.slide-title`, `.slide-subtitle`, `.slide-badge`, `.slide-body`, `.default-deco`
- Card variants: `.module-card`, `.cat-card`, `.pilar-card`, `.kpi-card` (drop), `.akses-card`
- Stepper: `.coach-journey`, `.coach-step`, stepper number badge
- Stairs: `.stairs`, `.step`, `.step-num`, `.step-role`, `.step-access`
- Penutup: `.penutup`, `.penutup-title`, `.penutup-cta`
- Navigation: `.controls`, `.ctrl-btn`, `.slide-counter`, progress bar top
- Animation: keyboard nav, click goTo, slide active transition

## Style Tokens BARU yang Perlu Ditambah

- **Chip role pill** (slide 6) — pill kecil untuk multi-role per step
- **2-row card** (slide 7) — card dengan section divider per row (Kategori/Metode/Penilaian)
- **3-swimlane** (slide 10, 18, 19) — group horizontal dengan label, multiple step pill di dalamnya
- **Stepper variant offline** (slide 13) — color theme orange/red + badge mode
- **Tabel progresi highlight** (slide 17) — cell highlight color untuk diff
- **Tree contoh paralel** (slide 16) — 2 kolom tree (generic + contoh konkret)
- **Faded card** (slide 5 BP) — dashed border + opacity untuk "Coming Soon"
- **Card status badge** (slide 22) — `AKTIF SEKARANG` vs `BELUM AKTIF`
- **URL perkiraan annotation** (slide 22) — `ⓘ` icon + label

## Konvensi Eyebrow Header

Tiap slide selain cover, agenda, penutup pakai eyebrow di header berisi BAGIAN/SECTION label:

| Slide | Eyebrow |
|---|---|
| 1, 2, 3 | (tidak ada — section pengenalan) |
| 4 | (tidak ada — pengenalan lanjutan) |
| 5, 6 | (tidak ada) |
| 7 | `BAGIAN 1` |
| 8 | `BAGIAN 1 · CMP` |
| 9 | `BAGIAN 1 · CMP` |
| 10 | `BAGIAN 1 · ALUR` |
| 11 | `BAGIAN 2` |
| 12, 13 | `BAGIAN 2 · ALUR` |
| 14, 15 | `BAGIAN 3 · CDP` |
| 16 | `BAGIAN 3 · STRUKTUR` |
| 17 | `BAGIAN 3 · PROGRESI` |
| 18 | `COACHING · TAHUN 1 & 2` |
| 19 | `COACHING · TAHUN 3 (MAHIR)` |
| 20 | `RINGKASAN` |
| 21, 22, 23 | (tidak ada — operasional/penutup) |

Counter format: **`SLIDE X / 23`** di pojok kanan atas tiap slide (lewat `.slide-badge`).

## Behavior / Interaksi

- **Navigation:** Pertahankan keyboard (← → Space), tombol Prev/Next, click agenda → goTo(N)
- **Total slide:** Update konstanta `const total = 23`
- **Counter:** Update `document.getElementById('totalNum').textContent = total` ke 23
- **DROP chart:** Hapus init Chart.js untuk slide 11 (Dashboard Analytics drop), hapus library Chart.js dari `<head>`
- **DROP mockup filter:** Hapus fungsi `filterMock()` (mockup CMP+CDP drop)

## Acceptance Criteria

1. Total 23 slide aktif, navigasi keyboard + tombol + agenda click berfungsi.
2. Semua data konten dari PDF 18 page terpetakan ke slide 1-23 (no missing data).
3. Tidak ada data fiktif (no dummy KPI, no dummy mockup row).
4. URL Production ditandai "perkiraan" + badge "Belum Aktif".
5. URL Development ditandai aktif + badge "Aktif Sekarang".
6. CTA penutup arah ke URL Dev.
7. Tanggal "25 Mei 2026" konsisten di Cover + Penutup.
8. Label OJT (bukan OJ) di slide 8.
9. BP "Coming Soon" tampil di slide 5 (3 platform).
10. 10 role granular tampil di slide 6 stairs.
11. Style HTML existing dipertahankan (dark mode toggle, hover, transisi, gradient).
12. File output: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (overwrite, single file).

## Implementasi — Catatan Awal untuk Plan

- File ini = **1 file HTML monolitik** (CSS + JS + content inline). Tidak split jadi multi-file.
- Edit di-tempat (overwrite `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`), bukan buat file baru.
- Re-use existing CSS class sebanyak mungkin. Tambah class baru hanya jika perlu (chip, swimlane, dll).
- Test di browser lokal sebelum commit (open file langsung, tidak perlu dotnet run karena ini static HTML).
- Backup file lama sebelum overwrite (sekedar git commit dulu kalau ada changes uncommitted).
