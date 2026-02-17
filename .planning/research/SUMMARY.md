# Project Research Summary

**Project:** Portal HC KPB v1.1 - CDP Coaching Management & Development Dashboard
**Domain:** HR Coaching Session Management integrated with existing Competency Management Platform
**Researched:** 2026-02-17
**Confidence:** HIGH

## Executive Summary

Portal HC KPB v1.1 extends the validated v1.0 competency assessment platform with CDP (Competency Development Plan) coaching session management and development dashboards. This research confirms that all new features can be built within the existing ASP.NET Core MVC stack without additional NuGet packages. The existing infrastructure (Entity Framework Core, Chart.js, ClosedXML, role-based authorization) provides all necessary capabilities for coaching CRUD operations, multi-level approval workflows, and progress visualization.

The recommended approach leverages proven patterns from the v1.0 CMP module: extend the existing CDPController rather than creating separate controllers, reuse the established 3-tier approval workflow (SrSpv → SectionHead → HC), and integrate Chart.js for development dashboards. Two lightweight client-side libraries (Flatpickr for date picking, TinyMCE for rich text editing) will be added via CDN to enhance UX without introducing dependency bloat.

Critical risks center on data integrity and security: the existing CoachingLog model has a broken foreign key relationship (TrackingItemId references a non-existent table), role-based access control vulnerabilities identified in the codebase could expose sensitive coaching feedback if replicated, and N+1 query performance issues already present in the dashboard must be prevented from scaling. All three risks can be mitigated in Phase 1 (Foundation) by fixing the schema, implementing resource-based authorization from day one, and using eager loading patterns with proper indexing.

## Key Findings

### Recommended Stack

**No new backend packages required.** All coaching features can be built with the existing validated stack (EF Core 8.0.24, ASP.NET Core MVC, SQL Server/SQLite, ASP.NET Identity). This minimizes risk, maintains consistency, and leverages infrastructure already proven in the CMP module.

**Core technologies:**
- **Entity Framework Core 8.0.24** (existing): CRUD operations, relationships, approval workflow tracking — already handling complex CMP assessment data successfully
- **Chart.js 4.5.1** (existing): Progress visualization (line charts for competency progression, donut charts for goal completion) — reuse existing integration from CMP radar charts
- **ClosedXML 0.105.0** (existing): Excel export for coaching session reports — extend existing HC reporting pattern
- **Flatpickr 4.6.x** (CDN): Date/time picker for session logging — lightweight, no dependencies, better UX than native HTML5 controls
- **TinyMCE 7** (CDN, free tier): Rich text editor for coaching notes and action items — familiar Word-like interface, simple integration, no commercial license needed for this use case

**Critical version note:** EF Core 8.0.24 support ends November 2026. Plan migration to EF Core 11 in Q4 2026 or early 2027.

### Expected Features

**Must have (table stakes) — v1.1:**
- **Log coaching sessions** — date, topic, notes, coach/coachee identification with rich text support
- **Document action items** — each session can have 0-N action items with description, due date, status tracking
- **Approval workflow for action items** — reuse IdpItem 3-tier approval pattern (SrSpv → SectionHead → HC)
- **Session history view** — chronological list with filtering by date range and status
- **Link sessions to competency gaps** — reference UserCompetencyLevel gaps from CMP assessments
- **Personal development dashboard** — single view: competency radar, recent assessments, coaching sessions, active action items
- **Supervisor team view** — aggregated team competency gaps, last coaching date, pending action items
- **Progress visualization** — line chart showing competency level changes over time using existing Chart.js
- **Export coaching reports** — Excel export for HC/SectionHead using existing ClosedXML pattern
- **CMP integration** — pre-fill competency gaps in session creation from assessment results

**Should have (competitive) — v1.2+:**
- **Calendar integration** — two-way sync with Google/Outlook (97% of users rate as important, but adds complexity)
- **Session templates** — pre-built frameworks for common coaching scenarios
- **Auto-suggest IDP actions** — AI-driven recommendations from competency gaps (leverages existing CPDP framework)
- **Development timeline** — visual roadmap of past/future development activities
- **Mobile-responsive design** — optimize dashboards for field workers (high value for Pertamina operations)

**Defer (v2+):**
- **Coaching effectiveness metrics** — correlation analysis between coaching frequency and competency improvement (requires statistical analysis capability)
- **Goal cascading visualization** — align individual goals to organizational objectives (requires org goal data not in system)
- **AI session note summary** — automatic summarization (requires LLM integration, significant stack change)

