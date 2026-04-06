# Feature Landscape

**Domain:** Online Assessment Platform Enhancement — Corporate HR (Pertamina PortalHC KPB)
**Researched:** 2026-04-06
**Milestone:** v14.0 Assessment Enhancement
**Overall Confidence:** HIGH (multi-source: W3C official, UW Assessment, BMC Medical Education, Baymard Institute)

---

## Existing Baseline (Sudah Ada — Jangan Duplikasi)

Fitur berikut SUDAH SHIPPED dan menjadi fondasi v14.0:

- Multiple choice 4 opsi, shuffle soal/opsi, auto-grade
- Server-enforced timer + 2-menit grace period
- SignalR real-time monitoring
- Anti-copy protection (copy/paste/context menu block)
- Token-based exam access
- Certificate generation (KPB/{SEQ}/{MONTH}/{YEAR})
- Renewal chain (session-to-session FK)
- Activity logging (page_nav, reconnect, disconnect)
- Analytics dashboard (fail rate, trend, ET heatmap, expiring certs)
- Attempt history & reset
- Interview mode (Proton Tahun 3, manual scoring)

---

## Table Stakes

Fitur yang WAJIB ada. Tanpa ini, platform tidak layak disebut "enhanced assessment platform."

| Fitur | Mengapa Wajib | Kompleksitas | Ketergantungan pada Existing |
|-------|---------------|--------------|------------------------------|
| **Tipe Soal: True/False** | Standar minimum semua LMS (Blackboard, Canvas, Moodle). Memungkinkan test recall cepat dan populer untuk compliance check. Peserta punya 50% chance menebak — perlu dikombinasikan dengan soal lain. | Rendah | Auto-grade engine existing bisa dipakai langsung; tambah QuestionType enum + TF renderer di exam view |
| **Tipe Soal: Multiple Answer** | Diperlukan untuk soal "pilih semua yang benar" — tidak bisa digantikan MCQ biasa. Standar untuk K3 dan compliance test yang punya multiple correct answer. | Sedang | Scoring logic harus diubah. **Wajib putuskan scoring policy (all-or-nothing vs partial credit) sebelum coding** karena mempengaruhi semua kalkulasi skor |
| **Tipe Soal: Essay** | Diperlukan untuk assessment kompetensi yang butuh argumentasi, bukan hanya recall. Mirip pola Interview mode yang sudah ada — manual grading oleh HC. | Sedang | Reuse pola manual grading dari Interview mode; butuh notifikasi ke HC jika ada essay pending review |
| **Tipe Soal: Fill in the Blank** | Assessment teknis KPB (Oil & Gas) sering butuh recall presisi: nama prosedur, kode regulasi, istilah teknis. Berbeda dari MCQ karena menguji recall, bukan recognition. | Sedang | Auto-grade dengan exact match + case-insensitive. Butuh answer variations table untuk mendukung sinonim dan variasi ejaan yang valid |
| **Pre-Post Test Linking** | Gain score analysis adalah standar industri untuk membuktikan efektivitas pelatihan. Tanpa ini, HC tidak bisa tunjukkan ROI training ke manajemen Pertamina. Setiap peserta butuh unique identifier yang otomatis menghubungkan Pre dan Post — tidak boleh manual matching. | Tinggi | LinkedGroupId FK baru di AssessmentSession; wizard create assessment butuh langkah tambahan pilih tipe; schedule Post-Test terpisah |
| **Gain Score Display (Pre vs Post)** | Tanpa ini Pre-Post Test hanya 2 exam biasa yang tidak terhubung. Side-by-side comparison adalah output utama yang ditunggu HC untuk laporan ke manajemen. | Sedang | **Tergantung Pre-Post Test Linking selesai dahulu.** Sertifikat hanya dari Post-Test (sudah diputuskan di PROJECT.md) |
| **Responsive/Mobile Layout Exam** | Pekerja lapangan KPB banyak yang akses via HP Android. Jika exam tidak bisa dipakai di mobile, adoption akan sangat rendah. Studi menunjukkan 5x lebih mungkin meninggalkan site jika tidak mobile-friendly (Baymard Institute). | Sedang | Pure frontend refactor — tidak mengubah logic backend. Perlu test di Android Chrome minimum |

---

## Differentiators

Fitur yang MEMBEDAKAN platform ini dari sekadar "LMS biasa." Tidak expected, tapi tingkatkan nilai signifikan untuk HC dan manajemen.

