# Project Research Summary

**Project:** Portal HC KPB — v14.0 Assessment Enhancement
**Domain:** Brownfield Online Assessment Platform Enhancement — Corporate HR (ASP.NET Core MVC)
**Researched:** 2026-04-06
**Confidence:** HIGH

## Executive Summary

Portal HC KPB v14.0 adalah peningkatan brownfield pada platform assessment yang sudah matang. Stack utama (ASP.NET Core MVC + EF Core + SignalR + Bootstrap 5 + Chart.js 4) sudah tervalidasi di production dan tidak diubah. Penambahan library baru sangat minimal: hanya Quill.js 2.0.3 (rich text editor soal essay) dan chartjs-plugin-annotation 3.1.0 (visualisasi gain score) via CDN conditional per halaman. Mobile navigation menggunakan CSS Scroll Snap native — zero dependency. Pendekatan ini sangat tepat karena menjaga overhead dependency serendah mungkin di lingkungan MVC tanpa bundler.

Fitur inti yang ditarget adalah penambahan 4 tipe soal baru (True/False, Multiple Answer, Essay, Fill in the Blank), sistem Pre-Post Test dengan gain score analysis, mobile optimization untuk pekerja lapangan, advanced reporting item analysis, dan WCAG accessibility quick wins. Riset membuktikan bahwa semua fitur ini memiliki fondasi arsitektur yang kuat di kodebase existing — sebagian besar adalah ekstensi data model yang backward-compatible, bukan penulisan ulang. Sertifikat hanya diterbitkan dari Post-Test dan nilai Pre-Post bersifat independen (sudah diputuskan di PROJECT.md).

Risiko terbesar adalah teknis, bukan domain: `AssessmentAdminController.cs` sudah 3.828 baris dan grading engine memiliki asumsi "single-correct answer" yang tersebar di 4+ titik berbeda. Menambah Multiple Answer atau Essay tanpa mengekstrak `GradingService` terlebih dahulu akan menciptakan bug yang sulit ditemukan dan berisiko tinggi. **Langkah pertama wajib adalah ekstraksi GradingService dan penambahan `QuestionType` ke model sebelum fitur apapun dikerjakan.** Di luar itu, semua dependensi antar fase sudah terpetakan dengan jelas dan fase admin Pre-Post serta fase Question Types bisa dikerjakan secara paralel.

## Key Findings

### Recommended Stack

Stack existing tidak diubah. Penambahan runtime hanya 2 CDN script (~430KB total) yang dimuat secara conditional. Tidak ada npm, bundler, atau build tool baru yang diperlukan.

**Core technologies (tambahan):**
- **Quill.js 2.0.3**: Rich text editor untuk soal dan jawaban essay — gratis (BSD-3-Clause), TypeScript rewrite April 2024, mobile-optimized, integrasi Razor via hidden field
- **chartjs-plugin-annotation 3.1.0**: Anotasi garis referensi dan label pada radar/bar chart — extends Chart.js 4 yang sudah ada, tidak perlu library chart baru
- **CSS Scroll Snap (native browser API)**: Navigasi swipe soal di mobile — zero download, GPU-accelerated, Hammer.js sengaja dihindari karena tidak dimaintain 6+ tahun
- **axe DevTools (browser extension only)**: WCAG audit manual selama development — tidak masuk production bundle

Lihat detail: `.planning/research/STACK.md`

### Expected Features

**Harus ada (table stakes) — v14.0:**
- True/False — tipe soal dasar, kompleksitas rendah, pakai auto-grade engine existing
- Multiple Answer — pilih semua yang benar, scoring all-or-nothing (direkomendasikan untuk compliance K3)
- Essay — manual grading oleh HC, reuse pola interview mode existing
- Fill in the Blank — recall presisi teknis, case-insensitive + answer variations
- Pre-Post Test Linking + Gain Score Display — feature paling ditunggu HC untuk bukti ROI training
- Mobile Responsive Exam Layout — adoption blocker untuk pekerja lapangan