### Architecture Approach

**Extend existing ASP.NET Core MVC monolith** following established codebase patterns. Add new domain models (CoachingSession, ActionItem, CoachingApproval) to existing ApplicationDbContext, add actions to existing CDPController organized in sections, and reuse role-based authorization pattern with view-switching for Admin users. This maintains consistency with v1.0 architecture and avoids introducing complexity until proven necessary.

**Major components:**
1. **Data Models** — CoachingSession (coach/coachee FKs, session details, CMP competency link, approval status), ActionItem (child of CoachingSession, progress tracking), CoachingApproval (workflow tracking with approver chain)
2. **CDPController Extensions** — new action sections for session CRUD, action item management, approval workflow, development dashboard (keep all CDP features in one controller per existing pattern)
3. **ViewModels** — CoachingDashboardViewModel (aggregate metrics, chart data, team progress), SessionFormViewModel (complex forms with dropdowns and related entities)
4. **Integration Points** — CMP → CDP flow (create coaching plan from competency gap), CDP → CMP flow (update competency level after successful coaching), shared Chart.js visualization library

**Critical pattern: Resource-based authorization.** Every coaching action must validate coach-coachee relationship via CoachCoacheeMapping table, not just check user role. Prevents security vulnerability similar to existing BPController issue where URL parameter manipulation exposes cross-section data.

### Critical Pitfalls

1. **Orphaned Coaching Sessions After Role Changes** — Coach promoted or transferred leaves coaching relationships in limbo. **Avoid:** Add EndReason enum to CoachCoacheeMapping, implement relationship lifecycle management with auto-deactivation triggers, build "transfer coaching relationship" workflow from Phase 1.

2. **CoachingLog to IDP Schema Mismatch** — Existing CoachingLog.TrackingItemId references non-existent table, should link to IdpItem.Id. **Avoid:** Fix schema immediately in Phase 1 migration (TrackingItemId → IdpItemId with proper foreign key), add referential integrity constraints, validate IDP ownership before creating coaching logs.

3. **Approval Workflow State Violations** — String-based status fields allow impossible transitions (HC approval before SrSpv review). **Avoid:** Implement status enums with state machine validator from Phase 1, add timestamp audit fields for approval sequence, database check constraints to enforce hierarchy.

4. **N+1 Query Explosion in Dashboards** — Loading coachee list then querying coaching logs/IDP items individually creates 250+ queries for 50-employee section. **Avoid:** Use `.Include()` eager loading, database aggregation with GROUP BY, projection to summary DTOs, composite indexes on CoachId+Status+Tanggal.

5. **Coaching Data Privacy Without Access Control Audit** — Sensitive performance feedback exposed via URL parameter manipulation. **Avoid:** Implement resource-based authorization handlers validating coach-coachee mapping, log all coaching data access to audit table, filter queries by authorization before loading data (never load then filter).

6. **Coaching Status Out of Sync with IDP Progress** — Coaching marked "Mandiri" but IDP still "Pending" creates conflicting sources of truth. **Avoid:** Define status synchronization rules in Phase 2, implement coordination logic when coaching conclusion affects IDP status, display warnings for status conflicts in UI.

7. **Empty Coaching Sessions Marked Submitted** — Missing server-side validation allows blank forms to be saved with Status="Submitted". **Avoid:** Add Required/MinLength validation attributes to model, validate ModelState before save, add database check constraints for content quality enforcement.

## Implications for Roadmap

Based on research, suggested phase structure prioritizes foundation (data integrity + security) before features to prevent technical debt:

### Phase 1: Foundation — Data Models & Authorization
**Rationale:** Fix existing schema issues and security gaps before building new features. The CoachingLog → IdpItem foreign key mismatch and authorization vulnerabilities must be resolved at the data layer before any UI is built. Building on broken foundations creates unfixable data integrity issues.

**Delivers:**
- Fixed schema: CoachingSession, ActionItem, CoachingApproval models with proper foreign keys to IdpItem
- Database migration with indexes on foreign keys and status fields
- Resource-based authorization handlers for coaching data access
- Approval workflow state machine with enum-based validation
- CoachCoacheeMapping lifecycle management (EndReason, auto-deactivation)

**Addresses:**
- Pitfall #1 (Orphaned Sessions), #2 (Schema Mismatch), #3 (Approval Violations), #5 (Privacy), #7 (Validation)
- Foundation for all table stakes features from FEATURES.md

**Avoids:** Building UI on broken foreign keys, inheriting BPController security vulnerabilities, allowing invalid approval states in database

