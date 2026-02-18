# Project Research Summary

**Project:** Portal HC KPB — v1.2 UX Consolidation
**Domain:** ASP.NET Core 8 MVC — role-filtered views, heterogeneous data merge, dashboard tab consolidation, clean feature removal
**Researched:** 2026-02-18
**Confidence:** HIGH — all four research areas grounded in direct codebase inspection; no external dependencies or speculative patterns

---

## Executive Summary

This is a restructuring milestone against an already-shipped ASP.NET Core 8 MVC portal. The four goals — (1) restricting the Assessment page to actionable items per role, (2) merging completed AssessmentSession history into the Training Records view, (3) consolidating DevDashboard and HC Reports into a single tabbed Dashboard, and (4) removing the Gap Analysis page — require no new packages, no schema migrations, and no new frameworks. Every pattern needed already exists in the codebase. The recommended approach is to apply established codebase patterns (Bootstrap 5 `nav-tabs`, server-side `@if` role gating, in-memory LINQ union ViewModel projection) to new integration points, following the build order the architecture research derives from inter-feature dependencies.

The primary architectural decision is the `UnifiedCapabilityRecord` ViewModel: rather than attempting a SQL UNION across incompatible schemas or performing the merge in Razor, both data sources are queried separately in the controller, projected to a shared DTO with a `RecordType` discriminator, merged and sorted in memory, then passed as a single typed model to the view. This is the pattern already used for `CoacheeProgressRow` and `TrackingItem` in the existing codebase. The Dashboard consolidation follows the same principle — a composite `CDPDashboardViewModel` absorbs `ReportsDashboardViewModel` and `DevDashboardViewModel` as sub-models, with role-gated population so only authorized users incur the cost of those queries.

The critical risks all share a common root cause: changes that are individually safe become breaking if sequencing is violated. Specifically — removing Completed assessments from the worker view before the history destination exists, deleting the CompetencyGap route before its hub-page links are updated, and moving `ReportsIndex` to CDPController before all cross-controller `Url.Action` calls are audited. The research is unambiguous: deletion and removal must come last, never first. A secondary risk unique to this codebase is the Admin `SelectedView` two-layer auth system, which must be verified across all five view values for every modified controller action.

---

## Key Findings

### Recommended Stack

The existing stack handles every requirement without modification. No new NuGet packages are warranted. All patterns (Bootstrap tab component, server-side role gating, EF Core in-memory projection) are already loaded and in active use throughout the codebase. The only new code artifacts are ViewModels: `UnifiedCapabilityRecord.cs`, `CDPDashboardViewModel.cs`, and two partial views (`_HCReportsPartial.cshtml`, `_DevDashboardPartial.cshtml`).

**Core technologies:**
- ASP.NET Core 8 MVC: request routing and server-side rendering — all changes are controller action modifications and view refactors; no framework change
- EF Core 8: data access — in-memory LINQ `.Concat().OrderByDescending()` across two separate queries is the established merge pattern; SQL UNION is not viable due to schema incompatibility between `AssessmentSession` and `TrainingRecord`
- Razor Views (.cshtml): all role gating done via server-side `@if (userRole == ...)` blocks — restricted content is never emitted to the DOM, consistent with existing patterns in `Progress.cshtml`, `_Layout.cshtml`, `Coaching.cshtml`
- Bootstrap 5.3 (CDN): `data-bs-toggle="tab"` for all tab UIs — already loaded in `_Layout.cshtml`; zero additional JavaScript required
- ASP.NET Identity: `UserManager.GetRolesAsync()` for role resolution — established pattern already used in both `CDPController` and `CMPController`

### Expected Features

All features restructure existing data and logic; no new capabilities are introduced. The distinction between must-have and should-have is well-established by the codebase audit.

