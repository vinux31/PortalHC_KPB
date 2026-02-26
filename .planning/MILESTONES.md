# Milestones

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

