# Milestones
## v5.0 Guide Page Overhaul (Shipped: 2026-03-16)

**Phases completed:** 2 phases (171-172), 4 plans, 8 tasks
**Timeline:** 2026-03-16 (1 day)
**Code changes:** 17 files changed, +1,709 / -385 lines
**Commits:** 18

**Delivered:** Guide & FAQ system cleanup and UI polish — redundant accordion guides removed (covered by PDF tutorials), dynamic role-based card counts, FAQ expand/collapse toggle, unified badge and button styling, back-to-top navigation, and breadcrumb navigation on GuideDetail pages.

**Key accomplishments:**
1. GuideDetail accordion simplification — CMP reduced from 7 to 4 items (5 for Admin/HC), CDP reduced from 7 to 3 items (5 for Admin/HC), redundant step-by-step items removed as covered by PDF tutorials
2. Tutorial card CSS refactor — Inline styles replaced with CSS variant modifier classes (guide-tutorial-card--cmp/cdp/admin), AD guide tutorial card added for admin module
3. Dynamic guide card counts — All 5 module cards show role-conditional counts via Razor int variables, matching actual GuideDetail accordion item counts
4. FAQ improvements — Expand/collapse all toggle button, categories reordered by priority, redundant step-by-step FAQ items removed
5. UI consistency — Unified .guide-role-badge class across Guide and GuideDetail, .step-variant-blue replacing pink, shared accordion base styling
6. Navigation — Floating back-to-top button on both Guide pages, GuideDetail breadcrumb (Beranda > Panduan > Module Name) replacing back button

**Tech Debt (1 item, non-blocking):**
- Legacy CSS alias .guide-step-badge-role kept in guide.css (no view references it)

---

## v4.3 Bug Finder (Shipped: 2026-03-13)

**Phases completed:** 3 phases (168-170), 8 plans
**Timeline:** 2026-03-13 (1 day)
**Code changes:** 49 files changed, +2,319 / -325 lines
**Commits:** 17

**Delivered:** Comprehensive codebase, file system, database, and security audit. Removed dead code, temp files, and unused imports. Fixed CSRF gap, XSS patterns, and file upload validation. Portal is clean, secure, and free of technical debt.

**Key accomplishments:**
1. Dead code removed — 2 unreachable controller actions (CleanupDuplicateAssignments, SearchUsers), 3 unused imports cleaned
2. Logic bugs fixed — 2 silent catch blocks now log at Warning level, all null dereference risks verified
3. File system cleaned — 40+ temp screenshots/artifacts removed, .gitignore hardened with 5 new patterns
4. Database verified — All 35 DbSets active, FK integrity confirmed, seed data properly gated
5. CSRF gap closed — NotificationController's [IgnoreAntiforgeryToken] removed, JS updated to send token header
6. XSS patterns fixed — 4 unsafe Html.Raw(x.Replace()) replaced with Json.Serialize, all 8 upload endpoints secured

**Tech Debt (5 items, all non-blocking):**
- Pre-existing bare catch at AdminController:1072 (intentional audit-log pattern)
- 1 null-forgiving operator deferred ([Authorize] guarantee)
- 3 orphaned KkjMatrixItemId columns (documented from Phase 90)
- 5 near-duplicate code pairs (below extraction threshold)
- SUMMARY prose counting error (27 vs 35 DbSets, non-blocking)

---

## v4.0 E2E Use-Case Audit (Shipped: 2026-03-12)

**Phases completed:** 6 phases (153-158), 16 plans
**Timeline:** 2026-03-11 → 2026-03-12 (2 days)
**Code changes:** 18 files changed, +2,737 / -66 lines
**Commits:** 72

**Delivered:** Comprehensive end-to-end audit of the entire portal organized by 6 use-case flows — code review + browser UAT per flow. All 33 requirements verified, 10+ bugs fixed, 10 tech debt items documented.

