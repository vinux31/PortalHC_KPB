# Roadmap Implications: QA Testing Architecture for v3.0

**For:** v3.0 Milestone Planning
**Date:** 2026-03-01
**Audience:** Project Leads, QA Manager, Technical Leads

---

## Summary of Research Findings

PortalHC v3.0's testing challenge is **not** technical complexity of individual features, but **coordination complexity** of multi-role workflows with strict data dependencies. The portal's architecture requires a fundamentally different testing approach than traditional feature-area testing.

**Key Insight:** A test that passes with single-role features (Assessment creation, Silabus editing) will fail with multi-role workflows (Coaching Proton approval) if not designed with **workflow orchestration in mind**.

---

## Recommended Milestone Structure

### Current Planned v3.0: "Full QA & Feature Completion"

**Current targets:**
- End-to-end QA of Assessment flow
- End-to-end QA of Coaching Proton flow
- Master data management verification
- Plan IDP development
- Code cleanup (remove duplicates, orphans)
- UI rename ("Proton Progress" → "Coaching Proton")

**Research recommendation:** Keep these targets but **reorder phases by data dependency** instead of feature area.

---

## Recommended Phase Ordering (Replaces Current Unordered Approach)

### Phase Group A: Foundation (Weeks 1-2) — START HERE

| Phase | Duration | Purpose | Blockers | Blocks |
|-------|----------|---------|----------|--------|
| **Phase 80: Master Data Validation** | 1-2 weeks | Verify KKJ/CPDP/Silabus/ProtonTracks seed correctly; validate seeding infrastructure | None | All downstream phases |
| **Phase 81: User Management & Roles** | 1-2 weeks | Verify auth, user import, role assignment, multi-unit support | Phase 80 | All downstream phases |
| **Concurrent: Test Infrastructure Setup** | 1 week | Build IntegrationTestBase, WebApplicationFactory config, in-memory SQLite, test user seeders | Phase 80 | All integration/E2E tests |

**Success Criteria:**
- Master data seeding takes <500ms
- All 6 roles (Admin, HC, SrSpv, SectionHead, Coach, Coachee) can log in
- Multi-unit users can switch units without errors
- 8-15 tests pass, ~500ms total

---

### Phase Group B: Independent Flows (Weeks 3-5) — Can run PARALLEL to Group C

| Phase | Duration | Purpose | Blockers | Blocks |
|-------|----------|---------|----------|--------|
| **Phase 82: Assessment E2E** | 2-3 weeks | Test Assessment creation → assign → exam → review → competency grant (single-role happy path) | A | None (independent) |
| **Phase 83: Master Data CRUD** | 1-2 weeks | Test KKJ/Silabus editing, guidance file upload, cascade delete | A | None |

**Why Parallel:** Assessment doesn't depend on Coaching Proton; Coaching doesn't depend on Assessment. Test independently to isolate issues.

**Success Criteria:**
- 20-25 Assessment E2E tests pass (~2-3 seconds)
- 12-15 Admin CRUD tests pass
- Competency levels correctly granted after assessment completion
- Files upload/download without path errors

---

### Phase Group C: Dependent Workflows (Weeks 4-7) — Can start once Group A completes

| Phase | Duration | Purpose | Blockers | Blocks |
|-------|----------|---------|----------|--------|
| **Phase 84: Coaching Proton Core E2E** | 3-4 weeks | Test Track assignment → Deliverable submission → Multi-role approvals → Final assessment (orchestrate 4 roles) | A | Phase 85 |
| **Phase 85: Approval Workflow Edge Cases** | 1-2 weeks | Test rejection/resubmission, HC override, parallel approval conditions | 84 | Phase 86 |

**Why Sequential:** Phase 85 builds on Phase 84's multi-role orchestration infrastructure.

**Critical Dependency:** Phase 84 must solve **multi-role session management** before Phase 85 can test complex workflows.

**Success Criteria:**
- 35-45 E2E tests covering all approval paths
- SrSpv and SectionHead approvals are independent (not sequential)
- Rejection → Resubmit → Re-approve cycle works
- All state combinations tested (see QA-PITFALLS.md Pitfall 1)

