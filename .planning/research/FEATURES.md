# Feature Gap Analysis: Assessment & Training Management System

**Domain:** Corporate HR/HC Assessment & Training Management (Internal Portal Pertamina)
**Researched:** 2026-03-21
**Confidence:** HIGH — berdasarkan code inspection langsung + industry best practices research

---

## Apa yang SUDAH ADA dan BEKERJA DENGAN BAIK

Dokumentasikan dulu kekuatan sistem sebelum gap, agar roadmap tidak re-invent wheel.

| Fitur | Status | Catatan |
|-------|--------|---------|
| Exam engine with package shuffle | SOLID | Cross-ET coverage guarantee, Fisher-Yates shuffle |
| Real-time monitoring SignalR | SOLID | Connected/disconnected tracking, force-close |
| Certificate generation (HTML + PDF) | SOLID | QuestPDF, NomorSertifikat auto-gen |
| Certificate renewal chain | SOLID | Union-Find cross-type (Assessment↔Training) |
| Session resume (ElapsedSeconds + LastActivePage) | SOLID | Persist elapsed time per 30s polling |
| Attempt history archive | SOLID | Archive before reset, AttemptNumber tracking |
| ExamActivityLog audit trail | SOLID | started/page_nav/disconnected/reconnected/submitted |
| AuditLog untuk admin actions | SOLID | CreateAssessment/BulkAssign/AkhiriUjian/Reset dll |
| AssessmentCategory hierarchical | SOLID | Parent-child + signatory per category |
| Training Excel bulk import | SOLID | 12 kolom, DownloadImportTemplate |
| CertificationManagement page | SOLID | Role-scoped, all statuses |
| RenewalCertificate page | SOLID | Expired/akan expired filter |
| Auto TrainingRecord dari assessment selesai | SOLID | Assessment → TrainingRecord bridge |

---

## Gap Analysis: ASSESSMENT SYSTEM

### GAP-A1: Question Bank Independen (KRITIS)

**Standard practice:** Sistem assessment enterprise menyimpan soal di question bank terpusat yang independen dari assessment instance. Soal bisa di-tag, dicari, di-reuse, dan dianalisis kualitasnya lintas assessment.

**Kondisi sekarang:** `AssessmentQuestion` dan `PackageQuestion` keduanya punya FK ke session/package — soal hanya hidup dalam konteks satu assessment. Untuk buat assessment baru dengan topik sama, soal harus di-import ulang dari Excel.

**Akibat:**
- Tidak bisa reuse soal lintas assessment tanpa reimport
- Tidak ada item analysis (difficulty index, discrimination index per soal)
- Jika soal perlu dikoreksi, harus cari dan koreksi di setiap assessment yang pakai soal itu
- Question pool semakin besar tapi tidak bisa dikelola secara terpusat

**Rekomendasi:**
Buat `QuestionBank` model terpusat dengan `QuestionBankItem` yang independen dari session. Assessment creation flow memilih soal dari bank (bukan import per-session). Legacy path tetap bisa coexist.

---

### GAP-A2: Item Analysis — Difficulty & Discrimination Index (PENTING)

**Standard practice:** Setelah cukup peserta mengerjakan soal, sistem hitung:
- **Difficulty Index (P-value):** % peserta yang menjawab benar. Ideal: 0.3–0.7
- **Discrimination Index (D):** Kemampuan soal membedakan peserta high-performer vs low-performer. Ideal: ≥0.3
- Soal dengan D negatif atau P > 0.9 perlu di-review/ganti

**Kondisi sekarang:** `PackageUserResponse` menyimpan jawaban per soal, data ada untuk hitung metrics ini. Tapi tidak ada aggregation query, tidak ada view untuk tampilkan item quality, dan tidak ada flagging soal bermasalah.

**Akibat:** HC tidak tahu soal mana yang "terlalu mudah", "terlalu susah", atau "tidak discriminating" — soal buruk terus dipakai tanpa diketahui.

**Rekomendasi:**
Tambah view "Item Analysis" di admin assessment detail. Hitung P-value dan D per soal dari `PackageUserResponse` history. Flag soal dengan P > 0.85 (too easy) atau D < 0.2 (poor discriminator).

---

### GAP-A3: ElemenTeknis Score Tidak Dipersist (DESIGN ISSUE)

**Kondisi sekarang:** ElemenTeknis scoring dihitung di `SubmitExam` action saat grading, ditampilkan di results view, tapi **tidak disimpan ke database**. Data ini menghilang setelah session — tidak bisa di-query untuk analytics.

**Akibat:** HC tidak bisa melihat tren kelemahan ElemenTeknis lintas peserta atau lintas waktu. Laporan "nilai rata-rata per ET untuk assessment X" tidak bisa dibuat.

