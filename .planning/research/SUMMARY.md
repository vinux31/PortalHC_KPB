# Project Research Summary

**Project:** PortalHC KPB — Proton Coaching Ecosystem Audit (v8.2)
**Domain:** Competency-based coaching/mentoring platform audit — ASP.NET Core MVC (brownfield)
**Researched:** 2026-03-22
**Confidence:** HIGH

## Executive Summary

PortalHC KPB sudah memiliki fondasi Proton Coaching yang lengkap secara arsitektur: 3-level silabus hierarchy, multi-role approval chain (SrSpv → SH → HC), evidence submission, coaching sessions, action items, dashboard analytics, dan notification system. Audit v8.2 bukan membangun dari nol — melainkan memperbaiki implementasi gap, memperkuat keamanan, dan memastikan semua business rule terpenuhi di server side (bukan hanya di UI). Stack yang ada sudah adequate dan tidak perlu library baru sama sekali.

Pendekatan yang direkomendasikan adalah audit berbasis domain: mulai dari Setup (silabus, mapping, assignment), lanjut ke Execution (evidence upload, sequential lock, approval chain), kemudian Completion (final assessment, coaching sessions, HistoriProton), dan terakhir Monitoring (dashboard, analytics, export). Urutan ini mengikuti dependency data aktual — monitoring hanya bisa diverifikasi akurat setelah semua data upstream benar. Setiap fase harus memverifikasi keamanan server-side, bukan hanya fungsionalitas UI.

Risiko terbesar adalah approval state inconsistency: `ProtonDeliverableProgress` punya 4 flag status independen yang bisa tidak sinkron jika dimodifikasi parsial. Risk kedua adalah security: evidence download tanpa auth, sequential lock bypass via direct POST, dan export tanpa role attribute adalah tech debt eksplisit dari v4.0 yang belum terselesaikan. Ketiga risiko ini harus menjadi prioritas utama Fase 2 (Execution Flow).

---

## Key Findings

### Recommended Stack

Stack yang ada sudah cukup untuk seluruh milestone audit ini — tidak ada package baru yang perlu diinstall. ASP.NET Core MVC (net8.0) + EF Core + SQLite adalah tulang punggung, dengan ClosedXML untuk Excel, QuestPDF untuk PDF, Chart.js untuk dashboard, dan custom services (`INotificationService`, `AuditLogService`, `FileUploadHelper`) yang sudah ter-cover semua kebutuhan Proton.

**Core technologies (existing, tidak ada perubahan):**
- **ASP.NET Core MVC net8.0**: Web framework, semua controller action — SOLID
- **EF Core 8.0**: ORM + migrations — cukup untuk audit, tidak perlu cache layer
- **INotificationService**: Template sudah cover semua Proton events — perlu audit apakah semua trigger benar-benar dipanggil
- **DeliverableStatusHistory**: Audit trail per deliverable — perlu audit completeness setiap insert point
- **ClosedXML**: Import/export Excel (silabus, mapping) — SOLID
- **Chart.js 4.x**: Dashboard charts — ada, perlu audit data accuracy dan role scoping query

Teknologi yang secara eksplisit **tidak perlu ditambahkan**: SignalR hub baru untuk Proton, Hangfire, Redis, React/Vue, AutoMapper, MediatR, Azure Blob Storage.

### Expected Features

Berdasarkan perbandingan dengan platform industri (360Learning, BetterUp, CoachHub, Torch, MentorcliQ, Qooper, Simply.Coach), sebagian besar table stakes sudah ada. Gaps yang perlu diaudit adalah detail implementasi, bukan ketiadaan fitur.

**Must have (table stakes) — sudah ada, perlu audit detail:**
- Hierarchical competency framework 3 level — sudah ada
- Status per deliverable + multi-role approval chain — sudah ada, perlu audit state machine konsistensi
- Evidence submission + file upload — sudah ada, perlu audit security dan reject/resubmit flow
- Reject + komentar pada evidence — perlu diverifikasi apakah sudah ada atau belum
- Notifikasi ke approver (SrSpv, SH) saat ada item menunggu — perlu diverifikasi coverage
- Coaching sessions + action items — sudah ada, perlu audit linkage ke deliverable
- Dashboard role-scoped — sudah ada, perlu audit konsistensi filter