| Fitur | Nilai Proposisi | Kompleksitas | Catatan |
|-------|----------------|--------------|---------|
| **Item Analysis: Difficulty Index** | HC bisa identifikasi soal terlalu mudah (p > 0.85) atau terlalu sulit (p < 0.25) dan revisi bank soal. Formula standar: p = (jumlah peserta menjawab benar) / (total peserta). Ini yang dilakukan Blackboard/Canvas secara otomatis. | Sedang | Hitung per soal saat exam session ditutup; simpan di tabel ItemStatistics baru. Valid dari n=1, tapi semakin akurat dengan data lebih banyak |
| **Item Analysis: Discrimination Index** | Identifikasi soal yang tidak membedakan peserta yang menguasai materi vs yang tidak. Formula Kelley: D = (% benar upper 27%) - (% benar lower 27%). Nilai D < 0.20 = soal perlu direvisi. | Tinggi | Butuh minimal 30 responden per soal untuk valid secara psikometrik. Tampilkan "Data belum cukup (n < 30)" jika kurang. Proses batch setelah session ditutup |
| **Time-per-Question Analytics** | Identifikasi soal yang memakan waktu tidak proporsional — indikasi ambiguitas, terlalu panjang, atau terlalu sulit. Data ini juga bisa deteksi soal yang di-skip dan dikerjakan terakhir. | Sedang | Data timestamp per soal sudah ada di activity log. Butuh aggregation query baru per QuestionId lintas session |
| **Comparative Report Pre vs Post** | Laporan kelompok: berapa persen peserta yang improve, flat, atau decline antara Pre dan Post. Sangat dibutuhkan untuk laporan efektivitas pelatihan ke manajemen Pertamina. | Sedang | Tergantung Pre-Post Test Linking selesai. Output: tabel dan chart distribusi gain score |
| **Font Size Control (Aksesibilitas)** | Pekerja senior (40+) sering kesulitan baca teks kecil di layar, terutama di HP. Feature sederhana tapi berdampak tinggi untuk populasi pekerja KPB. | Rendah | CSS custom property (--font-size-base) + localStorage untuk persist preference. Opsi: Kecil/Normal/Besar |
| **Skip-to-Content Link (WCAG Level A)** | Compliance dasar WCAG 2.1. Berguna untuk keyboard user yang tidak ingin tab melalui seluruh navigation bar. Satu link tersembunyi yang visible saat di-focus. | Rendah | 1 anchor link di awal halaman. Dampak minimal, compliance penting. Wajib untuk Level A |
| **Screen Reader Timer Announcement** | Timer countdown diumumkan via ARIA live region di interval tertentu (mis: setiap 5 menit, lalu setiap menit saat < 5 menit tersisa). Diperlukan untuk pengguna low-vision. | Rendah | `aria-live="polite"` + interval announcement JavaScript. WCAG 2.2.1 mewajibkan user diberi warning sebelum waktu habis |
| **Extra Time Accommodation** | Pekerja penyandang disabilitas butuh waktu tambahan. Mengikuti WCAG 2.2.1 dan Undang-Undang No. 8 Tahun 2016 tentang Penyandang Disabilitas. | Sedang | Tambah field ExtraTimeMinutes di AssessmentParticipant; HC set per-peserta saat setup assessment. Override timer calculation di server |

---

## Anti-Features

Fitur yang SENGAJA TIDAK dibangun untuk v14.0 — beserta alasannya.

| Anti-Feature | Mengapa Dihindari | Yang Dilakukan Sebagai Gantinya |
|--------------|-------------------|---------------------------------|
| **Webcam Proctoring** | Infrastruktur berat (storage video), butuh bandwidth tinggi, privacy concern di lingkungan kerja, dan platform sudah punya anti-copy protection yang memadai (Phase 280). Over-engineering untuk konteks internal corporate. | Anti-copy yang ada dipertahankan; tidak ditambah |
| **AI Auto-Grading Essay** | Akurasi tidak bisa diandalkan untuk penilaian kompetensi teknis Oil & Gas. HC perlu kontrol penuh atas grading narasi teknis yang domain-specific. | Manual grading oleh HC dengan workflow notifikasi |
| **Adaptive/Branching Questions (IRT)** | Implementasi Item Response Theory sangat kompleks. ROI tidak sebanding untuk jumlah pengguna saat ini. Butuh statistician khusus untuk kalibrasi model. | Item analysis statis (CTT — Classical Test Theory) cukup untuk kebutuhan KPB |
| **Gamification (Badges, Leaderboard)** | Kontraproduktif untuk assessment serius (K3, safety compliance). Peserta fokus ke skor relatif, bukan ke pemahaman materi. | Certificate generation yang sudah ada sudah cukup sebagai reward formal |
| **Multi-Language Support** | Semua pengguna adalah pegawai Pertamina yang berbahasa Indonesia. Investasi i18n tidak ada ROI untuk platform internal. | Tetap Bahasa Indonesia |
| **Full WCAG 2.1 AA Audit** | Cakupan terlalu luas untuk satu milestone (meliputi semua halaman, semua komponen, semua role). Perlu resource aksesibilitas khusus. | Implementasi WCAG quick wins yang paling relevan untuk exam flow (skip-link, keyboard nav, screen reader timer, font size) |
| **Social/Peer Learning** | Tidak relevan untuk konteks competency assessment korporat. Distraksi dari fokus pengukuran kompetensi individual. | Tetap fokus pada individual assessment dan HC reporting |