**Rekomendasi:**
Tambah tabel `SessionElemenTeknisScore` (SessionId, ElemenTeknis, Score, MaxScore, Percentage) yang diisi saat grading. Query-able untuk analytics.

---

### GAP-A4: UserResponse Tidak Menyimpan Timestamp (AUDIT ISSUE)

**Kondisi sekarang:** `UserResponse` dan `PackageUserResponse` tidak punya field timestamp kapan jawaban di-submit. `PackageUserResponse.SubmittedAt` ada di model, tapi ini timestamp auto-generated saat save, bukan waktu sebenarnya peserta menjawab.

**Akibat dari perspektif integrity audit:**
- Tidak bisa tahu apakah peserta menjawab soal 1 dalam 2 detik (suspicious) atau 3 menit
- Tidak ada "time per question" analysis untuk deteksi cheating pattern
- Jika ada dispute tentang jawaban, tidak ada timestamp per-answer untuk dijadikan bukti

**Rekomendasi:**
Tambah `AnsweredAt DateTime` di `PackageUserResponse` yang di-set dari client-side timestamp (dikirim via SaveAnswer endpoint). Ini juga enable "time-per-question" reporting untuk admin.

---

### GAP-A5: Tidak Ada Question Randomization Per-Option (MINOR)

**Standard practice:** Selain mengacak urutan soal, opsi jawaban (A/B/C/D) juga diacak per peserta.

**Kondisi sekarang:** Letter/opsi di-assign saat render berdasarkan shuffled position (`PackageOption` tidak punya Letter field — letters display-only). Shuffle opsi SUDAH ada implisit karena options dirender dari collection order.

**Catatan:** Perlu verifikasi apakah opsi memang di-shuffle atau hanya dirender dalam fixed DB order.

---

### GAP-A6: Tidak Ada Exam Lockdown / Tab-Switch Detection (SECURITY)

**Standard practice untuk assessment korporat:** Deteksi/log ketika peserta berpindah tab atau minimize browser selama ujian.

**Kondisi sekarang:** `ExamActivityLog` hanya log: started, page_nav, disconnected, reconnected, submitted. Tidak ada event untuk tab-switch, focus-loss, atau window-blur.

**Rekomendasi:**
Tambah `visibilitychange` / `blur` event listener di exam page. Log event "focus_lost" + "focus_returned" ke `ExamActivityLog`. Tampilkan warning di monitoring view jika ada banyak focus_lost events (bukan automatic terminate — HC yang decide).

---

## Gap Analysis: TRAINING MANAGEMENT SYSTEM

### GAP-T1: Tidak Ada Training Compliance Matrix (KRITIS)

**Standard practice:** TMS korporat punya definisi "training apa yang wajib untuk jabatan/posisi X". Dari sini bisa auto-detect: worker dengan jabatan "Senior Operator" wajib selesaikan 8 training tertentu — berapa yang sudah selesai?

**Kondisi sekarang:** Sistem tahu SIAPA sudah TRAINING APA, tapi tidak ada definisi JABATAN → TRAINING WAJIB. `WorkerTrainingStatus.CompletionPercentage` dihitung dari total training yang DI-ASSIGN ke worker, bukan dari "% training wajib yang sudah selesai".

**Akibat:**
- Compliance % tidak meaningful karena denominator (total training wajib) tidak didefinisikan
- HC tidak bisa auto-generate daftar "gap training" per individu
- Audit compliance harus dilakukan manual per-person
- Tidak ada view "section mana yang paling banyak gap wajib trainingnya"

**Rekomendasi:**
Buat `RequiredTraining` model: `{PositionTitle, SubKategori/TrainingType, MinimumScore?}`. Buat halaman admin untuk kelola matriks ini. Tampilkan compliance gap per worker di RecordsWorkerDetail.

---

### GAP-T2: Training Status Logic Ambigu (DESIGN ISSUE)

**Kondisi sekarang:** `TrainingRecord.Status` adalah string: "Passed", "Wait Certificate", "Valid". `WorkerTrainingStatus.CompletedTrainings` dihitung dari Status == "Passed" || Status == "Valid".

**Masalah:**
- "Passed" dan "Valid" keduanya dianggap "complete" tapi semantiknya berbeda
- "Wait Certificate" = sedang menunggu sertifikat, tapi training-nya sudah selesai — apakah ini "complete"?
- Tidak ada status "Expired" — hanya ada `ValidUntil` date yang lewat; expired detection via computed property `IsExpiringSoon` tapi tidak ada status "Expired" eksplisit

**Rekomendasi:**
Definisikan lifecycle training record secara eksplisit:
- `Active` = berlaku (punya sertifikat valid, tidak expired)
- `Expired` = melewati ValidUntil
- `Pending` = selesai training, belum dapat sertifikat
- `Void` = dibatalkan/tidak valid

