# Research Summary — Portal HC KPB v3.0 QA & Feature Completion

**Project:** Portal HC KPB v3.0 (ASP.NET Core 8 MVC)
**Domain:** Enterprise Portal — Comprehensive QA Testing, Code Cleanup, and Feature Completion
**Researched:** 2026-03-01
**Confidence:** HIGH

---

## Executive Summary

Portal HC KPB v3.0 is a **brownfield QA & consolidation milestone**, not a new feature release. The portal has existed through v2.6 with core features (assessments, coaching, master data) largely implemented; v3.0's role is to systematically test all features via end-to-end flows, eliminate dead code from previous cleanup attempts, complete the IDP Plan page, and rename "Proton Progress" → "Coaching Proton" throughout the codebase. The research strongly recommends a **pragmatic, phase-based approach**: start with code analysis (NetAnalyzers + SonarQube) to establish a baseline of technical debt, then build lightweight unit and functional tests (xUnit + WebApplicationFactory) covering critical flows, then execute manual QA against a detailed checklist, and finally address code cleanup and rename tasks. This ordering ensures testing occurs before cleanup (reducing risk of deleting code tests depend on), and prioritizes high-value QA activities over UI automation (which would be slow and brittle on a brownfield portal).

**Key risks identified:** (1) Dead code cleanup breaking dependent code if "Find All References" is skipped; (2) Authorization gaps (workers accessing admin features) if role-based tests don't cover all role combinations; (3) Test flakiness from non-deterministic seeding or concurrency issues in auto-save logic. Prevention strategies are concrete and actionable for each.

---

## Key Findings

### Recommended Stack

The research recommends a **minimal, Microsoft-endorsed stack** requiring **no new NuGet dependencies** beyond testing frameworks (which the project likely already uses or will add):

**Core testing frameworks:**
- **xUnit 2.6+** for unit testing — Microsoft's official .NET Core recommendation, default in all Microsoft testing docs, strong DI support
- **WebApplicationFactory** (built into ASP.NET Core 8) for functional/integration tests — enables end-to-end workflow testing via TestServer without requiring IIS
- **Xunit.DependencyInjection 8.9+** for test DI configuration — reduces boilerplate in large test suites

**Code quality & analysis tools:**
- **Microsoft.CodeAnalysis.NetAnalyzers 8.0+** — Roslyn-based static analysis, successor to deprecated FxCopAnalyzers, finds dead code and unsafe patterns, built into .NET SDK
- **StyleCop.Analyzers 1.2+** — code style consistency, identifies naming violations and documentation gaps
- **SonarQube Community 9.9 LTA** — enterprise-grade code quality dashboard, detects duplications and security hotspots, free community edition
- **Bogus 35.3+** for test data generation (optional, but recommended for realistic scenarios)
- **EF Core In-Memory Database 8.0+** (built-in) for test isolation

**Development tools:**
- `dotnet test` (CLI) — already available
- Visual Studio Test Explorer — built-in IDE integration
- Coverlet for code coverage measurement
- SonarScanner CLI for SonarQube integration

**Key Finding:** Stack is **lean and testable**. No commercial tools required. Analysis can start immediately with `dotnet build /p:AnalysisLevel=latest` to identify warnings.

---

### Expected Features

v3.0 QA focuses on verification of **existing features** (built in v2.0-v2.6) and **one new feature** (IDP Plan page).

**Must-have features (table stakes) — all need verification:**
1. **Assessment End-to-End Flow** (core product) — create → assign → schedule → exam → auto-save → session resume → results → history
2. **Coaching Proton Workflow** (core product) — coach mapping → coaching sessions → evidence upload → HC approval → deliverable progress tracking
3. **Master Data CRUD** (operational) — KKJ Matrix, CPDP Items, Proton Silabus, Training Records all editable in Kelola Data hub
4. **User Role Authorization** (security-critical) — Worker, Coach, HC, Admin roles enforce access gates consistently
5. **Dashboard / Home Pages** — IDP stats, assessment counts, coaching progress display correctly per role
6. **Data Export** (Admin/HC feature) — Excel/PDF exports of assessments, coaching reports, audit logs

