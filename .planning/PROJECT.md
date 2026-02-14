# Portal HC KPB - Pertamina HR Portal

**Project Type:** Brownfield Enhancement
**Created:** 2026-02-14
**Status:** Active Development

## Vision

Portal web untuk HC (Human Capital) dan Pekerja Pertamina yang mengelola dua platform utama:
- **CMP** (Competency Management Platform) - Assessment, skills tracking, competency matrix
- **CDP** (Competency Development Platform) - IDP, coaching, development plans

Platform ini menyediakan sistem komprehensif untuk tracking kompetensi, assessment online, dan pengembangan SDM Pertamina.

## Current State

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
  - Certificate view (after completion)

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

## Problems to Solve

### Priority 1: Complete CMP Assessment Flow

**Current Gap:**
Users can take assessments and get auto-scored, but there's no results page. After submission, the system just redirects to the assessment lobby. Users can't see:
- Their score
- Pass/fail status
- Which questions they answered correctly/incorrectly
- Detailed feedback

HC staff also have no way to view and analyze assessment results across all users.

**User Pain Points:**
1. **Coachee/Worker:** Takes an assessment → Gets redirected to lobby → No idea what their score is or if they passed
2. **HC Staff:** Wants to see who passed/failed, export results, track performance → No reporting tools exist
3. **Manager (Atasan):** Wants to review team's assessment performance → No dashboard

**Business Impact:**
- Cannot measure training effectiveness
- Cannot validate competency achievement
- Cannot make data-driven decisions about development needs
- Poor user experience (users feel like their effort was wasted)

### Priority 2: KKJ & CPDP Integration

**Current Gap:**
- KKJ matrix shows target competency levels per position
- CPDP shows competency framework
- BUT: No way to track individual progress against these targets
- No gap analysis showing what competencies need development
- No connection between KKJ → Assessments → IDP

### Priority 3: HC Reports & Analytics

**Current Gap:**
- No centralized reporting dashboard
- Cannot export data to Excel/PDF
- No performance analytics or trends
- No answer history across assessments

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

## Success Criteria

**Phase 1 Complete when:**
1. Users can see their assessment results immediately after submission
2. Results page shows: score, pass/fail status, answer review (if enabled)
3. HC can configure pass threshold per assessment category
4. HC can toggle answer review visibility per assessment
5. Assessment history is viewable (past completed assessments with results)

**Phase 2 Complete when:**
1. HC reports dashboard shows all assessment results
2. Can filter/search/export results to Excel
3. Performance analytics visible (pass rates, averages, trends)
4. Individual user assessment history accessible to HC

**Phase 3 Complete when:**
1. KKJ competency tracking per user (current level vs target)
2. Gap analysis visualization
3. Automatic IDP suggestions based on gaps
4. CPDP progress tracking

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
