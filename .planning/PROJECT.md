# Portal HC KPB - Pertamina HR Portal

**Project Type:** Brownfield Enhancement
**Created:** 2026-02-14
**Status:** Active Development

## Vision

Portal web untuk HC (Human Capital) dan Pekerja Pertamina yang mengelola dua platform utama:
- **CMP** (Competency Management Platform) - Assessment, skills tracking, competency matrix
- **CDP** (Competency Development Platform) - IDP, coaching, development plans

Platform ini menyediakan sistem komprehensif untuk tracking kompetensi, assessment online, dan pengembangan SDM Pertamina.

## Current State (Phase 295 complete, 2026-04-03)

**v1.0 through v5.0 shipped** — 43 milestones, 172 phases.
**v6.0 closed** — Deployment Preparation defined but not executed.
**v7.1–v7.12 shipped** — Export/Import, Certification, Assessment Form, Code Dedup, Renewal Certificate, KKJ/IDP, Records, Struktur Organisasi (phases 176-222).
**v8.0–v8.6 shipped** — Assessment Integrity, Renewal Audit, Proton Audit, Filters, Alarm, UAT, Hardening (phases 223-252).
**v9.1 shipped** — UAT Coaching Proton (Phase 257 only, 258-261 skipped).
**Phase 262-263 shipped** — Sub-path deployment fixes (URLs + upload paths).
**v10.0 shipped** — UAT Assessment OJT di Server Development (phases 264-268): admin setup, worker exam flow, review/submit hasil, resilience/edge cases, monitoring dashboard.
**v11.2 shipped** — Admin Platform Enhancement (phases 282-283): Maintenance Mode + User Impersonation.
**v12.0 shipped** — Controller Refactoring (phases 286-291): AdminController dipecah menjadi 8 controller per domain, AdminController dikurangi dari 8,514 → 108 baris.

**Current focus:** v13.0 Redesign Struktur Organisasi

## Current Milestone: v13.0 Redesign Struktur Organisasi

**Goal:** Redesign halaman ManageOrganization dari tabel flat menjadi tree view modern dengan UX yang lebih fleksibel dan intuitif.

**Target features:**
- Tree view visual (bukan tabel) dengan recursive rendering (unlimited depth)
- Modal CRUD (tambah/edit unit tanpa page reload)
- Drag-and-drop reorder (ganti tombol up/down)
- Aksi ringkas (dropdown menu menggantikan 5 tombol inline)
- Kode view dari ~520 baris repetitif → ~150 baris recursive partial

## Next Milestone Goals

- Competency gap heatmap (worker x kompetensi matrix)
- Scheduling integration / calendar untuk coaching sessions
- AI-generated coaching session summaries
- SLA/escalation otomatis untuk approval yang terlalu lama
- Predicted completion date berdasarkan historical pace

## Architecture Decisions

### CLN-06: Override Silabus & Coaching Guidance Tabs (2026-03-02)

**Decision:** KEEP as-is — no changes.

**Rationale:** The `ProtonData/Index` page with Silabus and Coaching Guidance tabs is fully functional and tested. It serves as the canonical admin interface for managing silabus entries and uploading coaching guidance files. These tabs are used by Plan IDP (Phase 86) and Coaching Proton (Phase 85) as data sources. Removing or restructuring them would break downstream feature flows.

**Alternative considered:** Removing the Override tabs and merging into a simpler flat list. Rejected because the tabbed interface cleanly separates two distinct data types (silabus vs. guidance files) and the current implementation has no known bugs.

## Shipped Milestones

### ✅ v8.3 - Date Range Filter Team View Records (2026-03-23)

**Delivered:** Ganti search nama dengan date range filter pada Team View CMP/Records. 2 input date native, filter workers + count berdasarkan rentang tanggal, export ikut date range, reset clear semua filter.

**Metrics:** 1 phase (239), 2 plans

### ✅ v8.2 - Proton Coaching Ecosystem Audit (2026-03-23)

**Delivered:** End-to-end audit ekosistem Proton coaching berdasarkan riset 3 platform enterprise (360Learning, BetterUp, CoachHub). Audit setup flow (silabus delete safety, mapping transaction, import all-or-nothing), execution flow (evidence resubmit traceability, race guard, notification), completion (unique constraint, session CRUD, HistoriProton criteria), monitoring (filter fix, override validation). Differentiator: workload indicator, batch HC approval, bottleneck analysis chart, 3 export baru.

**Metrics:** 6 phases (233-238), 16 plans, 86 files changed, +17,252 / -297 lines

### ✅ v8.1 - Renewal & Assessment Ecosystem Audit (2026-03-22)

**Delivered:** Riset best practices, audit renewal logic/UI/cross-page, audit assessment management/monitoring, audit worker exam flow.

### ✅ v8.0 - Assessment Integrity & Analytics (2026-03-22)

**Delivered:** Assessment integrity (SessionElemenTeknisScore, exam activity log), analytics dashboard (fail rate, trend, ET breakdown, expiring certs), legacy data migration dan cleanup.

### ✅ v7.12 - Struktur Organisasi CRUD (2026-03-21)

**Delivered:** Migrasi penuh dari static class OrganizationStructure ke database-driven CRUD. Entity OrganizationUnit (adjacency list), CRUD page di Kelola Data (indented table, tambah/edit/pindah/hapus/reorder), integrasi 15+ dropdown/filter di 4 controller ke database, cleanup final + seed data + ImportWorkers validation.

**Metrics:** 4 phases (219-222), 7 plans, 28 files changed, +3,961 / -380 lines

### ✅ v7.11 - CMP Records Bug Fixes & Enhancement (2026-03-21)

**Delivered:** Category/Status filter fix (per-kategori, Permanent count, NIP case), SubKategori model + CRUD, Team View filter enhancement, Category dropdown dari master table, RecordsWorkerDetail redesign (hapus Score, tambah Kategori/SubKategori/Action), ImportTraining 12-column update.

### ✅ v7.10 - RenewalCertificate Bug Fixes & Enhancement (2026-03-21)

**Delivered:** Bulk renew FK chain fix, data/display fixes (ValidUntil null, category prefill, grouping), tipe filter Assessment/Training, renewal method modal, AddTraining renewal mode.

### ✅ v7.9 - Renewal Certificate Grouped View (2026-03-20)

**Delivered:** Grouped view struktur untuk RenewalCertificate dengan bulk renew dan filter compatibility.

### ✅ v7.8 - Dokumen KKJ & Alignment KKJ/IDP — Combine Menu (2026-03-20)

**Delivered:** Gabung 2 menu KKJ + Alignment di CMP Index menjadi 1 halaman DokumenKkj dengan 2 tab stacked sections, role-based filtering, dan visual polish (pemisah bagian, compact empty state).

### ✅ v7.7 - Renewal Certificate & Certificate History (2026-03-19)

**Delivered:** Full certificate renewal lifecycle — renewal chain data model, CreateAssessment pre-fill, dedicated Renewal Certificate admin page with bulk renew, certificate history modal with Union-Find chain grouping, CDP Certification Management hiding renewed certs with toggle.

### ✅ v7.6 - Code Deduplication & Shared Services (2026-03-18)

**Delivered:** Pure refactoring — extracted shared services, consolidated CRUD, unified code patterns. Net reduction of ~700+ lines of duplicated code.

**What Shipped:**
1. **IWorkerDataService** — 4 helper methods extracted from Admin+CMP controllers (561 lines removed)
2. **ExcelExportHelper** — ~170 lines of ClosedXML boilerplate eliminated across 15 export actions
3. **Training CRUD Consolidation** — CMP orphan actions removed, ImportTraining moved to Admin
4. **FileUploadHelper + PaginationHelper** — 6 inline patterns replaced across 3 controllers
5. **Role-scoping helper** — GetCurrentUserRoleLevelAsync extracts repeated pattern from 5 CMP actions