---

### GAP-T3: Tidak Ada Training Plan / Schedule (PENTING)

**Standard practice:** TMS memiliki kemampuan untuk menjadwalkan training yang akan datang (plan), tidak hanya mencatat training yang sudah selesai.

**Kondisi sekarang:** `AssessmentSession` punya `Schedule` dan `Status = "Upcoming"/"Open"/"Completed"` — ini sudah ada untuk assessment online. Tapi `TrainingRecord` hanya untuk training yang sudah SELESAI. Tidak ada mekanisme untuk record "training X dijadwalkan untuk bulan depan untuk section Y".

**Akibat:**
- HC tidak bisa track training pipeline (yang akan datang)
- Dashboard worker hanya tampilkan upcoming assessment, bukan upcoming training
- Tidak ada planning view untuk HC tentang training schedule yang sudah direncanakan

**Rekomendasi:**
Tambah `Status` field ke TrainingRecord (atau buat `TrainingPlan` model terpisah) untuk represent planned/upcoming training. Extend DashboardHomeViewModel untuk include upcoming training.

---

### GAP-T4: Tidak Ada Bulk Status Update untuk Training Records (USABILITY)

**Kondisi sekarang:** Training record bisa di-bulk-import via Excel, tapi update status (misal: semua peserta batch training X sudah selesai) harus dilakukan manual satu per satu atau via import ulang.

**Rekomendasi:**
Tambah fitur "bulk update status" — pilih multiple training records dari tabel (filter by title/tanggal/penyelenggara) → ubah status bersama-sama. Pattern: checkbox per row + bulk action dropdown.

---

### GAP-T5: SubKategori Training Masih Raw String (DATA QUALITY)

**Kondisi sekarang:** `TrainingRecord.SubKategori` adalah raw string yang punya FK ke `AssessmentCategory.Name` (sub-category). Tapi `TrainingRecord.Kategori` adalah raw string terpisah ("PROTON", "OJT", "MANDATORY") yang tidak ter-normalize.

**Akibat:** Filtering by kategori di Records view rentan typo. Tidak ada constraint yang memastikan nilai valid.

**Rekomendasi:**
Normalize `TrainingRecord.Kategori` ke FK atau enum. Setidaknya buat lookup list yang digunakan saat import/input.

---

## Gap Analysis: ANALYTICS & REPORTING

### GAP-R1: Tidak Ada Analytics Dashboard HC (KRITIS)

**Standard practice:** TMS/LMS enterprise memiliki dashboard analitik dengan:
- Pass rate per assessment category / section
- Score distribution (histogram)
- Completion rate trend over time
- Sertifikat at-risk count (expiring in 30/60/90 days)
- Training compliance % per section

**Kondisi sekarang:**
- `ExportAssessmentResults` action ada — export ke Excel untuk satu assessment
- `ExportRecords` action ada — export semua training records ke Excel
- `UserAssessmentHistoryViewModel` ada dengan TotalAssessments, PassedCount, PassRate, AverageScore
- TAPI tidak ada halaman dashboard yang aggregate data ini secara visual

**Akibat:** HC harus export Excel, lalu buat chart manual di Excel setiap kali butuh insight. Proses yang memakan waktu dan error-prone.

**Rekomendasi:**
Buat `HCDashboard` atau extend `AdminDashboard` dengan 4-6 chart/metric card:
1. Pass rate per category (bar chart)
2. Score distribution untuk assessment terpilih (histogram)
3. Expiring certificates dalam 30/60/90 hari (heatmap atau sortable table)
4. Section compliance % (training wajib terpenuhi per section)

---

### GAP-R2: Tidak Ada Report "Assessment yang Sering Gagal" (PENTING)

**Kondisi sekarang:** Tidak ada agregasi lintas-assessment untuk tahu "assessment mana yang punya fail rate tertinggi?" atau "soal mana yang paling banyak dijawab salah?"

**Rekomendasi:**
Buat view "Assessment Health" di admin: tabel assessment dengan kolom avg_score, pass_rate, attempt_count. Sortable by pass_rate ascending untuk identify assessments yang perlu review soal-soalnya.

---

### GAP-R3: Tidak Ada Report Trend Waktu (MINOR untuk sekarang)

**Kondisi sekarang:** Semua data adalah snapshot — tidak ada query "bagaimana pass rate section X berubah dari bulan ke bulan?"

**Catatan:** Ini bisa di-derive dari data yang ada (`CompletedAt` timestamps) tapi tidak ada view yang menampilkannya.

---

## Gap Analysis: NOTIFICATION & WORKFLOW

