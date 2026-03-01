# QA Testing Features: Use-Case Flows for Multi-Role Portal

**Domain:** Comprehensive end-to-end testing of multi-role HR competency portal (PortalHC KPB v3.0)
**Researched:** 2026-03-01
**Confidence:** HIGH (codebase analysis + UAT/QA best practices)

## Executive Summary

This research defines the QA feature landscape using **use-case flow organization** (not feature checklists) following industry best practices for multi-role systems (BrowserStack UAT, Katalon workflow testing standards).

PortalHC KPB v3.0 serves 10 roles across two platforms (CMP Assessment, CDP Coaching) with complex dependencies:
- **Assessment E2E Flow** (Create → Assign → Exam → Monitor → Results → History)
- **Coaching Proton E2E Flow** (Map → Session → 3-tier Approval → Auto-Assessment → Competency Update)
- **Master Data Management Flow** (CRUD with referential integrity)
- **Role-Based Access Control Flow** (10 roles, 6 levels, section/unit filtering)
- **Dashboard & Navigation Flow** (Role-aware hub organization)

Each flow has **table stakes** (non-negotiable), **differentiators** (competitive advantage), and **anti-features** (explicitly avoid).

---

## Testing Complexity & Effort Matrix

| Flow | Complexity | Duration | # Roles | DB Changes | Risk Level |
|------|-----------|----------|---------|-----------|-----------|
| Assessment E2E | HIGH | 45 min | 2 (HC, Worker) | 20+ tables | HIGH (core) |
| Coaching Proton E2E | MEDIUM | 30 min | 4 (Coach, Sr Sup, Sec Head, HC) | 8 tables | MEDIUM (approval chain) |
| Master Data CRUD | MEDIUM | 20 min | 2 (Admin, HC) | 3-5 tables/entity | MEDIUM (integrity) |
| Dashboard & Nav | LOW | 15 min | 10 roles | 0 (read-only) | LOW (display) |
| Auth Flow | LOW | 10 min | All | 1-2 tables | MEDIUM (access gate) |
| **Total Manual QA Effort** | **MEDIUM-HIGH** | **2-3 hours per complete cycle** | **All 10** | **50+ tables touched** | **HIGH (E2E impact)** |

---

## Recommended Testing Order & Gates

1. **Phase 1: Foundation (30 min)** — Must pass before other flows
   - Auth (AD login, local fallback) ✓
   - Dashboard/Nav (cards visible, links work) ✓
   - Master Data seeding (KKJ, workers, coaches) ✓

2. **Phase 2: Assessment Core (45 min)** — Blocks Coaching E2E
   - Create assessment, assign, auto-save/resume, submit
   - HC monitoring, actions (reset, close early)
   - Results display, history
   - Competency auto-update on pass

3. **Phase 3: Coaching Flow (30 min)** — Depends on Assessment
   - Coach-coachee mapping, session creation
   - Approval chain (Sr Sup → Sec Head → HC)
   - Auto-assessment creation on final approval
   - Evidence collection

4. **Phase 4: Data Integrity (20 min)** — Concurrent with Phase 2-3
   - CRUD on each master entity
   - Referential integrity (FK cascades)
   - Audit log entries for each action
   - Bulk import for workers/mappings

5. **Phase 5: Edge Cases & Cross-Role (15 min)** — After Phase 2-4
   - Role boundary violations (attempt cross-section access)
   - Multi-unit worker scenarios
   - Archived assessment recovery
   - Admin "View As" HC toggle

**Total Duration:** 2.5-3 hours per full QA cycle
**Frequency:** After each major feature merge; before UAT gate

---

## Feature Categories

### Table Stakes (Non-Negotiable for v3.0)

Features users expect. Missing = portal is broken.

| Feature | Why Expected | Complexity | QA Scope |
|---------|--------------|-----------|----------|
| **Role-Based Access** (10 roles) | Different views for Admin, HC, Coach, Coachee, Sr Supervisor, etc.; no cross-role data leakage | MEDIUM | Test each role navigates correctly; verify 403 on unauthorized access |
| **Assessment E2E Flow** | Workers must create exam → take → get results → see history; HC monitors progress | HIGH | Create → assign → exam (auto-save, resume) → results → history → monitoring |
| **Coaching Proton E2E Flow** | Coaches map coachees → create sessions → get approval (3 tiers) → final assessment auto-created → competency updates | MEDIUM | Mapping → session → approval chain → auto-assessment → competency update |
| **Master Data CRUD** | Admin/HC manage KKJ, CPDP, workers, coach-coachee mappings, questions, packages without data corruption | MEDIUM | Create/edit/delete each entity; verify FK integrity; test bulk import |
| **Exam Auto-Save & Resume** | Worker auto-saves every 30sec; browser refresh recovers last state without data loss | MEDIUM | Answer questions → wait 30sec → refresh → verify recovery |
| **Dual Authentication** | AD (primary) + local fallback; no hardcoded passwords | LOW | AD login → local login → fallback when AD down |
| **Dashboard & Navigation** | Role-specific cards (Assessment Lobby, Manage Assessments, Kelola Data hub); no broken links | MEDIUM | Verify correct cards visible per role; all nav links work |
| **Multi-Unit User Support** | Users belong to multiple units; filter/picker reflects all assigned units | LOW | Assign user to 2+ units → verify filter shows all |
| **Training Records History** | All assessment attempts (including archived) show in chronological Riwayat tab with attempt # | MEDIUM | Complete exam → reset → complete again → verify both in history |
| **Assessment Result Configuration** | HC sets pass threshold (0-100%) per assessment; toggles answer review visibility | LOW | Set threshold 70% → submit 65% → verify fail; toggle review → verify visibility |
| **Section-Level Access Filtering** | Section Heads, Sr Supervisors, Coaches see only their section data; URL bypass blocked with 403 | MEDIUM | Log in as Section Head → verify picker/tables section-filtered; try cross-section URL → 403 |
| **Kelola Data Hub** | 3 domain sections (Manajemen Pekerja, Kelola Assessment, Data Proton); HC nav visible for HC role | MEDIUM | Verify hub nav visible; cards link to correct pages; stats accurate |