**Must have (table stakes):**
- Worker Assessment view restricted to Open + Upcoming — completed items are noise for workers who cannot act on them
- HC/Admin Assessment: Management tab (CRUD) and Monitoring tab (oversight) — two separate intents currently conflated in a single toggle
- Unified Training Records table merging completed AssessmentSession rows with TrainingRecord rows — workers expect their full history in one place
- Source type badge (`Assessment` vs `Training`) on unified table rows — heterogeneous rows require differentiation for users to understand available actions
- Unified Dashboard: Proton Progress tab (all roles) + Assessment Analytics tab (HC/Admin only) — consolidates two currently separate pages
- Gap Analysis entry points removed — CMP Index card and CpdpProgress nav link; all other CompetencyGap assets may remain intact
- Empty states per tab where applicable
- Certificate expiry warning preserved in merged Training Records view

**Should have (differentiators — polish, not blocking):**
- Count badges on Assessment tabs (e.g., "Open (3) / Upcoming (1)")
- Persistent tab state via URL query param (`?tab=monitoring`)
- Unified table sorted most-recent-first using common `Date` field across both record types
- Conditional column rendering per row type (Score only for Assessment rows; ValidUntil only for Training rows)

**Defer (v1.3+):**
- Full paginated inline Assessment Analytics table in Dashboard — show KPI cards + "View Full Reports" link instead
- Server-side pagination for the unified history table — current data volume does not warrant it
- Admin personal assessment tab — deferred until Admin SelectedView behavior is fully verified post-Phase 08

### Architecture Approach

The system is a monolithic ASP.NET Core MVC portal with two large controllers (`CMPController` ~1840 lines, `CDPController` ~1475 lines) and EF Core direct data access. No service layer exists at current scale, and none is warranted for this milestone. All v1.2 changes are scoped to controller action modifications and view refactors — no schema migrations, no new DbSets, no new routes requiring authorization policy changes.

**Major components and their v1.2 changes:**
1. `CMPController` — Assessment action gains status filter and monitor branch; Records action gains `BuildUnifiedRecords()` helper; CompetencyGap action and GenerateIdpSuggestion helper deleted
2. `CDPController.Dashboard()` — absorbs ReportsIndex query logic and DevDashboard query logic; builds `CDPDashboardViewModel` with role-gated sub-model population
3. `UnifiedCapabilityRecord` ViewModel (new) — discriminated union DTO projecting both `AssessmentSession` (completed+passed) and `TrainingRecord` rows to a common shape with `RecordType` discriminator field
4. `CDPDashboardViewModel` (new) — composite model with `HcReports` and `DevDashboard` as typed sub-models; populated conditionally per role
5. `_HCReportsPartial.cshtml` + `_DevDashboardPartial.cshtml` (new) — partials extracted from existing standalone views for embedding in Dashboard tabs
6. `Views/CMP/Records.cshtml` and `Views/CMP/Assessment.cshtml` — updated model signatures, tab structure, and JS filter attribute mappings
7. `Views/CDP/Dashboard.cshtml` — gains Bootstrap tab nav; renders partials for HC Reports and Dev Dashboard tabs

### Critical Pitfalls

Eight pitfalls were identified across the four research areas. The five highest-severity are:

1. **History disappears when Completed is filtered from worker Assessment view** — The history destination (unified Training Records) must exist and be navigable before the Completed status filter is removed from the Assessment query. These two steps must never be in the same commit. Recovery if violated: revert the filter removal, deploy, build history page, re-attempt.

2. **Admin SelectedView ignored in new or modified actions** — The codebase has two orthogonal auth layers: `[Authorize(Roles)]` for coarse access and runtime `user.SelectedView` for view-scoped filtering. Every modified or new controller action must be manually tested across all five SelectedView values (HC, Atasan, Coach, Coachee, Admin) before the phase is marked complete. Violations are silent — wrong data renders with no error.

3. **Authorization drift when ReportsIndex moves to CDPController** — `CDPController` has only class-level `[Authorize]` (any authenticated user). The `[Authorize(Roles = "Admin, HC")]` attribute from `CMPController.ReportsIndex` must be explicitly re-declared on the destination action. Missing this exposes HC-level analytics to all authenticated users with no error or log entry.