---

### Phase Group D: Validation & Cleanup (Weeks 8-9) — START AFTER C completes

| Phase | Duration | Purpose | Blockers | Blocks |
|-------|----------|---------|----------|--------|
| **Phase 86: Dashboard & Reporting** | 1 week | Verify all role dashboards render correct data (Assessment pending count, Approval pending count, etc.) | C | Phase 87 |
| **Phase 87: Code Cleanup & Regression** | 1 week | Remove orphaned CMP/CpdpProgress, consolidate duplicate paths, rename "Proton Progress", add AuditLog card; re-run all tests | None | v3.0 complete |

**Success Criteria:**
- No broken links after cleanup
- All 110-150 tests still pass post-cleanup
- Zero new bugs introduced by refactoring

---

## Timeline Visualization

```
Week:   1  2  3  4  5  6  7  8  9
        ┌──┐
Group A ├──┤ Master Data + Roles
        └──┘
           ├──────────┐
Group B    │ Assessment│  Master Data CRUD
           ├──────────┤  (can overlap with C)
           └──┐
           ┌──┤
Group C    │  ├─────────────┐
           │  │ Coaching    │
           │  │ Proton Core │
           │  │ + Approvals │
           │  └─────────────┤
           │        └──────────┐
           │                   │
Group D    └──────────────────┴────┐
                                   ├────┐
                         Dashboard │ +  │ Cleanup
                                   └────┘
```

**Total Duration:** 9 weeks (can compress to 7-8 with aggressive parallelization)

---

## Key Resource Requirements

### Personnel
- **QA Lead/Architect** (1 person, full-time): Design test infrastructure, mentor test writing
- **QA Engineers** (2-3 people, full-time): Write and execute tests
- **Tech Lead** (part-time): Clarify approval workflow business rules, validate data dependencies

### Tooling
- **xUnit framework** (free, already in .NET ecosystem)
- **Moq library** (free, for unit test mocking)
- **FluentAssertions** (free, improved test readability)
- **WebApplicationFactory** (built-in to ASP.NET Core)
- **SQLite in-memory** (free, for test DB)
- **CI/CD pipeline** (GitHub Actions, Azure DevOps, or similar) — ensure test suite runs pre-commit

### Infrastructure
- **Build server** with .NET SDK pre-installed
- **Test reporting dashboard** (optional but recommended; many teams use xUnit + ReportGenerator)

### Estimated Cost
- **Labor:** ~3 FTE × 9 weeks = 135 person-days
- **Tooling:** Free (all open-source)
- **Infrastructure:** Minimal (CI/CD already exists for build)

---

## Risk Mitigation

### Risk 1: Multi-Role Workflow Orchestration is Harder Than Expected

**Likelihood:** Medium (no existing E2E tests for approval workflows in codebase)

**Mitigation:**
- Phase 84 is sized at 3-4 weeks to allow for learning curve
- Build proof-of-concept in first week: can we create 2+ authenticated users in one E2E test?
- If PoC fails, pivot to simpler E2E structure (separate tests per role, validate state via DB)

**Go/No-Go Gate:** End of Week 4, before Phase 85 starts. If Phase 84 isn't 50% done, reassess approach.

---

### Risk 2: Approval Workflow Business Logic is Undocumented

**Likelihood:** High (Phase 65 approval redesign was recent, rules may not be documented)

**Mitigation:**
- Phase 80-81 includes requirement to document approval state machine (see QA-PITFALLS.md, Pitfall 1)
- Phase 84 test design includes explicit state transition tests (see prevention strategies)
- Escalate to Tech Lead immediately if business rules unclear

**Go/No-Go Gate:** End of Week 2. Must have documented approval state machine before Phase 84 starts.

---

### Risk 3: Test Infrastructure Setup Takes Longer Than Estimated

**Likelihood:** Low-Medium (WebApplicationFactory is well-documented, but in-memory DB setup can have quirks)

