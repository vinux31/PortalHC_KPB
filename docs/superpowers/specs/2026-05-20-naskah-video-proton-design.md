# Naskah Video PROTON — Design Spec

**Tanggal:** 2026-05-20
**Status:** Draft v1 (untuk review)
**Sumber revisi:**
- `docs/Naskah Video CPDP.docx` (naskah lama)
- `docs/Fitur PROTON — Portal HC KPB (1).pdf` (definisi fitur resmi)
- `docs/Sosialisasi PortalHC v2 — Slide Deck 2026.pdf` (konteks platform)
- Kode `Views/CDP/*` dan `Controllers/ProtonDataController.cs` (verifikasi fitur eksis)

---

## 1. Ringkasan & Goal

**Goal:** Video sosialisasi 2:30 yang memperkenalkan fitur **PROTON** (Program Coaching Pekerja) di **Portal HC KPB** kepada coachee dan stakeholder.

**Audiens utama:** Coachee (pekerja yang baru masuk fase operasi), dengan stakeholder sekunder coach, Sr Supervisor, Section Head, dan tim HC.

**Format:**
- Total durasi: 2:30 (target 2–3 menit)
- Narator: voice-over (VO) tunggal, Bahasa Indonesia formal-korporat
- Visual: motion graphic + b-roll lapangan + talking-head silent cutaway (coach & coachee)
- Tidak ada dialog/soundbite — semua narasi via VO

## 2. Perubahan Kunci dari Naskah Lama (CPDP → PROTON)

| Elemen | Naskah Lama (CPDP) | Naskah Baru (PROTON) |
|--------|--------------------|----------------------|
| Akronim | CPDP = Craft Personal Development Plan | **PROTON = Program Coaching Pekerja** |
| Platform | "website CPDP" | **Portal HC KPB** (modul **CDP** untuk Coaching Proton, modul **CMP** untuk Assessment Proton) |
| Framing pembuka | "pengganti On-the-Job Training" | "program coaching digital di Portal HC KPB" (drop OJT framing) |
| Segmen 3 (Skill Group) | Panelman+Operator+5 kompetensi (Safe Work Practice, Energy Mgmt, Catalyst & Chemical, Process Control, Refinery Operations) | **Track Panelman vs Operator + progresi Tahun 1/2/3** (5 kompetensi dihapus, sesuai PDF baru yang tidak detail) |
| Alur Pelaksanaan | 6 langkah lama: Kick Off → Feedback → Monitoring → Coaching → Assessment → Final Eval | **6 langkah PDF baru**: HC Assign → Coachee upload Deliverable → Coach catat sesi → **Multi-role Approval** (Sr Supervisor → Section Head → HC) → Assessment → Histori PROTON |
| Penyebutan fitur | Tidak ada | **IDP, Coaching Proton, Assessment Proton, Deliverable, Histori PROTON, Sertifikasi** disebut by name |
| Closing | 2 paragraf | Dipadatkan 1 paragraf |
| SMART | Dipertahankan | Dipertahankan (rename CPDP→PROTON saja) |

## 3. Struktur Final (7 Segmen × 2:30)

| # | Segmen | Timing | Durasi |
|---|--------|--------|--------|
| 1 | Opening + Pengenalan PROTON | 0:00–0:30 | 30s |
| 2 | Kerangka & Prinsip SMART | 0:30–0:50 | 20s |
| 3 | Track & Progresi Tahunan | 0:50–1:15 | 25s |
| 4 | Alur Pelaksanaan PROTON (6 langkah) | 1:15–1:55 | 40s |
| 5 | Assessment Akhir Per Tahap | 1:55–2:10 | 15s |
| 6 | Histori PROTON & Sertifikasi | 2:10–2:20 | 10s |
| 7 | Closing & Motivasi | 2:20–2:30 | 10s |

Total: 150 detik = 2:30 ✅

---

## 4. Naskah Lengkap — Per Segmen

### Segmen 1 — Opening & Pengenalan PROTON (0:00–0:30)

**Visual:**
- (0:00–0:05) Logo PT Kilang Pertamina Balikpapan + tagline. Fade-in.
- (0:05–0:15) B-roll: layar laptop membuka **Portal HC KPB**. Klik menu utama → muncul tiga ikon platform: **CMP**, **CDP**, **BP (Coming Soon)**.
- (0:15–0:25) Zoom-in ke CDP → muncul kartu **Coaching Proton**, **IDP**, **Histori PROTON**. Tema warna konsisten dengan UI Portal HC KPB.
- (0:25–0:30) Title card: **"PROTON — Program Coaching Pekerja"**.