**Should have (differentiators bernilai tinggi untuk milestone ini):**
- Workload indicator coach (berapa coachee aktif) — belum ada, kompleksitas rendah
- Sequential lock dengan penjelasan yang jelas ke coachee (bukan hanya lock icon) — UX improvement
- Batch approval untuk HC Review — belum ada, moderate complexity
- Bottleneck analysis (deliverable mana yang paling sering pending lama) — belum ada

**Defer (v9+):**
- Competency gap heatmap (worker x kompetensi matrix)
- Scheduling integration / calendar
- AI-generated session summaries
- SLA/escalation otomatis
- Predicted completion date

### Architecture Approach

Proton Coaching terdistribusi di tiga controller: `AdminController` (setup: mapping + assignment), `CDPController` (execution + monitoring: 20+ actions, 3405 baris), dan `ProtonDataController` (content: silabus + guidance). Business logic ada inline di controller — ini pola yang disengaja dan konsisten di seluruh project, bukan anti-pattern untuk skala ini. Data mengalir dari ProtonTrack → ProtonKompetensi → ProtonSubKompetensi → ProtonDeliverable → ProtonDeliverableProgress → DeliverableStatusHistory, dengan CoachingSession sebagai entitas paralel yang terhubung via nullable FK ke progress.

**Major components:**
1. **AdminController (Setup)** — CoachCoacheeMapping CRUD + TrackAssignment + bulk seed ProtonDeliverableProgress saat mapping dibuat
2. **CDPController (Execution + Monitoring)** — UploadEvidence + ApproveDeliverable + RejectDeliverable + HCReviewDeliverable + Dashboard + CoachingProton list + HistoriProton
3. **ProtonDataController (Content)** — SilabusSave/Delete/Import/Export + GuidanceUpload/Replace/Delete
4. **ProtonDeliverableProgress** — pusat data; 4 flag approval independen; harus selalu konsisten
5. **DeliverableStatusHistory** — append-only audit trail per state change; harus diisi di setiap transition
6. **INotificationService** — template-based notification; semua Proton events sudah ter-define; perlu audit bahwa semua trigger dipanggil

**Dua pola khusus yang harus diperhatikan saat audit:**
- AssignmentUnit fallback: `mapping?.AssignmentUnit ?? user.Unit` — coachee bisa di-assign ke unit berbeda dari profil mereka
- Dua sistem notifikasi paralel: `ProtonNotification` (coaching-specific) dan `UserNotification` (bell icon umum) — belum dikonsolidasi, ada overlap di `ApproveDeliverable`

### Critical Pitfalls

1. **Approval state inconsistency (CRITICAL)** — 4 flag independen (`Status`, `SrSpvApprovalStatus`, `ShApprovalStatus`, `HCApprovalStatus`) bisa tidak sinkron. Override admin adalah sumber utama (tech debt v4.0). Solusi: buat `ApprovalStateMachine` helper kecil, setiap action approval hanya memanggil helper, tulis unit test per kombinasi transisi legal.

2. **Sequential lock bypass via direct POST (CRITICAL)** — validasi lock mungkin hanya ada di UI (button disabled), bukan di controller. Solusi: setiap `SubmitEvidence` action harus cek di server apakah semua deliverable dengan Urutan lebih kecil sudah Approved. Return `Forbid()` jika belum.

3. **Evidence download tanpa auth (CRITICAL — tech debt v4.0)** — akses file tanpa verifikasi kepemilikan memungkinkan coachee A mengunduh evidence milik coachee B. Solusi: load progress dari DB, verifikasi `CoacheeId == currentUser` atau role berwenang, konstruksi path dari DB record (bukan dari request parameter), validasi `Path.GetFullPath()` masih dalam uploads directory.

4. **Final Assessment double-creation (HIGH)** — tidak ada unique constraint di DB untuk `ProtonTrackAssignmentId`. Double-click atau dua HC bersamaan bisa buat dua record. Solusi: tambah `.AnyAsync()` check sebelum `.Add()` + unique index di migration.

5. **Silabus delete tanpa peringatan dampak (HIGH — tech debt v4.0)** — hard delete kompetensi yang punya progress aktif membuat orphan records. Solusi: query pre-delete untuk hitung dampak, modal konfirmasi dengan impact count, gunakan soft delete saja jika ada progress aktif.

6. **Dashboard N+1 dan over-fetching (MEDIUM)** — load semua kolom termasuk EvidencePath untuk dashboard yang hanya butuh Status. Solusi: projection `.Select()` untuk aggregation, build `IQueryable` dengan WHERE sebelum `ToListAsync`.