**Key accomplishments:**
1. Assessment flow hardened — Fixed DeleteQuestion FK crash, open redirect in Results, certificate access control (IsPassed guard), TrainingRecord auto-creation on exam submission
2. Coaching Proton bugs fixed — CoachCoacheeMappingReactivate cascades to restore ProtonTrackAssignments; SubmitInterviewResults creates ProtonFinalAssessment on pass
3. Admin data management audited — Fixed ProtonFinalAssessment cascade order in DeleteWorker, CPDP download MIME type, added missing audit log entries
4. CDP Dashboard scoping fixed — Coachee URL manipulation prevented (server-side override), duplicate key crash on multiple assignments resolved
5. Auth & authorization verified — Full controller authorization matrix confirmed across all 7 controllers, AccessDenied flow validated
6. Navigation integrity confirmed — All navbar links, guide pages, and hub cards verified; GuideDetail case-sensitivity bug fixed

**Tech Debt (10 items):**
- 2 deferred browser tests (assessment validation, certificate negative test)
- 3 coaching edge cases (ExportProgressExcel role attr, evidence storage, download auth)
- 2 admin edge cases (silabus delete warning, override status validation)
- 1 pre-existing (Chart.js rendering)
- 2 silent catch blocks (AD sync)

---

## v3.21 Account Profile & Settings Cleanup (Shipped: 2026-03-11)

**Phases completed:** 1 phase (152), 1 plan, 2 tasks
**Timeline:** 2026-03-11
**Files modified:** 5 (4 modified, 1 created)

**Delivered:** Account Profile & Settings page cleanup — authorization pattern, client-side validation, phone regex, ViewModel refactor, button label fix, and UI spacing consistency.

**Key accomplishments:**
1. Class-level `[Authorize]` on AccountController with `[AllowAnonymous]` on Login/AccessDenied
2. New ProfileViewModel replacing ViewBag for role display on Profile page
3. Client-side validation on Settings page via `_ValidationScriptsPartial`
4. Phone regex updated to accept international formats (+62 812-3456-7890)
5. Profile button label corrected to "Pengaturan", all rows unified to mb-3

---

## v3.8 CoachingProton UI Redesign (Shipped: 2026-03-07)

**Phases completed:** 1 phase (112), 1 plan, 2 tasks
**Timeline:** 2026-03-07
**Files modified:** 1 (Views/CDP/CoachingProton.cshtml)

**Delivered:** Complete visual redesign of CoachingProton page — clickable badges converted to proper buttons, status badges given bold+border treatment for resolved states, JS innerHTML synchronized with server-rendered styling, and Export PDF recolored for consistency.

**Key accomplishments:**
1. Converted 4 Pending badge spans to proper `btn-outline-warning` Tinjau buttons with preserved modal triggers
2. Added `fw-bold` + colored border to Approved/Rejected/Reviewed status badges via Razor helpers
3. Updated 6 JS innerHTML locations to match new badge styling after AJAX operations
4. Changed Export PDF button from red to green outline, matching Excel export
5. Unified Evidence column: Sudah Upload = bold green+border, Belum Upload = plain gray

---

## v3.6 Histori Proton (Shipped: 2026-03-06)

**Phases completed:** 2 phases (107-108), 4 plans
**Timeline:** 2026-03-06

**Delivered:** Proton History feature in CDP menu — role-scoped worker list with search/filter and vertical timeline detail page showing each worker's Proton journey (Tahun 1-3) with expandable nodes, status badges, and responsive styling.

**Key accomplishments:**
1. **CDP Histori Proton menu** — New navbar item with role-scoped access (Coachee self-redirect, Coach/SrSpv/SH section-scoped, HC/Admin all workers)
2. **Worker list page** — Table with search by nama/NIP, filter by unit/section, step indicator showing Tahun progress, status badges
3. **Timeline detail page** — Vertical left-aligned timeline with colored circles (green=Lulus, yellow=Dalam Proses), expandable Bootstrap Collapse cards per Proton year
4. **Per-node detail** — Each node shows Tahun, Unit, Coach name, Status, Competency Level (if lulus), Start/End dates
5. **Responsive design** — Bootstrap 5 consistent styling, mobile-friendly layout

