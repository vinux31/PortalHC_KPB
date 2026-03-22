# Phase 228: Best Practices Research - Research

**Researched:** 2026-03-22
**Domain:** Certificate renewal UX, assessment/exam management, real-time exam monitoring, exam flow
**Confidence:** MEDIUM-HIGH (knowledge synthesis dari training data + web search 2024-2025)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Riset mencakup UX flow detail step-by-step untuk semua 4 aspek (renewal, assessment, monitoring, admin management)
- **D-02:** Sertakan deskripsi teks detail tentang UI platform pembanding sebagai visual reference (tanpa link ke halaman asli)
- **D-03:** Dokumentasikan semua aspek UX: renewal flow, exam/assessment flow, real-time monitoring, dan admin management
- **D-04:** 4 dokumen terpisah per topik + 1 dokumen ringkasan perbandingan = 5 dokumen total
- **D-05:** Format: tabel perbandingan fitur + narasi analisis per aspek penting
- **D-06:** Dokumen disimpan di `docs/` sebagai HTML, konsisten dengan dokumen project lainnya
- **D-07:** Ranking 3-tier: Must-fix (bug/UX kritis), Should-improve (best practice gap), Nice-to-have (enhancement)
- **D-08:** Tiap rekomendasi di-map ke target phase (229/230/231/232)
- **D-09:** Platform renewal: Coursera, LinkedIn Learning, HR portals sejenis
- **D-10:** Platform assessment: Moodle, Google Forms Quiz, Examly (kini iamneo.ai)
- **D-11:** Untuk HR portals, Claude riset dan pilih 1-2 yang relevan dengan konteks industrial/manufacturing

### Claude's Discretion
- Pemilihan HR portal spesifik untuk perbandingan
- Styling dan layout HTML dokumen riset
- Kedalaman narasi per aspek berdasarkan relevansi untuk portal

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — diskusi tetap dalam scope phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Research Support |
|----|-----------|-----------------|
| RSCH-01 | Riset best practices certificate renewal UX dari platform sejenis (Coursera, LinkedIn Learning, HR portals) | Covered: Coursera, LinkedIn Learning, TalentGuard/Certemy sebagai HR portal industrial |
| RSCH-02 | Riset best practices assessment/exam management dari platform sejenis (Moodle, Google Forms Quiz, Examly) | Covered: Moodle 5.0, Google Forms Quiz, Examly/iamneo.ai, SpeedExam |
| RSCH-03 | Riset best practices real-time exam monitoring UX dari platform sejenis | Covered: SpeedExam, Exam.net, online proctoring patterns 2024-2025 |
| RSCH-04 | Dokumen perbandingan fitur portal vs best practices dengan rekomendasi improvement per halaman | Template untuk 5 dokumen HTML disiapkan; planner buat task menulis tiap dokumen |
</phase_requirements>

---

## Summary

Phase 228 adalah fase riset murni — outputnya bukan kode, melainkan 5 dokumen HTML di folder `docs/` yang akan menjadi "lens" bagi audit phases 229-232. Riset ini membandingkan Portal HC KPB dengan platform terkemuka di 4 area: certificate renewal, assessment management, real-time exam monitoring, dan exam flow worker-side.

**Temuan utama:** Portal HC KPB sudah mengimplementasikan banyak fitur yang sejalan dengan best practices (sticky timer, auto-save, paginated questions, pass/fail indicator). Gap terbesar ada di (1) renewal flow yang belum memiliki proactive expiry notification dan visual urgency indicator, dan (2) monitoring dashboard yang belum menampilkan live progress per-peserta secara real-time.

**Primary recommendation:** Tulis 5 dokumen HTML riset yang terstruktur — 4 topik-spesifik + 1 ringkasan perbandingan — mengikuti styling `docs/audit-assessment-training-v8.html` sebagai template.

---

## Standard Stack (untuk output dokumen)