**Metrics:**
- 4 phases (196-199), 6 plans
- 38 files changed, +3,311 / -1,206 lines

---

### ✅ v7.5 - Assessment Form Revamp & Certificate Enhancement (2026-03-18)

**Delivered:** 4-step wizard for assessment creation, DB-driven categories with Admin CRUD, certificate expiry dates with auto-numbering, PDF certificate download, hierarchical sub-categories with signatory settings.

**What Shipped:**
1. **DB Categories** — AssessmentCategory model with seed data, Admin CRUD, CreateAssessment dropdown from DB
2. **Wizard UI** — 4-step Bootstrap wizard (Kategori→Users→Settings→Konfirmasi) with per-step validation
3. **ValidUntil & NomorSertifikat** — Certificate expiry date capture, auto-generated unique certificate numbers
4. **PDF Certificate Download** — QuestPDF A4 landscape layout with Download PDF button
5. **Sub-Categories & Signatory** — ParentId self-ref FK, expandable tree CRUD, per-category signatory name

**Metrics:**
- 6 phases (190-195), 10 plans

---

### ✅ v7.1 - Export & Import Data (2026-03-16)

**Delivered:** Excel export and import capabilities across 6 data areas — Records, RecordsTeam, CoachCoacheeMapping, AuditLog, Silabus Proton, Training, and HistoriProton. All using ClosedXML with consistent patterns.

**What Shipped:**
1. **Export Records & RecordsTeam** — Personal training history export + team export with role-scoped filters (Assessment & Training tabs)
2. **Import CoachCoacheeMapping** — Bulk coach-coachee mapping via Excel template with NIP-based lookup and validation
3. **Export AuditLog** — Date-filtered audit trail export for compliance review
4. **Export & Import Silabus Proton** — Roundtrip 3-level hierarchy (Kompetensi→SubKompetensi→Deliverable) with upsert logic
5. **Import Training & Export HistoriProton** — Bulk training record import + worker Proton history summary export

**Metrics:**
- 5 phases (176-180), 5 plans, 11 feat commits
- Timeline: 2026-03-16 (single day)

---

### ✅ v5.0 - Guide Page Overhaul (2026-03-16)

**Delivered:** Guide & FAQ system cleanup and UI polish — redundant accordion guides removed, dynamic card counts, FAQ toggle, unified badge/button styling, back-to-top button, and breadcrumb navigation.

**What Shipped:**
1. **Guide Cleanup** — CMP/CDP accordions simplified (redundant items covered by PDF tutorials removed), tutorial card CSS refactored, AD guide tutorial card added
2. **FAQ Improvements** — Expand/collapse all toggle, categories reordered, redundant step-by-step FAQ items removed
3. **UI Consistency** — Unified .guide-role-badge class, .step-variant-blue, shared accordion base styling
4. **Navigation** — Back-to-top button on Guide pages, GuideDetail breadcrumb (Beranda > Panduan > Module)

**Metrics:**
- 2 phases (171-172), 4 plans, 18 commits
- 17 files changed, +1,709 / -385 lines

---

### ✅ v4.3 - Bug Finder (2026-03-13)

**Delivered:** Full codebase, file system, database, and security audit. Dead code removed, temp files cleaned, CSRF/XSS gaps closed, all uploads secured.

**What Shipped:**
1. **Code Audit** — 2 dead actions removed, 2 silent catches fixed with logging, 3 unused imports cleaned, 53 views verified reachable
2. **File & Database Audit** — 40+ temp files removed, .gitignore hardened, all 35 DbSets verified active, seed data properly gated
3. **Security Review** — NotificationController CSRF gap closed, 4 Html.Raw XSS patterns fixed, 2 import endpoints secured with file type allowlists

**Metrics:**
- 3 phases (168-170), 8 plans, 17 commits
- 49 files changed, +2,319 / -325 lines

---

### ✅ v4.0 - E2E Use-Case Audit (2026-03-12)

**Delivered:** Comprehensive end-to-end audit of 6 use-case flows — code review + browser UAT per flow. All 33 requirements verified, 10+ bugs fixed, 10 tech debt items documented.

**What Shipped:**
1. **Assessment Flow Audit** — Fixed DeleteQuestion FK crash, open redirect, certificate IsPassed guard, TrainingRecord auto-creation
2. **Coaching Proton Audit** — Fixed mapping reactivation cascade, SubmitInterviewResults ProtonFinalAssessment creation
3. **Admin Kelola Data Audit** — Fixed DeleteWorker cascade order, CPDP MIME type, added missing audit log entries
4. **CDP Dashboard Audit** — Fixed coachee URL manipulation, duplicate key crash on multiple assignments
5. **Auth & Authorization Audit** — Full 7-controller authorization matrix verified, AccessDenied flow confirmed
6. **Navigation Audit** — All links verified, GuideDetail case-sensitivity fix

**Known Gaps (resolved):**
- Phase 89 PlanIDP gap: covered by Phase 156 audit
- ASSESS-04 PositionTargetHelper: confirmed removed in Phase 90

**Tech Debt:** 10 items (deferred browser tests, coaching edge cases, silent catch blocks)

**Metrics:**
- 6 phases (153-158), 16 plans, 72 commits
- 2026-03-11 → 2026-03-12

---

### ✅ v3.0 - Full QA & Feature Completion (2026-03-05)

**Delivered:** Comprehensive end-to-end QA of all portal features organized by use-case flows, code cleanup, UI rename, and PlanIDP 2-tab redesign. All major user flows verified working.

**What Shipped:**
1. **Cleanup & Rename** — "Proton Progress" → "Coaching Proton" throughout portal, orphaned CMP pages removed, AuditLog card added to Kelola Data
2. **Master Data QA** — All Kelola Data CRUD verified working, Worker/Silabus soft delete infrastructure with IsActive filters
3. **Assessment Flow QA** — Question import template, full assessment lifecycle verified (create, assign, exam, results, certificate)
4. **Coaching Proton QA** — Full coaching workflow verified (mapping, evidence upload, approval chain, exports)
5. **Dashboard & Navigation QA** — All dashboards show correct role-scoped data, login flow secure, navigation enforces visibility rules
6. **KKJ Matrix Full Rewrite** — Document-based file management (KkjFile/CpdpFile), dynamic columns per section
7. **PlanIDP 2-Tab Redesign** — Unified Silabus + Coaching Guidance tabs for all roles with read-only consumer view
8. **Admin Assessment Pages Audit** — ManageAssessment + AssessmentMonitoring all 11 flows verified with IsActive filters and bug fixes
9. **CMP Assessment Pages Audit** — Assessment + Records pages verified with CSRF fixes and Records redesign

**Known Gaps:**
- Phase 89 PlanIDP: No VERIFICATION.md file (5 requirements unverified)
- ASSESS-04: Competency display may be broken (PositionTargetHelper missing from codebase)
- Phase 88: KKJ Matrix verification claims don't match actual implementation

**Metrics:**
- 10 phases (82-91, excluding superseded 86), 46 plans
- 2026-03-02 → 2026-03-05

---

### ✅ v2.6 - Codebase Cleanup (2026-03-01)

**Delivered:** Removed all dead code, fixed critical runtime errors, cleaned up placeholder stubs, resolved HC role mismatches, and deduplicated CMP/ProtonMain page.

**What Shipped:**
1. **Critical Fixes** — AccessDenied view created, dead CMPController.WorkerDetail removed with 5 redirect fixes
2. **Dead Code Removal** — 6 orphaned Razor views deleted, 3 dead controller actions removed, unused site.css and site.js deleted
3. **Placeholder Cleanup** — BP module stub page removed, Admin hub stub cards deleted, Settings disabled items cleaned up
4. **Role Fixes & Broken Link** — HC-only card visibility fixed with User.IsInRole() gates, Deliverable Progress Override tab link fixed
5. **CMP/ProtonMain Deduplication** — CDP/ProtonMain action + view + ViewModel deleted, Training Records hub card added to Kelola Data Section C

