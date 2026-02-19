# Portal HC KPB - Pertamina HR Portal

**Project Type:** Brownfield Enhancement
**Created:** 2026-02-14
**Status:** Active Development

## Vision

Portal web untuk HC (Human Capital) dan Pekerja Pertamina yang mengelola dua platform utama:
- **CMP** (Competency Management Platform) - Assessment, skills tracking, competency matrix
- **CDP** (Competency Development Platform) - IDP, coaching, development plans

Platform ini menyediakan sistem komprehensif untuk tracking kompetensi, assessment online, dan pengembangan SDM Pertamina.

## Current Milestone: v1.4 Assessment Monitoring

**Goal:** Replace the passive monitoring tab with grouped progress view — completion rate, pass rate, and per-user expandable detail

**Target features:**
- Group monitoring by assessment (Title + Category + Schedule.Date)
- Each group shows completion count + pass rate with progress bar
- Expandable rows showing per-user: name, status, score, pass/fail
- Include recently closed assessments (last 30 days) alongside Open + Upcoming
- Sort by schedule date

## Shipped Milestones

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

## Current State (v1.3)

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
  - Take exam interface (StartExam view)
  - Auto-scoring, pass/fail with configurable thresholds, conditional answer review
  - Certificate view (after completion)
  - Training Records callout for workers to find their completion history

- ✅ **Assessment Management (HC/Admin — CMP):**
  - Dedicated Manage Assessments card on CMP Index (HC/Admin only; workers see Assessment Lobby only)
  - Manage view: grouped assessment cards (1 card per unique Title+Category+Date assessment, compact user list, group delete)
  - Bulk Assign: EditAssessment page shows currently-assigned users + section-filtered picker to add more; new AssessmentSessions created on save without altering existing ones

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
- Large monolithic controllers (CMPController = 1047 lines)

## Shipped Requirements

All requirements from v1.0–v1.3 are satisfied (7/9 in v1.3; QED-01 and QED-02 cancelled). See milestone archives for traceability:
- `milestones/v1.0-REQUIREMENTS.md` — 6 requirements (Phases 1-3)
- `milestones/v1.2-REQUIREMENTS.md` — 11 requirements (Phases 9-12, all ✅ Shipped)
- `milestones/v1.3-REQUIREMENTS.md` — 9 requirements (Phases 13-15; 7 shipped, 2 cancelled)

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

- Large monolithic controllers (CMPController ~1180 lines post-bulk-assign, CDPController 1000+ lines)
- No automated testing — manual QA only
- No audit logging — all changes untracked
- `GetPersonalTrainingRecords()` in CMPController is dead code (not called) — retained to avoid scope risk
- N+1 queries addressed in batch where identified (GetWorkersInSection batch GroupBy) but not systematically audited

## References

- Codebase analysis: `.planning/codebase/ARCHITECTURE.md`
- Known issues: `.planning/codebase/CONCERNS.md`
- Tech stack: `.planning/codebase/STACK.md`
- Milestone history: `.planning/MILESTONES.md`

---

*Last updated: 2026-02-19 after completing v1.3 milestone*
