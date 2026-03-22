# Feature Landscape — Proton Coaching Ecosystem Audit

**Domain:** Competency-based coaching/mentoring platform (enterprise HR, industrial workforce)
**Researched:** 2026-03-22
**Context:** Audit existing PortalHC KPB Proton Coaching system terhadap best practices platform 360Learning, BetterUp, CoachHub, Torch, MentorcliQ, Qooper, Simply.Coach

---

## Metodologi Penelitian

Platform yang dibandingkan:
- **360Learning** — Collaborative LMS, skill matrix, competency tracking
- **BetterUp** — 1:1 coaching, behavioral science, AI-personalized journeys
- **CoachHub** — Digital coaching platform, progress tracking, HR integration
- **Torch** — Leadership coaching + mentoring, analytics tied to business outcomes
- **MentorcliQ** — Enterprise mentoring, matching, multi-format programs
- **Qooper** — AI matching, training, guidance, reporting
- **Simply.Coach** — Session management, action items, progress tracking

---

## Area 1: Competency-Based Development Tracking

### Table Stakes

Fitur yang pengguna dan admin harapkan ada. Tidak ada = platform terasa tidak lengkap.

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Hierarchical competency framework (Kompetensi → SubKompetensi → Deliverable) | Standar industri: setiap platform kompetensi enterprise menggunakan hirarki | Rendah | **Sudah ada** di PortalHC |
| Status progress per deliverable (belum/dalam proses/selesai) | Pengguna harus tahu posisi mereka | Rendah | **Sudah ada** |
| Overall completion percentage per worker | Dashboard standar di semua platform (CoachHub, BetterUp, MentorcliQ) | Rendah | **Sudah ada** di CDP Dashboard |
| Track/path assignment sesuai peran dan tahun | 360Learning dan Torch menggunakan learning paths per role | Rendah | **Sudah ada** (6 tracks) |
| Competency level granting setelah completion | Final certification setelah semua deliverable selesai — standar CoachHub & Torch | Sedang | **Sudah ada** (Final Assessment) |
| Audit trail perubahan status | Compliance requirement enterprise; semua platform enterprise mendukung ini | Rendah | **Sudah ada** (Deliverable status history) |

### Differentiators

Fitur yang membedakan, tidak diharapkan tapi bernilai tinggi.

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Real-time skills gap visualization (radar/spider chart per worker) | BetterUp dan CoachHub menampilkan gap kompetensi secara visual; meningkatkan self-awareness | Sedang | **Belum ada** — CDP Dashboard saat ini hanya bar/line chart agregat |
| Competency matrix view (worker x kompetensi, color-coded) | Weever Skills Matrix, TalentGuard — memungkinkan HC melihat seluruh tim sekaligus | Sedang | **Belum ada** |
| Individual development timeline (milestone history visual) | Torch dan Simply.Coach menampilkan timeline perjalanan pengembangan | Rendah | **Sebagian ada** di HistoriProton, bisa diperluas |
| Predicted completion date berdasarkan kecepatan progress | Qooper dan MentorcliQ menyediakan proyeksi | Tinggi | **Belum ada** — kompleksitas tinggi, low priority |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Self-assessment competency rating tanpa bukti (subjektif) | Rentan gaming; tidak credible untuk industrial certification | Gunakan evidence submission berbasis file/artefak nyata |
| Competency framework yang terlalu dalam (lebih dari 4 level hirarki) | Overhead kognitif tinggi; MentorcliQ menyarankan max 3 level untuk engagement | Tetap di 3 level: Kompetensi - SubKompetensi - Deliverable |
| Mandatory scheduled re-assessment tanpa trigger event | Friction tinggi; CoachHub merekomendasikan on-demand atau milestone-triggered | Assessment triggered oleh completion event |

### Dependensi pada Implementasi Existing

- Silabus CRUD (3-level hierarchy) — fondasi, harus stabil sebelum tambah visualisasi
- Track Assignment (6 tracks) — data track dibutuhkan untuk competency matrix
- Final Assessment → CompetencyLevel — endpoint yang harus ada sebelum gap analysis

---

## Area 2: Evidence/Artifact Submission Workflows