**Metrics:**
- 5 phases (73-78), 10 plans
- 2026-03-01

---

### ✅ v2.5 - User Infrastructure & AD Readiness (2026-03-01)

**Delivered:** Full user system overhaul — dynamic profile/settings pages, ManageWorkers migrated to AdminController with HC access, Kelola Data hub reorganized into 3 domain sections, dual authentication (Active Directory + local) via IAuthService abstraction with hybrid AD-first + local fallback for admin, Supervisor role added, SectionHead demoted to level 3.

**What Shipped:**
1. **Dynamic Profile Page** — Profile bound to @model ApplicationUser with real data; null-safe em dash fallback; avatar initials from FullName
2. **Functional Settings Page** — Change password, edit FullName/Position; non-functional items removed/disabled
3. **ManageWorkers Migration** — 11 actions moved from CMPController to AdminController; HC access via [Authorize(Roles = "Admin, HC")]; standalone navbar button removed
4. **Kelola Data Hub** — 3 domain sections (Manajemen Pekerja, Kelola Assessment, Data Proton); HC nav access extended
5. **LDAP Auth Infrastructure** — IAuthService + LocalAuthService + LdapAuthService; config toggle; System.DirectoryServices NuGet
6. **Dual Auth Login Flow** — IAuthService-based login; AD hint; profile sync (FullName/Email); unregistered user rejection
7. **Hybrid Auth** — HybridAuthService (AD-first + local fallback for admin); Supervisor role (level 5); SectionHead level 3
8. **User Structure Polish** — UserRoles.GetDefaultView() helper; SeedData modernized; AuthSource field lifecycle (added Phase 69, removed Phase 72)

**Metrics:**
- 8 phases (65-72), 14 plans
- 41 files changed, +12,297 / -1,055
- 2026-02-27 → 2026-02-28

---

### ✅ v2.4 - CDP Progress (2026-03-01)

**Delivered:** CDP/Progress page rebuilt from scratch — data source corrected to ProtonDeliverableProgress, all filters wired to real queries with role-scoping, per-role approval workflow (SrSpv/SH/HC) with coaching report + evidence, Excel/PDF export, and server-side group-boundary pagination with empty states.

**What Shipped:**
1. **Data Source Fix** — ProtonProgress queries ProtonDeliverableProgress + ProtonTrackAssignment; real coachee list from CoachCoacheeMapping; correct summary stats
2. **Functional Filters** — 5 filter parameters wired to EF Core Where composition; role-scope-first; client-side search
3. **Per-role Approval** — SrSpv/SectionHead/HC independent approval columns; rejection takes precedence; migration backfills
4. **Coaching Report + Evidence** — Combined evidence+coaching modal; CoachingSession FK; Deliverable detail coaching display
5. **Export** — Excel (ClosedXML) and PDF (QuestPDF) from ProtonProgress page
6. **UI Polish** — Group-boundary pagination (20 rows/page); 3 empty state scenarios; filter result counter

**Metrics:**
- 4 phases (61-64), 9 plans
- 49 files changed, +20,101 / -6,105
- 2026-02-27 → 2026-02-28

---

### ✅ v2.3 - Admin Portal (2026-03-01)

**Delivered:** Admin has full CRUD control over master data (KKJ Matrix, CPDP Items), operational records (Coach-Coachee Mapping, DeliverableProgress Override, Final Assessment), and assessment management — all consolidated under /Admin with role-gated access. ProtonCatalog page removed after full migration to /Admin/ProtonData.

**What Shipped:**
1. **Admin Portal infrastructure** — AdminController with 12-card hub page, role-gated navigation
2. **KKJ Matrix & CPDP Items managers** — Spreadsheet-style inline editing with bulk-save, multi-cell clipboard, Excel export
3. **Assessment Management migration** — All manage actions moved from CMP to Admin with AuditLog
4. **Coach-Coachee Mapping manager** — Grouped-by-coach view with bulk assign, soft-delete, Excel export
5. **Proton Silabus & Coaching Guidance** — Two-tab /Admin/ProtonData with full silabus CRUD and guidance file management
6. **DeliverableProgress Override** — Third ProtonData tab for HC to override stuck statuses; sequential lock removed
7. **Final Assessment Manager** — Assessment Proton exam category with eligibility-gated coachee picker, Tahun 3 interview workflow
8. **ProtonCatalog cleanup** — Redirect-only controller and views deleted

**Known Gaps:** OPER-05, CRUD-01 through CRUD-04 deferred (phases removed/never planned)

**Metrics:**
- 8 phases (47-53, 59), 29 plans
- 274 files changed, +82,601 / -8,074
- 2026-02-26 → 2026-03-01

---

### ✅ v2.2 - Attempt History (2026-02-26)

**Delivered:** HC and Admin can view a full chronological record of every assessment attempt per worker — including attempts previously erased by Reset — with sequential Attempt # numbering and dual sub-tabs (Riwayat Assessment / Riwayat Training) at /CMP/Records.

**What Shipped:**
1. **AssessmentAttemptHistory model + migration** — New SQL Server table capturing SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt per archived attempt
2. **Archive-before-clear in ResetAssessment** — Completed sessions archived with AttemptNumber = existing count + 1 before fields are wiped; shares one SaveChangesAsync; only Completed sessions (never unstarted) produce history rows
3. **Unified history query with Attempt #** — GetAllWorkersHistory() returns (assessment, training) tuple; batch GroupBy/ToDictionary computes Attempt # for current sessions without N+1; archived rows carry stored AttemptNumber
4. **Riwayat Assessment + Riwayat Training sub-tabs** — Bootstrap nested nav-tabs replace single History table; client-side worker/NIP text + assessment title dropdown filter with no round-trip

**Metrics:**
- 1 phase (46), 2 plans, 4 tasks
- 15 files changed, 2,851 insertions / 82 deletions
- 2026-02-26

See `.planning/milestones/v2.2-ROADMAP.md` for full details.

---

### ✅ v2.1 - Assessment Resilience & Real-Time Monitoring (2026-02-25)

**Delivered:** Workers never lose exam progress (auto-save per-click + session resume from exact page with accurate remaining time), HC can monitor live during assessments (progress/status/countdown auto-refresh), and cross-package shuffle gives each worker a unique question mix from multiple packages.

**What Shipped:**
1. **Auto-Save** — SaveAnswer atomic upsert (ExecuteUpdateAsync + UNIQUE constraint); SaveLegacyAnswer for legacy path; visual feedback on save
2. **Session Resume** — ElapsedSeconds + LastActivePage persisted; resume modal on reconnect; pre-populated answers; offline time excluded from duration
3. **Worker Polling** — 10s poll interval with IMemoryCache (5s TTL); auto-redirect to Results on HC close-early; CloseEarly cache invalidation
4. **Real-Time Monitoring** — GetMonitoringProgress JSON endpoint; 10s table auto-refresh + 1s countdown; JS-rendered Reset/ForceClose action buttons; 8-column table restructure
5. **Cross-Package Per-Position Shuffle** — BuildCrossPackageAssignment: per-position random package selection with even distribution; import validation enforces equal question counts; all 5 consumers updated; ManagePackages summary panel

**Metrics:**
- 5 phases (41-45), 13 plans
- 52 files changed, 12,184 insertions / 255 deletions
- 2026-02-24 → 2026-02-25

See `.planning/milestones/v2.1-ROADMAP.md` for full details.

---

### ✅ v2.0 - Assessment Management & Training History (2026-02-24)

**Delivered:** HC can close active assessments early with fair scoring, both Management and Monitoring tabs auto-hide stale groups after 7 days, and a new History tab on RecordsWorkerList shows all workers' training and assessment completions in one combined view.