**Sebaiknya ada (differentiator) — v14.0:**
- Item Analysis: Difficulty Index (p-value per soal)
- Item Analysis: Discrimination Index (valid hanya jika n >= 30, tampilkan warning)
- Comparative Report Pre vs Post (kelompok)
- Font Size Control (CSS variable + localStorage)
- Skip-to-Content Link (WCAG Level A, 1 anchor element)
- Screen Reader Timer Announcement (aria-live="polite", interval bermakna)
- Extra Time Accommodation (ExtraTimeMinutes per peserta)

**Defer (bukan v14.0):**
- Full WCAG 2.1 AA audit lintas halaman
- Advanced psychometric (IRT, Cronbach's alpha)
- AI essay grading
- Webcam proctoring
- Gamification

**Keputusan wajib sebelum coding Multiple Answer:** All-or-Nothing vs Partial Credit. Rekomendasi penelitian: All-or-Nothing — konsisten dengan konteks compliance K3 dan safety assessment Oil & Gas.

Lihat detail: `.planning/research/FEATURES.md`

### Architecture Approach

Arsitektur brownfield mengekstensi model data yang sudah ada secara backward-compatible. Semua migrasi DB menggunakan nullable column dengan default value sehingga tidak ada breaking change. Pre-Post Test diimplementasi sebagai 2 `AssessmentSession` terpisah yang dihubungkan via `LinkedGroupId` (GUID) — keputusan yang benar karena scoring engine, SignalR batchKey, dan certificate logic semuanya berbasis SessionId tunggal. Advanced reporting tidak memerlukan tabel baru sama sekali — semua data sudah ada di `PackageUserResponse`, `ExamActivityLog`, dan `AssessmentSession`.

**Komponen baru utama:**
1. `GradingService` — ekstrak dari `AssessmentAdminController`, memusatkan semua grading logic
2. `AssessmentSession` (+5 kolom) — AssessmentType, LinkedGroupId, LinkedSessionId, AssessmentPhase, ExtraTimeMinutes
3. `PackageQuestion.QuestionType` — enum sebagai string, default "MultipleChoice", backward-compatible
4. `PackageUserResponse.TextAnswer` — untuk Essay dan Fill in the Blank, nullable
5. `CMPController.PrePostComparison` — action baru, side-by-side + gain score worker
6. `AssessmentAdminController.GradeEssay` — HC manual grading Essay
7. `ItemAnalysisReport` + `PrePostGainScoreRow` ViewModels
8. `wwwroot/js/exam-mobile.js` + CSS mobile exam

**Urutan build berdasarkan dependensi:**
```
Fase 1 (Migration DB + GradingService)
  ├── Fase 2 (Admin Pre-Post) → Fase 4 (Worker Pre-Post)
  └── Fase 3 (Question Types) → Fase 5 (Mobile) → Fase 7 (Accessibility)
                              └── Fase 6 (Reporting) ← juga dari Fase 2
```

Lihat detail: `.planning/research/ARCHITECTURE.md`

### Critical Pitfalls

1. **Grading engine asumsi single-correct hancur oleh Multiple Answer** — `GradeFromSavedAnswers` iterasi satu OptionId per soal; Multiple Answer akan grade sebagai 0. Cegah dengan ekstraksi `GradingService` di Fase 1 dan storage `AnswerText` (comma-separated IDs) untuk Multiple Answer. Update semua 4 titik grading (GradeFromSavedAnswers, SubmitExam, EndExam, EndAllExams).

2. **Essay `HasManualGrading` tidak di-guard di semua jalur auto-grade** — sesi dengan Essay akan auto-grade dengan skor inflated (maxScore salah karena soal Essay tidak punya options). Cegah dengan `QuestionType` di model sebelum Essay UI dibuat, guard `NomorSertifikat` dengan `!session.HasPendingManualGrading`.

3. **Pre-Post `LinkedGroupId` cascade tidak konsisten** — Reset/Delete/Renew yang hanya update satu session tanpa memeriksa pasangannya. Cegah dengan helper `GetLinkedPartner()` yang dipanggil di semua action state-changing, dan state machine tabel Pre-Post yang terdokumentasi.

4. **`AssessmentAdminController` 3.828 baris — perubahan berisiko tinggi** — Setiap tambahan meningkatkan risiko bug secara non-linear. Cegah dengan ekstraksi `GradingService` SEBELUM menambah fitur apapun. Pola ini sudah terbukti berhasil di v12.0 (8.514 baris → 108 baris).

5. **Mobile touch events konflik dengan anti-copy JavaScript** — Swipe navigation (touchstart/touchmove) bisa ter-intercept oleh anti-copy handler. Cegah dengan audit semua `addEventListener` di exam view JS sebelum menulis touch handler mobile, dan test di device fisik (bukan DevTools emulator).

6. **`aria-live` timer konflik dengan SignalR DOM updates** — `aria-live` pada container besar yang juga di-update SignalR akan membuat screen reader announce setiap SignalR message. Cegah dengan scope `aria-live` hanya pada element timer sendiri, dengan interval announce bermakna (bukan tiap detik).

Lihat detail: `.planning/research/PITFALLS.md`

## Implications for Roadmap

Berdasarkan riset, struktur fase yang disarankan:

### Phase 1: Data Foundation + GradingService Extraction
**Rationale:** Semua fase lain bergantung pada migrasi DB yang selesai dan GradingService yang sudah terekstrak. Ini adalah perubahan zero-breaking-change dan zero-new-feature — murni fondasi. Tanpa ini, Multiple Answer dan Essay akan menciptakan grading bug silent yang sulit debug di production.
**Delivers:** EF Core migration tunggal dengan semua kolom baru (backward-compatible), `GradingService` terekstrak dari controller, sistem existing tetap berjalan 100%.
**Addresses:** True/False, Multiple Answer, Essay, Fill in the Blank (fondasi model), Pre-Post linking (fondasi model)
**Avoids:** Pitfall grading single-correct, Pitfall Essay guard, Pitfall controller terlalu panjang

### Phase 2: Assessment Type + Pre-Post Test (Admin Side)
**Rationale:** HC butuh bisa membuat dan mengelola assessment PrePostTest sebelum worker bisa menggunakannya. Bisa dikerjakan paralel dengan Phase 3 karena menyentuh area kode yang berbeda (CreateAssessment vs Question Types).
**Delivers:** HC bisa create Pre-Post assessment, monitoring view terupdate dengan grouping Pre+Post, cascade reset berfungsi.
**Uses:** LinkedGroupId, LinkedSessionId, AssessmentPhase kolom dari Phase 1
**Avoids:** Pitfall cascade Pre-Post tidak konsisten — state machine didefinisikan dan tested di sini

### Phase 3: Question Types (Admin + Worker)
**Rationale:** Fondasi fitur assessment yang paling banyak diminta. True/False paling sederhana, Multiple Answer butuh storage decision (AnswerText CSV), Essay butuh grading workflow, Fill in the Blank butuh answer variations. Bisa paralel dengan Phase 2.
**Delivers:** HC bisa buat soal 4 tipe baru, worker bisa mengerjakan soal tipe baru, scoring per tipe berfungsi.
**Uses:** Quill.js 2.0.3 untuk Essay editor, QuestionType + TextAnswer kolom dari Phase 1
**Avoids:** Pitfall grading Multiple Answer (AnswerText storage), Pitfall Essay manual grading guard

### Phase 4: Worker Flow Pre-Post + Comparison View
**Rationale:** Worker perlu bisa mengerjakan Pre-Post dan melihat hasilnya. Bergantung pada Phase 2 selesai (Admin Pre-Post harus ada dulu).
**Delivers:** Worker lihat 2 card Pre/Post dengan badge dan status gating, bisa akses PrePostComparison view dengan gain score.
**Implements:** CMPController.PrePostComparison action baru, status gating Post hanya setelah Pre Completed

### Phase 5: Mobile Optimization
**Rationale:** Adoption blocker untuk pekerja lapangan. Dikerjakan setelah Phase 3 karena StartExam view harus sudah stabil (Phase 3 melakukan refactor besar di view yang sama) untuk menghindari conflict besar.
**Delivers:** Exam UI optimal di Android Chrome dan iOS Safari, swipe navigation, bottom nav bar, timer sticky header.
**Uses:** CSS Scroll Snap (native, zero dependency), wwwroot/js/exam-mobile.js baru
**Avoids:** Pitfall mobile touch konflik anti-copy — audit anti-copy JS sebelum touch handler ditulis

### Phase 6: Advanced Reporting
**Rationale:** Nilai tambah untuk HC dan manajemen Pertamina. Bergantung pada Phase 2 (Pre-Post data) dan Phase 3 (question response data) selesai.
**Delivers:** Item Analysis (Difficulty + Discrimination Index), Pre-Post Gain Score Report kelompok, export Excel.
**Uses:** chartjs-plugin-annotation 3.1.0 untuk radar chart + anotasi garis threshold
**Implements:** ItemAnalysisReport + PrePostGainScoreRow ViewModels, ExportItemAnalysis action

### Phase 7: Accessibility (WCAG Quick Wins)
**Rationale:** Polesan terakhir — harus dikerjakan setelah StartExam view final (Phase 3 dan 5). Aksesibilitas adalah lapisan di atas UI yang sudah stabil.
**Delivers:** Skip-to-content link, keyboard navigation soal, screen reader timer (aria-live polite), font size control, extra time accommodation.
**Avoids:** Pitfall aria-live konflik SignalR — scope aria-live ke element timer sendiri, bukan parent container

### Phase Ordering Rationale

- **Phase 1 tidak bisa dilewati** — grading bug dari Multiple Answer dan Essay adalah silent failure yang sulit didiagnosa di production. Investasi ekstraksi GradingService di awal sangat sepadan dengan risiko yang dicegah.
- **Phase 2 dan 3 paralel** — area kode berbeda (CreateAssessment vs StartExam/SubmitExam). Hemat waktu tanpa risiko conflict.
- **Phase 4 setelah Phase 2** — worker tidak bisa akses Pre-Post flow sebelum Admin bisa membuatnya.
- **Phase 5 setelah Phase 3** — StartExam view direfactor besar di Phase 3; mengerjakan mobile di view yang belum stabil akan membuat merge conflict besar.
- **Phase 6 setelah Phase 2 dan 3** — butuh data Pre-Post dan question responses untuk reporting yang bermakna.
- **Phase 7 terakhir** — aksesibilitas adalah polesan di atas UI yang sudah stabil; melakukannya lebih awal berarti mengerjakan dua kali.

### Research Flags

Phases yang mungkin perlu `/gsd-research-phase` saat planning:
- **Phase 3 (Question Types):** Multiple Answer storage decision (CSV vs multi-row) memiliki implikasi jangka panjang untuk item analysis. Perlu keputusan eksplisit dan terdokumentasi sebelum coding dimulai.
- **Phase 6 (Reporting):** Discrimination Index (Kelley Upper/Lower 27%) memiliki edge case pada n < 30. Perlu spesifikasi UX eksplisit tentang bagaimana dashboard menangani data belum cukup.

Phases dengan pola standar (skip research-phase):
- **Phase 1 (DB Migration):** Pola EF Core migration sudah sangat well-documented di codebase ini. Semua kolom baru nullable dengan default.
- **Phase 5 (Mobile):** CSS Scroll Snap adalah browser native API dengan dokumentasi MDN lengkap dan implementasi straightforward.
- **Phase 7 (Accessibility):** WCAG quick wins (skip-link, aria-live, font size) adalah pola standar.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Semua library diverifikasi dari official docs + npm resmi. CSS Scroll Snap dari MDN + web.dev. Quill 2.0.3 dari official release docs April 2024. |
| Features | HIGH | Multi-source: W3C official, University of Washington Assessment, BMC Medical Education (2024), Baymard Institute, University of Waterloo |
| Architecture | HIGH | Berdasarkan pembacaan kode sumber langsung — bukan inference. Semua titik integrasi diverifikasi dari file aktual dengan line numbers spesifik. |
| Pitfalls | HIGH | Berdasarkan analisis kodebase aktual (line numbers, method names spesifik) + pola brownfield yang sudah terbukti di proyek ini (v12.0 refactoring, v7.6 IWorkerDataService) |

**Overall confidence: HIGH**

### Gaps to Address

- **Multiple Answer Scoring Policy:** Riset merekomendasikan All-or-Nothing untuk konteks K3/compliance, tapi keputusan final harus dikonfirmasi dengan stakeholder HC sebelum Phase 3 coding dimulai. Ini mempengaruhi scoring calculation dan penjelasan skor ke peserta.
- **Essay Maximum Character Limit:** Belum ada keputusan tentang batas panjang jawaban essay. Perlu diputuskan saat Phase 3 planning agar TextAnswer column type (nvarchar(max) vs nvarchar(2000)) tepat.
- **Item Analysis n-threshold Display:** Discrimination Index tidak valid jika n < 30. Perlu keputusan UX: apakah soal disembunyikan dari report, ditampilkan dengan warning, atau di-flag berbeda? Rekomendasi: tampilkan dengan warning "Data belum cukup (butuh min. 30 responden)".
- **Pre-Post Renewal Behavior:** Jika HC membuat renewal untuk Pre-Post Test assessment, apakah renewal otomatis membuat 2 sesi baru (Pre baru + Post baru)? Behavior ini belum diputuskan eksplisit di PROJECT.md dan mempengaruhi `CreateRenewal` action.

## Sources

### Primary (HIGH confidence)
- Kode sumber aktual: `Controllers/AssessmentAdminController.cs` (3828 baris), `Controllers/CMPController.cs`, `Models/AssessmentSession.cs`, `Models/AssessmentPackage.cs`, `Models/PackageUserResponse.cs`
- `.planning/PROJECT.md` — keputusan desain Pre-Post Test, certificate dari Post-Test, cascade spec
- [Quill 2.0 Official Docs](https://quilljs.com/) + [npm v2.0.3](https://www.npmjs.com/package/quill)
- [chartjs-plugin-annotation npm (v3.1.0)](https://www.npmjs.com/package/chartjs-plugin-annotation)
- [CSS Scroll Snap — MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/scroll-snap-type) + [web.dev](https://web.dev/css-scroll-snap/)
- [Deque.AxeCore.Playwright NuGet (v4.11.1)](https://www.nuget.org/packages/Deque.AxeCore.Playwright)
- [University of Washington: Understanding Item Analyses](https://www.washington.edu/assessment/scanning-scoring/scoring/reports/item-analysis/)
- [WCAG 2.1 Official Standard — W3C](https://www.w3.org/TR/WCAG21/)
- [ERIC: Brief Guide to Pre-Post Assessments (SD DOE)](https://files.eric.ed.gov/fulltext/ED604574.pdf)

### Secondary (MEDIUM confidence)
- [Baymard Institute: Mobile UX Trends 2025](https://baymard.com/blog/mobile-ux-ecommerce) — mobile adoption impact data
- [Which rich text editor in 2025 — Liveblocks](https://liveblocks.io/blog/which-rich-text-editor-framework-should-you-choose-in-2025) — Quill vs TinyMCE comparison
- [BMC Medical Education: Item Analysis (2024)](https://link.springer.com/article/10.1186/s12909-024-05433-y) — discrimination index validity threshold
- [NIU Blackboard: Online Assessment Question Types](https://www.niu.edu/blackboard/assess/tests-and-quizzes/question-types.shtml)
- [University of Waterloo: Exam Questions Types](https://uwaterloo.ca/centre-for-teaching-excellence/catalogs/tip-sheets/exam-questions-types-characteristics-and-suggestions)

### Tertiary (LOW confidence)
- Pola umum brownfield assessment system — inference dari pengalaman general; divalidasi terhadap kodebase aktual

---
*Research completed: 2026-04-06*
*Ready for roadmap: yes*