### Table Stakes

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| File upload per deliverable | Standar di semua platform assessment/competency; bukti konkret wajib ada | Rendah | **Sudah ada** |
| Preview/download file yang disubmit | Reviewer harus bisa melihat evidence tanpa download lokal | Rendah | **Perlu audit** — apakah preview tersedia atau hanya download? |
| Submission timestamp dan submitter identity | Audit trail dasar; semua platform enterprise mendukung ini | Rendah | **Sudah ada** (audit trail) |
| Reject dengan komentar/catatan | BetterUp, CoachHub — feedback loop untuk submission adalah standar | Rendah | **Perlu audit** — apakah reject + comment tersedia? |
| Resubmission setelah reject | Workflow tidak lengkap tanpa ini | Rendah | **Perlu audit** — apakah ada state "rejected, awaiting resubmission"? |
| Status submission yang jelas (pending/approved/rejected) | Visual state yang jelas untuk worker dan reviewer | Rendah | **Sudah ada** (status history) |

### Differentiators

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Multiple file per deliverable | Simply.Coach mendukung multi-artifact; memungkinkan foto + dokumen + video | Sedang | **Perlu audit** — saat ini mungkin single file per deliverable |
| Link URL submission (bukan hanya file) | Platform modern mendukung submission berupa tautan (video, repository, dokumen cloud) | Rendah | **Belum ada** |
| Submission comment/note dari worker | Konteks dari worker sebelum approval; mendukung komunikasi async | Rendah | **Perlu audit** — apakah ada field komentar saat submit? |
| Inline preview untuk gambar dan PDF | UX improvement signifikan; reviewer tidak perlu download | Sedang | **Belum ada** |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Tidak ada batasan tipe/ukuran file | Risiko keamanan dan storage bloat | Enforce allowlist tipe file + batas ukuran (sudah ada di beberapa endpoint, perlu konsisten) |
| Sequential lock yang terlalu rigid tanpa exception path | Memblokir progress jika ada blocker eksternal | Pertimbangkan override admin untuk kondisi tertentu — sudah ada Deliverable Override |

### Dependensi pada Implementasi Existing

- Evidence submission — ada, perlu audit detail fitur reject/resubmit
- Sequential lock — ada, perlu audit apakah ada edge case yang memblokir

---

## Area 3: Multi-Level Approval Chain

### Table Stakes

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Role-based approval stages yang jelas | Standar governance enterprise; semua platform HR enterprise mendukung RBAC approval | Sedang | **Sudah ada** (SrSpv - SH - HC) |
| Notifikasi ke approver saat ada item menunggu | MentorcliQ, CoachHub — automated reminders adalah table stakes | Rendah | **Perlu audit** — in-app notification sudah ada untuk worker; apakah ada untuk approver? |
| History lengkap approval chain per deliverable | Compliance dan audit; semua platform enterprise mendukung ini | Rendah | **Sudah ada** |
| Visibility status approval ke worker (siapa menyetujui, siapa selanjutnya) | Transparency di BetterUp dan Torch; worker tahu posisi mereka di queue | Rendah | **Perlu audit** — apakah worker bisa lihat siapa approver-nya? |
| Kemampuan approver untuk memberikan komentar | Feedback loop dari reviewer ke worker | Rendah | **Perlu audit** |

### Differentiators

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Delegation approval (approver bisa delegate ke pengganti saat absen) | MentorcliQ mendukung; menghindari bottleneck | Tinggi | **Belum ada** — kompleksitas tinggi, low priority untuk konteks ini |
| Batch approval (approve multiple deliverables sekaligus) | Admin efficiency; MentorcliQ dan Torch mendukung | Sedang | **Belum ada** — HC Review mungkin perlu ini jika banyak worker |
| SLA/deadline untuk setiap tahap approval | Torch dan enterprise platforms gunakan SLA untuk governance | Tinggi | **Belum ada** — tidak prioritas untuk saat ini |
| Escalation otomatis jika approval terlambat | Best practice enterprise; reduces bottleneck | Tinggi | **Belum ada** — perlu SLA dulu |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Approval chain hardcoded di kode (bukan data-driven) | Tidak fleksibel saat ada perubahan organisasi | Audit apakah chain bisa dikonfigurasi atau hardcoded |
| Tidak ada rollback/revert approval | Jika approver salah approve, tidak ada jalan kembali | Pastikan HC memiliki override capability |

### Dependensi pada Implementasi Existing