4. **Orphaned links after CompetencyGap deletion** — Two live nav links remain after route removal: `CMP/Index.cshtml` line 72 and `CMP/CpdpProgress.cshtml` line 19. One is a hardcoded JavaScript string literal (not a tag helper), invisible to a tag-helper-only audit. Both must be updated in the same commit as the deletion.

5. **Cross-controller `Url.Action` calls break silently after ReportsIndex moves** — `Views/CMP/UserAssessmentHistory.cshtml` contains two `@Url.Action("ReportsIndex")` calls without an explicit controller argument. These silently produce 404 after the move. Must grep for the literal string `"ReportsIndex"` across all `.cshtml` files (not just tag-helper audit) before the move.

---

## Implications for Roadmap

Based on the combined research, the architecture's recommended build order maps directly to four implementation phases plus a cleanup pass. The ordering is strictly dependency-driven.

### Phase 1: Gap Analysis Removal

**Rationale:** Zero dependencies on any other phase. Pure nav link deletion and optional controller/view cleanup. Creates a clean compile baseline before other changes begin. Eliminates dead code that would create noise during Phases 2-4. The architecture research explicitly states this as the mandatory first step.

**Delivers:** CMP hub page and CpdpProgress view with no orphaned navigation elements; codebase free of CompetencyGap dead code; confirmed clean build baseline.

**Addresses:** Feature 4 (Gap Analysis clean removal). P1 priority from features prioritization matrix.

**Avoids:** Pitfall 7 (orphaned links after deletion). Requires same-commit update of `CMP/Index.cshtml` line 72 and `CMP/CpdpProgress.cshtml` line 19. Grep for literal string `CompetencyGap` across all `.cshtml` files before committing.

**Research flag:** No further research needed — full deletion surface area mapped in ARCHITECTURE.md with exact file locations and line numbers.

---

### Phase 2: Unified Training Records (UnifiedCapabilityRecord ViewModel)

**Rationale:** Must be completed and verified before Phase 3 ships. The unified Training Records view is the history destination that workers rely on once Completed items are removed from the Assessment page. This is a hard sequencing constraint: destination before source filter.

**Delivers:** `UnifiedCapabilityRecord.cs` ViewModel, `BuildUnifiedRecords()` helper in `CMPController`, updated `Records.cshtml` with type-differentiated column rendering, preserved certificate expiry warning, source badge per row, correct JS filter attribute mapping for both row types.

**Addresses:** Feature 2 (unified history table), must-have list items for merged data, conditional Score/certificate columns, expiry warning preservation.

