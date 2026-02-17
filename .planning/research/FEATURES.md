# Feature Research: CDP Coaching Management

**Domain:** HR Coaching Session Management & Development Dashboard
**Researched:** 2026-02-17
**Confidence:** HIGH

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Log coaching sessions** | Core functionality - supervisors must record 1-on-1 meetings with date, topic, and notes | LOW | Industry standard per [UC Berkeley HR](https://hr.berkeley.edu/hr-network/central-guide-managing-hr/managing-hr/managing-successfully/performance-management/check-in/coaching). Already has CoachingLog model with TrackingItem relation. |
| **Document session notes** | HR compliance requires factual, objective session documentation | MEDIUM | Must follow best practices: objective/factual language, concise summaries, essential elements documented per [HR Certification](https://hrcertification.com/blog/hr-documentation-best-practices-biid1000103). |
| **Track action items from sessions** | Every coaching session produces actionable follow-ups that need tracking | MEDIUM | 97% of users rate task management as important per [GetApp coaching software analysis](https://www.getapp.com/hr-employee-management-software/coaching/f/session-notes/). AI-powered action item extraction becoming standard in 2026. |
| **Schedule/calendar integration** | Users expect two-way calendar sync to avoid double-booking | MEDIUM | 97% of reviewers rated calendar sync as important per [GetApp](https://www.getapp.com/hr-employee-management-software/coaching/f/session-notes/). Essential for supervisor workflow. |
| **Session history view** | Both coach and coachee need access to past sessions for continuity | LOW | Standard chronological list with filtering. Part of coaching continuity best practices. |
| **Approval workflow for action items** | Corporate hierarchy requires multi-level approval (SrSpv → SectionHead → HC) | HIGH | Already implemented in IdpItem model (ApproveSrSpv, ApproveSectionHead, ApproveHC). Must extend to coaching action items. Critical for Pertamina governance. |
| **Competency progress visualization** | Users expect to see how competency levels change over time | HIGH | Radar charts and skill matrices are standard per [TalentGuard](https://www.talentguard.com/competency-management-system). Already using Chart.js in v1.0 (CompetencyGap radar). |
| **Development dashboard (personal)** | Coachees need a single view showing their goals, progress, gaps, and recent activity | MEDIUM | Employee dashboards showing skills, gaps, and learning resources are table stakes per [iMocha](https://www.imocha.io/blog/skills-tracking-software). |
| **Supervisor team view** | Supervisors need aggregated view of their team's development status | MEDIUM | Skills matrix dashboards filtered by team/department/role are standard per [Weever](https://weeverapps.com/reporting-dashboard/training-dashboards/skills-matrix-report/). |
| **Progress tracking with metrics** | Both parties expect quantifiable progress indicators (completion %, competency levels, assessment scores) | LOW | Industry standard: track performance metrics, skill assessments, retention/promotion rates per [AIHR IDP guide](https://www.aihr.com/blog/individual-development-plan-examples/). |
| **Link sessions to competency gaps** | Sessions should reference which competency gaps are being addressed | MEDIUM | Depends on existing AssessmentCompetencyMap and UserCompetencyLevel from v1.0. Integration pattern already established. |
| **Export/reporting capability** | HC needs to generate reports on coaching activity and development trends | MEDIUM | Excel export already implemented in v1.0 (ClosedXML). Extend to coaching sessions. Standard HR workflow automation per [Aelum](https://aelumconsulting.com/blogs/hr-workflow-automation-chro-guide/). |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Auto-suggest IDP actions from gaps** | AI-driven recommendations based on competency gap analysis save time and improve development quality | MEDIUM | v1.0 already has IDP suggestions from CPDP framework. Extend to coaching context. GrowthSpace model: AI analytics + 1-on-1 coaching per [AIHR](https://www.aihr.com/blog/coaching-plan-template/). |
| **Coaching effectiveness metrics** | Show correlation between coaching frequency and competency improvement (e.g., "5 sessions → +2 competency levels") | HIGH | Advanced analytics: coaching session count vs competency level progression. Differentiator for data-driven HC teams. |
| **Session templates/frameworks** | Pre-built templates for common coaching scenarios (performance issue, skill development, career planning) | LOW | Structured pathways while allowing personalization per [Thrive Partners 2026 trends](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/). Templates standardize quality. |
| **Cross-functional competency tracking** | Track both technical (KKJ) and behavioral (CPDP) competencies in unified view | MEDIUM | Portal HC KPB advantage: already has KKJ + CPDP integration. Leverage existing dual-framework approach. |
| **Goal cascading visualization** | Show how individual development goals align with section/unit/directorate objectives | HIGH | Strategic alignment visualization. Requires organizational goal data (not currently in system). High value for HC reporting. |
| **Development timeline/roadmap** | Visual timeline showing past assessments, coaching sessions, milestones, and future goals | MEDIUM | Gantt-style view of development journey. Helps coachees see long-term progress and plan ahead. |
| **Peer comparison (anonymized)** | Show how user's competency levels compare to section/position averages | MEDIUM | Motivational when anonymized. Requires aggregate queries on UserCompetencyLevel. Privacy-sensitive - needs careful UX. |
| **Coaching notes AI summary** | Automatically summarize key points from session notes using AI | HIGH | 2026 trend: AI-enabled platforms with automatic note-taking per [Thrive Partners](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/). Requires LLM integration (out of current tech stack). |
| **Mobile-friendly responsive design** | Coaching logs and dashboards accessible on mobile devices for field workers | MEDIUM | Portal HC KPB is web-only but Razor views can be made responsive. High value for Pertamina field operations. |
| **Group coaching session support** | Log group coaching sessions with multiple participants | LOW | 2026 trend: group coaching gaining traction per [Thrive Partners](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/). Shared learning and accountability without sacrificing depth. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Real-time collaboration** | Teams want simultaneous editing of coaching notes | Adds significant technical complexity (WebSockets, conflict resolution) for minimal value. Coaching notes are typically private supervisor-coachee documents. | Use simple edit timestamps and "last updated by" indicator. Coaching is inherently asynchronous. |
| **Unlimited custom fields** | Users want to track "everything" about coaching sessions | Creates data sprawl, inconsistent reporting, and poor UX. HC can't analyze data when every team uses different fields. | Provide 2-3 optional custom text fields maximum. Enforce standard structure for reportable data. |
| **Gamification (points/badges)** | Make development "fun" and "engaging" | Development is serious professional growth. Gamification can trivialize competency gaps and create wrong incentives (gaming the system vs actual skill improvement). | Use clear progress metrics and competency level advancement. Intrinsic motivation > extrinsic rewards. |
| **Public coaching notes** | "Transparency" advocates want all notes visible to team | Violates psychological safety of coaching relationship. Supervisors won't document honestly if notes are public. HR compliance requires confidentiality per [HR Acuity](https://www.hracuity.com/blog/workplace-documentation-best-practices/). | Keep notes private to coach/coachee/HC. Share action items and outcomes (not process notes). |
| **Automatic scheduling** | AI automatically books coaching sessions | Removes human agency and can create resentment. Coaching timing matters - auto-scheduling can't judge readiness or urgency. Calendar integration already handles 97% of value. | Provide scheduling suggestions based on past patterns. Let supervisor/coachee agree on timing. |
| **Video call integration** | Embed Zoom/Teams directly in portal | Pertamina likely already has corporate video solution. Duplicating video infrastructure adds complexity and cost. Single sign-on integration is fragile. | Use calendar integration. Let video platform link appear in session notes. Focus on session documentation, not hosting. |
| **Blockchain-verified credentials** | "Immutable" record of coaching sessions | Massive over-engineering. No HR compliance need for blockchain. Traditional audit logging sufficient for legal defensibility. | Use standard database audit fields (CreatedAt, UpdatedAt, CreatedBy). Rely on SQL Server transaction logs. |
| **Anonymous coaching feedback** | Coachees rate supervisors anonymously | Damages trust in coaching relationship. Coaching is developmental (not evaluative). Anonymous feedback creates defensiveness vs growth mindset. | Use upward feedback in separate performance management process. Keep coaching developmental and non-evaluative. |

## Feature Dependencies

```
[Coaching Session Logging]
    └──requires──> [ApplicationUser] (existing)
    └──requires──> [CoachingLog model] (existing, needs extension)

[Action Items from Sessions]
    └──requires──> [Coaching Session Logging]
    └──requires──> [Approval Workflow] (existing in IdpItem)

[Development Dashboard (Personal)]
    └──requires──> [UserCompetencyLevel] (existing from v1.0)
    └──requires──> [AssessmentSession history] (existing from v1.0)
    └──requires──> [Coaching Session history]
    └──requires──> [Action Items tracking]

[Development Dashboard (Supervisor)]
    └──requires──> [Development Dashboard (Personal)]
    └──requires──> [Section filtering] (existing in User model)
    └──enhances──> [Team competency visualization]

[Session Notes Documentation]
    └──requires──> [Coaching Session Logging]
    └──enhances──> [HR Compliance] (best practices)

[Calendar Integration]
    └──requires──> [Coaching Session Logging]
    └──optional──> External calendar API (Google/Outlook)

[Link Sessions to Competency Gaps]
    └──requires──> [Coaching Session Logging]
    └──requires──> [UserCompetencyLevel] (existing from v1.0)
    └──requires──> [AssessmentCompetencyMap] (existing from v1.0)

[Progress Visualization]
    └──requires──> [UserCompetencyLevel history]
    └──requires──> [AssessmentSession completion dates] (existing)
    └──requires──> [Chart.js] (already integrated in v1.0)

[Export Coaching Reports]
    └──requires──> [Coaching Session Logging]
    └──requires──> [ClosedXML] (already integrated in v1.0)

[Auto-suggest IDP actions]
    └──requires──> [UserCompetencyLevel] (gaps)
    └──requires──> [CPDP mapping] (existing from v1.0)
    └──enhances──> [Action Items from Sessions]

[Coaching Effectiveness Metrics]
    └──requires──> [Coaching Session history]
    └──requires──> [UserCompetencyLevel history]
    └──requires──> Advanced analytics queries
```

### Dependency Notes

- **CoachingLog model exists but is incomplete:** Current model links to TrackingItem (undefined relationship). Needs action items collection, better status tracking, and competency gap linkage.
- **Approval workflow proven pattern:** IdpItem already implements 3-level approval (SrSpv → SectionHead → HC). Reuse for coaching action items.
- **Chart.js already integrated:** v1.0 uses Chart.js for radar charts (CompetencyGap) and analytics (ReportsIndex). Extend for progress timelines.
- **ClosedXML already integrated:** v1.0 uses ClosedXML for Excel export. Extend to coaching session reports.
- **CMP integration established:** v1.0 built full integration loop (Assessments → KKJ → CPDP → IDP). CDP coaching should leverage this infrastructure.

## MVP Definition

### Launch With (v1.1)

Minimum viable product for coaching session management.

- [x] **Log coaching sessions** — Date, topic, notes, coach/coachee identification. Core CoachingLog model enhancement.
- [x] **Document action items** — Each session can have 0-N action items with description, due date, status.
- [x] **Approval workflow for action items** — Reuse IdpItem approval pattern (SrSpv → SectionHead → HC).
- [x] **Session history view** — Chronological list of sessions for coach and coachee. Filter by date range, status.
- [x] **Link sessions to competency gaps** — Reference UserCompetencyLevel gaps when creating session. Shows which competencies session addresses.
- [x] **Personal development dashboard** — Single view showing: current competency levels (radar), recent assessments, coaching sessions, active action items, IDP status.
- [x] **Supervisor team view** — Aggregated view: list of team members, their competency gaps, last coaching session date, pending action items count.
- [x] **Progress visualization** — Line chart showing competency level changes over time. Use existing Chart.js integration.
- [x] **Export coaching reports** — Excel export for coaching sessions (HC/SectionHead). Use existing ClosedXML pattern.
- [x] **Integration with CMP gap data** — Pre-fill competency gaps in coaching session creation. Show assessment results that created gaps.

**Rationale:** These features deliver core value (coaching documentation + development tracking) while leveraging v1.0 infrastructure (Chart.js, ClosedXML, approval workflow, CMP integration). Focused on HC workflow compliance and supervisor-coachee collaboration.

### Add After Validation (v1.2+)

Features to add once core is working and users provide feedback.

- [ ] **Calendar integration** — Two-way sync with Google/Outlook calendars. Requires external API integration (complexity).
- [ ] **Session templates** — Pre-built templates for common coaching scenarios. Wait to see what patterns emerge organically.
- [ ] **Auto-suggest IDP actions** — AI-driven recommendations from gaps. Validate that manual action item creation works first.
- [ ] **Development timeline** — Visual roadmap of past/future development activities. Nice visualization but not critical for v1.1.
- [ ] **Mobile-responsive design** — Optimize dashboards for mobile. Current Razor views work but aren't optimized. Add if field users request.
- [ ] **Peer comparison (anonymized)** — Compare competency levels to section averages. Wait for sufficient data volume.
- [ ] **Group coaching support** — Log sessions with multiple participants. Validate demand first (may not be Pertamina pattern).

**Trigger for adding:** User feedback indicating demand, sufficient data volume (for comparisons/timelines), or HC requesting advanced analytics.

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Coaching effectiveness metrics** — Correlation analysis between coaching frequency and competency improvement. Requires statistical analysis capability and long-term data.
- [ ] **Goal cascading visualization** — Align individual goals to organizational objectives. Requires organizational goal data not currently in system.
- [ ] **AI session note summary** — Automatically summarize session notes. Requires LLM integration (significant tech stack change).
- [ ] **Real-time collaboration** — Simultaneous editing (anti-feature for most use cases, but may have niche value).
- [ ] **Video call integration** — Embed video platform. Likely not needed if calendar integration works.

**Why defer:** These require significant new infrastructure (AI/LLM, organizational goal data, WebSockets), statistical analysis capabilities, or address edge cases. Focus on core coaching workflow first.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Log coaching sessions | HIGH | LOW | P1 |
| Action items tracking | HIGH | MEDIUM | P1 |
| Approval workflow (action items) | HIGH | LOW (reuse existing) | P1 |
| Session history view | HIGH | LOW | P1 |
| Link to competency gaps | HIGH | MEDIUM | P1 |
| Personal development dashboard | HIGH | MEDIUM | P1 |
| Supervisor team view | HIGH | MEDIUM | P1 |
| Progress visualization (chart) | HIGH | MEDIUM (reuse Chart.js) | P1 |
| Export coaching reports | MEDIUM | LOW (reuse ClosedXML) | P1 |
| Document session notes (compliance) | HIGH | LOW | P1 |
| Calendar integration | MEDIUM | MEDIUM | P2 |
| Session templates | MEDIUM | LOW | P2 |
| Auto-suggest IDP actions | MEDIUM | MEDIUM | P2 |
| Development timeline | MEDIUM | MEDIUM | P2 |
| Mobile-responsive design | MEDIUM | MEDIUM | P2 |
| Peer comparison | LOW | MEDIUM | P2 |
| Group coaching | LOW | LOW | P2 |
| Coaching effectiveness metrics | MEDIUM | HIGH | P3 |
| Goal cascading | LOW | HIGH | P3 |
| AI note summary | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for v1.1 launch (table stakes + core differentiators)
- P2: Should have when possible (v1.2+ based on feedback)
- P3: Nice to have, future consideration (v2+ strategic features)

## Integration with Existing v1.0 Features

| v1.0 Feature | How v1.1 Integrates | Value |
|--------------|---------------------|-------|
| **UserCompetencyLevel** | Coaching sessions reference competency gaps. Action items target specific competencies. Progress dashboard shows level changes over time. | Core integration - coaching addresses gaps identified by assessments. |
| **AssessmentCompetencyMap** | Links assessment results to coaching context. "You scored low in X category (maps to Y competency) - let's coach on that." | Evidence-based coaching topics. |
| **CompetencyGap radar chart** | Embedded in development dashboard. Shows current state. Coaching sessions aim to close visible gaps. | Visual focus for coaching conversations. |
| **CPDP Progress** | Action items from coaching link to CPDP deliverables. IDP items and coaching actions unified view. | Connects coaching (CDP) to formal development (IDP). |
| **ReportsIndex (HC dashboard)** | Add coaching activity summary: sessions logged this month, pending action items, team engagement. | HC visibility into coaching program health. |
| **Excel export (ClosedXML)** | Extend pattern to coaching sessions and action items. HC can export for external analysis. | Consistent export capability across modules. |
| **Approval workflow (IdpItem)** | Reuse exact pattern for coaching action items. Same 3-level chain (SrSpv → SectionHead → HC). | Governance consistency. Users already understand workflow. |
| **Chart.js integration** | Add line charts for competency progression. Extend existing radar charts. | Consistent visualization library. |
| **Multi-role authorization** | Coaching sessions: Coachee (view own), Supervisor (view team + log sessions), HC (view all). | Reuse existing role hierarchy (RoleLevel property). |
| **Section-based filtering** | Supervisor team view filters by user.Section. HC reports filter by section. | Organizational structure already modeled. |

## User Flows for v1.1

### Flow A: Supervisor Logs Coaching Session
1. Navigate to "CDP" > "Coaching Sessions"
2. Click "Log New Session"
3. Select coachee from team dropdown (pre-filtered by section)
4. System pre-fills: coach info, date (today), coachee's current competency gaps
5. Supervisor enters: session topic, notes, coaching observations
6. Supervisor adds 0-N action items (description, due date, linked competency)
7. Submit → Session saved, action items created with "Pending" status
8. Coachee receives notification (if implemented) or sees in dashboard

### Flow B: Coachee Views Development Dashboard
1. Navigate to "CDP" > "My Development"
2. Dashboard shows 4 sections:
   - **Competency Status:** Radar chart (current vs target levels) from v1.0
   - **Recent Activity:** Last 5 coaching sessions + last 3 assessment results
   - **Active Goals:** Action items from coaching (pending/in-progress) + IDP items
   - **Progress Over Time:** Line chart showing competency level changes by month
3. Click coaching session → view session notes and action items
4. Click action item → mark progress, upload evidence, request approval
5. Click competency gap → see related assessments and IDP suggestions

### Flow C: HC Analyzes Coaching Effectiveness
1. Navigate to "CDP" > "Coaching Reports"
2. View summary cards: total sessions this month, active action items, approval queue
3. Filter by: date range, section, supervisor, competency
4. View table: session date, coach, coachee, competencies addressed, action items count, approval status
5. Click "Export to Excel" → ClosedXML generates report with filters applied
6. Drill down: click session → view full notes and action items
7. Analytics tab: Chart showing coaching frequency by section, competency improvement correlation

### Flow D: Action Item Approval Workflow
1. Coachee completes action item → marks "Complete" and uploads evidence
2. System triggers approval chain:
   - SrSpv receives notification → reviews → approves/rejects
   - If approved → SectionHead receives → reviews → approves/rejects
   - If approved → HC receives → final approval
3. If rejected at any stage → returns to coachee with feedback
4. If fully approved → action item status = "Approved", competency evidence recorded
5. Dashboard reflects approval status at each stage

## Sources

**Industry Best Practices:**
- [HR Coaching Documentation Best Practices](https://hrcertification.com/blog/hr-documentation-best-practices-biid1000103)
- [UC Berkeley Coaching as Effective Feedback Tool](https://hr.berkeley.edu/hr-network/central-guide-managing-hr/managing-hr/managing-successfully/performance-management/check-in/coaching)
- [HR Acuity Workplace Documentation Best Practices](https://www.hracuity.com/blog/workplace-documentation-best-practices/)

**Coaching Software Features (2026):**
- [GetApp Coaching Software with Session Notes](https://www.getapp.com/hr-employee-management-software/coaching/f/session-notes/)
- [Thrive Partners: Coaching in 2026 - 7 Trends HR Leaders Can't Ignore](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/)

**Competency Management & Development Dashboards:**
- [iMocha: Best Competency Management Software 2026](https://blog.imocha.io/competency-management-software)
- [iMocha: Top Skills Tracking Software 2026](https://www.imocha.io/blog/skills-tracking-software)
- [TalentGuard: Best Competency Management System](https://www.talentguard.com/competency-management-system)
- [Weever: Skills Matrix Dashboard](https://weeverapps.com/reporting-dashboard/training-dashboards/skills-matrix-report/)

**IDP & Development Planning:**
- [AIHR: Individual Development Plan Examples](https://www.aihr.com/blog/individual-development-plan-examples/)
- [AIHR: Coaching Plan Template 2026](https://www.aihr.com/blog/coaching-plan-template/)
- [TeamGPS: Individual Development Plan Guide](https://teamgps.com/blog/employee-engagement/individual-development-plan-idp-guide/)

**HR Workflow Automation:**
- [Aelum: HR Workflow Automation CHRO Guide](https://aelumconsulting.com/blogs/hr-workflow-automation-chro-guide/)
- [People Managing People: Best HR Workflow Software](https://peoplemanagingpeople.com/tools/best-hr-workflow-software/)
- [Fellow: Managing Meeting Action Items](https://fellow.ai/blog/how-to-manage-meeting-tasks-and-action-items/)

**Skills Gap Analysis:**
- [iMocha: Bridging the Skills Gap 2026](https://www.imocha.io/blog/bridging-the-skills-gap)
- [AG5: Conducting Competency Gap Analysis](https://www.ag5.com/conducting-a-competency-gap-analysis/)

---
*Feature research for: Portal HC KPB CDP v1.1 Coaching Management*
*Researched: 2026-02-17*
*Confidence: HIGH (verified with Context7 best practices, official HR documentation, 2026 industry trends)*
