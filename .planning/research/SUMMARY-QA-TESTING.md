# Research Summary: Comprehensive QA Testing for Multi-Role Portal

**Project:** PortalHC KPB v3.0 — Full QA & Feature Completion
**Domain:** QA testing of multi-role HR competency management portal
**Researched:** 2026-03-01
**Overall Confidence:** HIGH (codebase analysis + industry UAT/QA best practices)

---

## Executive Summary

PortalHC KPB v3.0 is a **brownfield enhancement milestone** focused on comprehensive end-to-end testing of existing features (Assessment, Coaching, Master Data, Auth, Navigation) plus code cleanup (dead code removal, naming fixes, duplicate consolidation) and new features (Plan IDP, AuditLog hub card).

**Testing Approach:** Organized by **use-case flows** (Assessment E2E, Coaching Proton E2E, Master Data, Dashboard/Nav) rather than feature checklists. Each flow has table stakes (must-have), differentiators (competitive advantage), and anti-features (explicitly avoid).

**Testing Scope:** 5 major flows, 10 user roles, 6 approval tiers, 20+ database tables, ~2-3 hours per full QA cycle. All flows are interconnected with hard dependencies (Assessment blocks Coaching; Coaching blocks Competency Update; Master Data blocks all flows).

**Quality Gate:** QA passes when all 5 flows achieve table-stakes status. Differentiators can be deferred to v3.1 if needed. Anti-features are explicitly **not** implemented.

---

## Key Findings

### Stack & Architecture

**Testing Framework:** Manual QA with code analysis (no Selenium/Playwright at this phase)
**Database:** SQL Server with Entity Framework Core; complex schema with 50+ tables and multiple FK relationships
**Roles:** 10 roles (Admin, HC, 3 management levels, Sr Supervisor, Coach, Supervisor, Coachee) across 6 authorization levels
**Multi-Unit Support:** Users can belong to 2+ units; filtering by unit + section maintains data boundaries
**Approval Chain:** 3-tier coaching approval (Sr Supervisor → Section Head → HC) with rejection feedback loops

### Core Testing Flows (High Priority)

1. **Assessment E2E Flow** (HIGH complexity, 45 min)
   - Create assessment with package + schedule + threshold
   - Bulk assign workers to assessment
   - Worker takes exam with auto-save (every 30sec) + resume (browser close → refresh → recover)
   - Exam auto-closes at timeout; worker sees results (score, pass/fail, earned competencies)
   - Worker sees all attempt history (including archived attempts after reset) with attempt numbering
   - HC monitors progress (real-time or refresh); HC actions (Reset, Close Early, Force Close, Regenerate Token)
   - **Dependencies:** KKJ Matrix, Assessment Package, Question Management, Session Tracking
   - **Blockers:** If Assessment E2E fails, Coaching flow is blocked (final approval creates auto-assessment)

2. **Coaching Proton E2E Flow** (MEDIUM complexity, 30 min)
   - Admin/HC creates Coach-Coachee Mapping (same-section validation)
   - Coach creates Coaching Session (select mapped coachee, fill Kompetensi + notes)
   - Coach adds Evidence (notes + file upload)
   - Sr Supervisor approves/rejects (Status: Pending → L1 Approval → L2 Approval → L3 Approval)
   - HC final approval auto-creates "Proton Assessment" for coachee
   - Coachee completes Proton Assessment at 75%+ (pass)
   - Worker's KKJ competency level auto-updates (monotonic: never decreases)
   - **Dependencies:** Coach-Coachee Mapping, CoachingSession Model, Approval Workflow, Assessment E2E
   - **Blockers:** If Approval Workflow fails, competency updates never trigger

3. **Master Data Management Flow** (MEDIUM complexity, 20 min per entity type)
   - KKJ Matrix: Create competency → Edit level → Delete with impact warning
   - CPDP Items: CRUD with validation
   - Worker Management: Create/bulk import with multi-unit assignment
   - Coach-Coachee Mapping: CRUD with section validation
   - Assessment Packages & Questions: Create package → add questions → shuffle validation → delete with cascade
   - Silabus & Coaching Guidance: File upload + metadata CRUD
   - **Critical:** Every CRUD operation must log to AuditLog (governance trail)
   - **Data Integrity:** FK cascades prevent orphaning; FK constraints enforced

### Differentiators (Competitive Advantage)