- Multi-role approval chain (SrSpv - SH - HC) — ada, perlu audit detail UX dan notifikasi
- ProtonNotification — ada untuk worker; perlu audit coverage untuk approver
- Deliverable Override — ada sebagai safety valve

---

## Area 4: Coaching Session Management

### Table Stakes

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Pencatatan coaching session (tanggal, peserta, topik) | Standar absolut Simply.Coach, BetterUp, CoachHub | Rendah | **Sudah ada** (Coaching Sessions) |
| Action items per session dengan status selesai/belum | Simply.Coach, Torch — action items adalah deliverable dari setiap session | Rendah | **Sudah ada** (Action Items) |
| History semua session per pasangan coach-coachee | Audit trail dan progress review; semua platform mendukung ini | Rendah | **Sudah ada** (History) |
| Asosiasi session dengan progress deliverable | Menghubungkan aktivitas coaching ke outcome konkret | Sedang | **Perlu audit** — apakah session terhubung ke deliverable specific? |

### Differentiators

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Session notes yang bisa dilihat oleh coach dan coachee | Simply.Coach mendukung shared notes; meningkatkan alignment | Rendah | **Perlu audit** — siapa yang bisa lihat session notes? |
| Session templates/agenda standar | Torch dan CoachHub menyediakan template sesi berdasarkan tahap | Sedang | **Belum ada** |
| Ringkasan otomatis session dan next actions | BetterUp AI fall 2025 — AI-generated session summaries | Tinggi | **Belum ada** — low priority, AI feature |
| Session scheduling integration (calendar) | CoachHub, BetterUp — scheduling adalah core feature | Tinggi | **Belum ada** — out of scope untuk konteks PortalHC |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Session tidak terhubung ke deliverable/progress | Session management terasa terpisah dan tidak bermakna | Pastikan ada linkage session ke deliverable status atau kompetensi |
| Unlimited free-form fields di session notes | Susah di-aggregate untuk analytics | Gunakan structured fields + optional notes |

### Dependensi pada Implementasi Existing

- Coaching Sessions + Action Items — ada
- CoachCoachee Mapping — prerequisite untuk session management

---

## Area 5: Progress Dashboards dan Analytics

### Table Stakes

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Completion rate per worker dan agregat tim | KPI utama di semua platform: MentorcliQ, CoachHub, Torch | Rendah | **Sudah ada** (CDP Dashboard) |
| Filter by track, unit, status | Standar drill-down analytics | Rendah | **Sudah ada** (CoachingProton tracking page) |
| Export data progress ke Excel | Compliance reporting; diharapkan di konteks enterprise Indonesian | Rendah | **Sudah ada** |
| Stats summary: total worker, aktif, selesai | Dashboard header stats adalah table stakes | Rendah | **Sudah ada** |
| Chart visualisasi trend progress | 360Learning, CoachHub — visual dashboard adalah standar | Sedang | **Sudah ada** (Chart.js) |

### Differentiators

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Competency gap heatmap (worker x kompetensi matrix) | TalentGuard, iMocha — visual matrix untuk HC planning | Sedang | **Belum ada** |
| Bottleneck analysis (deliverable mana yang paling sering pending lama) | Torch dan Chronus analytics — mengidentifikasi hambatan di program | Sedang | **Belum ada** |
| Coach effectiveness metrics (berapa persen coachee selesai tepat waktu) | MentorcliQ impact dashboard; berguna untuk evaluasi coach | Sedang | **Belum ada** |
| Trend comparison antar periode (bulan ini vs bulan lalu) | 360Learning dan BetterUp mendukung period comparison | Sedang | **Belum ada** |
| Print/PDF snapshot dashboard | Kebutuhan reporting ke manajemen; umum di konteks enterprise Indonesia | Rendah | **Belum ada** |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Vanity metrics (jumlah session tanpa outcome) | Torch merekomendasikan menghubungkan metrics ke business outcome | Tampilkan completion rate dan competency level granted, bukan hanya session count |
| Dashboard tanpa filter role-scoped | HC melihat semua; SectionHead hanya section-nya — tidak ada granularitas = overload | Implement role-scoped view (sudah sebagian ada, perlu audit) |

### Dependensi pada Implementasi Existing

- CDP Dashboard dengan Chart.js — ada, perlu audit scope dan accuracy
- CoachingProton tracking page — ada, perlu audit filter completeness
- Role-scoped data access — perlu audit konsistensi lintas halaman