**Should-have features (competitive advantages) — new or incomplete:**
1. **IDP Plan Page** (NEW) — coachee dashboard showing assigned silabus items, guidance PDFs, real-time progress %; integrate with coaching evidence tracking
2. **Audit Log** (NEW) — admin can see user actions logged; compliance requirement
3. **Live HC Monitoring** (v2.1) — HC dashboard auto-refreshes exam progress; verify polling mechanism works
4. **Assessment History** (v2.2) — workers see all past attempts with timestamps and attempt numbers

**Anti-features to eliminate or ignore:**
1. **Selenium/Playwright UI Tests** — defer to v3.1+; too slow/brittle for brownfield; manual QA + code analysis better ROI
2. **CMP/ProtonMain Page** — removed in v2.6; verify no orphaned links remain
3. **Duplicate Admin CRUD Paths** — CMP had redundant ManageQuestions; Admin panel is canonical; verify CMP versions deleted
4. **"Proton Progress" terminology** — rename to "Coaching Proton" throughout (UI, comments, docs)

---

### Architecture Approach

The portal uses **ASP.NET Core 8 MVC with class-level authorization**. Two main controllers (CMPController ~1840 lines, CDPController ~1475 lines) handle assessment and coaching workflows respectively. Data model has ~20 DbSet entities, with key tables: AssessmentSessions, TrainingRecords, ProtonDeliverableProgresses, CoachingSessions, AspNetUsers (ApplicationUser with FullName, NIP, RoleLevel). Authorization pattern uses `[Authorize]` at controller class level with per-action role attributes like `[Authorize(Roles = "Admin, HC")]`.

**Major components & their responsibilities:**
1. **CMPController** — Assessment lifecycle (create, assign, schedule, exam submit), Training Records management, Competency Gap reporting (scheduled for deletion), HC Reports dashboard
2. **CDPController** — CDP Dashboard, Dev Dashboard (for supervisors), Coaching workflow (mapping, sessions, approval), Proton deliverables and final assessment
3. **ApplicationDbContext** — EF Core with 20+ entities; includes AssessmentSessions, ProtonDeliverableProgresses, CoachingSessions, TrainingRecords, KkjMatrices
4. **ApplicationUser** — custom ASP.NET Identity user; has RoleLevel (integer hierarchy), NIP, SelectedView (for role-switching UI), multi-unit membership
5. **Razor Views** — Bootstrap 5 + Chart.js for dashboards; Assessment/Records tabs, Coaching evidence upload, Hub-based master data management (Kelola Data)

**Data flow pattern:** Controllers query EF Core context directly (no separate service layer); views receive typed ViewModels (DashboardViewModel, CoachingViewModel, etc.); POST actions return redirects or JSON for AJAX. Test isolation uses EF Core in-memory database per test class.

---

### Critical Pitfalls

**Five high-priority risks to prevent:**

1. **Null Reference Exceptions from Reckless Dead Code Cleanup** — Code analyzers flag methods as "unused" but they're called via reflection, LINQ.Invoke, or interface implementations. Developers delete without checking "Find All References", tests break with NullReferenceException at runtime.
   - **Prevention:** Always "Find All References" (Ctrl+Shift+F) before deleting anything. Comment out first, run tests, then delete. Two-person review for method/class deletion.

2. **Test Flakiness from Non-Deterministic Data Seeding** — Bogus generates random test data each run; if seeding depends on data not guaranteed to exist, tests pass sometimes, fail other times. CI/CD becomes unreliable ("just rerun it").
   - **Prevention:** Use fixed test data for critical tests (Coach "John" always mapped to Coachee "Alice"). Seed idempotently with `if (!db.Coaches.Any()) { ... }`. Isolate test DB per test class with unique in-memory names.

3. **Authorization Gaps — Workers Access Admin Features** — Controllers have `[Authorize]` but missing role checks. Any authenticated user (even Worker) can GET /Admin/ManageAssessments if endpoint only checks `[Authorize]` not `[Authorize(Roles = "Admin, HC")]`.
   - **Prevention:** Write explicit 403 tests for every protected endpoint per role. Create authorization matrix documenting who can do what. Use consistent pattern: class-level `[Authorize(Roles = "...")]` on AdminController. SonarQube flags bare `[Authorize]` as medium risk.

4. **Database Migration Drift** — Tests run against in-memory DB with stale schema. New migration adds NOT NULL column, but test seed doesn't set it. Tests pass locally, fail in CI/CD where migrations run.
   - **Prevention:** Use `db.Database.Migrate()` (not `EnsureCreated()`) in test setup to apply all pending migrations. Test migrations locally before commit: `dotnet ef migrations add MyMig; dotnet test; dotnet ef database update 0; dotnet ef database update` to verify idempotency.