### Phase 2: Core Coaching CRUD
**Rationale:** With solid foundation in place, implement basic coaching session management. This delivers immediate user value (coaches can log sessions and document action items) while proving the data model and authorization patterns work correctly.

**Delivers:**
- CDPController extensions: Sessions(), SessionDetails(), CreateSession(), EditSession()
- SessionFormViewModel with coachee selection and competency gap pre-fill
- Razor views: Sessions.cshtml, SessionDetails.cshtml, CreateSession.cshtml
- Action item creation and tracking within coaching sessions
- Rich text editor (TinyMCE) and date picker (Flatpickr) integration

**Uses:**
- Existing EF Core patterns for CRUD operations
- Existing role-based filtering from HomeController/BPController (but with fixed authorization)
- Flatpickr (CDN) for date/time picking
- TinyMCE (CDN) for coaching notes rich text

**Implements:**
- SessionFormViewModel pattern for complex forms
- Eager loading with `.Include()` to prevent N+1 queries
- Audit timestamps (CreatedAt, UpdatedAt) on all coaching records

**Addresses:**
- Table stakes: Log coaching sessions, Document action items, Session history view, Link to competency gaps
- Pitfall #4 (N+1 queries) — prevented via eager loading from start

### Phase 3: Approval Workflow
**Rationale:** With session CRUD validated, add governance layer. The 3-tier approval workflow is critical for Pertamina compliance and already proven in IdpItem implementation. Reusing this pattern reduces risk.

**Delivers:**
- SubmitForApproval, ApproveSession, RejectSession controller actions
- CoachingApproval workflow tracking with approver chain
- Pending approvals queue for Section Head/HC dashboards
- Status transition validation using state machine from Phase 1
- Approval timeline display in session detail view

**Addresses:**
- Table stakes: Approval workflow for action items
- Pitfall #3 (Approval violations) — enforced via state machine validator

**Implements:**
- SessionWorkflow helper class for state transition validation
- Approval timestamp audit trail (ApprovedSrSpvAt, ApprovedSectionHeadAt, ApprovedHCAt)
- Approval status filtering for role-based views

### Phase 4: Development Dashboard
**Rationale:** With coaching data being captured and approved, surface insights via dashboards. This phase delivers the "competency progress over time" and "team overview" features that differentiate CDP from simple coaching logs.

**Delivers:**
- CoachingDashboardViewModel with aggregate metrics
- DevelopmentDashboard.cshtml with Chart.js visualizations
- Progress over time line chart (competency level progression)
- Goal completion donut chart (action item status breakdown)
- Team overview bar chart (supervisor view of team competency gaps)
- Summary statistics: sessions count, action item completion rate, pending approvals

**Uses:**
- Chart.js (existing) — reuse patterns from CMP CompetencyGap radar charts
- Database aggregation queries (COUNT, SUM, GROUP BY) for metrics
- Response caching (5-minute expiration) to prevent dashboard query overload

**Addresses:**
- Table stakes: Personal development dashboard, Supervisor team view, Progress visualization
- Pitfall #4 (N+1 queries) — use projection to summary DTOs, indexed queries

**Implements:**
- CoachingDashboardViewModel with chart data arrays
- Database view for complex dashboard queries (if performance testing shows need)
- Pagination for coaching history (20 records per page)

### Phase 5: CMP Integration & Reports
**Rationale:** Connect coaching back to assessment system, closing the CMP → CDP → CMP feedback loop. This phase makes coaching actionable by linking gaps identified in assessments to coaching plans and allowing coaching outcomes to update competency levels.

**Delivers:**
- "Create Coaching Plan" button in CMP AssessmentResults view (links to CDP CreateSession)
- Pre-fill coaching session from competency gap query parameter
- Competency level update modal in SessionDetails (HC can update UserCompetencyLevel after successful coaching)
- Excel export for coaching session reports using ClosedXML
- Status synchronization logic (coaching "Mandiri" conclusion suggests IDP "Completed" status)

**Addresses:**
- Table stakes: CMP integration, Export coaching reports
- Pitfall #6 (Status sync) — implement coordination logic with UI warnings for conflicts

**Implements:**
- CMP → CDP flow: CreateSession?gap={KkjMatrixItemId} with pre-filled objectives
- CDP → CMP flow: Update UserCompetencyLevel.CurrentLevel from coaching conclusion
- Excel export extending existing ClosedXML pattern from v1.0
- Status provenance display: show why status was set (coaching vs. IDP vs. HC override)