**Files Modified:** Models (2 ViewModels), Controllers/CDPController.cs, Views/CDP (3 views)

---


## v3.2 Bug Hunting & Quality Audit (Shipped: 2026-03-05)

**Phases completed:** 7 phases (92-98, 99), 31 plans, 95 tasks

**Delivered:** Comprehensive audit of all portal sections — Homepage, CMP, CDP, Admin Portal, Account pages, Authentication/Authorization, and Data Integrity. Fixed 20+ bugs across UI, navigation, localization, authorization, soft-delete cascades, and audit logging.

**Key accomplishments:**
1. **Homepage Audit** — Fixed 5 bugs: deadline links, pluralization, localization, query consistency, negative days display
2. **CMP Section Audit** — Fixed 6 bugs: localization (Indonesian dates), validation errors, navigation flow
3. **CDP Section Audit** — Fixed 8 bugs: auth issues, navigation gaps, ProtonGuidance access, edge cases
4. **Admin Portal Audit** — Fixed 4 bugs: role gates, UI issues, ManageWorkers validation
5. **Account Pages Audit** — Profile/settings verified working, avatar display fixes
6. **Auth & Authorization Audit** — Verified login flow (local/AD), inactive user block, AccessDenied page, role-based navigation, return URL security
7. **Data Integrity Audit** — Fixed 7 bugs: 3 orphan leaks (parent.IsActive filters), 4 missing AuditLog calls
8. **CDP Cleanup** — Removed broken Deliverable card from CDP Index

**Bug Summary:**
- UI/Localization: 9 bugs fixed
- Navigation: 5 bugs fixed
- Authorization/Security: 4 bugs fixed
- Data Integrity: 7 bugs fixed
- Validation: 3 bugs fixed

**Files Modified:** 15+ controllers, 20+ views

---

## v3.1 CPDP Mapping File-Based Rewrite (Shipped: 2026-03-03)

**Phases completed:** 1 phase (88), 6 plans, 17 tasks

**Delivered:** Full rewrite of KKJ Matrix from fixed 15-column spreadsheet model to dynamic key-value relational model with document-based file management system.

**Key accomplishments:**
1. **Dynamic Schema** — KkjColumn and KkjTargetValue tables replace fixed columns; administrators can add/edit/delete competency columns dynamically
2. **Document-Based File Management** — KkjFile and CpdpFile models with upload/download/archive functionality; versioned file tracking with AuditLog
3. **File Management UI** — Silabus tab and Coaching Guidance tab with full file CRUD operations, archive status filtering, and role-based access control
4. **Migration** — Existing KKJ Matrix data migrated from fixed columns to key-value model

**Files Modified:** Models (KkjColumn, KkjTargetValue, KkjFile, CpdpFile), AdminController, ProtonDataController, Views

---

## v3.0 Full QA & Feature Completion (Shipped: 2026-03-05)

**Phases completed:** 10 phases (82-91, 86 superseded), 34 plans
**Timeline:** 2026-03-02 to 2026-03-05 (4 days)

**Delivered:** Comprehensive end-to-end QA of all portal features organized by use-case flows, code cleanup removing orphaned/duplicate pages, UI rename "Proton Progress" → "Coaching Proton" throughout portal, KKJ Matrix full rewrite to document-based file management, and PlanIDP 2-tab redesign. All major user flows verified working.

**Key accomplishments:**
1. Cleanup & Rename — "Proton Progress" renamed consistently, 3 orphaned CMP pages removed, AuditLog card added to Kelola Data hub
2. Master Data QA — All Kelola Data CRUD verified, Worker/Silabus soft delete infrastructure with IsActive filters fully implemented
3. Assessment Flow QA — DownloadQuestionTemplate action created, full assessment lifecycle verified across 10 requirements
4. Coaching Proton QA — Full coaching workflow verified with browser testing (8 requirements, all flows pass)
5. Dashboard & Navigation QA — SeedDashboardTestData action created, all dashboards show correct role-scoped data, login flow secure with inactive user block
6. KKJ Matrix Full Rewrite — Document-based file management system (KkjFile/CpdpFile) replacing spreadsheet editor, 3 plans complete
7. PlanIDP 2-Tab Redesign — Unified Silabus + Coaching Guidance tabs for all roles, 3 plans complete with read-only consumer view
8. Admin Assessment Pages Audit — ManageAssessment + AssessmentMonitoring all 11 flows verified, RegenerateToken multi-sibling fix, IsActive filters added
9. CMP Assessment Pages Audit — Assessment + Records pages verified, CSRF fixes applied, Records redesigned with 2-tab layout

