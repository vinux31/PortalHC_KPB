# QA Testing Stack Research Summary

**Project:** Portal HC KPB v3.0 — Full QA & Feature Completion
**Researched:** 2026-03-01
**Overall Confidence:** HIGH

---

## Executive Summary

This research defines a **pragmatic, brownfield-focused QA testing approach** for Portal HC KPB. The stack separates unit testing (xUnit for services), functional testing (WebApplicationFactory for workflows), and code quality analysis (Roslyn analyzers + SonarQube for cleanup). Test data seeding uses EF Core migrations. **Key finding:** No new NuGet packages required beyond testing frameworks. Skip UI automation (Selenium/Playwright) for now — code analysis + manual QA provides better ROI on brownfield applications.

### Key Recommendations

| Area | Recommendation | Why |
|------|-----------------|-----|
| **Unit Testing Framework** | xUnit 2.6+ | Microsoft standard for .NET Core; built-in DI; used in EF Core tests |
| **Functional Testing** | WebApplicationFactory (.NET 8 built-in) | Integrated with ASP.NET Core; TestServer; in-memory DB seeding |
| **Code Quality** | NetAnalyzers (free) + StyleCop + SonarQube | Find dead code, null safety, architectural issues; free tools sufficient |
| **Test Data** | EF Core migrations + Bogus | Deterministic seeding; reproducible across runs |
| **UI Testing** | Manual QA checklist (not Selenium/Playwright) | Brownfield codebase; better ROI from analysis tools now; plan automation v3.1+ |
| **Coverage Target** | 60-70% services; 40-50% controllers | Don't over-engineer; focus on critical paths and business logic |

---

## Key Findings

### Technology Stack

1. **Zero new NuGet dependencies** beyond testing frameworks
2. **Existing infrastructure** (EF Core, ASP.NET Core MVC) already supports all needed patterns
3. **Code quality tools are free** (NetAnalyzers, StyleCop, SonarQube Community)
4. **Testing pattern** is pyramidal: many unit tests, fewer integration tests, fewest functional tests

### Critical Pitfalls Identified

| Pitfall | Impact | Prevention |
|---------|--------|-----------|
| Dead code cleanup breaks tests | **CRITICAL** | Use "Find All References" before deletion |
| Authorization gaps not tested | **CRITICAL** (security) | Write 403 tests for every protected route |
| Test data flakiness (non-deterministic) | **HIGH** (CI/CD fails) | Use fixed test data, isolate DB per test |
| Concurrency in SaveAnswer | **HIGH** (production bug) | UNIQUE constraint + UPSERT pattern |
| Test fixture shared state | **MEDIUM** (intermittent failures) | Fresh DB per test class with unique names |

### Feature Coverage

**Table stakes (must test):**
- Assessment creation → assignment → exam → results (end-to-end)
- Coaching Proton workflow (mapping → session → approval)
- Master data CRUD (KKJ, CPDP, Silabus)
- Authorization per role (Admin, HC, Coach, Worker)
- Data exports (Excel, PDF)

**Differentiators (should test):**
- IDP Plan page (NEW feature)
- Session resume after disconnect (v2.1)
- Auto-save during exam (v2.1)
- Live monitoring (HC real-time progress)
- Audit log (NEW feature)