### Format Dokumen
| Aspek | Spesifikasi | Alasan |
|-------|------------|--------|
| Format file | HTML statik | Konsisten dengan semua docs/ project lainnya |
| Styling template | `docs/audit-assessment-training-v8.html` | CSS custom vars, sidebar nav, tabel, badge — sudah ada |
| Bahasa dokumen | Bahasa Indonesia (campuran istilah teknis Inggris) | Sesuai konvensi project |
| Color palette | `--blue: #005baa`, `--dark: #1a1a2e`, dll. | Inherit dari template |
| Tabel perbandingan | HTML `<table>` dengan `<th>` blue header | Pattern dari commit-log.html |

### Lima Dokumen yang Harus Dibuat
| No | File | Konten |
|----|------|--------|
| 1 | `docs/research-renewal-certificate.html` | RSCH-01: UX renewal Coursera, LinkedIn, TalentGuard |
| 2 | `docs/research-assessment-management.html` | RSCH-02: Moodle, Google Forms, Examly |
| 3 | `docs/research-exam-monitoring.html` | RSCH-03: SpeedExam, Exam.net, proctoring patterns |
| 4 | `docs/research-exam-flow.html` | RSCH-02+03: Worker exam UX flow end-to-end |
| 5 | `docs/research-comparison-summary.html` | RSCH-04: Tabel perbandingan + rekomendasi 3-tier |

---

## Architecture Patterns

### Struktur Tiap Dokumen HTML

```
docs/research-[topik].html
├── <head> CSS (inherit dari audit template)
├── .header  — judul, subtitle, badge versi
├── .sidebar — navigasi anchor dalam dokumen
└── .main
    ├── <section id="overview">     — ringkasan singkat + konteks
    ├── <section id="platform-A">   — Platform pertama (flow detail + tabel fitur)
    ├── <section id="platform-B">   — Platform kedua
    ├── <section id="platform-C">   — Platform ketiga
    ├── <section id="comparison">   — Tabel perbandingan fitur (semua platform)
    └── <section id="recommendations"> — Rekomendasi 3-tier (Must-fix / Should / Nice)
```

### Struktur Dokumen Ringkasan (research-comparison-summary.html)

```
docs/research-comparison-summary.html
├── .header
├── .sidebar (link ke 4 dokumen lain)
└── .main
    ├── <section id="renewal">     — tabel ringkasan + top 3 rekomendasi → Phase 229/230
    ├── <section id="assessment">  — tabel ringkasan + top 3 rekomendasi → Phase 231
    ├── <section id="monitoring">  — tabel ringkasan + top 3 rekomendasi → Phase 231
    ├── <section id="exam-flow">   — tabel ringkasan + top 3 rekomendasi → Phase 232
    └── <section id="priority">    — Master priority table semua rekomendasi + phase mapping
```

### Rekomendasi Format 3-Tier

```html
<!-- Tier badge pattern -->
<span class="badge-must">Must-fix</span>    <!-- bg: --red -->
<span class="badge-should">Should-improve</span>  <!-- bg: --yellow -->
<span class="badge-nice">Nice-to-have</span> <!-- bg: --blue-light -->

<!-- Recommendation row pattern -->
<tr>
  <td>[TIER BADGE]</td>
  <td>[Aspek yang perlu diperbaiki]</td>
  <td>[Gap vs best practice]</td>
  <td>[Rekomendasi konkret]</td>
  <td>Phase [229/230/231/232]</td>
</tr>
```

---

## Findings: Renewal Certificate Best Practices (RSCH-01)

### Platform 1: Coursera
**Model sertifikat:** Coursera menerbitkan sertifikat completion permanen (tidak expired) untuk most courses. Untuk Professional Certificates (Google, IBM), sertifikat tidak ada masa berlaku — tapi platform mendorong learner untuk take refresher courses yang diterbitkan per tahun.

**UX Flow Renewal:**
1. Learner melihat dashboard "My Learning" → course cards dengan status (Completed/In Progress)
2. Untuk re-certification: tidak ada "renewal" eksplisit — learner mendaftar ulang versi terbaru course
3. Notifikasi: email "New course version available" ketika kurikulum diperbarui
4. History: semua sertifikat tersimpan di profil dengan tanggal completion