### GAP-N1: Tidak Ada Email Notification (PENTING)

**Standard practice:** TMS mengirim email reminder:
- 90 hari sebelum sertifikat expired
- 30 hari sebelum expired
- 7 hari sebelum expired
- Hari H (expired)
- Eskalasi ke Section Head jika tidak ada action setelah 14 hari

**Kondisi sekarang:** Sistem punya `Notification` dan `UserNotification` model — ini adalah in-portal notification. Tidak ada email. Tidak ada scheduled job (background service) untuk push notification secara otomatis.

**Akibat:** Workers dan HC hanya tahu sertifikat mau expired kalau secara aktif buka portal. Silent expiry adalah risiko compliance HSE.

**Rekomendasi:**
Implementasi `IHostedService` (BackgroundService) yang jalan daily, query sertifikat yang akan expired dalam 90/30/7 hari, kirim email via `IEmailSender`. Pertamina internal pasti punya SMTP atau Exchange.

---

### GAP-N2: Notifikasi Assessment Tidak Ada untuk Worker (MINOR)

**Kondisi sekarang:** Ketika admin assign assessment ke worker, worker dapat notification in-portal. Tapi tidak ada reminder "assessment Anda dijadwalkan besok" atau "assessment Anda akan segera dimulai".

---

## Anti-Features (Jangan Dibangun)

| Anti-Feature | Kenapa Jangan | Alternatif |
|--------------|---------------|------------|
| AI-powered question generation | Over-engineering untuk context internal; butuh LLM integration yang kompleks | Perbaiki quality review workflow yang ada |
| Adaptive testing (CAT) | Terlalu kompleks untuk benefit yang minimal di konteks ini | Item difficulty analysis cukup |
| SCORM/xAPI integration | Portal ini bukan LMS eksternal; tidak ada SCORM content | Tetap gunakan custom exam engine |
| Video-based training modules | Out of scope untuk portal ini | Link ke LMS Pertamina jika ada |
| Multi-tenant / multi-company | Portal ini khusus internal KPB | Tidak perlu |
| Real-time proctoring / webcam | Over-engineering; internal corporate tidak perlu level ini | Tab-switch detection + SignalR monitoring sudah cukup |
| Gamification / badges / leaderboard | Tidak sesuai konteks assessment kompetensi teknis | Fokus pada compliance tracking |

---

## Feature Prioritization Matrix

| Feature | Operational Value | Effort | Priority |
|---------|------------------|--------|----------|
| Analytics Dashboard HC | CRITICAL | MEDIUM | P1 |
| Training Compliance Matrix | CRITICAL | MEDIUM | P1 |
| Persist ElemenTeknis Score | HIGH | LOW | P1 |
| Email notification sertifikat expired | HIGH | MEDIUM | P1 |
| Tab-switch detection logging | MEDIUM | LOW | P2 |
| Question Bank Library | HIGH | HIGH | P2 |
| Item Analysis (difficulty/discrimination) | MEDIUM | MEDIUM | P2 |
| Answer timestamp (AnsweredAt) | MEDIUM | LOW | P2 |
| Training Plan / Upcoming Training | MEDIUM | MEDIUM | P3 |
| Bulk status update training records | LOW | LOW | P3 |
| Training Status lifecycle normalize | MEDIUM | LOW | P3 |

---

## Sources

- Direct code inspection: `Models/AssessmentSession.cs`, `AssessmentQuestion.cs`, `AssessmentPackage.cs`, `PackageUserResponse.cs`, `UserResponse.cs`, `TrainingRecord.cs`, `ExamActivityLog.cs`, `AuditLog.cs`, `WorkerTrainingStatus.cs`
- Direct code inspection: `Controllers/CMPController.cs` — ElemenTeknis scoring (L2088-2136), SubmitExam flow
- Direct code inspection: `Controllers/AdminController.cs` — ExportAssessmentResults, ExportAuditLog
- Web research: TMS best practices — [Edstellar TMS Features](https://www.edstellar.com/blog/key-training-management-system-tms-features), [Training Orchestra](https://trainingorchestra.com/top-training-management-system-features/)
- Web research: Item analysis — [University of Washington Item Analysis](https://www.washington.edu/assessment/scanning-scoring/scoring/reports/item-analysis/)
- Web research: Anti-cheating — [Synap Anti-Cheat Methods](https://synap.ac/blog/anti-cheat-methods-for-online-exams)
- Web research: Certificate expiry automation — [ExpiryEdge](https://expiryedge.com/features/)
- Web research: Training matrix — [AIHR Training Matrix Guide](https://www.aihr.com/blog/training-matrix/)

---
*Feature gap research untuk: Portal HC KPB — Assessment & Training Management System*
*Researched: 2026-03-21*
