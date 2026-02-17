# Portal HC KPB - Pertamina HR Portal

**Project Type:** Brownfield Enhancement
**Created:** 2026-02-14
**Status:** Active Development

## Vision

Portal web untuk HC (Human Capital) dan Pekerja Pertamina yang mengelola dua platform utama:
- **CMP** (Competency Management Platform) - Assessment, skills tracking, competency matrix
- **CDP** (Competency Development Platform) - IDP, coaching, development plans

Platform ini menyediakan sistem komprehensif untuk tracking kompetensi, assessment online, dan pengembangan SDM Pertamina.

## Shipped Milestones

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

## Current State (v1.0)

**Tech Stack:**
- ASP.NET Core 8.0 MVC (C#)
- Entity Framework Core
- SQL Server / SQLite
- Razor Views (server-side rendering)
- ASP.NET Identity (authentication)

**Working Features:**

### Authentication & Authorization
- ✅ Login/logout system
- ✅ 6-level role hierarchy (Admin, HC, SectionHead, SrSpv, Spv, Coachee)
- ✅ Multi-view system (users can switch between Coachee/Atasan/HC perspectives)
- ✅ Section-based access control

### CMP Module (Competency Management)
- ✅ **Assessment Engine:**
  - Create assessments with multiple users assignment
  - Edit/delete assessments (HC/Admin only)
  - Assessment lobby (list, search, filter, pagination)
  - Token-based access control (optional per assessment)
  - Question management (add/delete questions with multiple choice options)
  - Take exam interface (StartExam view)
  - **Auto-scoring:** System calculates score automatically on submission
  - Submit exam and save responses to database
  - **Results page (v1.0):** Score display, pass/fail badge, conditional answer review
  - **Pass/Fail logic (v1.0):** Configurable pass thresholds per assessment (0-100%)
  - **Answer review (v1.0):** HC can toggle visibility of correct answers per assessment
  - Certificate view (after completion)

- ✅ **HC Reports Dashboard (v1.0):**
  - Summary statistics (total assigned, completed, pass rate, average score)
  - Multi-parameter filtering (category, date range, section, user search)
  - Paginated results table with drill-down to individual results
  - Excel export with ClosedXML (respects all filters)
  - Individual user assessment history
  - Chart.js analytics (pass rate by category, score distribution histogram)
  - Authorization: Admin/HC only

- ✅ **Competency Tracking (v1.0):**
  - **Auto-update on assessment completion:** UserCompetencyLevel created/updated when user passes assessment
  - **Assessment-Competency mapping:** AssessmentCompetencyMap links assessment categories to KKJ competencies
  - **Gap Analysis Dashboard:** Radar chart showing current vs target competency levels
  - **IDP Suggestions:** Automatic training recommendations based on CPDP framework
  - **CPDP Progress Tracking:** Assessment evidence displayed per CPDP competency
  - **Cross-navigation:** Seamless switching between gap analysis and CPDP progress views
  - **Position-based targets:** 15 position mappings with reflection-based target level resolution

- ✅ **KKJ Matrix:**
  - Skill matrix with target competency levels per position
  - Section-based filtering
  - Display skill groups, sub-groups, and competencies

- ✅ **CPDP Mapping:**
  - Competency framework (nama kompetensi, indikator perilaku, silabus)
  - Mapping view

- ✅ **Training Records:**
  - Personal training history
  - Status tracking (Passed, Valid, Pending, etc.)
  - Expiry tracking for certifications
  - Section-level worker list view

### CDP Module (Competency Development)
- ✅ IDP (Individual Development Plan) management
- ✅ Coaching logs
- ✅ Approval workflow (SrSpv → SectionHead → HC)
- ✅ Dashboard with IDP statistics

### Dashboard
- ✅ Role-based views
- ✅ Recent activities
- ✅ Upcoming deadlines
- ✅ Assessment tracking
- ✅ Training records with expiry alerts

### BP Module
- ⏸️ **NOT IN SCOPE** - Talent profiles, eligibility, point system (postponed)

## Problems Solved (v1.0)

### ✅ Priority 1: Complete CMP Assessment Flow — RESOLVED

**Solution Delivered (Phase 1):**
- ✅ Results page displays score, pass/fail status, and passing threshold immediately after submission
- ✅ Conditional answer review shows correct/incorrect answers per question (if HC enabled)
- ✅ HC can configure pass percentage (0-100%) with category-based defaults
- ✅ HC can toggle "Allow Answer Review" per assessment
- ✅ Assessment history accessible from lobby via "View Results" link
- ✅ Authorization enforced (owner/Admin/HC only)

**Impact:**
- Users receive immediate feedback on performance
- Learning outcomes improved through answer review
- HC can maintain appropriate standards per assessment type

### ✅ Priority 2: KKJ & CPDP Integration — RESOLVED

**Solution Delivered (Phase 3):**
- ✅ Auto-competency update: Assessment completion triggers UserCompetencyLevel creation/update
- ✅ Gap analysis dashboard with Chart.js radar chart (current vs target levels)
- ✅ IDP suggestions generated from CPDP framework based on competency gaps
- ✅ CPDP progress tracking shows assessment evidence per competency
- ✅ Full traceability: Assessments → KKJ → CPDP → IDP

**Impact:**
- Competency tracking now automated and evidence-based
- Users and HC can identify development needs at a glance
- Training recommendations driven by actual performance data

### ✅ Priority 3: HC Reports & Analytics — RESOLVED

**Solution Delivered (Phase 2):**
- ✅ Centralized reports dashboard with summary statistics
- ✅ Multi-parameter filtering (category, date range, section, user)
- ✅ Excel export with ClosedXML (respects all filters)
- ✅ Individual user assessment history with pass rate and average score
- ✅ Chart.js analytics (pass rate by category, score distribution)

**Impact:**
- HC can measure training effectiveness across the organization
- Data-driven decisions enabled through performance analytics
- Export capability supports external analysis and reporting

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

## Success Criteria (v1.0 — All Met ✅)

**Phase 1 — Assessment Results & Configuration:**
- ✅ Users can see their assessment results immediately after submission
- ✅ Results page shows: score, pass/fail status, answer review (if enabled)
- ✅ HC can configure pass threshold per assessment category
- ✅ HC can toggle answer review visibility per assessment
- ✅ Assessment history is viewable (past completed assessments with results)

**Phase 2 — HC Reports Dashboard:**
- ✅ HC reports dashboard shows all assessment results
- ✅ Can filter/search/export results to Excel
- ✅ Performance analytics visible (pass rates, averages, trends)
- ✅ Individual user assessment history accessible to HC

**Phase 3 — KKJ/CPDP Integration:**
- ✅ KKJ competency tracking per user (current level vs target)
- ✅ Gap analysis visualization
- ✅ Automatic IDP suggestions based on gaps
- ✅ CPDP progress tracking

**All 14 success criteria achieved in v1.0 milestone.**

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

## Notes

- Existing codebase has technical debt (hardcoded data, security gaps, N+1 queries)
- Should address critical issues as we build new features
- User doesn't know exact development stage - needs help understanding what's complete vs incomplete
- Focus on CMP first, CDP enhancements later
- Recent work: Assessment multi-user assignment, edit/delete functionality (Feb 2026)

## References

- Codebase analysis: `.planning/codebase/ARCHITECTURE.md`
- Known issues: `.planning/codebase/CONCERNS.md`
- Tech stack: `.planning/codebase/STACK.md`
- Milestone history: `.planning/MILESTONES.md`

---

*Last updated: 2026-02-17 after v1.0 milestone completion*