**Avoids:** Pitfall 3 (column rendering null breaks — use `RecordType` discriminator, switch on type in Razor, never map fields that don't exist on the source type); Pitfall 4 (broken pagination — in-memory sort acceptable at this scale, document assumption with `.Take(500)` safety cap per source); Pitfall 8 (JS tab filter breaks with mixed model — define `data-kategori` attribute mapping for AssessmentSession rows before writing any Razor).

**Research flag:** No further research needed — `UnifiedCapabilityRecord` shape, query patterns, and column rendering matrix are fully specified in ARCHITECTURE.md Integration Point 2.

---

### Phase 3: Assessment Page Role Filter

**Rationale:** Ships only after Phase 2 is verified. Workers must be able to reach completed assessment history via Training Records before Completed is removed from the Assessment list. Independent of Phase 4 — can run in parallel with two developers since they touch different controllers and views with no shared integration points.

**Delivers:** Assessment page filtered to Open + Upcoming only for workers (RoleLevel >= 5); HC/Admin gains `?view=monitor` as a third view alongside existing Management view; callout/info link on worker personal view pointing to Training Records for history; empty states per tab.

**Addresses:** Feature 1 (Assessment page split by role), all Assessment table-stakes items, HC/Admin Management + Monitoring tabs, empty states.

**Avoids:** Pitfall 1 (history must exist first — Phase 2 must be verified before this phase ships); Pitfall 2 (Admin SelectedView — five-SelectedView manual test mandatory before phase completion); Anti-Pattern 3 from ARCHITECTURE.md (add `monitor` as third value to existing `view` param, not a new bool flag, to preserve existing bookmarks and hard-coded links).

**Research flag:** No further research needed — query changes and view structure are fully specified in ARCHITECTURE.md Integration Point 1.

---

### Phase 4: Dashboard Consolidation (CDPDashboardViewModel)

**Rationale:** Most complex phase; runs after Phases 1-3 are verified. Absorbs two standalone pages into a tabbed unified Dashboard. The architecture recommends keeping standalone pages (`ReportsIndex`, `DevDashboard`) alive until tabs are verified, then redirecting or removing them in a subsequent cleanup step.

**Delivers:** `CDPDashboardViewModel.cs`, `_HCReportsPartial.cshtml`, `_DevDashboardPartial.cshtml`, rewritten `CDPController.Dashboard()` with role-gated sub-model population, updated `Views/CDP/Dashboard.cshtml` with Bootstrap tab nav and partial rendering, removal of standalone Dev Dashboard nav item from `_Layout.cshtml` (post-verification only).

**Addresses:** Feature 3 (Unified Dashboard with role-scoped tabs), Assessment Analytics tab (HC/Admin only — absent from DOM for other roles), Proton Progress tab (all roles, exact DevDashboard content).

**Avoids:** Pitfall 5 (cross-controller links — audit all `asp-action="ReportsIndex"` and literal `"ReportsIndex"` strings before moving; fix both `Url.Action` calls in `UserAssessmentHistory.cshtml`); Pitfall 6 (authorization drift — explicitly re-declare `[Authorize(Roles = "Admin, HC")]` on HC Reports content; test as Coachee-role user expecting 403); Pitfall 2 (Admin SelectedView — five-SelectedView test on modified Dashboard action); Anti-Pattern 4 from ARCHITECTURE.md (gate sub-model population behind role checks — do not run ReportsIndex aggregate queries for Coachee/Coach roles).

**Research flag:** No further research needed — `CDPDashboardViewModel` shape, partial view structure, role-gating conditions, and tab HTML structure are fully specified in ARCHITECTURE.md Integration Point 3.

---

### Phase 5: Cleanup and Verification Pass

**Rationale:** After Phase 4 tabs are verified, superseded standalone pages can be removed or redirected. This is a low-risk cleanup pass, not a feature phase. Intentionally deferred to avoid removing navigation during verification windows.

**Delivers:** Optional redirect stubs from `CMPController.ReportsIndex` and `CDPController.DevDashboard` to their new Dashboard tab URLs; deletion of `DashboardViewModel.cs` if no references remain; removal of `GetPersonalTrainingRecords()` private method from CMPController if fully replaced by `BuildUnifiedRecords()`.

**Addresses:** Technical debt noted in ARCHITECTURE.md Anti-Pattern 2 — mark duplicated query logic with `// TODO(v1.3): extract to shared service` comment during Phase 4; execute extract in v1.3.

**Research flag:** No research needed — pure cleanup with bounded, documented scope.

---

### Phase Ordering Rationale

- Phase 1 is unconditionally first — zero dependencies, creates clean baseline, fastest to verify.
- Phase 2 must precede Phase 3 — the sequencing constraint between history destination and source filter is non-negotiable per Pitfall 1.
- Phase 3 and Phase 4 are independent and can run in parallel with sufficient developer capacity.
- Phase 4 is last among feature phases because it is the most cross-cutting and benefits from the clean compile state established by Phases 1-3.
- Phase 5 is explicitly deferred post-verification to protect navigation paths during the stabilization window.

### Research Flags

Phases needing deeper research during planning:
- None. All four active phases have fully specified implementation paths grounded in direct codebase inspection. The patterns are established; the surface area is mapped; the ViewModel contracts are defined at field level.

Phases with standard/established patterns (skip research-phase):
- All phases. Bootstrap nav-tabs, server-side `@if` role gating, in-memory LINQ union, EF Core async queries are all in active use throughout the portal. No third-party API integration, no new framework, no niche domain requiring external research.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All technologies verified against `HcPortal.csproj` and `_Layout.cshtml`; no speculative recommendations; no new packages required |
| Features | HIGH | All features grounded in direct view and controller inspection; existing data models verified to support every requirement without migration |
| Architecture | HIGH | Component boundaries, ViewModel shapes, build order, and anti-patterns derived from direct inspection of 1840-line CMPController and 1475-line CDPController; nothing theoretical |
| Pitfalls | HIGH | All 8 pitfalls traced to specific file locations and line numbers; recovery strategies confirmed against codebase structure; no generic warnings |

**Overall confidence:** HIGH

### Gaps to Address

- **Admin SelectedView behavior on unified Training Records:** The correct behavior when Admin is in Coachee SelectedView and accesses unified Training Records is not explicitly specified in the research. Audit the current `Records()` action behavior for this case before Phase 2 implementation and define the contract (own records only vs all records regardless of SelectedView).

- **Assessment Analytics tab inline scope:** FEATURES.md specifies KPI cards + chart + "View Full Reports" link (not the full paginated table). ARCHITECTURE.md specifies `ReportsDashboardViewModel` as the sub-model (which includes the full paginated data). These are compatible but the exact inline content scope should be confirmed before Phase 4 implementation to avoid over-building the Dashboard tab.

- **CompetencyGap action retention vs deletion:** FEATURES.md recommends retaining the `CompetencyGap()` action as a safety measure; ARCHITECTURE.md lists it for explicit deletion. Resolution: delete the action per ARCHITECTURE.md and add a temporary `RedirectToAction("CpdpProgress", "CMP")` stub for one release cycle to handle bookmarked URLs, satisfying both concerns.

---

## Sources

### Primary (HIGH confidence — direct codebase inspection, 2026-02-18)

- `Controllers/CMPController.cs` (1840 lines) — Assessment, Records, ReportsIndex, CompetencyGap, CpdpProgress action patterns and query logic
- `Controllers/CDPController.cs` (1475 lines) — Dashboard, DevDashboard, Coaching role-gating patterns
- `Views/CMP/Assessment.cshtml` — existing tab structure, canManage pattern, JS status filter
- `Views/CMP/Records.cshtml` — existing JS category tab filter, TrainingRecord model usage
- `Views/CDP/Dashboard.cshtml`, `Views/CDP/DevDashboard.cshtml` — current standalone page structure
- `Views/Shared/_Layout.cshtml` — Bootstrap 5.3 CDN load, jQuery 3.7.1 CDN, nav item locations confirmed
- `Views/CMP/Index.cshtml` line 72, `Views/CMP/CpdpProgress.cshtml` line 19 — CompetencyGap cross-links confirmed
- `Views/CMP/UserAssessmentHistory.cshtml` lines 11 and 201 — implicit-controller `Url.Action` calls confirmed
- `Models/AssessmentSession.cs`, `Models/TrainingRecord.cs` — full field inventory for ViewModel projection design
- `Models/ApplicationUser.cs` — SelectedView, RoleLevel fields
- `Models/UserRoles.cs` — role constants
- `Models/DashboardViewModel.cs`, `Models/ReportsDashboardViewModel.cs`, `Models/DevDashboardViewModel.cs` — sub-model shapes for CDPDashboardViewModel
- `HcPortal.csproj` — package versions confirmed

### Secondary (HIGH confidence — stable public API documentation)

- Bootstrap 5.3 tab component (`data-bs-toggle="tab"`) — stable API, no version-specific gotchas in 5.3.x
- ASP.NET Core 8 Razor `@if` role gating — established pattern, in active use in multiple views throughout the portal

---

*Research completed: 2026-02-18*
*Ready for roadmap: yes*