### Phase 6: UX Enhancements (Optional for v1.1)
**Rationale:** After core functionality validated, improve user experience based on feedback. These features are valuable but not essential for launch. Can be deferred to v1.2 if timeline is constrained.

**Delivers:**
- Session templates for common coaching scenarios (performance issue, skill development, career planning)
- Auto-suggest IDP actions from competency gaps (leverage existing CPDP framework)
- Development timeline visualization (Gantt-style roadmap of development journey)
- Mobile-responsive design optimizations for dashboards
- Peer comparison (anonymized section/position averages)

**Addresses:**
- Competitive features from FEATURES.md (session templates, auto-suggest, timeline)
- v1.2+ features that enhance but don't block core workflows

### Phase Ordering Rationale

- **Phase 1 before 2:** Cannot build coaching CRUD on broken foreign keys and insecure authorization. Schema and security must be fixed at foundation level.
- **Phase 2 before 3:** Approval workflow needs coaching sessions to exist. Prove basic CRUD works before adding governance complexity.
- **Phase 3 before 4:** Dashboard needs approved coaching data to display meaningful metrics. Approval status is key dimension in dashboards.
- **Phase 4 before 5:** Dashboard validates that coaching data is being captured correctly before integrating with CMP. Easier to debug dashboard issues when data flow is one-way.
- **Phase 5 after 4:** CMP integration creates bidirectional data flow. Requires both systems (CMP assessments and CDP coaching) to be stable first.
- **Phase 6 optional:** UX enhancements add polish but don't change data model or business logic. Safe to defer without blocking other phases.

**Dependencies:**
```
Phase 1 (Foundation)
    ↓
Phase 2 (CRUD) ← [Required for all later phases]
    ↓
Phase 3 (Approval) ← [Can parallel with Phase 4 if resources allow]
    ↓
Phase 4 (Dashboard)
    ↓
Phase 5 (CMP Integration)
    ↓
Phase 6 (UX) ← [Optional for v1.1]
```

### Research Flags

**Phases needing deeper research during planning:**
- **Phase 5 (CMP Integration):** Status synchronization logic requires business rule validation with HC stakeholders. Research identified multiple possible sync strategies (auto-update vs. suggestion vs. manual override) — need user input on preferred workflow.
- **Phase 6 (Auto-suggest IDP actions):** If included, requires research into existing CPDP framework mapping logic. Research confirmed pattern exists in v1.0 but implementation details need codebase review.

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Foundation):** EF Core migrations, authorization handlers, state machine validation — all well-documented ASP.NET Core patterns
- **Phase 2 (CRUD):** Standard MVC controller actions with ViewModels — established patterns in existing codebase
- **Phase 3 (Approval):** Reuses existing IdpItem approval workflow pattern — already proven in v1.0
- **Phase 4 (Dashboard):** Chart.js integration already validated in CMP module — extend existing pattern
- **Phase 5 (Excel Export):** ClosedXML pattern already implemented in v1.0 — copy and adapt

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | **HIGH** | No new packages needed. Existing stack (EF Core, Chart.js, ClosedXML) validated in v1.0. Client libraries (Flatpickr, TinyMCE) are industry standard with extensive documentation. |
| Features | **HIGH** | Table stakes features verified against industry best practices (UC Berkeley HR, HR Certification, GetApp coaching software analysis). Feature priorities validated by v1.0 existing CoachingLog and IdpItem models showing original intent. |
| Architecture | **HIGH** | ASP.NET Core MVC patterns from official Microsoft docs. Existing codebase analysis (ARCHITECTURE.md, CONCERNS.md) provides concrete examples of current patterns to extend/fix. Resource-based authorization pattern well-documented. |
| Pitfalls | **HIGH** | Critical pitfalls (schema mismatch, security gaps, N+1 queries) identified via codebase analysis in CONCERNS.md. Solutions validated against official ASP.NET Core security docs and EF Core performance guidance. GDPR compliance requirements from official sources. |

**Overall confidence:** HIGH

Research is based on official Microsoft documentation, verified industry best practices, and direct codebase analysis of Portal HC KPB v1.0. The existing system provides concrete implementation patterns to follow (CMP module) and anti-patterns to avoid (BPController security issues, HomeController N+1 queries).

### Gaps to Address

**Business rule validation needed during Phase 5 planning:**
- **Coaching-IDP status synchronization rules:** Research identified the problem (status divergence) and possible solutions (auto-update, suggestion, manual override), but which approach fits Pertamina workflow requires stakeholder input. Recommendation: Start with "suggestion + warning UI" in v1.1, add automation in v1.2 after user feedback.