**What Shipped:**
1. **Close Early** — "Tutup Lebih Awal" button on AssessmentMonitoringDetail; sets ExamWindowCloseDate=now; InProgress sessions scored from actual PackageUserResponse answers (same grading as SubmitExam); audit log entry
2. **Auto-Hide Filter (7 days)** — Both GetManageData and GetMonitorData apply identical 7-day cutoff using ExamWindowCloseDate ?? Schedule.Date; no frontend changes needed
3. **All Workers History Tab** — RecordsWorkerList gains a "History" tab; AllWorkersHistoryRow + RecordsWorkerListViewModel; GetAllWorkersHistory() merges TrainingRecords + completed AssessmentSessions sorted by date descending; type badges (blue=Assessment Online, green=Manual); URL-persisted tab via ?activeTab=history

**Metrics:**
- 3 phases (38-40), 5 plans
- 2026-02-24

See `.planning/milestones/v2.0-ROADMAP.md` for full details.

---

### ✅ v1.9 - Proton Catalog Management (2026-02-24)

**Delivered:** HC/Admin can manage the full Proton program catalog through a single web page — add/rename/delete Kompetensi, SubKompetensi, and Deliverables inline with no database access needed; delete guards show active coachee impact counts.

**What Shipped:**
1. **ProtonTrack Schema** — ProtonTrack first-class table; ProtonKompetensi FK migration; all TrackType+TahunKe string fields eliminated; AssignTrack uses ProtonTrackId
2. **Catalog Page** — ProtonCatalogController; Kompetensi→SubKompetensi→Deliverable tree with Bootstrap expand/collapse; track dropdown; Add Track modal; CDP nav link (HC/Admin only)
3. **CRUD Add and Edit** — Inline add inputs for all 3 levels; pencil-icon in-place rename via AJAX; antiforgery token wired
4. **Delete Guards** — Trash icon → Bootstrap modal showing affected active coachee count; cascade delete (Deliverables→SubKompetensi→Kompetensi→Track); FK-safe order enforced

**What Was Cancelled:**
- **Drag-and-Drop Reorder (Phase 37)** — SortableJS incompatible with nested-table tree structure; collapse-state preservation shipped instead as bonus fix

**Metrics:**
- 4 shipped phases (33-36), 8 plans (Phase 37 cancelled)
- 2026-02-23 → 2026-02-24

See `.planning/milestones/v1.9-ROADMAP.md` for full details.

---

### ✅ v1.7 - Assessment System Integrity (2026-02-21)

**Delivered:** Full exam lifecycle management, package answer persistence, server-side token enforcement, HC audit trail, worker history and competency feedback, and data integrity safeguards.

**What Shipped:**
1. **Exam State Tracking** — InProgress status + StartedAt timestamp on first exam load; idempotent write; 4-state monitoring badge (Not started / InProgress / Abandoned / Completed)
2. **Exam Lifecycle Actions** — Worker abandon flow (Keluar Ujian); HC force-close sets Completed+Score=0; HC reset clears score+answers+StartedAt; server-side timer with 2-min grace; configurable ExamWindowCloseDate lockout
3. **Package Answer Integrity** — PackageUserResponse table stores one row per question on submit; answer review for package exams works identically to legacy path; server-side token guard (TempData) blocks direct URL bypass
4. **HC Audit Log** — AuditLogService (scoped DI) logs all 7+ HC management actions with actor NIP/name; paginated read-only AuditLog page (HC/Admin only, 25/page); accessible from Assessment manage view header
5. **Worker UX** — Riwayat Ujian table on Assessment page listing completed history; Kompetensi Diperoleh card on Results page showing earned competency levels (IsPassed=true only)
6. **Data Integrity Safeguards** — DeletePackage shows assignment count in JS confirm dialog + cascades PKR→UPA→options→questions→package; EditAssessment JS IIFE warns on schedule date change when packages attached

**Metrics:**
- 6 phases (21-26), 14 plans
- 83 files changed, 17,854 insertions / 222 deletions
- 2026-02-20 → 2026-02-21

See `.planning/milestones/v1.7-ROADMAP.md` for full details.

---

### ✅ v1.6 - Training Records Management (2026-02-20)

**Delivered:** HC and Admin can fully manage manual training records — create with certificate upload, edit all fields via in-page Bootstrap modal, and delete with file cleanup

**What Shipped:**
1. **Data Foundation** — TrainingRecord model extended with TanggalMulai, TanggalSelesai, NomorSertifikat, Kota; IWebHostEnvironment injected in CMPController; `wwwroot/uploads/certificates/` upload directory created
2. **Create Training Record** — "Create Training" button on RecordsWorkerList; system-wide worker dropdown; all form fields with required validation; PDF/JPG/PNG certificate upload to `wwwroot/uploads/certificates/`; certificate downloadable from WorkerDetail
3. **Edit via Bootstrap Modal** — Pencil button on each manual training row in WorkerDetail opens pre-populated in-page modal; all fields editable except Pekerja; uploading new cert replaces old file on disk; POST-only (no separate edit page)
4. **Delete with Cleanup** — Trash button triggers browser `confirm()`; on confirm removes DB row and certificate file from disk; TempData success alert on return

**Metrics:**
- 3 phases (18-20), 3 plans + quick-11
- 55 files changed, 8,230 insertions / 2,973 deletions
- 2026-02-20

See `.planning/milestones/v1.6-ROADMAP.md` for full details.

---

### ✅ v1.5 - Question and Exam UX (2026-02-19)

**Delivered:** HC can manage multi-package test sets with Excel import; workers take paged exams with per-user randomization, pre-submit review, and ID-based grading

**What Shipped:**
1. **Package Data Model** — AssessmentPackage, PackageQuestion, PackageOption, UserPackageAssignment entities + AddPackageSystem EF migration
2. **HC Package Management** — ManagePackages page (create/delete), ImportPackageQuestions (Excel upload + paste with flexible Correct-column parser), PREVIEW MODE for HC review
3. **Per-User Randomization** — Random package assignment on exam start; Fisher-Yates shuffle for question order and option order per user; shuffled order persisted as JSON in UserPackageAssignment
4. **Paged Exam View** — 10 questions/page, Prev/Next, countdown timer (red at 5 min, auto-submit at 0), collapsible question number panel, "X/N answered" header counter
5. **Pre-Submit Review + Grading** — ExamSummary page with unanswered warning; SubmitExam updated with if/else package-path (ID-based PackageOption.IsCorrect grading) and legacy-path (unchanged)

**Metrics:**
- 1 phase (17), 7 plans
- 31 files changed, 6,951 insertions / 169 deletions
- 2026-02-19

See `.planning/milestones/v1.5-ROADMAP.md` for full details.

---

### ✅ v1.4 - Assessment Monitoring (2026-02-19)

**Delivered:** Grouped monitoring tab replacing flat per-session list — completion rate, pass rate, and dedicated per-user detail page

**What Shipped:**
1. **Grouped Monitoring Tab** — One row per assessment group (Title+Category+Date); completion progress bar; pass rate indicator
2. **Monitoring Detail Page** — AssessmentMonitoringDetail view with per-user table (Name, NIP, Status, Score, Pass/Fail, Completed At)
3. **Recently Closed Sessions** — Monitoring includes sessions closed within last 30 days alongside Open + Upcoming; sorted by schedule date

**Metrics:**
- 1 phase (16), 3 plans
- 2026-02-19

See `.planning/milestones/v1.5-REQUIREMENTS.md` for MON requirement traceability.

---

### ✅ v1.3 - Assessment Management UX (2026-02-19)

**Delivered:** Clean assessment management for HC — dedicated navigation cards, restored creation flow, and bulk user assignment directly on the Edit Assessment page