**UX Patterns yang Relevan:**
- Dashboard card menampilkan status completion dengan tanggal dan badge visual
- "Your certificates" section terpisah dari course catalog
- Sertifikat downloadable/shareable langsung dari dashboard
- Empty state yang jelas: "Complete a course to earn your first certificate"
- Progress tracker per course (module completion %)

**Confidence:** MEDIUM (berdasarkan knowledge training + public documentation)

---

### Platform 2: LinkedIn Learning
**Model sertifikat:** Certificate of Completion setelah menyelesaikan course. Sertifikat bisa dipost langsung ke LinkedIn profile. Tidak ada expiry date pada sertifikat completion.

**UX Flow:**
1. Selesaikan course → otomatis dapat certificate
2. Dashboard "My Learning" → tab "Completed" → list sertifikat
3. Tiap sertifikat ada tombol "Add to Profile" dan "Download"
4. LinkedIn profile menampilkan certifications sorted by expiration date (jauh dulu, expired terakhir)

**UX Patterns yang Relevan:**
- Immediate certificate generation setelah completion (no delay)
- One-click share ke LinkedIn profile
- Certificate card: nama course, tanggal completion, credential URL
- Status badge: "Completed" vs "In Progress" jelas dibedakan
- Tidak ada renewal flow karena sertifikat tidak expired

**Gap dengan Portal KPB:** LinkedIn tidak ada renewal karena sertifikat tidak expired. Portal KPB justru lebih maju karena harus handle renewal yang real.

**Confidence:** MEDIUM-HIGH (verified dari LinkedIn Help documentation)

---

### Platform 3: TalentGuard (HR Portal Industrial — pilihan Claude)
**Alasan dipilih:** TalentGuard adalah platform talent management enterprise yang banyak digunakan di sektor industrial/manufacturing. Fokus pada certification tracking dan compliance. Relevan karena konteks KPB.

**Model sertifikat:** Sertifikat dengan expiry date, linked ke job roles. Compliance tracking per employee.

**UX Flow Renewal:**
1. Dashboard admin menampilkan "Upcoming Expirations" widget (90/60/30/7 hari)
2. Filter by department, role, certification type
3. Automated email alerts ke employee + manager pada interval 90/30/7 hari
4. Employee menerima email → klik link → landing di renewal action page
5. Renewal: re-take assessment ATAU submit dokumen training eksternal
6. Post-renewal: sertifikat baru terbit dengan tanggal baru, history chain tersimpan

**UX Patterns yang Relevan:**
- Color-coded urgency: hijau (valid), kuning (akan expired <30 hari), merah (expired)
- "Expiring Soon" dashboard widget dengan count per kategori
- Bulk action: admin bisa trigger renewal reminder ke multiple employees sekaligus
- Audit trail: semua riwayat renewal tersimpan dengan timestamp
- Compliance score per department (% sertifikat aktif)
- Manager view: lihat status sertifikat seluruh tim

**Confidence:** MEDIUM (dari web search + Certifier/TalentGuard documentation)

---

## Findings: Assessment Management Best Practices (RSCH-02)

### Platform 1: Moodle Quiz
**Model:** Open-source LMS. Quiz module adalah fitur inti. Moodle 5.0+ memperkenalkan New Quiz (pre-creating attempts).

**Admin UX Flow (membuat assessment):**
1. Course → Add Activity → Quiz
2. Quiz settings: nama, tanggal open/close, time limit, grading (highest/last/first/average)
3. Question bank: buat pertanyaan di bank DULU, lalu tambahkan ke quiz (atau random dari bank)
4. Preview: admin bisa preview quiz sebagai student
5. Monitoring: Reports → Quiz → Responses/Statistics

**UX Patterns yang Relevan:**
- Separate "Question Bank" dari quiz instance (reusability)
- Random question dari bank per kategori (soal berbeda tiap peserta)
- Review settings: kapan student bisa lihat jawaban benar (saat attempt/setelah close/tidak sama sekali)
- Attempt tracking per student dengan status (In Progress/Completed/Abandoned)
- Grade override: instructor bisa override nilai individual
- Bulk re-grading setelah edit pertanyaan