---

## Area 6: Coach-Coachee Assignment Management

### Table Stakes

| Fitur | Mengapa Diharapkan | Kompleksitas | Catatan |
|-------|--------------------|--------------|---------|
| Manual assignment coach ke coachee | Kontrol penuh admin; standar semua platform | Rendah | **Sudah ada** |
| Deactivate/reactivate mapping | Perubahan organisasi membutuhkan ini | Rendah | **Sudah ada** |
| Bulk import assignment via Excel | Efisiensi admin untuk onboarding massal | Rendah | **Sudah ada** (Import CoachCoacheeMapping) |
| View siapa coach dari worker tertentu | Navigasi dasar; transparent ke worker | Rendah | **Perlu audit** — apakah worker bisa lihat coach-nya? |
| Satu worker hanya satu coach aktif pada satu waktu | Business rule yang umum; mencegah konflik | Rendah | **Perlu audit** — apakah ada validasi ini? |

### Differentiators

| Fitur | Nilai | Kompleksitas | Catatan |
|-------|-------|--------------|---------|
| Workload indicator untuk coach (berapa coachee aktif) | Qooper dan MentorcliQ menampilkan workload; mencegah overload coach | Rendah | **Belum ada** |
| Transfer coachee ke coach lain dengan history terjaga | Coach resign atau pindah divisi; history harus terbawa | Sedang | **Perlu audit** — apakah ada mekanisme transfer dengan history intact? |
| Coach dapat melihat semua coachee-nya dalam satu view | Standar dashboard coach di BetterUp, Simply.Coach | Rendah | **Perlu audit** — apakah ada dedicated coach view? |
| Suggested re-matching berdasarkan progress stagnant | Qooper AI matching; jika coachee tidak progress, sarankan ganti coach | Tinggi | **Belum ada** — low priority, AI feature |

### Anti-Features

| Anti-Feature | Mengapa Hindari | Alternatif |
|--------------|-----------------|------------|
| Delete mapping yang menghapus history | Kehilangan audit trail; tidak acceptable untuk enterprise HR | Implement soft-delete / deactivate saja — sudah ada deactivate |
| Coach bisa approve evidence coachee-nya sendiri di tahap final | Conflict of interest; integrity issue | Pastikan coach tidak bisa approve di tahap HC Review |

### Dependensi pada Implementasi Existing

- CoachCoachee Mapping (assign/edit/deactivate/delete/import) — ada, perlu audit detail
- Track Assignment — prerequisite untuk menentukan silabus yang dijalani coachee

---

## Ringkasan Konsolidasi

### Table Stakes Terpenuhi (Sudah Ada)

| Area | Fitur Existing |
|------|---------------|
| Competency Tracking | Hierarchy 3-level, status per deliverable, completion %, track assignment, competency level granting, audit trail |
| Evidence Submission | File upload per deliverable, status tracking |
| Approval Chain | SrSpv - SH - HC multi-role chain, status history |
| Session Management | Session records, action items, history |
| Dashboard | Completion stats, Chart.js, filters, export |
| Assignment Management | Manual assign, deactivate, bulk import |

### Kemungkinan Gap di Table Stakes (Perlu Diaudit Kode)

| Area | Yang Perlu Dicek | Dampak Jika Tidak Ada |
|------|------------------|-----------------------|
| Evidence Submission | Reject + komentar, resubmission flow | Worker tidak tahu mengapa ditolak; tidak bisa improve |
| Evidence Submission | Multiple file per deliverable | Tidak bisa lampirkan beberapa bukti untuk satu deliverable |
| Approval Chain | Notifikasi ke approver (SrSpv, SH) | Bottleneck tanpa notifikasi; chain terhenti |
| Approval Chain | Visibility chain ke worker | Worker tidak tahu status approval berada di level mana |
| Approval Chain | Komentar dari approver ke worker | Tidak ada feedback jika ada masalah |
| Session Management | Linkage session ke deliverable | Session terasa tidak bermakna untuk progress |
| Dashboard | Role-scoped filter consistency | HC/SH melihat data yang tidak relevan |
| Assignment | Worker bisa lihat coach-nya | Transparansi dasar tidak terpenuhi |
| Assignment | Validasi single active coach per worker | Potensi data conflict |