**Known Gaps:**
- Phase 89 PlanIDP: No VERIFICATION.md file (5 requirements unverified: PLANIDP-01 through PLANIDP-05)
- ASSESS-04: Assessment Results competency display may be broken (PositionTargetHelper missing from codebase)
- Phase 88: KKJ Matrix verification claims don't match actual implementation (discrepancy between claimed relational model and actual file-based approach)

---

## v2.7 Assessment Monitoring (Shipped: 2026-03-01)

**Phases completed:** 3 phases (79-81), 4 plans
**Files modified:** 7 | **Insertions:** 697 | **Deletions:** 9
**Timeline:** 2026-03-01

**Delivered:** Dedicated Assessment Monitoring page extracted from ManageAssessment dropdown into a first-class Kelola Data hub entry with group list, per-participant detail, full HC action suite, and Admin ManageQuestions feature — plus hub cleanup removing redundant cards.

**Key accomplishments:**
1. Assessment Monitoring group list — Dedicated page at /Admin/AssessmentMonitoring with real-time stats (participant count, completed, passed, status badge), search/filter bar, and Regenerate Token per group
2. Per-participant monitoring detail — Drill-down view showing each participant's live progress, status, score, countdown timer; token card with copy and inline regenerate
3. Full HC action suite on monitoring page — Reset, Force Close, Bulk Close, Close Early, Regenerate Token all available from the dedicated monitoring detail page
4. Admin ManageQuestions — New Admin-context question management page (ManageQuestions GET, AddQuestion POST, DeleteQuestion POST) accessible from ManageAssessment dropdown
5. Hub cleanup — Monitoring dropdown removed from ManageAssessment (CLN-01), Training Records hub card removed from Kelola Data Section C (CLN-02), AssessmentMonitoring table full-height styling

---


## v1.0 CMP Assessment Completion (Shipped: 2026-02-17)

**Phases completed:** 3 phases, 10 plans, 6 tasks

**Key accomplishments:**
1. Assessment Results Workflow — Users can view their assessment results immediately after completion with score, pass/fail status, and conditional answer review (if enabled by HC)
2. HC Configuration Controls — HC staff can configure pass thresholds (0-100%) and toggle answer review visibility per assessment with category-based defaults
3. Reports Dashboard & Analytics — HC can view, filter, and analyze assessment results across all users with Chart.js visualizations showing pass rates by category and score distributions
4. Excel Export & User History — HC can export assessment data to Excel format and drill down into individual user assessment history with complete performance tracking
5. Auto-Competency Tracking — Assessment completion automatically updates user competency levels via AssessmentCompetencyMap with monotonic progression ensuring levels only increase
6. CPDP Integration & Gap Analysis — Full integration loop connecting assessments → KKJ competencies → CPDP framework → IDP suggestions with radar chart visualization and evidence-based tracking

---


## v1.1 CDP Coaching Management (Shipped: 2026-02-18)

**Phases completed:** 4 phases (4-7), 11 plans, plus Phase 8 post-fix

**Key accomplishments:**
1. Coaching Sessions — Coaches can log sessions with domain-specific fields (Kompetensi, SubKompetensi, Deliverable, CatatanCoach) and action items with due dates against a stable data model
2. Proton Deliverable Tracking — Structured Kompetensi hierarchy with sequential lock enforcing ordered progression; coaches upload and revise evidence files per deliverable
3. Approval Workflow & Completion — Full SrSpv → SectionHead approval chain with rejection reasons; HC final approval triggers Proton Assessment that auto-updates competency levels
4. Development Dashboard — Role-scoped monitoring for Spv/HC with team competency progress, deliverable status, pending approvals, and Chart.js trend charts
5. Admin Role Switcher Fix — Admin can simulate all 5 role views (HC, Atasan, Coach, Coachee, Admin) with correct access gates and scoped data per simulated role