**Kelemahan yang teridentifikasi:**
- Setup awal cukup kompleks (course → activity → bank → questions → randomize)
- UI terasa "old" dibanding platform modern
- Monitoring tidak real-time tanpa plugin tambahan

**Confidence:** HIGH (Moodle documentation verified)

---

### Platform 2: Google Forms Quiz
**Model:** Form-based quiz. Grading otomatis untuk multiple choice. Tidak ada fitur time limit native.

**Admin UX Flow:**
1. Buat form → Settings → Make this a quiz
2. Add questions → set answer key → assign points
3. Bagikan via link atau embed
4. Responses → Spreadsheet (Google Sheets untuk analisis)

**UX Patterns yang Relevan:**
- Simplicity: sangat mudah dibuat (< 5 menit untuk basic quiz)
- Auto-grading multiple choice
- Response summary dengan chart otomatis
- Individual response view

**Keterbatasan (penting untuk perbandingan):**
- Tidak ada timer native (butuh add-on pihak ketiga)
- Tidak ada randomisasi soal native
- Tidak ada per-participant monitoring real-time
- Security lemah: siapa saja dengan link bisa akses kecuali dibatasi ke domain
- Tidak ada attempt management (bisa diisi berkali-kali oleh orang sama)
- Tidak ada sertifikat otomatis

**Confidence:** HIGH (Google Workspace documentation verified)

---

### Platform 3: Examly (kini iamneo.ai)
**Model:** Platform assessment enterprise. Fokus pada campus recruitment dan corporate assessment. Telah rebranding ke iamneo.ai.

**Admin UX Flow:**
1. Dashboard admin → Create Test
2. Add questions (manual, import Excel, atau dari question bank)
3. Assign ke candidates (individual atau bulk upload)
4. Proctoring settings (webcam, screen sharing, browser lockdown)
5. Schedule dengan start/end window
6. Monitor live → Reports post-exam

**UX Patterns yang Relevan:**
- AI-powered proctoring (eye tracking, face detection, tab monitoring)
- Company-specific test simulation (soal disesuaikan pattern perusahaan)
- Mobile responsive (tapi tidak ada native mobile app — feedback user)
- Bulk candidate import via Excel
- Automated scoring dan ranking
- Detailed analytics: per-question difficulty, time-per-question

**UX Gap yang ditemukan:** Rating "user delight" masih bisa ditingkatkan (dari user testing mereka sendiri). Mobile experience kurang optimal.

**Confidence:** MEDIUM (dari Tracxn profile + design portfolio case study)

---

## Findings: Real-Time Exam Monitoring Best Practices (RSCH-03)

### Pola Monitoring yang Diidentifikasi

#### SpeedExam (Platform komersial)
**Admin monitoring dashboard menampilkan:**
- Live candidates: status (Taking/Completed/Dropped)
- Device info: browser, OS, IP address, lokasi
- Attempt count per candidate
- Bulk actions: force-close exam, extend time, reset attempt

**UX Pattern:**
- List view dengan color coding: hijau (aktif), abu (belum mulai), biru (selesai), merah (dropped)
- Filter by status
- Per-candidate detail: klik nama → lihat progress (soal ke berapa, waktu tersisa)

#### Exam.net
**Monitoring focus:**
- Simple dashboard: student name + status badge
- Hand-raise feature: student bisa "raise hand" digital untuk minta bantuan
- Invigilator chat per student tanpa mengganggu exam lain
- Timer override: tambah waktu ke student tertentu

#### Online Proctoring Patterns (industri-wide 2024-2025)
**UX Patterns yang muncul berulang:**
1. **Progressive Disclosure:** Informasi kritis di depan, detail di klik/expand
2. **No-Navigation principle:** Admin bisa action tanpa navigasi jauh dari monitoring view
3. **Color urgency system:** Status visual tanpa perlu baca teks (green/yellow/red)
4. **Alert/flag system:** Abnormal behavior (tab switch, idle lama) langsung flagged
5. **Bulk actions:** Close multiple sessions sekaligus