---

## Feature Dependencies

```
Tipe Soal: True/False
  └── Tidak ada dependensi baru
  └── Pakai auto-grade engine yang ada
  └── Tambah QuestionType.TrueFalse enum value

Tipe Soal: Multiple Answer
  └── [KEPUTUSAN WAJIB] Scoring policy: all-or-nothing vs partial credit
      └── Mempengaruhi ExamSubmit score calculation
      └── Mempengaruhi tampilan score breakdown ke worker
  └── UI: checkbox (bukan radio button) untuk opsi jawaban

Tipe Soal: Essay
  └── Manual grading workflow
      └── Notifikasi ke HC saat essay pending review (reuse notification system)
      └── HC grading UI: input skor + komentar per essay
      └── Worker view hasil: tampil setelah HC submit grade
  └── Essay tidak termasuk dalam auto-grade total skor
      └── Final score = (skor auto-grade + skor essay manual) / total bobot

Tipe Soal: Fill in the Blank
  └── AnswerVariation table baru (QuestionId, AcceptableAnswer, CaseSensitive)
  └── Case-insensitive comparison (default)
  └── Auto-grade: exact match dari AnswerVariation list

Pre-Post Test Linking
  └── LinkedGroupId kolom baru di AssessmentSession
  └── AssessmentType enum (Standard, PrePostTest, Interview)
  └── Assessment creation wizard: tambah step pilih tipe + link Pre ke Post
  └── Post-Test scheduling terpisah dari Pre-Test
  └── Cascade reset: reset Pre → reset Post (wajib data integrity check)
  └── Nilai Pre dan Post independen (sudah diputuskan PROJECT.md)
      └── Sertifikat hanya dari Post-Test
          └── Gain Score Display
              └── Side-by-side comparison view (worker: 2 card linked)
              └── Comparative Report kelompok (HC/admin)

Item Analysis (Difficulty Index)
  └── ItemStatistics table baru (QuestionId, SessionId, CorrectCount, TotalCount, PValue)
  └── Dihitung saat exam session ditutup (background task / trigger)
  └── Tampil di admin question management

Item Analysis (Discrimination Index)
  └── Bergantung pada ItemStatistics (Difficulty Index) selesai
  └── Butuh n >= 30 responden per soal untuk valid
  └── Upper/Lower 27% calculation (Kelley, 1939)
  └── Tampilkan warning jika data belum cukup

Time-per-Question Analytics
  └── Data sudah ada di activity log (timestamps per question navigation)
  └── Butuh aggregation view/query baru

Comparative Report Pre vs Post
  └── Bergantung pada Pre-Post Test Linking selesai

Mobile Optimization
  └── Pure frontend — tidak ada dependensi logic backend
  └── Test: Android Chrome, iOS Safari
  └── Perlu refactor exam template CSS/HTML

WCAG: Skip-to-Content
  └── Independent — 1 HTML anchor element

WCAG: Keyboard Navigation Exam
  └── Refactor exam HTML menggunakan semantic elements (button, label, input)
  └── Pastikan tab order logis

WCAG: Screen Reader Timer
  └── Bergantung pada timer component
  └── Tambah aria-live region + JavaScript interval

WCAG: Font Size Control
  └── Independent — CSS variable + localStorage

WCAG: Extra Time Accommodation
  └── Bergantung pada session model
  └── Tambah field ExtraTimeMinutes di AssessmentParticipant
  └── Override timer calculation di server
```

---

## MVP Recommendation untuk v14.0

### Fase 1 — Foundation Question Types + Pre-Post
*Harus selesai dulu — semua fitur lain bergantung pada ini*

1. **Assessment Type enum** (Standard/PrePostTest/Interview) — prerequisite Pre-Post
2. **Tipe Soal: True/False** — kompleksitas rendah, langsung value
3. **Tipe Soal: Multiple Answer** — selesaikan scoring policy decision dulu
4. **Pre-Post Test Linking + Gain Score Display** — feature paling ditunggu HC

