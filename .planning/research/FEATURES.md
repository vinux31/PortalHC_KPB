# Feature Research: v1.2 UX Consolidation

**Domain:** HR Portal UX consolidation — role-filtered views, merged heterogeneous history tables, role-scoped dashboard tabs, clean feature removal
**Researched:** 2026-02-18
**Confidence:** HIGH (grounded in existing codebase; UX patterns verified against codebase behavior)

---

## Context: What Already Exists

This milestone refactors four areas of an already-shipped product. The data models, controller logic, and authorization are in place. Work is pure UX restructuring — no new data, no new migrations.

| Already Exists | What Changes in v1.2 |
|---|---|
| `AssessmentSession` with Status: Open / Upcoming / Completed | Page splits by role: workers see only Open+Upcoming; HC/Admin get Management + Monitoring tabs |
| `TrainingRecord` (manual entries, category tabs) | Merged with `AssessmentSession` completed records into one unified table, type-differentiated columns |
| `DevDashboard` (Proton Progress, Spv-scoped) | Becomes tab 1 of a Unified Dashboard; Assessment Analytics becomes tab 2 (HC/Admin only) |
| `CompetencyGap` radar chart page at CMP/CompetencyGap | Removed. Entry point and nav links deleted. No data model changes. |

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Data Dependency |
|---------|--------------|------------|-----------------|
| **Workers see only actionable assessments** | Showing completed items to Coachee/Coach is noise — they have nothing to act on. Standard list-page UX: filter to items requiring attention. | LOW | `AssessmentSession.Status` already exists. Controller already separates personal vs manage view. Gate needs to filter to "Open" + "Upcoming" only for RoleLevel >= 5. |
| **HC/Admin get separate Management tab** | HC needs to create, edit, delete, and assign assessments. Personal view conflates this with taking exams. Two intents = two tabs. | MEDIUM | `canManage` ViewBag already signals HC/Admin. Currently a toggle button; needs to become named tabs. |
| **HC/Admin get Monitoring tab (all users' assessments)** | HC oversight: see every user's status across Open/Upcoming/Completed. Currently implemented as "manage view" but visually undifferentiated. | MEDIUM | Controller already fetches all sessions for manage view. Tab label and content scope need separation from Management (CRUD) intent. |
| **Completed assessments preserved in Training Records** | When an assessment completes, users expect their history somewhere. Removing from Assessment page requires it appear in Training Records. | MEDIUM | `AssessmentSession` with `Status == "Completed"` + `CompletedAt` exists. Training Records query must `UNION` TrainingRecord rows + completed AssessmentSession rows. No new migration needed. |
| **Unified history table shows type-differentiated columns** | Heterogeneous rows (manual TrainingRecord vs system-generated AssessmentSession) have different metadata. Users expect columns relevant to each type to render, with graceful empty-column handling for the other type. | MEDIUM | `TrainingRecord` has: Judul, Kategori, Tanggal, Penyelenggara, Status, SertifikatUrl, ValidUntil, CertificateType. `AssessmentSession` has: Title, Category, Schedule, Score, IsPassed, PassPercentage, CompletedAt. These overlap partially (title, category, date) and diverge (certificate fields vs score fields). |
| **Source badge distinguishes record types in merged table** | When two different data sources merge into one table, users need to know which rows are from which source to understand what actions are available. | LOW | New "Type" column with badge: "Assessment" vs "Training" badges. No DB change. |
| **Empty state per tab** | If a worker has no Open assessments, the tab must show a meaningful empty state — not just a blank card grid. | LOW | Pattern already exists in Assessment.cshtml (bi-inbox icon + contextual message). Replicate for new tab structure. |
| **Proton Progress dashboard scoped by role** | All roles should see Proton progress, but scoped: Coachee sees own, Coach sees team, SectionHead sees section, HC sees all. | LOW | `DevDashboard` already implements scope via `ScopeLabel` and `CoacheeRows`. Controller already scopes by role. This tab is a lift-and-shift. |
| **Assessment Analytics tab (HC/Admin only)** | HC/Admin need assessment-level aggregates (pass rates, category breakdowns) in the same dashboard surface as Proton progress. Currently it is a separate Reports page (CMP/ReportsIndex). | MEDIUM | `ReportsIndex` logic, `ReportsDashboardViewModel`, and Chart.js analytics already exist. Consolidation: embed summary cards + link to full reports, OR replicate the cards inline. |
| **Role gate on Assessment Analytics tab** | Non-HC/Admin users must not see the Analytics tab, not even as a disabled tab. Hiding the tab entirely (not disabling it) is the correct pattern for role-exclusive content. | LOW | ASP.NET Identity `User.IsInRole()` check in Razor already used throughout. Apply same pattern to tab rendering. |
| **Gap Analysis removed cleanly from navigation** | If a feature is removed, all entry points (nav links, index page cards, breadcrumbs) must be removed. Orphaned links that 404 destroy trust. | LOW | Entry points: `Views/CMP/Index.cshtml` card link, `Views/Shared/_Layout.cshtml` nav items. No route deletion needed — action method can remain (defensive), but all links removed. |

### Differentiators (Design Quality, Not Functional)

These make the consolidation feel polished rather than bolted together.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Count badges on Assessment tabs** | "Open (3) / Upcoming (1)" badges on tabs let users see their load without clicking. Standard tab-badge pattern in HR portals. | LOW | Data is already fetched server-side. Pass counts to ViewBag. Render in tab `<span class="badge">` element. |
| **Persistent tab state via URL query param** | When HC navigates away from "Monitoring" tab and returns, they should land back on Monitoring. Use `?tab=monitoring` query param to restore. | LOW | Assessment.cshtml already uses `?view=manage` parameter. Extend same pattern to tabs. |
| **Merged table sorted with most recent first** | When TrainingRecords and AssessmentSessions are unioned, sort order must be consistent. Most-recent-first is the universal expectation. | LOW | `OrderByDescending(x => x.Date)` on unified result list. Need common date property across both types. |
| **Score column shown only for Assessment rows** | The merged table should render Score/Pass badge only on Assessment-type rows. Manual TrainingRecord rows show certificate columns only. | LOW | Conditional rendering in Razor based on row type. No data change. Pattern: `@if (row.SourceType == "Assessment") { ... }`. |
| **Expiry warning preserved in merged table** | `TrainingRecord.IsExpiringSoon` and `DaysUntilExpiry` logic must survive the merge. The existing alert banner on the old Records page should migrate. | LOW | Properties already on `TrainingRecord` model. Unified ViewModel must expose these for TrainingRecord-type rows. |

### Anti-Features (Explicitly Avoid)

| Anti-Feature | Why Avoid | What to Do Instead |
|---|---|---|
| **Soft-hiding Assessment Analytics for workers** | Rendering a grayed-out or disabled Analytics tab for Coachees creates confusion about why it's unavailable. They have no context for what the tab would do. | Conditional `@if (User.IsInRole("HC") or "Admin")` in Razor. If condition is false, tab is not rendered at all. Workers see a single-tab dashboard (Proton Progress) with no indication of hidden content. |
| **Deleting `CompetencyGap` controller action** | Removing the route entirely risks 404s from bookmarks, external links, or cached navigation. | Keep the controller action. Remove all navigation entry points. Optionally redirect the route to the Training Records page with a notice. |
| **Merging Training Records into AssessmentSession model** | The two types have fundamentally different schemas, approval workflows, and lifecycle states. A DB schema merge would require a migration and create nullable nulls everywhere. | Unified display ViewModel in the controller. Pull both types, project to a shared DTO with `SourceType`, union in memory, sort, pass to view. Zero schema change. |
| **Real-time tab badge refresh** | Dynamically updating "Open (3)" badge counts via polling adds complexity and server load. | Server-side counts on page load. Refresh on tab click (which triggers a full tab reload in server-rendered MVC anyway). |
| **Separate URLs per dashboard tab** | Using `/CDP/Dashboard/Proton` and `/CDP/Dashboard/Analytics` as separate URLs doubles controller actions and breaks back navigation. | Single URL `/CDP/Dashboard?tab=analytics`. Bootstrap tab state driven by query param on page load. |

---

## Feature Dependencies

```
[Feature 1: Assessment Page Split by Role]
    └──reads──> AssessmentSession.Status (existing)
    └──reads──> ApplicationUser.RoleLevel (existing)
    └──reads──> canManage flag (existing ViewBag pattern)
    └──replaces──> current personal/manage toggle button

[Feature 2: Unified Training Records Table]
    └──reads──> TrainingRecord (existing — all fields)
    └──reads──> AssessmentSession WHERE Status == "Completed" (existing)
    └──requires──> new ViewModel: UnifiedTrainingHistoryRow (no migration)
    └──replaces──> current category-tab Records.cshtml layout

[Feature 3: Unified Dashboard with Role-Scoped Tabs]
    └──requires──> DevDashboard (existing — lift-and-shift as Tab 1)
    └──requires──> ReportsIndex summary data (existing — embed in Tab 2)
    └──reads──> User.IsInRole("HC") or User.IsInRole("Admin") for tab 2 gate
    └──replaces──> CDP/Dashboard.cshtml (static mock data) and CDP/DevDashboard.cshtml (separate page)

[Feature 4: Gap Analysis Removal]
    └──removes──> nav link in _Layout.cshtml
    └──removes──> card in CMP/Index.cshtml
    └──keeps──> CMPController.CompetencyGap() action (defensive preservation)
    └──no DB change, no migration
```

### Dependency Notes

- **Features 1 and 2 are independent.** Either can ship alone without the other. They touch different pages (Assessment vs Records).
- **Feature 3 depends on DevDashboard being stable.** DevDashboard (Phase 07) is complete and verified. Tab wrapping is safe.
- **Feature 4 is zero-risk.** No data, no model, no migration. Pure Razor deletion. Can be done in minutes and should be first.
- **Feature 2 requires a new ViewModel.** The `UnifiedTrainingHistoryRow` DTO is the only new code artifact of substance in this milestone.

---

## Detailed Behavior Specification per Feature

### Feature 1: Assessment Page Split (Role-Filtered List)

**Worker view (RoleLevel >= 5: Coach, Coachee, SrSpv when in Coach view):**
- Shows: Open tab (default active) + Upcoming tab
- Does NOT show: Completed tab, Manage tab, Monitoring tab
- Empty state on Open tab: "No assessments available right now. Your HC will assign assessments as needed." (not a create button)
- Empty state on Upcoming tab: "No upcoming assessments scheduled."
- No count badges needed — workers rarely have many assessments

**HC/Admin view:**
- Tab 1: "Management" — create, edit, delete, regen token. Existing manage-view cards.
- Tab 2: "Monitoring" — all users' sessions visible with Open/Upcoming/Completed sub-filter. Existing personal-view cards but showing all users.
- Tab 3 (optional, if Admin SelectedView is personal): "My Assessments" — own Open + Upcoming only
- Count badges on Management tab: total count of all sessions

**Controller impact:** `Assessment()` action already receives `view` param. Extend to `tab` param. Logic stays the same; view layer changes.

**Key constraint:** Completed items never shown to workers on the Assessment page. They appear only in Training Records (Feature 2).

---

### Feature 2: Unified Training Records Table

**Expected columns (unified row):**

| Column | TrainingRecord source | AssessmentSession source |
|---|---|---|
| Type | "Training" badge | "Assessment" badge |
| Title / Judul | `TrainingRecord.Judul` | `AssessmentSession.Title` |
| Category | `TrainingRecord.Kategori` | `AssessmentSession.Category` |
| Date | `TrainingRecord.Tanggal` | `AssessmentSession.CompletedAt` |
| Provider / Source | `TrainingRecord.Penyelenggara` | "System Assessment" (hardcoded) |
| Score | — (empty) | `AssessmentSession.Score` + Pass/Fail badge |
| Certificate Type | `TrainingRecord.CertificateType` | — (empty) |
| Valid Until | `TrainingRecord.ValidUntil` | — (empty) |
| Status | `TrainingRecord.Status` | "Passed" / "Failed" derived from `IsPassed` |
| Actions | Download/View certificate if `SertifikatUrl` set | View Results link to `/CMP/Results/{id}` |

**What replaces the old category tabs:** Category tabs (PROTON, OTS, OJT, IHT, MANDATORY) are replaced by a single "All History" view with a Category dropdown filter. Filtering now works across both record types.

**Expiry warning:** The existing `IsExpiringSoon` / `DaysUntilExpiry` alert banner is retained. It applies only to TrainingRecord rows. AssessmentSession rows never have certificate expiry.

**Summary stat cards (top of page):** Retain the 4-card row (Total Training, Valid/Passed, Pending, Expiring Soon). Count TrainingRecord rows only for cert-specific stats; count AssessmentSession completed rows in Total.

**Controller change:** `Records()` action builds `List<UnifiedTrainingHistoryRow>`. For workers: union own TrainingRecords + completed AssessmentSessions, order by date desc. For HC/Admin: same but for any `workerId` parameter (existing WorkerDetail path preserved).

---

### Feature 3: Unified Dashboard with Role-Scoped Tabs

**Tab 1: "Proton Progress" (all authenticated roles)**
- Content: exact `DevDashboard.cshtml` content
- Scoped by role exactly as DevDashboard currently works
- Coachee sees own progress; Coach sees team; SectionHead sees section; HC sees all

**Tab 2: "Assessment Analytics" (HC and Admin only)**
- Tab not rendered at all for other roles (not hidden, not disabled — absent)
- Content: summary cards from `ReportsIndex` + Chart.js pass-rate chart + link "View Full Reports" → CMP/ReportsIndex
- Does not duplicate the full paginated table — just the KPI cards (Total Completed, Pass Rate, Avg Score) and the category breakdown chart

**Default tab:**
- HC/Admin: Assessment Analytics (they arrive for oversight)
- All other roles: Proton Progress (their primary task-oriented view)
- Controlled by `?tab=analytics` / `?tab=proton` query param, defaulting by role if param absent

**Replaces:**
- `Views/CDP/Dashboard.cshtml` — the current static-data IDP monitoring page (confusing, unused data)
- `Views/CDP/DevDashboard.cshtml` — currently a standalone page, becomes Tab 1

---

### Feature 4: Gap Analysis Clean Removal

**What is removed (exhaustive list):**
1. Card in `Views/CMP/Index.cshtml` linking to CompetencyGap
2. Any nav sidebar/menu entry in `Views/Shared/_Layout.cshtml` pointing to CMP/CompetencyGap
3. Any breadcrumb or "back" link referencing CompetencyGap from related pages (CpdpProgress, WorkerDetail)

**What is NOT removed:**
- `CMPController.CompetencyGap()` action — kept for safety (zero cost)
- `CompetencyGapViewModel`, `CompetencyGapItem` models — kept (no harm)
- `Views/CMP/CompetencyGap.cshtml` — kept (no harm; inaccessible without nav entry)
- The underlying `UserCompetencyLevel` data — unchanged

**Why removed:** The Gap Analysis page duplicates information available in CPDP Progress (`CpdpProgress.cshtml`) and is redundant once the Proton workflow covers development tracking. No user-facing loss since gap data remains visible via CpdpProgress.

---

## MVP Definition for v1.2

### Ship in v1.2 (all four features)

- [x] **Assessment page: worker view restricted to Open + Upcoming** — Removes noise, eliminates irrelevant completed-item cards from the worker's assessment lobby.
- [x] **Assessment page: HC/Admin tabs (Management + Monitoring)** — Separates CRUD intent from oversight intent. Currently conflated in a single toggle.
- [x] **Unified Training Records table** — Merges AssessmentSession completed rows into TrainingRecord view. Workers get full history in one place.
- [x] **Unified Dashboard: Proton Progress + Assessment Analytics tabs** — Consolidates two separate pages into one contextual surface.
- [x] **Gap Analysis entry points removed** — Clean up. Zero risk. First thing to do.

### Defer to v1.3+

- [ ] **Inline Assessment Analytics (full table in dashboard)** — Embedding the full paginated ReportsIndex in a dashboard tab is complex. Show KPI cards + link instead.
- [ ] **Training Records: pagination for unified table** — When rows from both sources grow large, pagination needed. Not required at current data volume.
- [ ] **Assessment page: "My Assessments" personal tab for Admin** — Admin using SelectedView=Coachee needs personal assessment view. Deferred until Admin SelectedView is fully verified post-08.

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Gap Analysis removal | LOW (cleanup only) | LOW (nav deletions) | P1 — do first, zero risk |
| Assessment page worker filter | HIGH | LOW | P1 |
| Assessment page HC/Admin tabs | HIGH | LOW | P1 |
| Unified Training Records table | HIGH | MEDIUM | P1 |
| Unified Dashboard tabs | MEDIUM | MEDIUM | P1 |
| Count badges on Assessment tabs | LOW | LOW | P2 |
| Persistent tab state via URL | LOW | LOW | P2 |
| Pagination for unified history table | LOW | MEDIUM | P3 |

---

## Sources

- Codebase audit (2026-02-18): `Views/CMP/Assessment.cshtml`, `Views/CMP/Records.cshtml`, `Views/CDP/DevDashboard.cshtml`, `Views/CDP/Dashboard.cshtml`, `Controllers/CMPController.cs`, `Models/AssessmentSession.cs`, `Models/TrainingRecord.cs`, `Models/ApplicationUser.cs`, `Models/UserRoles.cs`, `Models/DevDashboardViewModel.cs`
- Existing pattern reference: role-gating via `User.IsInRole()` / `ViewBag.CanManage` in Assessment.cshtml (lines 36–52, 113–128)
- Existing pattern reference: category-tab Records layout in Records.cshtml (lines 150–213)
- Existing pattern reference: DevDashboard scope-by-role in DevDashboard.cshtml + `ScopeLabel` in `DevDashboardViewModel`

---

*Feature research for: Portal HC KPB v1.2 UX Consolidation*
*Researched: 2026-02-18*
*Confidence: HIGH — all behaviors grounded in existing codebase inspection. No external library research required.*