**What Shipped:**
1. **Navigation & Creation Flow** — CMP Index redesigned with separate Assessment Lobby card (all roles) and Manage Assessments card (HC/Admin only); embedded Create Assessment form removed; CreateAssessment POST redirects to manage view on success
2. **Bulk Assign** — EditAssessment page extended with currently-assigned-users table, section-filtered user picker, and transaction-backed bulk AssessmentSession creation on save; existing sessions untouched

**What Was Cancelled:**
- **Quick Edit (Phase 15)** — Inline modal for status/schedule edit reverted; the existing Edit Assessment page covers the need without extra surface area

**Metrics:**
- 2 shipped phases (13-14), 2 plans, 7/9 requirements shipped
- 15 files changed, 1,731 insertions / 606 deletions
- 1 day (2026-02-19)

See `.planning/milestones/v1.3-ROADMAP.md` for full details.

---

### ✅ v1.2 - UX Consolidation (2026-02-19)

**Delivered:** Role-aware UX consolidation — Assessment page refocused, Training Records unified, Gap Analysis removed, three dashboards merged into one tabbed CDP Dashboard

**What Shipped:**
1. **Assessment Page Role Filter** — Workers see Open/Upcoming only; HC/Admin get Management + Monitoring tabs; Training Records callout directs workers to their history
2. **Unified Training Records** — Assessment sessions and manual training merged into single chronological table with type-differentiated columns; HC worker list with combined completion rate
3. **Gap Analysis Removed** — Page, nav links, controller action, view, and ViewModel deleted atomically
4. **CDP Dashboard Consolidated** — HC Reports and Dev Dashboard absorbed into two-tab dashboard (Proton Progress all-role scoped; Analytics HC/Admin only); three standalone pages retired

**Metrics:**
- 4 phases (9-12), 8 plans, 11/11 requirements shipped
- 25 files changed, 2,435 insertions / 2,995 deletions (net: -560)
- 12 feature commits over 2 days (2026-02-18 → 2026-02-19)

See `.planning/milestones/v1.2-ROADMAP.md` for full details.

---

### ✅ v1.1 - CDP Coaching Management (2026-02-18)

**Delivered:** Full coaching cycle — session logging, Proton deliverable tracking, approval workflow, role-scoped development dashboard, and Admin role switcher fix

**What Shipped:**
1. **Coaching Sessions** — Domain-specific session fields (Kompetensi, SubKompetensi, CatatanCoach), action items with due dates, and coaching history with filtering
2. **Proton Deliverable Tracking** — Structured Kompetensi hierarchy with sequential lock; coaches upload and revise evidence files per deliverable
3. **Approval Workflow & Completion** — SrSpv → SectionHead approval chain with rejection reasons; HC final approval triggers Proton Assessment that auto-updates competency levels
4. **Development Dashboard** — Role-scoped monitoring (Spv/SrSpv/SectionHead/HC/Admin) with Chart.js trend charts
5. **Admin Role Switcher** — Admin can simulate all 5 role views with correct access gates and scoped data

**Metrics:**
- 5 phases (4-8), 13 plans
- See `.planning/milestones/` for full details

---

### ✅ v1.0 - CMP Assessment Completion (2026-02-17)

**Delivered:** Complete assessment workflow with results display, HC analytics dashboard, and automated competency tracking

**What Shipped:**
1. **Assessment Results & Configuration** — Users see their scores immediately after completion with pass/fail status and conditional answer review. HC can configure pass thresholds (0-100%) and toggle review visibility per assessment.

2. **HC Reports Dashboard** — Analytics dashboard with multi-parameter filtering, Chart.js visualizations (pass rates by category, score distributions), Excel export via ClosedXML, and individual user assessment history.

3. **KKJ/CPDP Integration** — Automatic competency level updates on assessment completion, gap analysis dashboard with radar chart visualization, CPDP progress tracking with assessment evidence linking, and IDP suggestions based on competency gaps.

**Impact:**
- Users no longer confused after completing assessments
- HC can measure training effectiveness and make data-driven decisions
- Competency tracking now automated and evidence-based
- Full integration loop: Assessments → KKJ Competencies → CPDP Framework → IDP

**Metrics:**
- 3 phases, 10 plans completed
- 6/6 functional requirements satisfied
- 47 files changed, 7,826 lines added
- 22 feature commits over 43 days

See `.planning/milestones/v1.0-ROADMAP.md` for full details.

---

## Current State (v2.5)