**Gap Portal KPB vs best practice:**
- Portal sudah punya: status tracking, token management, Force Close, Bulk Close (AMON-03)
- Belum ada: live progress per-question (answered/total per peserta real-time)
- Belum ada: visual urgency coding yang jelas untuk status peserta

**Confidence:** MEDIUM-HIGH (SpeedExam documentation + web search synthesis)

---

## Findings: Exam Flow Worker-Side Best Practices (RSCH-02 + RSCH-03)

### Flow Terbaik yang Teridentifikasi

**Pre-Exam:**
1. Dashboard menampilkan upcoming exams dengan tanggal, deadline, dan instruksi jelas
2. "Start" CTA prominent dengan countdown timer sampai deadline
3. Token entry sebagai gate masuk (security layer — Portal KPB sudah punya ini)
4. Pre-exam instructions page sebelum timer mulai (opsional)

**During Exam:**
1. Sticky header: judul, progress (answered/total), timer dengan countdown visual
2. Navigation sidebar atau question grid: klik langsung ke soal tertentu
3. Status per soal: Belum dijawab (abu) / Dijawab (hijau) / Ragu-ragu (kuning — opsional)
4. Auto-save per jawaban (tidak perlu manual save)
5. Warning sebelum timeout (5 menit / 1 menit alert)
6. Confirm dialog sebelum submit (bukan langsung submit)

**Post-Exam:**
1. Immediate score display: nilai, pass/fail, dan threshold dengan jelas
2. Sertifikat nomor ditampilkan jika lulus (Portal KPB sudah punya ini)
3. Answer review: lihat jawaban benar vs pilihan user (jika diaktifkan admin)
4. Action setelah selesai: Kembali ke list atau lihat sertifikat

**Portal KPB saat ini (dari kode yang dibaca):**
- Sticky header: ada (title + progress + timer + hubStatusBadge) — BAIK
- Paginated 10 soal per halaman: ada — BAIK
- Auto-save per klik radio: ada (dari StartExam.cshtml) — BAIK
- Timer: ada (examTimer) — BAIK
- Results page: score + correct/total + pass/fail + badge — BAIK
- Answer review: ada kondisional jika HC aktifkan — BAIK
- Question grid navigation: BELUM ADA (hanya pagination, tidak ada jump ke soal)
- Pre-submit confirmation: ada (confirmAbandon tapi bukan untuk submit) — PERLU CEK

**Confidence:** HIGH untuk deskripsi portal KPB (dari kode aktual), MEDIUM untuk best practice perbandingan

---

## Don't Hand-Roll

| Problem | Don't Build | Gunakan Sebagai Referensi | Alasan |
|---------|-------------|--------------------------|--------|
| HTML template styling | Custom CSS baru dari nol | `docs/audit-assessment-training-v8.html` | CSS sudah mature dan konsisten |
| Color urgency system | Nilai warna baru | CSS vars yang sudah ada (`--red`, `--yellow`, `--green`) | Konsistensi visual |
| Dokumen format | JSON/Markdown | HTML (keputusan D-06) | Konsisten dengan semua project docs |

---

## Common Pitfalls

### Pitfall 1: Dokumen Terlalu Akademis
**Yang bisa salah:** Narasi panjang tanpa kesimpulan actionable; planner tidak bisa ekstrak rekomendasi
**Cara hindari:** Tiap section diakhiri dengan tabel rekomendasi 3-tier yang jelas dengan phase mapping
**Warning sign:** Section tanpa satu pun baris tabel rekomendasi

### Pitfall 2: Perbandingan Tidak Apple-to-Apple
**Yang bisa salah:** Membandingkan fitur Coursera (non-expiring cert) dengan Portal KPB (expiring cert) secara langsung, kesimpulan menyesatkan
**Cara hindari:** Selalu kontekstualisasi — "Platform X tidak punya fitur ini karena model bisnisnya berbeda, tapi pola UX-nya tetap bisa diadopsi"

### Pitfall 3: Styling Dokumen Tidak Konsisten
**Yang bisa salah:** Tiap dari 5 dokumen punya styling berbeda, tidak ada unified look
**Cara hindari:** Copy paste seluruh `<style>` block dari `audit-assessment-training-v8.html` ke semua 5 dokumen