### Fase 2 — Essay, Fill in the Blank, Mobile
*Setelah Fase 1 stable*

5. **Tipe Soal: Essay** dengan manual grading workflow
6. **Tipe Soal: Fill in the Blank** dengan answer variations
7. **Mobile Responsive Exam Layout** — adoption blocker untuk pekerja lapangan

### Fase 3 — Reporting + Accessibility
*Nilai tambah dan compliance*

8. **Item Analysis (Difficulty + Discrimination Index)**
9. **Font Size Control + Skip-to-Content** (WCAG quick wins)
10. **Screen Reader Timer + Extra Time Accommodation**
11. **Comparative Report Pre vs Post**
12. **Time-per-Question Analytics**

### Defer (Bukan v14.0)
- Full WCAG 2.1 AA audit lintas halaman
- Advanced psychometric (IRT, Cronbach's alpha) — butuh statistician
- AI essay grading

---

## Catatan Keputusan Penting

### [KEPUTUSAN WAJIB] Multiple Answer: Scoring Policy
Harus diputuskan SEBELUM coding Multiple Answer:

- **All-or-Nothing:** Nilai hanya jika semua opsi benar dipilih dan tidak ada yang salah. Lebih mudah dijelaskan ke peserta. Lebih tegas untuk compliance assessment.
- **Partial Credit:** Nilai proporsional. Lebih adil tapi kompleksitas penjelasan skor meningkat.

**Rekomendasi untuk KPB: All-or-Nothing** — konsisten dengan konteks compliance K3 dan safety assessment di industri minyak dan gas.

### Item Analysis: Minimum Responden
- Difficulty Index (p-value): valid dari n=1, semakin akurat dengan lebih banyak data
- Discrimination Index: **tidak valid jika n < 30** (standar psikometrik)
- Platform harus tampilkan "Data belum cukup (butuh min. 30 responden)" jika n < 30
- Gunakan Upper/Lower 27% (bukan 25% atau 33%) sesuai standar Kelley

### Pre-Post Test: Cascade Reset
Jika HC reset Pre-Test, Post-Test HARUS ikut direset. Business rule ini mempengaruhi data integrity. Implementasi sebagai application-level guard (bukan hanya DB cascade) agar ada audit trail reset.

### Essay: Skor Final Komposit
Ketika exam mengandung campuran soal auto-grade dan essay:
- Auto-grade diproses langsung saat submit
- Essay pending sampai HC grading
- Final score = (auto-grade score + essay score) / total bobot exam
- Status exam: "Menunggu Penilaian Essay" sampai HC submit grade

---

## Sources

- [University of Washington: Understanding Item Analyses](https://www.washington.edu/assessment/scanning-scoring/scoring/reports/item-analysis/) — HIGH confidence
- [BMC Medical Education: Item Analysis and Distractor Efficiency (2024)](https://link.springer.com/article/10.1186/s12909-024-05433-y) — HIGH confidence
- [WCAG 2.1 Official Standard — W3C](https://www.w3.org/TR/WCAG21/) — HIGH confidence
- [TestParty: WCAG 2.1.1 Keyboard Guide 2025](https://testparty.ai/blog/wcag-2-1-1-keyboard-2025-guide) — MEDIUM confidence
- [Baymard Institute: Mobile UX Trends 2025](https://baymard.com/blog/mobile-ux-ecommerce) — HIGH confidence
- [ScoreApp: 12 Ways to Make Quiz Mobile-Friendly](https://www.scoreapp.com/mobile-friendly-quiz/) — MEDIUM confidence
- [ERIC: Brief Guide to Pre-Post Assessments (SD DOE)](https://files.eric.ed.gov/fulltext/ED604574.pdf) — HIGH confidence
- [SpeedExam: Fill in the Blanks Question Types](https://www.speedexam.net/blog/what-is-fill-in-the-blanks-question-types-and-example/) — MEDIUM confidence
- [NIU Blackboard: Online Assessment Question Types](https://www.niu.edu/blackboard/assess/tests-and-quizzes/question-types.shtml) — HIGH confidence
- [University of Waterloo: Exam Questions Types and Characteristics](https://uwaterloo.ca/centre-for-teaching-excellence/catalogs/tip-sheets/exam-questions-types-characteristics-and-suggestions) — HIGH confidence

---
*Feature research for: v14.0 Assessment Enhancement — PortalHC KPB*
*Researched: 2026-04-06*