5. **Concurrency in SaveAnswer Auto-Save** — Worker A and Worker B save answers simultaneously, race condition hits unique constraint violation or duplicate records. Tests don't simulate concurrent load.
   - **Prevention:** Add UNIQUE constraint on (SessionId, QuestionId) via migration. Implement UPSERT pattern: ExecuteUpdateAsync + insert fallback. Load test with concurrent simulations (50 users simultaneously).

---

## Implications for Roadmap

Research suggests **six sequential phases** for v3.0, ordered by dependency and risk mitigation:

### Phase 1: Code Analysis Baseline (Week 1)
**Rationale:** Identify technical debt BEFORE testing or cleanup. Establish baseline of dead code, null safety issues, and naming inconsistencies. Informs cleanup priorities and test coverage areas.
**Delivers:** SonarQube dashboard with code smells, dead code report; NetAnalyzers baseline scan; priority list of issues to address
**Stack elements used:** Microsoft.CodeAnalysis.NetAnalyzers, StyleCop.Analyzers, SonarQube Community (CLI)
**Avoids pitfall #1:** By documenting what analyzers claim is dead, developers can validate before deleting

---

### Phase 2: Unit Test Framework & Service Tests (Weeks 2-3)
**Rationale:** Build test skeleton for critical service logic (Assessment creation/submission, Coaching approval, IDP Plan data). Small, fast tests give developers confidence before functional testing.
**Delivers:** PortalHC.Tests project structure; 60-80 unit tests covering services, validation, business logic; test data builders
**Uses:** xUnit, Xunit.DependencyInjection, EF Core in-memory DB, Bogus
**Implements:** Assessment service (create, assign, score), Coaching service (evidence, approval), IDP Plan builder logic
**Avoids pitfalls #2 & #5:** Fixed test data + UNIQUE constraint testing in unit layer

---

### Phase 3: Functional/Integration Tests (Weeks 3-4)
**Rationale:** End-to-end workflow tests (Worker takes assessment → sees results; Coach maps coachee → uploads evidence → HC approves). Catch integration bugs, authorization gaps, data persistence issues.
**Delivers:** 15-20 functional tests using WebTestFixture; critical workflow paths (Assessment E2E, Coaching approval, IDP Plan display); 403 authorization tests per role
**Uses:** WebApplicationFactory, WebTestFixture, test data seeding via EF Core
**Implements:** Authorization matrix testing; verify role-based view gates
**Avoids pitfall #3:** Explicit 403 tests for Worker access to /Admin routes

---

### Phase 4: Manual QA Execution (Week 4-5)
**Rationale:** Comprehensive manual testing of all features via checklist. Catch UI/UX issues, workflow edge cases, data display accuracy that automated tests miss.
**Delivers:** QA checklist executed (Assessment, Coaching, Master Data, Authorization, IDP Plan); bug report log; regression verification
**Testing approach:** Test each feature matrix (create → assign → execute → results) per role; verify Kelola Data hub cards load
**Avoids pitfall #4:** Database migrations applied before test DB setup; schema verified

---

### Phase 5: IDP Plan Page Development & Testing (Weeks 4-5, parallel)
**Rationale:** NEW feature requires development + integrated testing. Shows assigned silabus items, downloadable guidance, real-time progress % linked to coaching evidence.
**Delivers:** /CDP/IdpPlan or similar route; Razor view displaying coachee's deliverables; integration with coaching progress tracking
**Implements:** Query pulling assigned silabus + coaching evidence for coachee; calculate completion %; PDF download links
**Uses:** Existing EF Core models (ProtonDeliverableProgresses, ProtonSilabus); MVC controller action
**Avoids pitfall #5:** Functional tests verify IDP progress updates when coaching evidence uploaded

---

### Phase 6: Code Cleanup & Rename (Weeks 5-6+)
**Rationale:** Dead code removal (CompetencyGap action, unused methods) and "Proton Progress" → "Coaching Proton" rename. Only after testing ensures no tests depend on code being deleted.
**Delivers:** Dead code removed (verified by analyzers + tests pass); "Coaching Proton" terminology consistent in UI, views, comments
**Cleanup tasks:** Delete CompetencyGap() action, delete CompetencyGapViewModel, remove CMP/ProtonMain references, rename UI labels
**Avoids pitfall #1:** Tests already exist to catch deletion breakage

