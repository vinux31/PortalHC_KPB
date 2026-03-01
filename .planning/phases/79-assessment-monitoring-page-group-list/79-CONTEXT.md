# Phase 79: Assessment Monitoring Page — Group List - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Dedicated Assessment Monitoring page accessible from Kelola Data hub Section C. Shows all assessment groups with real-time monitoring stats, search/filter controls, and per-group actions (Regenerate Token). This is the group LIST page — per-participant detail and HC actions (Reset, ForceClose, etc.) are Phase 80.

</domain>

<decisions>
## Implementation Decisions

### Group Display
- Each group shows RICH stats + actions: title, category badge, status badge, participant count, completed/total, passed/total, progress percentage, schedule date
- Inline Regenerate Token button per group (visible for token-required assessments)
- Quick-action dropdown on each group (at minimum: link to monitoring detail)
- Navigation from group list to detail page is part of the display

### Filter & Search
- Filter controls: Status filter (Open/Upcoming/Closed), Category filter (OJT, IHT, Training Licencor, OTS, Mandatory HSSE Training, Proton, Assessment Proton), text search by assessment title
- Default filter on page load: Show **Open + Upcoming** only — HC sees active assessments first. Closed groups available via filter toggle.
- Matches ManageAssessment's filtering level but focused on monitoring-relevant data

### Claude's Discretion
- **Layout style**: Table with progress bars vs cards — pick what best serves scanability and quick status assessment
- **Summary stat cards** at top of page: aggregate totals across all visible groups (optional, Claude decides if it adds value)
- **Navigation pattern**: Row/card click vs explicit "View Detail" button — Claude picks the clearest UX
- **Client-side vs server-side filtering**: Claude decides based on expected data volume and existing patterns
- **Sort approach**: Fixed newest-first vs sortable columns — Claude decides
- **Live polling on list page**: Whether group stats auto-refresh (10-15s polling) or are static snapshot — Claude decides based on server load tradeoffs
- **Time window cutoff**: 7-day, 30-day, or no cutoff — Claude decides based on what's most useful for monitoring context

</decisions>

<specifics>
## Specific Ideas

- Page lives at a new AdminController action (e.g., `AssessmentMonitoring`) accessible from Kelola Data hub Section C
- Hub card should be in Section C (Assessment & Training) alongside the existing "Manage Assessment & Training" card
- Regenerate Token is available on BOTH this new page AND the existing ManageAssessment page (dual presence)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **MonitoringGroupViewModel** (`Models/AssessmentMonitoringViewModel.cs`): Already has Title, Category, Schedule, GroupStatus, TotalCount, CompletedCount, PassedCount, PendingCount, IsPackageMode, Sessions list — production-ready
- **GetMonitoringProgress** JSON endpoint (`AdminController.cs:1532`): Returns per-session progress/status/score JSON — can be adapted for group-level polling
- **Assessment grouping query** (`AdminController.cs:312-337`): Groups sessions by (Title, Category, Schedule.Date) with representative ID — exact pattern needed for the group list
- **Category badge styling**: Established color-coding (OJT=primary, IHT=success, Training Licencor=danger, OTS=warning, Mandatory=info, Proton=purple)
- **Status badge styling**: GroupStatus badges (Open=success, Upcoming=info, Closed=secondary)

### Established Patterns
- **Hub card layout** (`Views/Admin/Index.cshtml`): `col-md-4` cards with `shadow-sm border-0`, icon + title + description, wrapped in `<a>` tag
- **Server-side pagination** (`ManageAssessment`): 20 items/page with first/prev/next/last navigation
- **7-day cutoff**: ManageAssessment filters `ExamWindowCloseDate ?? Schedule.Date` to last 7 days
- **Authorization**: `[Authorize(Roles = "Admin, HC")]` per action — monitoring page should use same pattern
- **Regenerate Token**: Existing `RegenerateToken` POST action in AdminController — can be called from the new page

### Integration Points
- **Hub card**: New card in `Views/Admin/Index.cshtml` Section C row, gated with `User.IsInRole("Admin") || User.IsInRole("HC")`
- **Controller action**: New `AssessmentMonitoring` action in `AdminController.cs` — reuses grouping query from ManageAssessment
- **Detail link**: Groups link to existing `AssessmentMonitoringDetail` action (title/category/scheduleDate params)
- **Breadcrumb**: Admin > Kelola Data > Assessment Monitoring

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 79-assessment-monitoring-page-group-list*
*Context gathered: 2026-03-01*