---

## Implications for Roadmap

Berdasarkan dependency data aktual dan tech debt yang teridentifikasi, struktur 4 fase yang direkomendasikan:

### Fase 1: Audit Setup Flow — Silabus, Mapping, Assignment

**Rationale:** Semua data execution bergantung pada setup yang benar. Silabus corrupt atau cascade deactivation yang tidak atomik akan menyebabkan data hilang di semua fase berikutnya. Harus bersih dulu sebelum bisa verifikasi execution.

**Delivers:** Setup flow yang terjamin integritas datanya: silabus tidak bisa dihapus tanpa warning, cascade deactivation atomik via transaction, track assignment tidak bisa skip tahun sebelumnya, validasi duplikasi mapping.

**Addresses:** Coach-coachee assignment management (table stakes gaps), silabus CRUD safety, 3-year progression validation.

**Avoids:** Pitfall 2 (cascade deactivation race condition), Pitfall 5 (silabus delete tanpa warning), Pitfall 9 (3-year progression tanpa validasi tahun sebelumnya).

---

### Fase 2: Audit Execution Flow — Evidence Submission, Sequential Lock, Approval Chain

**Rationale:** Ini inti operasional harian Proton. Tech debt v4.0 yang belum terselesaikan (download auth, sequential lock server-side, ExportProgressExcel role attr) ada di sini. Approval state machine yang konsisten adalah dependency untuk semua reporting yang akurat.

**Delivers:** Evidence submission yang aman (file type validation, auth pada download, path traversal protection), sequential lock yang tidak bisa di-bypass via direct POST, approval chain dengan state machine yang konsisten dan ter-audit, notifikasi ke approver yang terverifikasi, reject + komentar + resubmission flow yang lengkap.

**Addresses:** Multi-level approval chain (table stakes gaps), evidence submission workflow (reject/resubmit), coaching session linkage ke deliverable, security tech debt v4.0.

**Avoids:** Pitfall 1 (approval state inconsistency), Pitfall 3 (sequential lock bypass), Pitfall 4 (evidence download tanpa auth), security mistakes (ExportProgressExcel auth, path traversal).

---

### Fase 3: Audit Completion — Final Assessment, Coaching Sessions, HistoriProton

**Rationale:** Dapat diaudit setelah execution flow bersih karena completion bergantung pada deliverable sudah Approved dengan state yang valid. CoachingSession dan ActionItem relatif independen dari approval chain, tapi harus diverifikasi linkage-nya ke deliverable progress.

**Delivers:** Final assessment yang tidak bisa duplikat (unique guard + unique index), HistoriProton yang benar menggabungkan data legacy CoachingLog dan data baru DeliverableStatusHistory, coaching sessions yang ter-linked ke deliverable.

**Addresses:** Completion dan history tracking (table stakes), coaching session management (perlu audit linkage dan visibility notes).

**Avoids:** Pitfall 7 (legacy CoachingLog ambiguity di HistoriProton), Pitfall 8 (final assessment double-creation).

---

### Fase 4: Audit Monitoring — Dashboard, Analytics, Export

**Rationale:** Dashboard hanya membaca data dari semua fase di atas. Audit monitoring adalah tahap terakhir karena membutuhkan data upstream yang sudah bersih untuk memverifikasi keakuratan angka. Query performance juga lebih mudah diidentifikasi setelah semua filter dan scoping sudah benar.

**Delivers:** Dashboard dengan role-scoped filtering yang konsisten dan akurat, query performance yang efisien (projection daripada over-fetching), export dengan role attribute yang benar, override admin dengan validasi state machine dan audit trail lengkap.

**Addresses:** Progress dashboards dan analytics (role-scoped accuracy), coach workload visibility, override admin audit trail.

**Avoids:** Pitfall 6 (dashboard N+1 performance), security mistakes (override tanpa audit log, export tanpa auth).

---

### Phase Ordering Rationale

- **Setup sebelum Execution:** `ProtonDeliverableProgress` di-seed saat mapping dibuat — jika mapping atau silabus corrupt, execution akan menghasilkan data salah yang sulit di-trace.
- **Execution sebelum Completion:** Final Assessment dibuat setelah semua deliverable Approved — angka completion hanya akurat jika approval chain sudah benar.
- **Completion sebelum Monitoring:** Dashboard menampilkan agregat dari semua data upstream — accuracy verification baru bermakna jika semua sumber data sudah bersih.
- **CoachingSession dan ActionItem bisa paralel dengan Approval:** Keduanya relatif independen dan bisa diaudit bersamaan dengan Fase 2 jika resource memungkinkan.