**Anti-features (don't build):**
- Selenium/Playwright UI tests (defer to v3.1+)
- Full integration test suite on real DB
- Over-engineered test data

---

## Implementation Roadmap

### Week 1: Code Analysis Baseline
```bash
dotnet build /p:AnalysisLevel=latest
dotnet sonarscanner begin /k:"PortalHC"
```
- Identify dead code, null safety issues, inconsistencies
- Create cleanup priority list
- Fix CRITICAL warnings first (null refs, unused methods)

### Week 2-3: Unit & Functional Test Foundation
- Create `PortalHC.Tests` project (xUnit)
- Setup `WebTestFixture` for integration tests
- Write 10-15 unit tests for core services
- Write 10-15 functional tests for workflows
- Target: 60-70% coverage on services

### Week 4: Manual QA Execution
- Use feature checklist (FEATURES.md) to drive testing
- Verify Assessment flow end-to-end
- Verify Coaching Proton workflow
- Verify Authorization per role
- Document bugs found

### Week 5+: Code Cleanup & IDP Development
- Apply code analysis findings
- Remove dead code safely (test after each deletion)
- Rename "Proton Progress" → "Coaching Proton"
- Develop IDP Plan page
- Add Audit Log feature

---

## Files Created

| File | Purpose | Roadmap Use |
|------|---------|-----------|
| **STACK.md** | Technology recommendations with installation & patterns | Tech decisions, tool selection |
| **FEATURES.md** | Feature landscape with testing checklist | QA scope, manual test cases |
| **PITFALLS.md** | Domain pitfalls to avoid | Risk mitigation, test design |
| **QA-TESTING-SUMMARY.md** | This file — executive summary | Phase planning |

---

## Confidence Assessment

| Area | Confidence | Reason |
|------|------------|--------|
| **Stack** | HIGH | Microsoft official docs, current frameworks (xUnit, WebApplicationFactory), verified patterns |
| **Features** | HIGH | Extracted from PROJECT.md scope; aligned with v2.1-v2.6 delivered features |
| **Architecture** | HIGH | Code analysis tools (NetAnalyzers, SonarQube) are industry-standard; patterns documented |
| **Pitfalls** | HIGH | Based on codebase review (10+ dead code removals), architectural complexity (multi-role system), known issues from previous milestones |
| **Brownfield Approach** | MEDIUM | No UI automation recommended (Selenium/Playwright deferred); focused on code analysis + manual QA; requires Phase 3.0 validation |

---

## Gaps & Open Questions

### Gaps Requiring Phase-Specific Research

1. **Exact scope of IDP Plan page** — Current research assumes coachee dashboard + silabus items. Needs detailed requirements.
2. **Approval workflow for Coaching** — Who approves (SrSpv? SectionHead? HC only?)? Research doesn't finalize all role combinations.
3. **Audit Log implementation** — What events to log? How detailed? Defer to phase planning.
4. **Performance targets** — What's acceptable response time for Admin dashboard with 10K records? Needs load testing plan.

### Topics for Phase 3.0 Detailed Planning

- [ ] Define authorization matrix (who can do what)
- [ ] Design IDP Plan page schema
- [ ] Decide on Audit Log scope and retention
- [ ] Plan code cleanup priority order
- [ ] Schedule load testing for SaveAnswer concurrency
- [ ] Define code coverage metrics (per component)

---

## Next Steps

1. **Review this research** with v3.0 phase team
2. **Create .editorconfig** in project root (enforces StyleCop)
3. **Run baseline code analysis** (Week 1 of phase)
4. **Create PortalHC.Tests project** with WebTestFixture
5. **Write first 10 unit tests** (services)
6. **Execute manual QA checklist** (FEATURES.md)

---

## Recommendations for Phase 3.0 Execution

### DO

- Start with code analysis baseline (find what to clean up)
- Write tests as you fix bugs (test-driven debugging)
- Use [Trait] markers to separate unit/integration/functional tests
- Run tests frequently (dotnet test on every commit)
- Keep unit tests under 100ms each
- Document authorization decisions in code

### DON'T

- Build Selenium/Playwright UI tests yet (defer to v3.1+)
- Assume all "dead code" analyzer findings are correct (verify first)
- Skip authorization testing for "edge cases" (test all roles)
- Use random test data (Bogus) for critical test scenarios
- Run full suite in parallel if tests share DB (mark as [Collection("Sequential")])

---

## Sources & References

### Official Microsoft Documentation
- [Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Testing ASP.NET Core MVC Apps](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/test-asp-net-core-mvc-apps)
- [EF Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [WebApplicationFactory Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

### Code Quality References
- [SonarQube .NET Integration](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/dotnet-environments/getting-started-with-net)
- [Roslyn Analyzers Overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)
- [StyleCop.Analyzers GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)

### Testing Frameworks
- [xUnit.net Documentation](https://xunit.net/docs/getting-started/netcore)
- [Bogus Fake Data Generator](https://github.com/bchavez/Bogus)

### Codebase References
- `.planning/PROJECT.md` — Scope for v3.0
- `.planning/milestones/v2.1-ROADMAP.md` — Auto-save, session resume features
- `.planning/milestones/v2.3-ROADMAP.md` — Master data CRUD
- `.planning/milestones/v2.4-ROADMAP.md` — Coaching workflow
- `.planning/milestones/v2.5-ROADMAP.md` — Authorization, hub structure
- `.planning/milestones/v2.6-ROADMAP.md` — Dead code removal

---

## Summary: What the Roadmap Should Include

Based on this research, Phase 3.0 roadmap should:

1. **Establish QA Testing Infrastructure**
   - Create test projects (Unit, Integration, Functional)
   - Setup WebTestFixture with deterministic seeding
   - Configure code analysis tools (NetAnalyzers, StyleCop, SonarQube)
   - Document authorization matrix

2. **Execute Code Analysis & Cleanup**
   - Run baseline scan (find dead code, null safety issues)
   - Remove truly dead code (verify before deleting)
   - Fix critical warnings (null refs, missing role checks)
   - Rename "Proton Progress" → "Coaching Proton"

3. **Build Test Foundation**
   - Write unit tests for core services (Assessment, Coaching, IDP)
   - Write functional tests for workflows (Assessment flow, Coaching workflow)
   - Verify authorization via 403 tests
   - Target 60-70% coverage on services

4. **Conduct Manual QA**
   - Follow checklist in FEATURES.md
   - Verify all table-stakes features work end-to-end
   - Log and triage bugs found
   - Fix critical issues

5. **Deliver New Features**
   - Develop IDP Plan page (dashboard + silabus + guidance downloads)
   - Add Audit Log feature (who changed what, when)
   - Verify features integrate with existing flows

6. **Maintain Quality**
   - Enforce code analysis in CI/CD (fail on critical warnings)
   - Keep test suite running (dotnet test on every commit)
   - Monitor test coverage trend

---

**Research completed:** 2026-03-01
**Confidence Level:** HIGH
**Ready for Phase 3.0 Roadmap Creation**

---

## Quick Reference: Key Commands

```bash
# Code Analysis
dotnet build /p:AnalysisLevel=latest /p:EnforceCodeStyleInBuild=true
dotnet sonarscanner begin /k:"PortalHC"
dotnet build
dotnet sonarscanner end

# Testing
dotnet test                              # Run all tests
dotnet test --filter "Category=Unit"     # Run only unit tests
dotnet test /p:CollectCoverageMetrics=true  # With coverage
dotnet test --watch                      # Watch mode

# Project Setup
dotnet new xunit -n PortalHC.Tests
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

---

**End of Research Summary**