- **Real-Time Assessment Monitoring:** HC sees live exam progress with countdown + HC action suite (Reset, Close Early, Regenerate Token)
- **Archive-Before-Clear:** Reset action archives completed session with original score; no attempt data loss; history shows all archived attempts
- **Auto-Update Competency:** Assessment completion triggers KKJ competency level update; monotonic progression (level never decreases)
- **Audit Trail:** Every HC management action logged (actor NIP, timestamp, action type); governance/compliance requirement
- **Bulk Import with Templates:** Workers/Coach-Coachee mappings via Excel templates with section-filtered picker

### Anti-Features (Explicitly Avoid)

- **Real-Time via WebSocket:** Not implemented; page-refresh sufficient
- **Soft Delete (Logical):** Not implemented; hard delete + AuditLog sufficient
- **Question Randomization Persistence:** Current approach (shuffledIds in session) sufficient
- **Per-Role Custom Assessment Rules:** Not implemented; admin sets threshold globally
- **Approval Auto-Escalation:** Not implemented; explicit approval required; escalation reminders deferred to v3.1

---

## Feature Landscape

### Table Stakes (Must-Have for v3.0)

| Feature | Complexity | Duration | Passes/Fails QA | Dependency |
|---------|-----------|----------|-----------------|-----------|
| Assessment E2E (create → exam → results → history → monitoring) | HIGH | 45 min | PASS/FAIL assessment flow tests | KKJ Matrix, Question Pool |
| Coaching Proton E2E (map → session → approval → auto-assessment) | MEDIUM | 30 min | PASS/FAIL coaching approval chain | Assessment E2E (blocks final assessment) |
| Master Data CRUD (KKJ, CPDP, Workers, Mappings, Packages, Silabus) | MEDIUM | 20 min | PASS/FAIL per-entity CRUD tests | (Blocks Assessment + Coaching if missing) |
| Role-Based Access (10 roles, 403 on unauthorized) | MEDIUM | 15 min | PASS/FAIL per-role access gate tests | (Blocks all flows if broken) |
| Exam Auto-Save & Resume | MEDIUM | 10 min | PASS/FAIL browser close → refresh → recover test | Session tracking, PackageUserResponse |
| Dashboard & Navigation | MEDIUM | 15 min | PASS/FAIL per-role card visibility + link tests | Role-aware view model filtering |
| Dual Authentication (AD + local fallback) | LOW | 10 min | PASS/FAIL AD login + fallback + sync tests | IAuthService abstraction |
| Training Records History (with attempt #) | MEDIUM | 10 min | PASS/FAIL history view + attempt numbering tests | AssessmentAttemptHistory table |

### Differentiators (Nice-to-Have)

| Feature | Complexity | Trigger for Adding |
|---------|-----------|-------------------|
| Real-Time Monitoring (live progress updates) | HIGH | Real-time feedback improves HC decision-making |
| Archive-Before-Clear (attempt recovery) | MEDIUM | Compliance requirement for attempt history |
| Auto-Update Competency (monotonic progression) | MEDIUM | Closed-loop development; verified in Coaching flow |
| Audit Trail (all HC actions logged) | MEDIUM | Governance/compliance; required by enterprise |
| Bulk Import Templates (workers/mappings) | MEDIUM | Efficiency for large setup batches |

---

## Implications for Roadmap

### Recommended Phase Structure for v3.0

**Phase 1: Foundation (1 week)**
- Verify Auth system (AD login, local fallback, user sync)
- Verify Dashboard & Navigation (role-aware cards, hub organization)
- Seed Master Data (KKJ, workers, coaches for testing)
- **Gate:** All foundation tests PASS before moving to Phase 2

**Phase 2: Assessment Core Testing (1.5 weeks)**
- Test Assessment Creation & Configuration (title, package, schedule, threshold, review visibility)
- Test Bulk User Assignment (idempotent, deduplication, section filtering)
- Test Exam Flow (auto-save, resume, question navigation, timeout)
- Test Results Display (score calculation, pass/fail, competencies earned)
- Test Assessment History (Riwayat Ujian, attempt numbering, archived recovery)
- Test HC Monitoring Dashboard (live progress, HC actions: Reset/Close Early/Force Close/Regenerate Token)
- **Gate:** All Assessment tests PASS before moving to Phase 3

**Phase 3: Coaching Proton Testing (1.5 weeks)**
- Test Coach-Coachee Mapping (create, section validation, bulk import)
- Test Coaching Session Creation (select coachee, fill notes, add evidence)
- Test 3-Tier Approval Workflow (Sr Supervisor → Section Head → HC, rejection feedback loops)
- Test Auto-Assessment Creation (HC final approval triggers assessment creation)
- Test Competency Auto-Update (assessment completion → KKJ level update, monotonic progression)
- Test Evidence & History (consolidated modal, download, audit trail)
- **Gate:** All Coaching tests PASS before moving to Phase 4

**Phase 4: Master Data & Integrity Testing (1 week)**
- Test CRUD for each entity: KKJ Matrix, CPDP Items, Workers, Coach-Coachee Mappings, Assessment Packages/Questions, Silabus/Guidance
- Test Referential Integrity (FK cascades, no orphaning, uniqueness constraints)
- Test Bulk Import (workers, coach-coachee mappings)
- Test Audit Log Entries (verify every CRUD action logged)
- **Gate:** All Master Data tests PASS before UAT gate

**Phase 5: Cross-Role & Edge Cases (1 week)**
- Test Role Boundary Violations (cross-section/unit access attempts → 403)
- Test Multi-Unit Scenarios (worker in 2+ units; assessment includes both)
- Test Archived Assessment Recovery (reset → retake → history shows both with attempt #)
- Test Admin "View As" HC Toggle (admin can see HC perspective)
- Code Analysis & Dead Code Cleanup (verify "ProtonMain" removed, "Proton Progress" renamed)

**Phase 6: Code Cleanup (1 week)**
- Verify Duplicate Path Removal (CMP ManageQuestions, CreateTrainingRecord consolidated in Admin)
- Verify "Proton Progress" → "Coaching Proton" Rename (UI, code comments, labels)
- Verify Orphaned Stub Card Removal (BP module, Settings placeholders)
- Verify No Hardcoded Passwords (audit code)

**Phase 7: UAT Gate & Release Prep (1 week)**
- Run full QA cycle end-to-end (2-3 hours)
- Verify all table-stakes features PASS
- Document any deferred features (differentiators for v3.1)
- Sign-off from PM + HC stakeholder before release

### Why This Order

1. **Foundation first** (Auth, Nav, Master Data) — These are blockers for all other flows. No point testing Assessment if login is broken.
2. **Assessment before Coaching** — Coaching flow depends on Assessment E2E; HC final approval creates auto-assessment.
3. **Data Integrity concurrent** (Phase 4) — Can run in parallel with Phases 2-3 once master data is seeded.
4. **Cross-role & cleanup** (Phase 5-6) — These are lower-priority edge cases; can defer some to v3.1 if schedule tight.
5. **UAT gate last** — Full cycle verification before any release.

### Research Flags for Future Phases

| Area | Flag | Reason | Mitigation |
|------|------|--------|-----------|
| Assessment Monitoring | NEEDS CLARIFICATION | 2-state vs 4-state user status (tech debt) | Document current behavior; defer fix to v3.1 |
| Plan IDP | NEW FEATURE | Stub page needs development (silabus display + guidance download) | Scope for v3.1 or v3.0 extension phase |
| Approval Escalation | DEFERRED | 7-day timeout escalation not yet implemented | Plan for v3.1 if business requires |
| WebSocket Real-Time | DEFERRED | SignalR integration overkill for v3.0; page-refresh sufficient | Revisit for v3.2 if HD monitoring feedback demands it |
| Mobile Responsive Assessment | DEFERRED | No mobile UX work planned for v3.0 | Plan for v3.1 based on user feedback |

---

## Confidence Assessment

| Area | Confidence | Evidence | Notes |
|------|-----------|----------|-------|
| **Stack** | HIGH | Codebase analysis (Controllers, Models, Data); PROJECT.md inventory; UserRoles.cs role hierarchy verified | 10 roles, 6 levels clearly defined; FK relationships mapped |
| **Features** | HIGH | BrowserStack UAT + Katalon workflow testing standards; codebase inspection; PROJECT.md features list | Assessment, Coaching, Master Data, Auth, Navigation all documented |
| **Architecture** | HIGH | EF Core migration analysis; controller authorization patterns; role-aware filtering gates verified | Approval chain, competency mapping, multi-unit support all implementable patterns |
| **Pitfalls** | MEDIUM | Industry best practices (WebSocket, Soft Delete anti-patterns identified); no project-specific post-mortems available | Recommend tracking actual bugs during v3.0 QA for pitfalls update |
| **Dependencies** | HIGH | Feature dependency map created (Assessment → Coaching → Competency Update); FK constraints verified | Clear ordering for phase execution |

---

## Gaps to Address

1. **Performance Baseline** — No performance SLA defined. When should Assessment creation/exam submission complete? Recommend benchmark testing for v3.1.

2. **Load Testing** — No concurrent user load testing planned. How does system behave with 100+ workers in same assessment exam window? Defer to v3.1.

3. **Accessibility Testing** — No WCAG compliance testing. Screen reader support for exam form? Defer to v3.1 based on stakeholder demand.

4. **Browser Compatibility** — Testing only assumes modern browsers (Chrome, Firefox, Safari). IE/Edge legacy support? Clarify requirements.

5. **Mobile Responsive Design** — Exam form not tested on mobile. Is portrait/landscape orientation handled? Defer to v3.1.

6. **Data Seed Strategy** — Test data (200 workers, 50 coaches, 10 assessments) assumed to exist. Document exact seed data requirements for each test phase.

7. **Regression Test Automation** — Manual QA sufficient for v3.0, but v3.1 should consider automated regression suite (Playwright/Cypress for smoke tests).

---

## Downstream Implications

### For Roadmap Planning

Use this research to:
1. **Structure v3.0 phases** in execution order (Foundation → Assessment → Coaching → Master Data → Cleanup)
2. **Define QA gates** (each phase must PASS table-stakes before next phase starts)
3. **Identify blockers** (Assessment blocks Coaching; Master Data blocks both)
4. **Plan resource allocation** (2-3 hours per full QA cycle; 7 phases × ~2 weeks each = ~14 weeks for v3.0 if sequential)
5. **Set stakeholder expectations** (differentiators can be deferred to v3.1 without blocking release)

### For QA Execution

Use FEATURES.md (checklist) + FEATURES-QA-FLOWS.md (detailed test cases) to:
1. Execute manual QA per phase (30-45 min per major flow)
2. Verify table-stakes tests first (quick pass/fail gate)
3. Document failures with reproduction steps
4. Track deferred features for v3.1 backlog
5. Update AuditLog as QA validates features

### For Code Analysis

Use codebase inspection to:
1. Verify no hardcoded passwords (audit appsettings.json, code comments)
2. Confirm "ProtonMain" removed (search → expect 0 results)
3. Confirm "Proton Progress" → "Coaching Proton" renamed (global find-replace)
4. Verify orphaned stub cards removed (Settings placeholders, BP module)
5. Confirm duplicate CRUD paths consolidated (CMP/ManageQuestions, CreateTrainingRecord → Admin only)

---

## Recommended Next Steps

1. **Week 1:** Publish this research to `.planning/research/`; create FEATURES.md + FEATURES-QA-FLOWS.md + SUMMARY-QA-TESTING.md
2. **Week 1-2:** Execute Phase 1 (Foundation) QA; document any issues
3. **Week 2-3:** Execute Phase 2 (Assessment Core) QA; verify all assessment tests PASS before Coaching
4. **Week 3-4:** Execute Phase 3 (Coaching Proton) QA; verify approval chain + auto-assessment + competency update
5. **Week 4-5:** Execute Phase 4 (Master Data) + Phase 5 (Edge Cases) QA in parallel
6. **Week 5-6:** Execute Phase 6 (Code Cleanup) + Phase 7 (UAT Gate); sign-off for release
7. **Week 6+:** Plan v3.1 features (Approval Escalation, Plan IDP, Reminders, Analytics, Mobile)

---

## Sources

- **UAT Best Practices:** [BrowserStack UAT Testing Checklist](https://www.browserstack.com/guide/user-acceptance-testing-checklist)
- **RBAC Testing:** [ServiceNow RBAC Testing Guide (Medium, Jan 2026)](https://medium.com/globant/role-based-access-control-rbac-testing-for-servicenow-applications-f14d44420186)
- **Workflow Testing:** [Katalon Workflow Testing Guide](https://katalon.com/resources-center/blog/workflow-testing)
- **Table Stakes Definition:** [LinkedIn: Table-stake Features in SaaS](https://www.linkedin.com/pulse/table-stake-features-saas-enterprise-products-rohit-pareek)
- **Enterprise QA Strategy:** [Thinksys QA Strategy for Enterprise Software](https://thinksys.com/qa-testing/qa-strategy-enterprise-software/)
- **Codebase:** PortalHC KPB `.planning/PROJECT.md`, `Models/UserRoles.cs`, `Data/SeedData.cs`, Controllers (Admin, CMP, CDP)

---

*Research Summary: Comprehensive QA Testing for Multi-Role Portal*
*Project: PortalHC KPB v3.0*
*Researched: 2026-03-01*
*Confidence: HIGH*