### Differentiators Bernilai Tinggi (Rekomendasikan untuk Milestone)

| Fitur | Prioritas | Kompleksitas | Alasan |
|-------|-----------|--------------|--------|
| Reject + komentar pada evidence submission | Tinggi | Rendah | Table stakes gap; hampir semua platform mendukung ini |
| Notifikasi ke approver (SrSpv, SH) | Tinggi | Rendah | Table stakes gap; bottleneck tanpa ini |
| Workload indicator coach | Sedang | Rendah | Cepat dibangun, nilai operasional nyata |
| Batch approval untuk HC Review | Sedang | Sedang | Efficiency gain untuk HC yang review banyak worker |
| Bottleneck analysis (deliverable pending lama) | Sedang | Sedang | Insight program management yang berguna |

### Defer (Kompleksitas Tinggi / ROI Rendah)

| Fitur | Alasan Defer |
|-------|-------------|
| Scheduling integration (calendar) | Out of scope; PortalHC bukan scheduling tool |
| AI-generated session summaries | Tidak ada AI infrastructure; ROI tidak jelas |
| Predicted completion date | Kompleksitas algoritma tinggi; rendah prioritas |
| SLA/escalation otomatis | Membutuhkan SLA framework terlebih dahulu |
| Competency gap heatmap | Nilai tinggi tapi kompleksitas sedang; defer ke milestone berikutnya |

---

## Feature Dependencies Map

```
Coach-Coachee Mapping
  → Session Management (harus ada mapping sebelum bisa catat session)
  → Track Assignment (tentukan silabus yang dijalani coachee)

Track Assignment
  → Silabus tampil di PlanIdp (worker lihat deliverable yang harus dikerjakan)

Silabus (Deliverable list)
  → Evidence Submission per deliverable
  → Sequential lock (urutan pengerjaan)

Evidence Submission
  → Approval Chain (SrSpv review → SH review → HC review)
  → Reject + komentar → Resubmission (loop)

Semua Deliverable Approved
  → ProtonNotification ke worker
  → Final Assessment tersedia
  → CompetencyLevel granted

CompetencyLevel
  → HistoriProton (rekam jejak completion)

Data dari seluruh chain
  → CoachingProton tracking page (monitoring HC/admin)
  → CDP Dashboard (stats, charts, team view)
```

---

## Sumber

- [360Learning Features 2026 — Capterra](https://www.capterra.com/p/230567/360Learning/)
- [BetterUp Fall 2025 Platform Release](https://www.betterup.com/platform-releases/fall-2025)
- [Top 11 Coaching Software Platforms — MentorcliQ](https://www.mentorcliq.com/insights/coaching-software-platform)
- [Competency Tracking: Essential Guide 2025 — SkillPanel](https://skillpanel.com/blog/competency-tracking/)
- [Best Competency Management Software 2026 — TalentGuard](https://www.talentguard.com/competency-management-software)
- [Top 10 Coaching Tools for Tracking Progress — Simply.Coach](https://simply.coach/blog/best-coaching-tools-tracking-progress/)
- [Compare BetterUp vs. CoachHub 2025 — Slashdot](https://slashdot.org/software/comparison/BetterUp-vs-CoachHub/)
- [The Future of Mentor Mentee Matching — Qooper](https://www.qooper.io/blog/the-future-of-mentor-mentee-matching)
- [Top Mentoring Software Platforms 2025 — Mentoring Complete](https://www.mentoringcomplete.com/best-mentoring-software-platforms-2025/)
- [Top 10 Tracking Progress Tools for L&D 2026 — Simply.Coach](https://simply.coach/blog/best-tracking-tools-ld-professionals/)
- [Best Competency Management Software Reviewed — Centranum](https://www.centranum.com/resources/capability-and-competency/best-competency-management-software/)
- [7 Best Competency Management Systems 2025 — HiPeople](https://www.hipeople.io/blog/competency-management-systems)

**Tingkat Kepercayaan:**
- Identifikasi table stakes: MEDIUM-HIGH (diverifikasi lintas beberapa platform)
- Peringkat differentiator: MEDIUM (berdasarkan perbandingan fitur platform dan pola industri)
- Penilaian fitur existing: HIGH (berdasarkan PROJECT.md dan riwayat milestone)
- Estimasi kompleksitas: MEDIUM (berdasarkan familiarity dengan codebase existing)