**Narasi:**
> "PROTON, singkatan dari **Program Coaching Pekerja**, adalah fitur pengembangan kompetensi digital di **Portal HC KPB**.
> Tersedia di dua modul: **CDP** untuk Coaching Proton dan **CMP** untuk Assessment Proton.
> PROTON dirancang untuk fase operasi guna meningkatkan kompetensi Anda secara terstruktur dan terukur selama tiga tahun."

---

### Segmen 2 — Kerangka & Prinsip SMART (0:30–0:50)

**Visual:**
- Diagram "Prinsip SMART" muncul di tengah layar.
- 5 poin muncul satu per satu (Specific, Measurable, Achievable, Relevant, Time-bound) dengan ikon pendukung dan animasi tipe sequential.

**Narasi:**
> "PROTON dirancang menggunakan prinsip **SMART**.
> Pertama, **Specific** — memiliki target dan deliverable yang jelas.
> Kedua, **Measurable** — setiap proses memiliki parameter evaluasi yang terukur.
> Ketiga, **Achievable** — semua tugas dapat diselesaikan sesuai kemampuan coachee.
> Keempat, **Relevant** — penugasan sesuai dengan lingkup pekerjaan untuk meningkatkan kompetensi.
> Dan kelima, **Time-Bound** — memiliki jadwal awal dan akhir yang sudah ditentukan."

---

### Segmen 3 — Track & Progresi Tahunan (0:50–1:15)

**Visual:**
- Dua ikon besar bersanding: **Panelman** & **Operator**. Garis vertikal pemisah.
- Di bawah masing-masing, timeline 3 tahap horizontal (Tahun 1 → Tahun 2 → Tahun 3) dengan badge warna berbeda.
- Tahun 1: ikon foundation/basic. Tahun 2: ikon advanced/process. Tahun 3: ikon mastery/optimization.
- Indikator panah "lulus → naik tahun" antara tahap.

**Narasi:**
> "PROTON memiliki dua track keahlian utama: **Panelman** dan **Operator**. Masing-masing track berjalan selama tiga tahun dengan deliverable dan kompetensi yang berbeda di setiap tahap.
> Tahun **pertama** fokus pada pengenalan dan praktik dasar.
> Tahun **kedua** pada pendalaman proses dan kemandirian.
> Tahun **ketiga** pada validasi kompetensi tingkat mahir.
> Setiap pekerja harus menyelesaikan satu tahap sebelum melanjutkan ke tahap berikutnya."

---

### Segmen 4 — Alur Pelaksanaan PROTON (1:15–1:55)

**Visual:**
- Flowchart animasi 6 langkah, muncul sequential:
  1. **HC Assign Coach** → ikon admin + arrow ke coach
  2. **Deliverable** → ikon coachee buka fitur **IDP** di Portal HC KPB, lihat daftar tugas, upload bukti pekerjaan
  3. **Coaching Proton** → talking-head silent coach+coachee (cutaway A, 3s)
  4. **Multi-Role Approval** → diagram 3-tingkat: Sr Supervisor ✓ → Section Head ✓ → HC ✓ (review final). Talking-head silent supervisor (cutaway B, 2s).
  5. **Final Assessment** → ikon ujian
  6. **Histori PROTON** → kartu rekam jejak otomatis terisi. Talking-head silent coachee tersenyum (cutaway C, 3s).

**Narasi:**
> "Pelaksanaan PROTON mengikuti enam langkah utama.
> Pertama, **HC** menentukan silabus per jabatan dan menugaskan **coach senior** kepada setiap coachee.
> Kedua, coachee melihat daftar tugas di fitur **IDP** lalu menyelesaikan dan mengunggah bukti **Deliverable** ke Portal HC KPB.
> Ketiga, coach mencatat hasil sesi pendampingan melalui fitur **Coaching Proton** — diskusi, kesimpulan, dan tindak lanjut.
> Keempat, bukti pekerjaan disetujui secara berlapis oleh **Sr Supervisor**, **Section Head**, dan direview final oleh **HC**.
> Kelima, setelah seluruh deliverable disetujui, coachee mengikuti **Final Assessment**.
> Dan keenam, seluruh perjalanan tersimpan permanen di **Histori PROTON**."

---

### Segmen 5 — Assessment Akhir Per Tahap (1:55–2:10)

**Visual:**
- Ikon ujian terbuka dengan soal pilihan ganda (Y1–Y2).
- Cut ke adegan wawancara panel juri dengan coachee (Y3).
- Hasil ujian muncul dengan nilai ≥ 75 → efek confetti & badge "Lulus".