---

## v1.2 UX Consolidation (Shipped: 2026-02-19)

**Phases completed:** 4 phases (9-12), 8 plans, 11 requirements shipped

**Key accomplishments:**
1. Gap Analysis Removed — CMP Index card, CPDP Progress cross-link, controller action, view, and ViewModel deleted atomically with zero dead routes remaining
2. Unified Training Records — Personal assessment sessions and manual training records merged into single chronological table with type-differentiated columns; HC worker list extended with combined completion rate
3. Assessment Page Role-Filtered — Workers see Open/Upcoming only at DB level; HC/Admin get restructured Management + Monitoring tab layout with callout directing workers to Training Records
4. CDP Dashboard Consolidated — CDPDashboardViewModel with three nullable role-branched sub-models; Proton Progress tab (all roles, role-scoped) and Assessment Analytics tab (HC/Admin only) replace three standalone pages
5. Standalone Pages Retired — DevDashboard and HC Reports pages fully deleted; Chart.js moved to _Layout.cshtml globally; universal Dashboard nav entry added for all authenticated roles

---


## v1.3 Assessment Management UX (Shipped: 2026-02-19)

**Phases completed:** 15 phases, 34 plans, 6 tasks

**Key accomplishments:**
- (none recorded)

---


## v1.6 Training Records Management (Shipped: 2026-02-20)

**Phases completed:** 20 phases, 47 plans, 6 tasks

**Key accomplishments:**
- (none recorded)

---


## v1.7 Assessment System Integrity (Shipped: 2026-02-21)

**Phases completed:** 6 phases (21-26), 14 plans
**Files modified:** 83 | **Insertions:** 17,854 | **Deletions:** 222
**Timeline:** 2026-02-20 → 2026-02-21

**Key accomplishments:**
1. Exam state tracking — Workers marked InProgress with timestamp on first exam load; idempotent guard prevents double-writes; visible as yellow badge in MonitoringDetail
2. Full exam lifecycle — Abandon flow (Keluar Ujian), HC force-close/reset, server-side timer enforcement (+2min grace), configurable exam window close dates with lockout
3. Package answer persistence & review — PackageUserResponse table; answer review works for package exams; token enforcement blocks direct URL bypass via TempData guard
4. HC audit log — All 7+ HC assessment management actions logged with actor NIP/name, timestamp; paginated read-only AuditLog page (HC/Admin only)
5. Worker UX — Riwayat Ujian history table on Assessment page; Kompetensi Diperoleh card on Results page showing earned competencies after passing
6. Data integrity safeguards — DeletePackage shows assignment count in confirm dialog with cascade cleanup; EditAssessment warns on schedule change when packages attached

---


## v1.9 Proton Catalog Management (Shipped: 2026-02-24)

**Phases completed:** 40 phases, 86 plans, 9 tasks

**Key accomplishments:**
- (none recorded)

---


## v2.0 Assessment Management & Training History (Shipped: 2026-02-24)

**Phases completed:** 40 phases, 86 plans, 9 tasks

**Key accomplishments:**
- (none recorded)

---


## v2.1 Assessment Resilience & Real-Time Monitoring (Shipped: 2026-02-25)

**Phases completed:** 5 phases (41-45), 13 plans
**Files modified:** 52 | **Insertions:** 12,184 | **Deletions:** 255
**Timeline:** 2026-02-24 → 2026-02-25

**Delivered:** Workers never lose exam progress (auto-save + session resume), HC can monitor live during assessments, and cross-package shuffle gives each worker a unique question mix from multiple packages.