### Pitfall 4: Rekomendasi Tanpa Phase Mapping
**Yang bisa salah:** Rekomendasi "improve renewal UI" tanpa explicit Phase 230 mapping — planner tidak tahu di mana harus menaruhnya
**Cara hindari:** Setiap baris rekomendasi WAJIB punya kolom "Target Phase"

### Pitfall 5: File Dibuat di Lokasi Salah
**Yang bisa salah:** File dibuat di `docs/plans/` bukan `docs/`
**Cara hindari:** Semua 5 file di `docs/research-*.html` (root docs folder)

---

## Code Examples

### HTML Template Base (untuk tiap dokumen riset)

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>[Judul Topik] — Portal HC KPB Research</title>
<style>
/* COPY SELURUH STYLE BLOCK DARI docs/audit-assessment-training-v8.html */
/* Tambahan untuk research docs: */
.badge-must { background: var(--red); color:#fff; padding:2px 8px; border-radius:10px; font-size:11px; font-weight:600; }
.badge-should { background: var(--yellow); color:#333; padding:2px 8px; border-radius:10px; font-size:11px; font-weight:600; }
.badge-nice { background: var(--blue-light); color:var(--blue); padding:2px 8px; border-radius:10px; font-size:11px; font-weight:600; }
.platform-section { border-left: 4px solid var(--blue); padding-left: 16px; margin: 20px 0; }
.portal-comparison { background: var(--blue-light); border-radius: 6px; padding: 12px 16px; margin: 8px 0; }
.portal-comparison.gap { background: var(--red-light); }
.portal-comparison.match { background: var(--green-light); }
</style>
</head>
<body>
<div class="header">
  <h1>[Judul Topik] <span class="badge-ver">Research v8.1</span></h1>
  <div class="subtitle">Portal HC KPB — Milestone v8.1 Best Practices Research · [Tanggal]</div>
</div>
<div class="layout">
  <nav class="sidebar">
    <div class="nav-section">Navigasi</div>
    <a href="#overview">Overview</a>
    <a href="#platform-a">[Platform A]</a>
    <a href="#platform-b">[Platform B]</a>
    <a href="#platform-c">[Platform C]</a>
    <a href="#comparison">Perbandingan</a>
    <a href="#recommendations">Rekomendasi</a>
  </nav>
  <div class="main">
    <!-- Konten sections di sini -->
  </div>
</div>
</body>
</html>
```

### Tabel Perbandingan Fitur (template)

```html
<table>
  <thead>
    <tr>
      <th>Fitur</th>
      <th>Coursera</th>
      <th>LinkedIn Learning</th>
      <th>TalentGuard</th>
      <th>Portal HC KPB</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Expiry date pada sertifikat</td>
      <td>Tidak</td>
      <td>Tidak</td>
      <td>Ya (configurable)</td>
      <td>Ya</td>
    </tr>
    <!-- dst -->
  </tbody>
</table>
```

### Tabel Rekomendasi (template)

```html
<table>
  <thead>
    <tr>
      <th>Tier</th>
      <th>Aspek</th>
      <th>Gap vs Best Practice</th>
      <th>Rekomendasi</th>
      <th>Target Phase</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><span class="badge-must">Must-fix</span></td>
      <td>Expiry urgency visual</td>
      <td>Tidak ada color coding (merah/kuning/hijau) pada status sertifikat</td>
      <td>Tambah color-coded badge: Expired (merah), Akan Expired &lt;30 hari (kuning), Valid (hijau)</td>
      <td>Phase 230</td>
    </tr>
    <tr>
      <td><span class="badge-should">Should-improve</span></td>
      <td>Notifikasi renewal proaktif</td>
      <td>Platform terbaik kirim reminder 90/30/7 hari; Portal KPB manual</td>
      <td>Tambah badge count "Expiring Soon" di Admin dashboard</td>
      <td>Phase 229</td>
    </tr>
    <tr>
      <td><span class="badge-nice">Nice-to-have</span></td>
      <td>One-click share sertifikat</td>
      <td>LinkedIn Learning punya tombol share langsung; Portal KPB download only</td>
      <td>Tambah copy-to-clipboard untuk nomor sertifikat</td>
      <td>Phase 230</td>
    </tr>
  </tbody>
</table>
```

---

## State of the Art

| Area | Pendekatan Lama | Pendekatan Saat Ini | Dampak untuk Portal |
|------|----------------|---------------------|---------------------|
| Exam monitoring | Manual proctor berjalan keliling | Real-time dashboard dengan live status | AMON-02 sudah ada, perlu live progress |
| Question randomisasi | Fixed question set | Random dari bank per peserta | Portal KPB punya packages — sudah mengarah ke sini |
| Sertifikat | PDF statis | Digital credential dengan URL verifikasi | Portal KPB generate nomor sertifikat — sudah ada dasar |
| Timer exam | Countdown display | Countdown + warning alerts (5 menit, 1 menit) | Timer ada, tapi perlu konfirmasi warning |
| Renewal tracking | Email manual dari HR | Dashboard dengan automated color-coded urgency | Gap terbesar Portal KPB |
| Answer review | Tidak ada feedback | Immediate atau delayed review setelah submit | Portal KPB punya (jika HC aktifkan) — baik |
| AI proctoring | Human proctor | AI + tab-switch detection | AINT-02/03 sedang dikembangkan di v8.0 |

---

## Preliminary Gap Analysis (Portal HC KPB vs Best Practices)

### Area: Renewal Certificate (→ Phase 229, 230)

| Gap | Severity | Recommendation Tier |
|-----|----------|---------------------|
| Tidak ada color-coded urgency (merah/kuning/hijau) pada status sertifikat | Tinggi | Must-fix |
| Tidak ada visual "Expiring Soon" counter di dashboard admin | Sedang | Should-improve |
| Renewal history chain belum tersedia sebagai modal | Sedang | Should-improve (UIUX-04) |
| Tidak ada automated notification (email/banner) sebelum expired | Rendah | Nice-to-have (NOTF-* future) |

### Area: Assessment Management (→ Phase 231)

| Gap | Severity | Recommendation Tier |
|-----|----------|---------------------|
| Tidak ada question bank terpisah (reusability soal) | Sedang | Should-improve (QBNK-* future) |
| Filter dan search di ManageAssessment perlu validasi | Sedang | Must-fix (AMGT-05) |
| Preview assessment sebagai worker tidak tersedia untuk admin | Rendah | Nice-to-have |
| Bulk package assignment flow perlu konfirmasi UX yang jelas | Sedang | Should-improve (AMGT-04) |

### Area: Exam Monitoring (→ Phase 231)

| Gap | Severity | Recommendation Tier |
|-----|----------|---------------------|
| Live progress per-peserta (answered/total) belum real-time di monitoring | Tinggi | Must-fix (AMON-02) |
| Tidak ada visual urgency pada peserta yang idle/stuck | Sedang | Should-improve |
| Token card ada, tapi UX copy belum optimal | Rendah | Nice-to-have (AMON-04) |

### Area: Exam Flow Worker (→ Phase 232)

| Gap | Severity | Recommendation Tier |
|-----|----------|---------------------|
| Tidak ada question grid navigator (jump ke soal tertentu) | Sedang | Should-improve |
| Warning alert sebelum timeout (5 menit / 1 menit) perlu dikonfirmasi ada | Sedang | Should-improve |
| Pre-submit confirmation dialog perlu dikonfirmasi (bukan hanya abandon) | Tinggi | Must-fix (AFLW-02) |
| Session resume: pre-populated answers perlu dikonfirmasi berfungsi | Tinggi | Must-fix (AFLW-04) |

---

## Open Questions

1. **Views yang direferensikan CONTEXT.md tidak ditemukan**
   - Yang diketahui: RenewalCertificate.cshtml, ManageAssessment.cshtml, AssessmentMonitoring.cshtml, TakeExam.cshtml tidak ada di Views/CMP/
   - Yang tidak jelas: Apakah sudah dihapus, belum dibuat, atau ada di path lain?
   - Rekomendasi: Planner harus konfirmasi dengan developer — kemungkinan view-view ini AKAN dibuat di phases 229-232 (Phase 228 riset dulu, baru buat view)

2. **Apakah AINT-02 (tab-switch detection) sudah diimplementasikan di v8.0?**
   - Yang diketahui: AINT-02 di REQUIREMENTS.md masih unchecked
   - Yang tidak jelas: Apakah ada partial implementation di StartExam.cshtml?
   - Rekomendasi: Dokumen riset harus mention tab-switch detection sebagai "in progress" untuk monitoring best practices

3. **Format data untuk comparison summary document**
   - Yang diketahui: 5 dokumen total (D-04)
   - Yang tidak jelas: Apakah dokumen summary cukup link ke 4 dokumen lain atau duplikasi data?
   - Rekomendasi: Summary menggunakan tabel ringkasan (bukan duplikasi narasi) + link ke dokumen detail

---

## Validation Architecture

> Phase ini adalah fase riset murni — tidak ada kode yang ditulis, tidak ada test yang diperlukan. Validasi dilakukan secara manual dengan checklist.

### Checklist Validasi Manual per Dokumen
| Check | Kriteria |
|-------|---------|
| Platform coverage | Minimal 3 platform per topik disebutkan |
| Flow detail | UX flow step-by-step untuk tiap platform |
| Tabel perbandingan | Ada tabel fitur dengan semua platform + Portal KPB |
| Rekomendasi 3-tier | Must-fix / Should-improve / Nice-to-have semua ada |
| Phase mapping | Tiap rekomendasi ada kolom "Target Phase" |
| HTML valid | Dokumen bisa dibuka di browser tanpa error |
| Styling konsisten | CSS vars dari audit template digunakan |

### RSCH-04 Verifikasi
RSCH-04 terpenuhi jika dokumen ke-5 (`research-comparison-summary.html`) memiliki:
- Tabel perbandingan per halaman: RenewalCertificate, ManageAssessment, AssessmentMonitoring, exam flow
- Minimal 1 rekomendasi Must-fix + 1 Should-improve + 1 Nice-to-have per halaman
- Semua rekomendasi mapped ke phase 229/230/231/232

---

## Sources

### Primary (HIGH confidence)
- Moodle Documentation (docs.moodle.org/501) — Quiz settings, admin interface patterns
- Google Workspace Learning Center (support.google.com) — Google Forms Quiz features dan limitasi
- LinkedIn Learning Help (linkedin.com/help/learning) — Certificate of Completion overview

### Secondary (MEDIUM confidence)
- SpeedExam Features Page (speedexam.net/features/) — real-time monitoring dashboard features
- TalentGuard Certification Tracking (talentguard.com/certification-tracking) — renewal workflow patterns
- Certifier Blog (certifier.io/blog) — certification management software feature comparison
- Smashing Magazine 2025 — UX strategies for real-time dashboards
- ExpirationReminder.com — notification timing patterns (90/30/14/7 hari)

### Tertiary (LOW confidence — synthesis/training knowledge)
- Examly/iamneo.ai features — dari Tracxn profile + design portfolio case study
- Exam.net monitoring features — dari general web search synthesis
- Coursera renewal patterns — dari training knowledge (tidak ada official doc yang spesifik)

---

## Metadata

**Confidence breakdown:**
- Renewal Certificate (RSCH-01): MEDIUM — Coursera dan LinkedIn tidak punya true renewal, TalentGuard dari web search
- Assessment Management (RSCH-02): HIGH — Moodle dan Google Forms dari official docs
- Exam Monitoring (RSCH-03): MEDIUM-HIGH — SpeedExam dari official features page, pola industri dari multiple sources
- Comparison Framework (RSCH-04): HIGH — template dan struktur jelas dari keputusan D-04 hingga D-08

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (30 hari — domain relatif stabil)

**Catatan penting untuk planner:**
Phase 228 adalah fase PENULISAN DOKUMEN, bukan implementasi kode. Tiap task dalam plan harus berupa "Tulis dokumen HTML [nama]" bukan "Implementasikan fitur X". Output phase adalah 5 file HTML di folder `docs/`.