**Technical investigation needed before Phase 1 implementation:**
- **CoachingLog legacy data migration:** Existing CoachingLogs table may contain production data. Research recommends keeping both tables initially and migrating in separate phase, but production database must be inspected to determine actual migration complexity. If CoachingLogs is empty (which is likely given v1.0 just launched), safe to deprecate immediately.

**Validation needed during Phase 6 (if included in v1.1):**
- **Mobile responsiveness scope:** Research confirms mobile optimization is valuable for Pertamina field operations, but existing Razor views' mobile compatibility unknown. Requires device testing to determine if full responsive redesign needed or just CSS tweaks. Recommendation: Test current coaching views on mobile in Phase 2 alpha, decide Phase 6 scope based on actual pain points.

**Indonesian HR compliance (MEDIUM priority gap):**
- Research focused on international HR best practices (US/UK sources). Specific Indonesian labor law requirements for coaching documentation not verified. Pertamina may have internal governance policies beyond general HR standards. Recommendation: Consult with Pertamina HC staff during Phase 1 to validate documentation requirements (retention periods, audit requirements, employee access rights, data privacy under Indonesian law).

## Sources

### Primary (HIGH confidence)

**Official Documentation:**
- [Entity Framework Core Releases](https://learn.microsoft.com/en-us/ef/core/what-is-new/) — EF Core 8.0.24 version and support timeline
- [ASP.NET Core Security Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0) — Resource-based authorization patterns
- [Chart.js Official Documentation](https://www.chartjs.org/docs/latest/) — Verified v4.5.1 as latest stable
- [Flatpickr Official Site](https://flatpickr.js.org/) — Date picker integration
- [TinyMCE Documentation](https://www.tiny.cloud/docs/) — Rich text editor setup

**Codebase Analysis:**
- Portal HC KPB v1.0 codebase (ARCHITECTURE.md, CONCERNS.md) — Existing patterns, identified security/performance issues
- ApplicationDbContext relationships — Current data model and cascade behaviors
- IdpItem approval workflow — Proven 3-tier approval pattern to reuse
- HomeController/BPController — Anti-patterns to avoid (N+1 queries, authorization gaps)

### Secondary (MEDIUM confidence)

**Industry Best Practices:**
- [UC Berkeley HR: Coaching as Effective Feedback Tool](https://hr.berkeley.edu/hr-network/central-guide-managing-hr/managing-hr/managing-successfully/performance-management/check-in/coaching) — Coaching session documentation standards
- [HR Certification: HR Documentation Best Practices](https://hrcertification.com/blog/hr-documentation-best-practices-biid1000103) — Compliance requirements for coaching records
- [GetApp Coaching Software Analysis](https://www.getapp.com/hr-employee-management-software/coaching/f/session-notes/) — User expectations (97% rate calendar sync, task management as important)
- [AIHR: Individual Development Plan Examples](https://www.aihr.com/blog/individual-development-plan-examples/) — IDP-coaching integration patterns
- [TalentGuard: Competency Management Systems](https://www.talentguard.com/competency-management-system) — Competency visualization standards (radar charts, skill matrices)

**HR Workflow Automation:**
- [Aelum: HR Workflow Automation Guide](https://aelumconsulting.com/blogs/hr-workflow-automation-chro-guide/) — Multi-level approval workflows in HR systems
- [iMocha: Skills Tracking Software 2026](https://www.imocha.io/blog/skills-tracking-software) — Development dashboard feature expectations

**GDPR Compliance:**
- [CIPHR: GDPR Employee Data Retention](https://www.ciphr.com/blog/gdpr-employee-data-retention-what-hr-needs-to-know) — 7-year retention requirement for development records
- [Redactable: GDPR for HR](https://www.redactable.com/blog/gdpr-for-human-resources-what-to-know-for-employee-data) — Article 88 special protection for employee performance data

### Tertiary (LOW confidence — informative only)

**Coaching Trends:**
- [Thrive Partners: Coaching in 2026](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/) — AI-enabled platforms, group coaching trends (informative but not prescriptive for v1.1)
- [GrowthSpace Coaching Model](https://www.aihr.com/blog/coaching-plan-template/) — AI analytics + 1-on-1 coaching (v2+ feature, not v1.1)

---

**Research completed:** 2026-02-17
**Ready for roadmap:** Yes
**Recommended next step:** Create roadmap based on suggested 6-phase structure, validating phase scope with stakeholders during requirements definition.