**Key accomplishments:**
1. Auto-save — Worker answers saved per-click via AJAX with atomic upsert (ExecuteUpdateAsync + UNIQUE constraint); legacy exam path also covered via SaveLegacyAnswer
2. Session resume — ElapsedSeconds + LastActivePage persisted; workers resume from exact page with accurate remaining time; pre-populated answers on reconnect
3. Worker polling — 10s poll interval with IMemoryCache (5s TTL, ~99% DB load reduction); auto-redirects worker to Results when HC closes session early
4. Real-time monitoring — HC sees live progress (answered/total), status, score, time remaining per worker; 10s auto-refresh + 1s countdown; JS-rendered Reset/ForceClose action buttons
5. Cross-package per-position shuffle — Each question slot independently picks which package's question to show; even distribution across packages; import validation enforces equal counts; all 5 consumers (StartExam, SubmitExam, ExamSummary, Results, CloseEarly) updated

---


## v2.2 Attempt History (Shipped: 2026-02-26)

**Phases completed:** 1 phase (46), 2 plans, 4 tasks
**Files modified:** 15 | **Insertions:** 2,851 | **Deletions:** 82
**Timeline:** 2026-02-26

**Delivered:** HC and Admin can view a full chronological record of every assessment attempt per worker — including attempts previously erased by Reset — with sequential Attempt # numbering and dual Riwayat Assessment / Riwayat Training sub-tabs at /CMP/Records.

**Key accomplishments:**
1. AssessmentAttemptHistory model + EF Core migration — new SQL Server table preserving SessionId, Score, IsPassed, AttemptNumber, StartedAt, CompletedAt at archive time
2. Archive-before-clear in ResetAssessment — Completed sessions archived with AttemptNumber = existing row count + 1 before wipe; unstarted sessions produce no history row
3. Unified assessment history query — GetAllWorkersHistory() returns (assessment, training) tuple; batch GroupBy/ToDictionary computes Attempt # for current sessions without N+1
4. Riwayat Assessment + Riwayat Training dual sub-tabs — Bootstrap nested nav-tabs; client-side worker/NIP text + title dropdown filter with no round-trip

---


## v2.3 Admin Portal (Shipped: 2026-03-01)

**Phases completed:** 8 phases (47-53, 59), 29 plans
**Files modified:** 274 | **Insertions:** 82,601 | **Deletions:** 8,074
**Timeline:** 2026-02-26 → 2026-03-01 (4 days)

**Delivered:** Admin has full CRUD control over master data (KKJ Matrix, CPDP Items), operational records (Coach-Coachee Mapping, DeliverableProgress Override, Final Assessment), and assessment management — all consolidated under /Admin with role-gated access.

**Key accomplishments:**
1. Admin Portal infrastructure — AdminController with 12-card hub page, role-gated navigation, and class-level authorization
2. KKJ Matrix & CPDP Items managers — Spreadsheet-style inline editing with bulk-save, multi-cell clipboard, and Excel export for master data
3. Assessment Management migration — All manage actions (Create, Edit, Delete, Reset, Force Close, Export, Monitoring, History) moved from CMP to Admin with AuditLog
4. Coach-Coachee Mapping manager — Grouped-by-coach view with bulk assign, soft-delete, section filter, and Excel export
5. Proton Silabus & Coaching Guidance — Two-tab /Admin/ProtonData replacing ProtonCatalog with full silabus CRUD and guidance file management
6. DeliverableProgress Override — Third ProtonData tab for HC to override stuck statuses; sequential lock removed (all deliverables Active on assignment)
7. Final Assessment Manager — Assessment Proton exam category with eligibility-gated coachee picker, Tahun 3 interview workflow; legacy HCApprovals removed
8. ProtonCatalog cleanup — Redirect-only controller and views deleted after full migration to /Admin/ProtonData

### Known Gaps
- **OPER-05**: CoachingSession & ActionItem admin override — phase never planned
- **CRUD-01**: AssessmentQuestion inline edit — phase never planned
- **CRUD-02**: PackageQuestion edit/delete — REMOVED (Phase 56)
- **CRUD-03**: ProtonTrack edit/delete — REMOVED (covered by Phase 59 ProtonData migration)
- **CRUD-04**: Password Reset standalone — superseded by v2.5 Phase 67 ManageWorkers migration