---

### Phase Ordering Rationale

1. **Code Analysis first** — informs what to test, what to clean up; risk mitigation
2. **Unit tests early** — fast feedback, foundation for functional tests
3. **Functional tests before manual QA** — automated tests catch integration issues, allowing manual QA to focus on UX
4. **IDP Plan parallel with manual QA** — feature development independent of other testing; shares same test infrastructure
5. **Cleanup last** — only after testing verifies what code is truly used

---

### Research Flags

**Phases needing deeper research during planning:**
- **Phase 5 (IDP Plan):** Requires validation of exact display requirements (silabus hierarchy, PDF organization, progress calculation formula). Recommend `/gsd:research-phase` for 30min to confirm UI/UX expectations with stakeholders.
- **Phase 4 (Manual QA Authorization):** 10+ roles (Admin, HC, SrSpv, SectionHead, Coach, Coachee, Worker) with overlapping permissions. Authorization matrix needs confirmation. Recommend clarifying "who can approve deliverable?" per role during planning.

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Code Analysis):** NetAnalyzers + SonarQube are well-documented, standard patterns. No research needed.
- **Phase 2 (Unit Tests):** xUnit + EF Core in-memory DB are Microsoft-endorsed, well-documented. Standard setup.
- **Phase 3 (Functional Tests):** WebApplicationFactory is ASP.NET Core standard. No research needed beyond STACK.md.
- **Phase 6 (Code Cleanup):** Straightforward deletions guided by analyzer reports. No research needed.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Stack** | HIGH | Microsoft official docs + proven frameworks (xUnit 2.6+, WebApplicationFactory, NetAnalyzers); no experimental or niche tools; version compatibility matrix validated |
| **Features** | HIGH | Directly sourced from PROJECT.md scope + v2.1-v2.6 milestone roadmaps; all features exist in codebase or are clearly scoped (IDP Plan) |
| **Architecture** | HIGH | Direct inspection of codebase: CMPController (1840 lines), CDPController (1475 lines), EF Core models examined; authorization pattern verified; data flow traced |
| **Pitfalls** | HIGH | Based on v2.6 cleanup experience (10+ dead code removals), codebase architectural review, multi-role system complexity; prevention strategies tested and proven patterns |

**Overall confidence:** **HIGH** — All four research areas backed by official documentation, codebase inspection, and proven enterprise patterns.

### Gaps to Address

1. **Authorization Matrix Clarity** — "Who exactly can approve a deliverable?" (Admin, HC, SrSpv, SectionHead, all of above?). Document per role during Phase 4 planning.
2. **IDP Plan Display Spec** — Exact silabus hierarchy display, PDF organization, progress calculation formula. Validate with Product during Phase 5 kickoff.
3. **Concurrent Load Expectations** — What's the expected simultaneous exam taker load? Affects SaveAnswer UPSERT testing (Phase 2-3).
4. **Code Coverage Goals** — Researchers recommend 60-70% on services, 40-50% on controllers. Confirm acceptable targets during Phase 2 kickoff.

---

## Sources

### Primary (HIGH confidence)
- **STACK.md** — Microsoft Learn (xUnit, WebApplicationFactory, EF Core testing), official .NET testing guides, Microsoft.CodeAnalysis.NetAnalyzers documentation
- **FEATURES.md** — `.planning/PROJECT.md` scope, v2.1-v2.6 milestone roadmaps (archived), feature dependency map verified against codebase
- **ARCHITECTURE.md** — Direct codebase inspection (Controllers/CMPController.cs, Controllers/CDPController.cs, Models/ApplicationUser.cs, EF Core DbContext)
- **PITFALLS.md** — v2.6 cleanup experience, codebase architectural patterns, multi-role authorization system review, EF Core best practices (Microsoft Learn + SQL Server)

### Secondary (MEDIUM confidence)
- BrowserStack: C# testing frameworks 2026 — consensus on xUnit + WebApplicationFactory for .NET Core
- StyleCop.Analyzers GitHub — community-driven code style enforcement, 21M+ downloads
- SonarQube .NET integration docs — free community edition capabilities

---

**Research completed:** 2026-03-01
**Ready for roadmap creation:** YES
**Next step:** Use phase suggestions and research flags to inform REQUIREMENTS.md and detailed phase planning