### Differentiators (Competitive Advantage)

Features that set product apart. Not required but valuable.

| Feature | Value Proposition | Complexity | QA Scope |
|---------|-------------------|-----------|----------|
| **Real-Time Assessment Monitoring** | HC sees live exam progress (status, score, countdown) with actions (Reset, Close Early, Regenerate Token) | HIGH | Monitor page shows live updates; test each HC action |
| **Archive-Before-Clear Lifecycle** | Reset action archives completed session with original score/pass-fail; no data loss | MEDIUM | Complete exam → reset → verify archived row created → history shows attempt |
| **Auto-Update Competency** | Assessment completion auto-updates worker's KKJ competency level via AssessmentCompetencyMap; monotonic progression | MEDIUM | Complete assessment → verify competency level updated; take again at lower score → verify level unchanged (monotonic) |
| **Coaching Evidence Consolidation** | Coaching session notes + files in single modal (not separate tabs); improves UX | LOW | Create session → add notes → attach file → verify consolidated modal |
| **Bulk Assign with Deduplication** | Assign multiple workers to same assessment; detects sibling sessions (same title+category+schedule) | MEDIUM | Assign user A → edit assign user B → verify 1 new session (not 2) |
| **Filtered Import Templates** | Workers/Coach-Coachee mappings via Excel with section-filtered picker; prevents cross-section assignment | MEDIUM | Download template → upload 20 workers → verify bulk creation |
| **Governance & Audit Trail** | Every HC management action logged to AuditLog (actor, timestamp, action type); paginated view (25/page) | MEDIUM | HC creates assessment → check AuditLog entry; HC resets worker → verify logged |

### Anti-Features (What to Avoid)

| Anti-Feature | Why Problematic | Recommended Approach |
|--------------|-----------------|----------------------|
| **Real-Time via WebSocket** | High infrastructure cost (SignalR), cache invalidation complexity; page-refresh sufficient for v3.0 | Keep page-refresh model; add 5-10 sec auto-refresh if needed |
| **Soft Delete (Logical)** | Doubles schema complexity, breaks FK constraints, obfuscates data model | Keep hard delete; AuditLog captures before-state; if compliance needs history, add separate archive table |
| **Question Randomization Persistence** | Adds complexity to PackageQuestion model; question-reveal in results sufficient | Current approach (shuffledIds in session, reveal in results) is fine |
| **Per-Role Custom Rules** (e.g., Direktur 80%, Coach 60% threshold) | Role-branching in grading logic; breaks test simplicity | Admin sets threshold globally per assessment; role-specific needs → defer to TenantId strategy |
| **Approval Auto-Escalation** (7-day auto-approve) | Bypasses accountability, legal liability | Keep explicit approval required; send escalation reminders instead (v3.1+ backlog) |
| **Live Competency Dashboard** (real-time aggregation) | Complex query, cache invalidation; hourly refresh sufficient | Keep current Analytics tab refresh pattern |

---

## MVP Definition (v3.0 Launch Requirement)

### Must Have (Launch Blockers)

- [ ] Assessment E2E (create, assign, exam, results, history, monitoring, integrity)
- [ ] Coaching Proton E2E (mapping, session, approval, competency update, evidence)
- [ ] Master Data CRUD (KKJ, CPDP, workers, mappings, packages/questions, silabus)
- [ ] Role-Based Access (all roles properly gated; 403 on unauthorized access)
- [ ] Dashboard & Navigation (role-aware cards; hub organization; all links work)
- [ ] Dual Authentication (AD primary, local fallback, user sync)

### Should Have (v3.0 + Nice-to-Have)

- [ ] Approval Escalation (7-day timeout escalation) → v3.1
- [ ] Plan IDP (coachee IDP page, silabus + guidance download) → v3.1
- [ ] Assessment Reminders (email 3 days/1 day/1 hour before) → v3.1
- [ ] Competency Analytics Dashboard (heatmap, gaps, trends) → v3.1+

### Defer (Out of Scope)

- [ ] Soft Delete (logical delete + archival)
- [ ] Multi-Tenant (separate orgs on platform)
- [ ] Real-Time via WebSocket
- [ ] Approval Conditional Logic

---

## Sources

- **UAT Best Practices:** [BrowserStack UAT Testing Checklist](https://www.browserstack.com/guide/user-acceptance-testing-checklist)
- **RBAC Testing:** [ServiceNow RBAC Testing Guide (Medium, Jan 2026)](https://medium.com/globant/role-based-access-control-rbac-testing-for-servicenow-applications-f14d44420186)
- **Workflow Testing:** [Katalon Workflow Testing Guide](https://katalon.com/resources-center/blog/workflow-testing)
- **Table Stakes:** [LinkedIn: Table-stake Features in SaaS](https://www.linkedin.com/pulse/table-stake-features-saas-enterprise-products-rohit-pareek)
- **Enterprise QA:** [Thinksys QA Strategy for Enterprise Software](https://thinksys.com/qa-testing/qa-strategy-enterprise-software/)
- **Project Context:** PortalHC KPB PROJECT.md, SeedData.cs, UserRoles.cs

---

*QA Testing Features: Multi-Role Portal — Use-Case Flow Organization*
*Researched: 2026-03-01 | Confidence: HIGH | Organized for manual testing with code analysis*