**Tech Stack:**
- ASP.NET Core 8.0 MVC (C#)
- Entity Framework Core
- SQL Server / SQLite
- Razor Views (server-side rendering)
- ASP.NET Identity (authentication)
- Chart.js (loaded globally via _Layout.cshtml)
- ClosedXML (Excel export)
- QuestPDF (PDF export — v2.4)
- System.DirectoryServices (LDAP auth — v2.5)

**Working Features:**

### Authentication & Authorization
- ✅ Dual authentication via IAuthService abstraction (v2.5): config toggle `Authentication:UseActiveDirectory` routes to Local or LDAP
- ✅ HybridAuthService: AD-first + local fallback for admin@pertamina.com (v2.5)
- ✅ 10-level role hierarchy: Admin, HC, SectionHead(3), SrSpv(4), Supervisor(5), Spv, Coach, Coachee, plus Worker and User
- ✅ Multi-view system — Admin can simulate all role views; UserRoles.GetDefaultView() single source of truth
- ✅ Section-based access control
- ✅ Profile sync on AD login: FullName and Email only (Role/SelectedView never changed)

### User Management (v2.5)
- ✅ Dynamic Profile page — bound to @model ApplicationUser; real user data; null-safe fallback; avatar initials
- ✅ Functional Settings page — Change password, edit FullName/Position; non-functional items disabled
- ✅ ManageWorkers in AdminController — 11 CRUD actions with [Authorize(Roles = "Admin, HC")]
- ✅ Kelola Data hub — 3 domain sections (Manajemen Pekerja, Kelola Assessment, Data Proton)

### CMP Module (Competency Management)
- ✅ **Assessment Engine:**
  - Create assessments with multiple users assignment (dedicated CreateAssessment page; HC redirected to manage view on success)
  - Edit/delete assessments (HC/Admin only); Edit page extended with bulk assign capability
  - Assessment lobby: role-filtered (workers see Open/Upcoming only; HC/Admin get Management + Monitoring tabs)
  - Token-based access control (optional per assessment)
  - Question management (add/delete questions with multiple choice options)
  - Take exam interface: paged (10/page), countdown timer, collapsible panel, answered counter
  - Pre-submit review (ExamSummary page with unanswered warning)
  - Auto-scoring, pass/fail with configurable thresholds, conditional answer review
  - ID-based grading for package exams (PackageOption.IsCorrect — never letter-based)
  - Certificate view (after completion)
  - Training Records callout for workers to find their completion history

- ✅ **Assessment Management (HC/Admin — CMP):**
  - Dedicated Manage Assessments card on CMP Index (HC/Admin only; workers see Assessment Lobby only)
  - Manage view: grouped assessment cards (1 card per unique Title+Category+Date assessment, compact user list, group delete)
  - Bulk Assign: EditAssessment page shows currently-assigned users + section-filtered picker to add more; new AssessmentSessions created on save without altering existing ones
  - **Test Packages (v1.5, updated v2.1):** HC creates packages per assessment; imports questions via Excel/paste; cross-package per-position shuffle gives each worker a unique question mix from multiple packages (v2.1 replaces single-package Fisher-Yates)
  - **Package Management (v1.5, updated v2.1):** ManagePackages page (create/delete + summary panel showing mode/badge/mismatch), ImportPackageQuestions (Excel upload + paste + cross-package count validation), PREVIEW MODE for HC
  - **Grouped Monitoring (v1.4):** Monitoring tab shows one row per assessment group with completion bar + pass rate; "View Details" links to per-user detail page with 4-state UserStatus (Not started/InProgress/Abandoned/Completed)
  - **Exam Lifecycle (v1.7):** InProgress tracking + StartedAt timestamp; worker Keluar Ujian abandon; HC ForceClose (Score=0) and Reset (clears for retake); server-side timer (+2min grace); configurable ExamWindowCloseDate lockout
  - **Answer Integrity (v1.7):** PackageUserResponse table; answer review for package exams; server-side token enforcement (TempData guard)
  - **HC Audit Log (v1.7):** All 7+ management actions logged with actor+timestamp; paginated read-only AuditLog page (HC/Admin only)
  - **Worker UX (v1.7):** Riwayat Ujian history table on Assessment page; Kompetensi Diperoleh card on Results page
  - **Data Integrity (v1.7):** DeletePackage assignment-count warning + cascade; EditAssessment schedule-change JS guard

- ✅ **Assessment Analytics (HC/Admin — in CDP Dashboard):**
  - KPI cards, multi-parameter filtering, paginated results table
  - Excel export with ClosedXML, individual user assessment history
  - Chart.js visualizations (pass rates by category, score distributions)
  - Absorbed into CDP Dashboard Analytics tab — standalone HC Reports page retired

- ✅ **Competency Tracking:**
  - Auto-update on assessment completion via AssessmentCompetencyMap
  - CPDP Progress Tracking with assessment evidence per competency
  - Position-based targets (15 position mappings)
  - ~~Gap Analysis Dashboard~~ — removed in v1.2

- ✅ **KKJ Matrix:** Skill matrix with target competency levels per position, section-based filtering

- ✅ **CPDP Mapping:** Competency framework (nama kompetensi, indikator perilaku, silabus)

- ✅ **Unified Training Records:**
  - Personal view: assessment sessions + manual training merged in one chronological table
  - Type badges differentiate Assessment Online (Score, Pass/Fail) vs Training Manual (Penyelenggara, Sertifikat, Berlaku Sampai)
  - HC/Admin worker list with combined completion rate from both data sources
  - **All Workers History Tab (v2.0):** RecordsWorkerList "History" tab — all workers' manual training + completed assessments merged, sorted by date descending; 8-column table with type badge; URL-persisted tab (?activeTab=history)

- ✅ **Assessment Resilience (v2.1):**
  - Auto-save: answers saved per-click via AJAX (atomic upsert + UNIQUE constraint); legacy path via SaveLegacyAnswer; visual "Tersimpan" feedback
  - Session resume: ElapsedSeconds + LastActivePage persisted; resume modal on reconnect; pre-populated answers; offline time excluded from duration
  - Worker polling: 10s poll with IMemoryCache (5s TTL); auto-redirect to Results on HC close-early; CloseEarly cache invalidation

- ✅ **Real-Time Monitoring (HC/Admin — v2.1):**
  - GetMonitoringProgress JSON endpoint; 10s table auto-refresh + 1s countdown timer; JS-rendered Reset/ForceClose action buttons
  - 8-column table: Name, Progress (answered/total), Status, Score, Result, CompletedAt, TimeRemaining, Actions

- ✅ **Assessment Lifecycle (HC/Admin — v2.0):**
  - Close Early: "Tutup Lebih Awal" button on MonitoringDetail (HC/Admin only, Open groups only); scores InProgress workers from actual answers; audit log entry
  - Auto-hide: Management and Monitoring tabs both apply 7-day cutoff filter post-close; fallback to Schedule date if no ExamWindowCloseDate set

- ✅ **Training Record Management (HC/Admin — v1.6):**
  - "Create Training" button on RecordsWorkerList → system-wide worker dropdown form
  - Fields: Nama Pelatihan, Penyelenggara, Kategori, Kota, Tanggal, Status, NomorSertifikat, Berlaku Sampai, Certificate file (PDF/JPG/PNG, max 10MB)
  - Certificate stored in `wwwroot/uploads/certificates/` with timestamp-prefixed filename; downloadable from WorkerDetail
  - Edit via in-page Bootstrap modal (pencil button on manual rows only); all fields editable except Pekerja; new cert upload replaces old file on disk
  - Delete with browser `confirm()`; removes DB row and certificate file from disk
  - Assessment session rows have no Edit/Delete actions — manual training rows only

### CDP Module (Competency Development)
- ✅ IDP (Individual Development Plan) management
- ✅ Coaching sessions with domain-specific fields (Kompetensi, SubKompetensi, CatatanCoach, action items)
- ✅ Proton deliverable tracking with evidence upload/revise
- ✅ Approval workflow (SrSpv → SectionHead → HC) with rejection reasons
- ✅ HC HCApprovals queue + final Proton Assessment creation
- ✅ **Proton Catalog Manager (v1.9):** ProtonCatalogController; tree view (Kompetensi→SubKompetensi→Deliverable) with Bootstrap expand/collapse; track dropdown; Add Track modal; inline add for all 3 levels; pencil-icon rename; trash icon delete with coachee impact modal (cascade: Deliverables→SubKompetensi→Kompetensi→Track)

- ✅ **Unified CDP Dashboard (two tabs):**
  - Proton Progress tab: role-scoped team data (Coachee=self, Spv=unit, SrSpv/SectionHead=section, HC/Admin=all)
  - Assessment Analytics tab: HC/Admin only (absorbed from standalone HC Reports)
  - ~~DevDashboard~~ and ~~HC Reports~~ standalone pages retired

- ✅ **CDP Progress Page (v2.4):**
  - Data sourced from ProtonDeliverableProgress + ProtonTrackAssignment (not IdpItems); real coachee list from CoachCoacheeMapping
  - 5 filters (Bagian/Unit, Coachee, Track, Tahun) wired to EF Core Where composition; role-scope-first pattern; client-side search
  - Per-role approval workflow: SrSpv/SectionHead/HC independent approval columns; rejection precedence
  - Coaching report + evidence combined modal; CoachingSession FK linked
  - Excel (ClosedXML) + PDF (QuestPDF) export
  - Group-boundary server-side pagination (20 rows/page); 3 empty state scenarios; filter counter

### BP Module
- ⏸️ **NOT IN SCOPE** - Talent profiles, eligibility, point system (postponed)

## Key Decisions

| Decision | Outcome | Milestone |
|----------|---------|-----------|
| Use configurable pass thresholds per assessment | HC sets 0-100% threshold per assessment category | v1.0 ✓ |
| Auto-update competency on assessment completion | AssessmentCompetencyMap links categories to KKJ competencies; monotonic progression | v1.0 ✓ |
| Admin role switcher — simulate all views | isHCAccess pattern (named bool) for HC gates; isCoacheeView for personal-records branch | v1.1 ✓ |
| Proton sequential lock | Only next deliverable unlocks after current is approved — enforced at controller level | v1.1 ✓ |
| Gap Analysis hard-deleted, no stub | Executed as complete removal; redirect stub from STATE.md decision overridden by PLAN spec | v1.2 ✓ |
| Admin always gets HC branch in Assessment() and Records() | Consistent regardless of SelectedView — SelectedView only affects personal-records Coachee/Coach | v1.2 ✓ |
| UnifiedTrainingRecord two-query in-memory merge | Separate EF Core queries for AssessmentSessions and TrainingRecords; merged in memory with OrderByDescending | v1.2 ✓ |
| Assessment filter at DB level, not view | `IsPassed==true, Status==Open\|Upcoming` filter applied in IQueryable before `.ToListAsync()` | v1.2 ✓ |
| Admin simulating Coachee still sees Analytics tab | SelectedView NOT checked for Analytics gate — Admin sees it regardless of simulated view | v1.2 ✓ |
| Chart.js CDN moved to _Layout.cshtml globally | Partials cannot use @section Scripts; layout-level CDN is the only pattern | v1.2 ✓ |
| activeTab hidden input for analytics filter | Filter form submits `activeTab=analytics` to re-activate correct tab on GET reload | v1.2 ✓ |
| Separate cards per concern (CMP Index) | Assessment Lobby (all roles) and Manage Assessments (HC/Admin) as independent cards — no branching inside a single card | v1.3 ✓ |
| Sibling session matching uses Title+Category+Schedule.Date | Consistent with existing CreateAssessment duplicate-check query; identifies all users on the same batch assessment | v1.3 ✓ |
| Bulk assign excluded users at Razor render time | Already-assigned users excluded via ViewBag.AssignedUserIds at Razor render, not JS — simpler and avoids client-side state issues | v1.3 ✓ |
| Quick Edit cancelled — Edit page sufficient | Phase 15 inline modal reverted before shipping; EditAssessment page covers status+schedule changes without extra controller surface area | v1.3 ✓ |
| Grouped monitoring tab (one row per assessment identity) | In-memory grouping after ToListAsync(); MonitoringGroupViewModel canonical shape for all monitoring data | v1.4 ✓ |
| No Letter field on PackageOption | Letters (A/B/C/D) are display-only at render time by position index; grading uses PackageOption.Id | v1.5 ✓ |
| UserPackageAssignment shuffle stored as JSON strings | ShuffledQuestionIds + ShuffledOptionIdsPerQuestion as JSON on the assignment row — no join tables needed | v1.5 ✓ |
| Package path and legacy path mutually exclusive in SubmitExam | if (packageAssignment != null) → package path; else → legacy loop — UserResponse not inserted for package exams (FK constraint incompatibility) | v1.5 ✓ |
| Packages found via sibling session query | StartExam GET searches siblings (same Title+Category+Date) — packages attached to representative session ID, not worker session ID | v1.5 ✓ |
| TempData int/long unboxing switch pattern | CookieTempDataProvider deserializes JSON integers as long in .NET; switch { int i => i, long l => (int)l, _ => null } | v1.5 ✓ |
| CreateTrainingRecord redirects to Records?isFiltered=true on success | Avoids blank initial state after creation; consistent with existing Records filter UX | v1.6 ✓ |
| Worker dropdown on CreateTrainingRecord is system-wide | All users, no section filter — HC can create records for any worker per TRN-01 | v1.6 ✓ |
| File validation errors added to ModelState inline (not TempData) | Consistent form UX; errors render next to fields with asp-validation-for spans | v1.6 ✓ |
| SertifikatUrl populated in GetUnifiedRecords | Certificate links appear in both WorkerDetail and Coach/Coachee Records views without separate queries | v1.6 ✓ |
| EditTrainingRecord has no GET action — Bootstrap modal only | Pre-populated inline via Razor in WorkerDetail; POST-only approach avoids separate page navigation per discuss-phase decision | v1.6 ✓ |
| WorkerId/WorkerName on EditTrainingRecordViewModel | POST redirect to WorkerDetail requires no extra DB lookup; passed as hidden inputs in modal form | v1.6 ✓ |
| File replace on edit: delete old file from disk, save new file | Prevents orphaned certificates accumulating in wwwroot/uploads/certificates/ | v1.6 ✓ |
| Delete removes DB row + certificate file from disk | Atomic cleanup; no orphaned files; matches user expectation from confirm() dialog | v1.6 ✓ |
| Clear-cert-without-replacing out of scope | Discuss-phase decision: HC can only replace; removed ROADMAP success criterion #4 to align | v1.6 ✓ |
| StartedAt == null as idempotent first-write sentinel | StartedAt is the authoritative guard (not Status string) for InProgress write in StartExam GET — timestamp-based prevents double-write on reload | v1.7 ✓ |
| Abandoned branch before InProgress in UserStatus projection | Abandoned sessions have StartedAt set — checking Abandoned before StartedAt!=null prevents misclassification in 4-state projection | v1.7 ✓ |
| ResetAssessment deletes UPA so next StartExam gets fresh package | Deleting UserPackageAssignment on reset forces new random package assignment on next StartExam; ForceCloseAssessment preserves answers for audit | v1.7 ✓ |
| TempData[TokenVerified_{id}] scoped by assessment ID | Token verification is scoped to session ID (not global) — prevents one session's token from bypassing another; InProgress sessions bypass guard | v1.7 ✓ |
| AuditLogService SaveChangesAsync internal | Audit rows written immediately by service; actor name stored as "NIP - FullName" at write time for permanence; delete actions wrap audit in try/catch | v1.7 ✓ |
| PackageOptionId nullable int on PackageUserResponse | null = skipped question (no answer selected); matches UserResponse.SelectedOptionId pattern | v1.7 ✓ |
| viewModel declared outside Results if/else branches | Enables shared competency lookup block after both package and legacy paths without code duplication | v1.7 ✓ |
| DeletePackage cascade order: PKR → UPA → options → questions → package | Correct FK-safe deletion order prevents constraint violations; assignment counts pre-computed in ManagePackages GET via GroupBy | v1.7 ✓ |
| GetMonitorData 2-state UserStatus (deferred tech debt) | GetMonitorData uses isCompleted ? "Completed" : "Not started" — Abandoned/InProgress show as "Not started" in card summary; 4-state view works correctly in AssessmentMonitoringDetail | v1.7 deferred |
| ProtonTrack as first-class entity | ProtonTrack table with Id/TrackType/TahunKe/DisplayName/Urutan; ProtonKompetensi and ProtonTrackAssignment reference via FK; all string-based filtering eliminated | v1.9 ✓ |
| Catalog tree uses Bootstrap collapse, not SortableJS | Nested-table structure with collapse containers; SortableJS attempted for drag-drop but proved incompatible (containers don't move with parent rows) | v1.9 ✓ |
| Drag-and-drop reorder cancelled entirely | SortableJS incompatible with nested-table tree; user decision to remove entirely rather than implement workaround; collapse-state preservation shipped as bonus | v1.9 cancelled |
| CloseEarly scores from PackageUserResponse (not Score=0) | InProgress sessions at early close scored from actual submitted answers using same grading logic as SubmitExam package path; fair to workers who had started | v2.0 ✓ |
| 7-day auto-hide uses ExamWindowCloseDate ?? Schedule.Date | Both GetManageData and GetMonitorData apply identical cutoff; fallback to Schedule ensures groups without explicit close date still age out | v2.0 ✓ |
| GetAllWorkersHistory merges in memory, not SQL UNION | Two separate EF Core queries (TrainingRecords + AssessmentSessions); merged and sorted in memory via LINQ OrderByDescending — consistent with UnifiedTrainingRecord pattern | v2.0 ✓ |
| RecordsWorkerListViewModel wraps existing Workers list | ViewModel wrapper preserves all existing worker-list functionality unchanged; History list added as second property — minimal disruption to existing code | v2.0 ✓ |
| ExecuteUpdateAsync atomic upsert for SaveAnswer | Avoids EF change tracking race on concurrent auto-saves; UNIQUE constraint (SessionId, QuestionId) as safety net | v2.1 ✓ |
| ElapsedSeconds non-nullable int DEFAULT 0 | Clean accumulation, no null checks; LastActivePage/SavedQuestionCount nullable (null = pre-v2.1 session) | v2.1 ✓ |
| Offline time excluded from exam duration | RemainingSeconds = (DurationMinutes*60) - ElapsedSeconds; disconnect gap is "free time" by design | v2.1 ✓ |
| IMemoryCache 5s TTL for CheckExamStatus | Session-scoped key; ~99% DB reduction for concurrent polls; CloseEarly invalidates immediately | v2.1 ✓ |
| GetMonitoringProgress single GROUP BY query | Answered counts via one GROUP BY on PackageUserResponse; not N+1 per session | v2.1 ✓ |
| Cross-package per-position shuffle replaces single-package | BuildCrossPackageAssignment: 1 pkg = DB order, N pkgs = even distribution + Fisher-Yates; sentinel FK; option shuffle removed | v2.1 ✓ |
| Import validation enforces equal question counts | Block import if count differs from existing non-empty packages; empty packages excluded from validation | v2.1 ✓ |
| All consumers load questions by ShuffledQuestionIds | SubmitExam, ExamSummary, Results, CloseEarly all use shuffledIds.Contains — no AssessmentPackageId filter | v2.1 ✓ |
| Archive-before-clear in ResetAssessment | Archival block placed before UserResponse deletion so Score/IsPassed are still intact; archive + reset share one SaveChangesAsync | v2.2 ✓ |
| AttemptNumber as count+1 (no sequence column) | AttemptNumber = existing AssessmentAttemptHistory rows for (UserId, Title) + 1; simple count, no DB sequence needed | v2.2 ✓ |
| GetAllWorkersHistory returns (assessment, training) tuple | Two lists have different sort orders and columns; tuple cleaner than single list with discriminator flag | v2.2 ✓ |
| Batch GroupBy/ToDictionary for archived counts | Compute archived count per (UserId, Title) once via GroupBy; ToDictionary lookup per current session — avoids N+1 against AssessmentAttemptHistory | v2.2 ✓ |
| Role-scope-first filter pattern | Derive scopedCoacheeIds from role before applying URL params; EF Where composition chains | v2.4 ✓ |
| Per-role approval columns (SrSpv/SH/HC) | Independent approval columns; any role rejecting sets Status=Rejected; data migration backfills from existing Approved | v2.4 ✓ |
| Group-boundary pagination | Group by (CoacheeName, Kompetensi, SubKompetensi) then slice ≤20 rows without splitting a group | v2.4 ✓ |
| QuestPDF for PDF export | QuestPDF NuGet installed for ProtonProgress PDF export alongside ClosedXML for Excel | v2.4 ✓ |
| ManageWorkers migration clean break | No redirect from old CMP URL — clean break; 11 actions moved to AdminController | v2.5 ✓ |
| IAuthService abstraction for dual auth | LocalAuthService + LdapAuthService behind common interface; DI factory delegate in Program.cs | v2.5 ✓ |
| Global config routing (not per-user AuthSource) | UseActiveDirectory config toggle routes all users to same auth; AuthSource field added then removed | v2.5 ✓ |
| HybridAuthService AD-first + local fallback | admin@pertamina.com gets local fallback after AD failure; all others AD-only in AD mode | v2.5 ✓ |
| Supervisor role level 5, SectionHead level 3 | New Supervisor role bridges Coach and SrSpv; SectionHead demoted for management-tier parity | v2.5 ✓ |

## Technical Constraints

**Must maintain:**
- ASP.NET Core MVC architecture (no API rewrite)
- Razor server-side rendering (no SPA framework)
- Entity Framework Core for data access
- Existing database schema (use migrations for changes)
- Role-based authorization system

**Database:**
- Development: SQLite or SQL Server LocalDB
- Production: SQL Server

**Limitations:**
- No email notification system (yet)
- No automated testing (manual QA only)
- Large monolithic controllers (CMPController ~3200+ lines post-v2.2, CDPController ~1000+ lines, ProtonCatalogController ~400 lines)

## Shipped Requirements

All requirements from v1.0–v2.5 are satisfied. See milestone archives for traceability:
- `milestones/v1.0-REQUIREMENTS.md` — 6 requirements (Phases 1-3)
- `milestones/v1.2-REQUIREMENTS.md` — 11 requirements (Phases 9-12, all ✅ Shipped)
- `milestones/v1.3-REQUIREMENTS.md` — 9 requirements (Phases 13-15; 7 shipped, 2 cancelled)
- `milestones/v1.6-REQUIREMENTS.md` — 4 requirements (TRN-01 through TRN-04, all ✅ Shipped)
- `milestones/v1.7-REQUIREMENTS.md` — 14 requirements (LIFE-01–05, ANSR-01–02, SEC-01–02, WRK-01–02, DATA-01–03, all ✅ Shipped)
- `milestones/v1.9-REQUIREMENTS.md` — 10 requirements (SCHEMA-01, CAT-01–09; 9 shipped, CAT-08 cancelled)
- `milestones/v2.0-REQUIREMENTS.md` — 4 requirements (ASSESS-01–03, HIST-01, all ✅ Shipped)
- `milestones/v2.1-REQUIREMENTS.md` — 11 requirements (SAVE-01–03, RESUME-01–03, POLL-01–02, MONITOR-01–03, all ✅ Shipped)
- `milestones/v2.2-REQUIREMENTS.md` — 3 requirements (HIST-01–03, all ✅ Shipped)
- `milestones/v2.3-REQUIREMENTS.md` — 12 v2.3 requirements (MDAT/OPER/CRUD; 7 shipped, 5 deferred)
- `milestones/v2.4-REQUIREMENTS.md` — 17 requirements (DATA/FILT/ACTN/UI, all ✅ Shipped)
- `milestones/v2.5-REQUIREMENTS.md` — 21 requirements (PROF/AUTH/USR/USTR/AUTH-HYBRID, all ✅ Shipped)

## Users & Roles

**Primary Users:**

1. **Coachee (Worker/Staff)** - Level 6
   - Take assessments assigned to them
   - View their results and certificates
   - Track their IDP
   - View personal training records

2. **Supervisor (Spv)** - Level 5
   - Same as Coachee +
   - Coach their team members
   - View team training records

3. **Senior Supervisor (SrSpv)** - Level 4
   - Same as Spv +
   - Approve IDP items
   - View section-level reports

4. **Section Head** - Level 3
   - Same as SrSpv +
   - Approve IDP after SrSpv
   - Manage section workers

5. **HC (Human Capital)** - Level 2
   - Create/edit/delete assessments
   - Assign assessments to users
   - View all reports and analytics
   - Final IDP approval
   - Manage training records
   - Export data

6. **Admin** - Level 1
   - Full system access
   - Can switch views (Coachee/Atasan/HC)
   - System configuration

## Out of Scope

❌ BP Module development (talent profiles, eligibility, point system)
❌ Email notifications (future enhancement)
❌ Mobile app (web-only)
❌ Real-time collaboration features
❌ Advanced security features (2FA, OAuth)
❌ Performance optimization (unless critical)
❌ Automated testing implementation
❌ API endpoints (MVC only)

## Technical Debt

- Large monolithic controllers (CMPController ~3200+ lines post-v2.1, CDPController 1000+ lines, ProtonCatalogController ~400 lines)
- No automated testing — manual QA only
- `GetMonitorData` uses 2-state UserStatus (`"Completed"` vs `"Not started"`) — Abandoned/InProgress show as "Not started" in monitoring card summary; 4-state view works correctly in AssessmentMonitoringDetail
- `GetPersonalTrainingRecords()` in CMPController is dead code (not called) — retained to avoid scope risk
- N+1 queries addressed in batch where identified but not systematically audited
- No UI to re-assign packages or manually override shuffle — edge case with no workaround
- `ShuffledOptionIdsPerQuestion` field on UserPackageAssignment is deprecated (always "{}") — kept for schema compatibility but unused since v2.1 cross-package shuffle removed option randomization
- Close Early + SubmitExam concurrency race not hardened (CONCUR-01 deferred; not in v2.3 scope)
- Proton catalog drag-and-drop reorder removed (SortableJS incompatible with nested-table tree); no UI reordering for catalog items
- Worker self-add training records deferred (WTRN-01, WTRN-02)

## References

- Codebase analysis: `.planning/codebase/ARCHITECTURE.md`
- Known issues: `.planning/codebase/CONCERNS.md`
- Tech stack: `.planning/codebase/STACK.md`
- Milestone history: `.planning/MILESTONES.md`

---

*Last updated: 2026-04-03 after Phase 295 Drag-drop Reorder — SortableJS sibling-only reorder for organization tree*