### Research Flags

Semua area sudah memiliki kode aktual untuk diinspeksi — tidak perlu penelitian fase tambahan (`/gsd:research-phase`). Pattern yang digunakan sudah well-documented dalam codebase:

- **Semua 4 fase:** Skip research-phase — audit berbasis inspeksi kode langsung, bukan penelitian teknologi baru.
- **Fase 2 (Security):** Gunakan checklist "Looks Done But Isn't" dari PITFALLS.md sebagai panduan verifikasi per-action. Setiap item checklist harus diverifikasi di controller, bukan di UI.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Berdasarkan inspeksi langsung source code + .csproj; tidak ada ketidakpastian library |
| Features | HIGH (existing) / MEDIUM (gaps) | Existing features dikonfirmasi dari source code; gaps diidentifikasi dari perbandingan platform industri multi-sumber |
| Architecture | HIGH | Berdasarkan inspeksi langsung 3 controller, 14+ model, semua data flow; bukan asumsi |
| Pitfalls | HIGH | Berdasarkan kode aktual + tech debt registry v4.0 yang eksplisit + audit history v4.0 dan v8.1 |

**Overall confidence:** HIGH

### Gaps to Address

- **Reject + komentar pada evidence submission:** Perlu dikonfirmasi saat audit Fase 2 apakah sudah ada field komentar saat reject, atau perlu ditambahkan baru.
- **Notifikasi ke approver (SrSpv, SH):** `INotificationService` punya template `COACH_EVIDENCE_SUBMITTED` — perlu verifikasi apakah trigger ini dipanggil ke SrSpv/SH, bukan hanya ke coachee.
- **Multiple file per deliverable:** Perlu verifikasi saat Fase 2 apakah `ProtonDeliverableProgress` hanya support single file atau sudah ada tabel terpisah untuk multi-file.
- **Worker bisa lihat nama coach-nya:** Perlu verifikasi di CDPController apakah ada tampilan info coach di PlanIdp atau Deliverable view.
- **Dua sistem notifikasi (ProtonNotification vs UserNotification):** Ada overlap yang belum dikonsolidasi — perlu keputusan apakah Fase 4 menyatukan keduanya atau membiarkan coexist dengan dokumentasi yang jelas.

---

## Sources

### Primary (HIGH confidence — inspeksi kode langsung)
- `Controllers/CDPController.cs` (3405 baris) — execution + monitoring, semua approval actions
- `Controllers/AdminController.cs` (7630 baris) — setup, mapping, cascade logic
- `Controllers/ProtonDataController.cs` (1361 baris) — silabus, guidance
- `Models/ProtonModels.cs` — 14 entity: ProtonTrack, ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonTrackAssignment, ProtonDeliverableProgress, DeliverableStatusHistory, ProtonNotification, CoachingGuidanceFile, ProtonFinalAssessment
- `Models/CoachCoacheeMapping.cs`, `Models/CoachingSession.cs`, `Models/ActionItem.cs`
- `Services/INotificationService.cs` + `Services/NotificationService.cs`
- `.planning/PROJECT.md` — tech debt registry v4.0, semua keputusan arsitektur

### Secondary (MEDIUM confidence — perbandingan platform industri)
- [360Learning](https://www.capterra.com/p/230567/360Learning/), [BetterUp](https://www.betterup.com/platform-releases/fall-2025), [CoachHub](https://slashdot.org/software/comparison/BetterUp-vs-CoachHub/), [Torch + MentorcliQ](https://www.mentorcliq.com/insights/coaching-software-platform), [Qooper](https://www.qooper.io/blog/the-future-of-mentor-mentee-matching), [Simply.Coach](https://simply.coach/blog/best-coaching-tools-tracking-progress/) — feature landscape dan table stakes identification
- [TalentGuard](https://www.talentguard.com/competency-management-software), [HiPeople](https://www.hipeople.io/blog/competency-management-systems), [Centranum](https://www.centranum.com/resources/capability-and-competency/best-competency-management-software/) — competency management patterns

---
*Research completed: 2026-03-22*
*Ready for roadmap: yes*