**Mitigation:**
- Allocate extra week (Week 1-2) for infrastructure setup before Phase 80 starts
- Use existing eShopOnWeb sample code (Microsoft's reference implementation) as template
- Have Tech Lead review WebTestFixture implementation before Phase 80 begins

---

### Risk 4: Cascade Delete Assumptions Cause Rewrites

**Likelihood:** Medium (cascade delete is fragile, easy to misconfigure)

**Mitigation:**
- Phase 83 (Master Data CRUD) includes explicit cascade delete tests (see QA-PITFALLS.md, Pitfall 6)
- Before Phase 83, audit ApplicationDbContext.OnModelCreating() for all cascade configurations
- Add database constraints to enforce referential integrity

---

## Success Metrics

### Quality Gates

| Gate | Metric | Target | Phase |
|------|--------|--------|-------|
| **Test Coverage** | Unit + Integration + E2E tests exist | 80-120 unit, 40-60 integration, 20-30 E2E = 140-210 total | All |
| **Test Execution Time** | Full test suite runs in CI | <5 seconds (all tests, no parallelization) | All |
| **Test Reliability** | Test pass rate consistency | 100% pass rate 10/10 runs (no flaky tests) | All |
| **Code Coverage** | Codebase coverage | >70% code coverage (unit + integration) | All |
| **Bug Escape Rate** | Bugs found in production that tests missed | <5% of shipped bugs (typical is 10-20%) | Post-v3.0 |

### Roadmap Gates (Go/No-Go Decisions)

| When | Decision | Criteria |
|------|----------|----------|
| **End of Phase 81** | Continue to B/C? | All master data + role tests pass; >90% test suite stable |
| **End of Phase 82** | Continue to 84? | Assessment E2E tests pass; state transitions documented |
| **End of Phase 84** | Continue to 85? | Multi-role workflow tests pass; approval state machine validated |
| **End of Phase 86** | Release v3.0? | All tests pass; coverage >70%; zero critical bugs |

---

## Impact on Existing v3.0 Tasks

### Master Data Management Verification
- **Old approach:** Manual testing by QA team
- **New approach:** Phase 80-81 automated tests; much faster and repeatable

### Plan IDP Development
- **Old approach:** Standalone feature
- **New approach:** Part of Phase 82 (Assessment) and Phase 84 (Coaching), tested as integrated workflows

### Code Cleanup (Orphaned Pages, Duplicate Paths)
- **Old approach:** Manual code review + careful testing
- **New approach:** Phase 87 (Code Cleanup & Regression) ensures no broken links with regression test suite

### UI Rename ("Proton Progress" → "Coaching Proton")
- **Old approach:** Find-and-replace + manual verification
- **New approach:** Phase 87 includes test validation (UI strings updated correctly)

---

## Decision Points for Stakeholders

### Option A: Recommended (Full Test Architecture)

**What:** Implement all phases 80-87 with full test pyramid (unit/integration/E2E).

**When:** v3.0 milestone, 9 weeks, start immediately.

**Benefit:** Comprehensive test coverage, fast CI/CD, low bug escape rate, maintainable long-term.

**Cost:** 3 FTE × 9 weeks + test infrastructure setup.

**Risk:** Multi-role workflow orchestration complexity; approval workflow business logic unclear.

---

### Option B: Lean (Integration + E2E Only, Skip Unit Tests)

**What:** Implement Phases 80-87 but skip unit test layer; focus on integration + E2E (faster execution, less maintenance).

**When:** v3.0 milestone, 6-7 weeks.

**Benefit:** Faster test execution (~2 seconds instead of ~5 seconds); less test code to maintain.

**Cost:** 2.5 FTE × 7 weeks; higher risk of missing business logic bugs.

**Risk:** No fast feedback loop for developers; unit test bugs only caught at integration level.

---

### Option C: MVP (E2E Tests Only, No Infrastructure)

**What:** Write E2E tests per-feature without shared infrastructure; minimal Phase 80-81 setup.

**When:** v3.0 milestone, 4-5 weeks.

**Benefit:** Fastest time-to-value for v3.0 release; tests documented for next team.

**Cost:** 2 FTE × 5 weeks; high technical debt; test suite slow (20+ seconds), brittle, hard to maintain.

**Risk:** Test suite becomes unmaintainable; tests skipped when they break; v3.1+ testing stalled.

---

### Recommendation

**Choose Option A (Recommended)** if:
- v3.0 is critical for business (approval workflows must be rock-solid)
- Long-term portal maintenance is priority
- Team has capacity for 3 FTE for 9 weeks

**Choose Option B (Lean)** if:
- v3.0 needs faster delivery (6-7 weeks hard deadline)
- Approval workflow business logic is already validated
- Willing to accept slightly higher unit-test-level bugs

**Avoid Option C** unless v3.0 is truly one-time release (not maintained beyond this milestone).

---

## Next Steps (This Week)

1. **Get stakeholder buy-in on phase structure** (Option A/B/C)
2. **Assign QA Lead to clarify approval workflow business rules** (enable Phase 84)
3. **Reserve Tech Lead for Phase 80-81 kickoff** (WebApplicationFactory setup)
4. **Create test infrastructure repository/project structure** (ready for code)
5. **Schedule Phase 80 kickoff for next Monday** (master data validation starts)

---

## Questions for Clarification (Before Phase 80 Starts)

1. **Approval Workflow State Machine:**
   - If SrSpv rejects and SectionHead approves same deliverable, what's the final state?
   - Is there an escalation timeout (auto-approve if no SectionHead response after N days)?

2. **Approver Assignment:**
   - How are SrSpv/SectionHead determined for a coachee? (By unit? By explicit mapping? By role filter?)
   - If coachee has no assigned SrSpv, can HC still grant competency?

3. **Multi-Unit User Behavior:**
   - Can user belong to 2+ units? How is unit selected in UI?
   - All tests should include 1-2 multi-unit users to validate unit isolation.

4. **File Storage:**
   - Are evidence files stored locally (/uploads/) or in cloud (S3, Azure Blob)?
   - Tests need appropriate abstraction layer (see QA-PITFALLS.md, Pitfall 4).

5. **Notification Triggers:**
   - When does ProtonNotification get created? (Explicit call? Background job? Event handler?)
   - Synchronous or async? Tests need to know how to trigger.

---

## Appendix: Phase Definitions

**Phase 80: Master Data Validation**
- Seed KKJ, CPDP, Silabus, ProtonTracks
- Verify counts, hierarchy, query performance
- Expected: 8-10 integration tests

**Phase 81: User Management & Roles**
- Role-based auth, user import, multi-unit assignment
- Expected: 12-15 integration tests

**Phase 82: Assessment E2E**
- Happy path: Create → Assign → Exam → Grant
- Expected: 20-25 E2E tests

**Phase 83: Master Data CRUD**
- KKJ editing, Silabus editing, cascade delete
- Expected: 12-15 integration tests

**Phase 84: Coaching Proton Core E2E**
- Track assignment → Submission → Multi-role approvals → Final assessment
- Expected: 35-45 E2E tests (most complex phase)

**Phase 85: Approval Edge Cases**
- Rejection/resubmit, HC override, conditional approvals
- Expected: 10-12 E2E tests

**Phase 86: Dashboard & Reporting**
- Home, Assessment, CDP dashboards for all roles
- Expected: 10-12 E2E tests

**Phase 87: Code Cleanup & Regression**
- Remove orphaned pages, consolidate duplicates, rename strings
- Re-run all 110-150 tests to ensure no regressions
- Expected: Full regression suite + 0 new failures

---

## Sources

- [QA-TESTING-ARCHITECTURE.md](QA-TESTING-ARCHITECTURE.md) — Full testing strategy
- [QA-TESTING-SUMMARY.md](QA-TESTING-SUMMARY.md) — Research findings summary
- [QA-PITFALLS.md](QA-PITFALLS.md) — 11 pitfalls to avoid in each phase
