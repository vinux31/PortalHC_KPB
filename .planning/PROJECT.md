# Portal HC KPB - Pertamina HR Portal

**Project Type:** Brownfield Enhancement
**Created:** 2026-02-14
**Status:** Active Development

## Vision

Portal web untuk HC (Human Capital) dan Pekerja Pertamina yang mengelola dua platform utama:
- **CMP** (Competency Management Platform) - Assessment, skills tracking, competency matrix
- **CDP** (Competency Development Platform) - IDP, coaching, development plans

Platform ini menyediakan sistem komprehensif untuk tracking kompetensi, assessment online, dan pengembangan SDM Pertamina.

## Current Milestone

**Status:** v1.6 shipped 2026-02-20. Planning next milestone.

## Current State (v1.6)

## Shipped Milestones

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

## Current State (v1.5)

**Tech Stack:**
- ASP.NET Core 8.0 MVC (C#)
- Entity Framework Core
- SQL Server / SQLite
- Razor Views (server-side rendering)
- ASP.NET Identity (authentication)
- Chart.js (loaded globally via _Layout.cshtml)
- ClosedXML (Excel export)

**Working Features:**

### Authentication & Authorization
- ✅ Login/logout system
- ✅ 6-level role hierarchy (Admin, HC, SectionHead, SrSpv, Spv, Coachee)
- ✅ Multi-view system — Admin can simulate all 5 role views (HC, Atasan, Coach, Coachee, Admin)
- ✅ Section-based access control

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
  - **Test Packages (v1.5):** HC creates packages per assessment; imports questions via Excel/paste; each user assigned random package with Fisher-Yates shuffled question + option order
  - **Package Management (v1.5):** ManagePackages page (create/delete), ImportPackageQuestions (Excel upload + paste), PREVIEW MODE for HC
  - **Grouped Monitoring (v1.4):** Monitoring tab shows one row per assessment group with completion bar + pass rate; "View Details" links to per-user detail page

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
- ✅ Proton deliverable tracking with sequential lock and evidence upload/revise
- ✅ Approval workflow (SrSpv → SectionHead → HC) with rejection reasons
- ✅ HC HCApprovals queue + final Proton Assessment creation

- ✅ **Unified CDP Dashboard (two tabs):**
  - Proton Progress tab: role-scoped team data (Coachee=self, Spv=unit, SrSpv/SectionHead=section, HC/Admin=all)
  - Assessment Analytics tab: HC/Admin only (absorbed from standalone HC Reports)
  - ~~DevDashboard~~ and ~~HC Reports~~ standalone pages retired

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
- No audit logging (all changes currently untracked)
- No automated testing (manual QA only)
- Large monolithic controllers (CMPController ~2300+ lines post-v1.6, CDPController 1000+ lines)

## Shipped Requirements

All requirements from v1.0–v1.6 are satisfied. See milestone archives for traceability:
- `milestones/v1.0-REQUIREMENTS.md` — 6 requirements (Phases 1-3)
- `milestones/v1.2-REQUIREMENTS.md` — 11 requirements (Phases 9-12, all ✅ Shipped)
- `milestones/v1.3-REQUIREMENTS.md` — 9 requirements (Phases 13-15; 7 shipped, 2 cancelled)
- `milestones/v1.6-REQUIREMENTS.md` — 4 requirements (TRN-01 through TRN-04, all ✅ Shipped)

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

- Large monolithic controllers (CMPController ~2300+ lines post-v1.6, CDPController 1000+ lines)
- No automated testing — manual QA only
- No audit logging — all changes untracked
- `GetPersonalTrainingRecords()` in CMPController is dead code (not called) — retained to avoid scope risk
- N+1 queries addressed in batch where identified but not systematically audited
- AllowAnswerReview silently non-functional for package-based exams — UserResponse rows not created (FK constraint); no PackageUserResponse table exists yet
- No UI to re-assign packages or manually override shuffle — edge case with no workaround
- Worker self-add training records deferred to v1.7+ (WTRN-01, WTRN-02)

## References

- Codebase analysis: `.planning/codebase/ARCHITECTURE.md`
- Known issues: `.planning/codebase/CONCERNS.md`
- Tech stack: `.planning/codebase/STACK.md`
- Milestone history: `.planning/MILESTONES.md`

---

*Last updated: 2026-02-20 after v1.6 milestone*