---


## v2.4 CDP Progress (Shipped: 2026-03-01)

**Phases completed:** 4 phases (61-64), 9 plans
**Files modified:** 49 | **Insertions:** 20,101 | **Deletions:** 6,105
**Timeline:** 2026-02-27 → 2026-02-28

**Delivered:** CDP/Progress page rebuilt from scratch — data source corrected to ProtonDeliverableProgress, all filters wired to real queries with role-scoping, per-role approval workflow (SrSpv/SH/HC) with coaching report + evidence, Excel/PDF export via QuestPDF, and server-side group-boundary pagination with empty states.

**Key accomplishments:**
1. Data source fix — ProtonProgress action queries ProtonDeliverableProgress + ProtonTrackAssignment (not IdpItems), real coachee list from CoachCoacheeMapping, correct summary stats
2. Role-scoped filtering — 5 filter parameters (Bagian/Unit, Coachee, Track, Tahun) wired to EF Core Where composition with role-scope-first pattern; client-side search box
3. Per-role approval workflow — SrSpv/SectionHead/HC each have independent approval columns; per-role migration backfills from existing Approved records; rejection takes overall precedence
4. Coaching report + evidence — SubmitEvidenceWithCoaching combined modal; CoachingSession FK linked; Deliverable detail page shows coaching report
5. Export — Excel export via ClosedXML and PDF export via QuestPDF from ProtonProgress page
6. UI polish — Group-boundary server-side pagination (20 rows/page), 3 empty state scenarios, "Menampilkan X dari Y" counter

---


## v2.5 User Infrastructure & AD Readiness (Shipped: 2026-03-01)

**Phases completed:** 8 phases (65-72), 14 plans
**Files modified:** 41 | **Insertions:** 12,297 | **Deletions:** 1,055
**Timeline:** 2026-02-27 → 2026-02-28

**Delivered:** Full user system overhaul — dynamic profile/settings pages, ManageWorkers migrated to AdminController with HC access, Kelola Data hub reorganized, dual authentication (Active Directory + local) via IAuthService abstraction, hybrid auth with AD-first + local fallback for admin, and role structure additions (Supervisor level 5).

**Key accomplishments:**
1. Dynamic profile page — Profile bound to @model ApplicationUser; real user data (Nama, NIP, Email, Position, Section, Unit, Role); null-safe em dash fallback; avatar initials from FullName
2. Functional settings page — Change password via ChangePasswordAsync; edit FullName/Position; non-functional items (2FA, Notifications, Language) removed or disabled
3. ManageWorkers migration — 11 actions (CRUD, import, export, detail) moved from CMPController to AdminController with [Authorize(Roles = "Admin, HC")]; standalone navbar button removed; 5 view files copied and updated
4. Kelola Data hub — Admin/Index.cshtml restructured into 3 domain sections (Manajemen Pekerja, Kelola Assessment, Data Proton); stale "Segera" items cleaned up; HC nav access extended
5. LDAP auth infrastructure — IAuthService interface + LocalAuthService + LdapAuthService (DirectoryEntry LDAP bind); config toggle UseActiveDirectory; System.DirectoryServices NuGet
6. Dual auth login flow — AccountController.Login POST uses IAuthService; AD hint on login page; profile sync (FullName/Email only); unregistered users rejected with message
7. Hybrid auth — HybridAuthService wraps AD-first + local fallback for admin@pertamina.com; Supervisor role (level 5) added; SectionHead demoted to level 3
8. User structure polish — UserRoles.GetDefaultView() single source of truth; SeedData modernized; AuthSource field added then removed (global config routing replaces per-user)

---


## v2.6 Codebase Cleanup (Shipped: 2026-03-01)

**Phases completed:** 46 phases, 98 plans, 13 tasks

**Key accomplishments:**
- (none recorded)

---