**Narasi:**
> "Untuk Tahun pertama dan kedua, assessment berupa **ujian online pilihan ganda**, dengan sepuluh soal per sub-kompetensi dan penilaian otomatis.
> Untuk Tahun ketiga, assessment berupa **wawancara tatap muka** oleh panel juri.
> Nilai minimal kelulusan adalah **tujuh puluh lima**, sebagai syarat naik ke tahap berikutnya."

---

### Segmen 6 — Histori PROTON & Sertifikasi (2:10–2:20)

**Visual:**
- Layar fitur **Histori PROTON** menampilkan timeline lengkap coachee dari Tahun 1 hingga 3, dengan status "Lulus" per tahap.
- Animasi sertifikat digital muncul dengan stempel dan tanda tangan.

**Narasi:**
> "Seluruh progress, deliverable, hasil coaching, dan assessment Anda terekam permanen di **Histori PROTON**.
> Setelah lulus, **sertifikat digital** terbit otomatis dan status kompetensi Anda terupdate di sistem."

---

### Segmen 7 — Closing & Motivasi (2:20–2:30)

**Visual:**
- Montase cepat 2 detik: coaching, upload deliverable, ujian, lulus.
- Karakter coachee tersenyum bangga dengan badge **"Full Point Prestasi"**.
- Closing: tagline perusahaan + logo Portal HC KPB.

**Narasi:**
> "Coachee yang lulus dengan nilai minimal tujuh puluh lima mendapatkan **full point prestasi** pada penilaian kinerja akhir tahun.
> Bersama **PROTON**, tingkatkan kompetensi Anda dan kontribusi pada ketahanan energi bangsa.
> Mari wujudkan pengembangan diri dan capai potensi terbaik Anda."

---

## 5. Daftar Aset Visual yang Dibutuhkan

**Motion graphic:**
- Logo PT Kilang Pertamina Balikpapan (file resmi)
- UI Screen record Portal HC KPB: menu utama, modul CDP (Coaching Proton, IDP, Histori), modul CMP (Assessment), Histori PROTON detail
- Ikon SMART (5 ikon)
- Ikon Panelman & Operator
- Ikon 6 langkah alur (assign, deliverable, coaching, approval, assessment, histori)
- Sertifikat digital template

**B-roll lapangan:**
- Talking-head silent coach (2 angle, ~10 detik total)
- Talking-head silent coachee (2 angle, ~10 detik total)
- Talking-head silent supervisor (1 angle, ~3 detik)
- Adegan sesi coaching di CCR/lapangan (b-roll)
- Adegan coachee mengoperasikan laptop di area kerja

**Title cards:**
- "PROTON — Program Coaching Pekerja"
- "Full Point Prestasi"

---

## 6. Catatan Produksi

- **Total kata narasi:** ~430 kata. Pace ~170 kata/menit (standar VO Indonesia formal).
- **Tone musik:** corporate-inspiring, low-key di segmen narasi, swell di Closing.
- **Lower-third nama fitur:** setiap fitur Proton yang disebut (IDP, Coaching Proton, Deliverable, Histori PROTON, Sertifikasi) ditampilkan dengan lower-third teks 2 detik untuk reinforcement.
- **Watermark Portal HC KPB** di pojok kanan bawah sepanjang video.

## 7. Verifikasi Fitur

Semua fitur yang disebut dalam naskah sudah dikonfirmasi eksis di kode:

| Fitur | Lokasi kode |
|-------|-------------|
| IDP | `Views/CDP/PlanIdp.cshtml` |
| Coaching Proton | `Views/CDP/CoachingProton.cshtml` |
| Deliverable | `Views/CDP/Deliverable.cshtml` |
| Histori PROTON | `Views/CDP/HistoriProton.cshtml`, `HistoriProtonDetail.cshtml` |
| Sertifikasi | `Views/CDP/CertificationManagement.cshtml` |
| Assessment Proton | `Views/CMP/Assessment.cshtml`, `Views/Admin/AssessmentMonitoring.cshtml` |
| Multi-Role Approval | `Controllers/ProtonDataController.cs`, `Views/ProtonData/Override.cshtml` |

---

## 8. Next Step

Setelah spec ini disetujui:
1. Tim Video/Multimedia produksi VO dari naskah.
2. Tim Multimedia produksi motion graphic & shot list b-roll.
3. Editing final + review HC.
4. Distribusi via Portal HC KPB (modul Sosialisasi) + channel internal.
